using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.ADT.Silicons.Borgs;
using Content.Server.Silicons.Borgs;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Corvax.TTS;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class SyndicateSaboteurChassisSwitchSystem : SharedSyndicateSaboteurChassisSwitchSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly InteractionPopupSystem _interactionPopup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SyndicateSaboteurChassisSwitchComponent>(SaboteurChassisSwitchUiKey.SelectChassis,
            sub =>
            {
                sub.Event<SaboteurSelectChassisMessage>(OnChassisSelected);
            });

        // Listen for death to disable disguise
        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref MobStateChangedEvent args)
    {
        // Check if borg has died
        if (args.NewMobState == MobState.Dead)
        {
            DisableDisguise(ent);
        }
    }

    private void DisableDisguise(Entity<SyndicateSaboteurChassisSwitchComponent> ent)
    {
        // Reset to syndicate saboteur appearance
        if (Prototypes.TryIndex<BorgSubtypePrototype>("syndicate_saboteur", out var saboteurSubtype) &&
            Prototypes.TryIndex<BorgTypePrototype>("engineering", out var engineeringType))
        {
            ent.Comp.CurrentBorgSubtype = "syndicate_saboteur";
            ApplyDisguise(ent, saboteurSubtype, engineeringType, forceReset: true);
            
            // Reset name
            _metaData.SetEntityName(ent.Owner, string.Empty);
            ent.Comp.CustomName = string.Empty;
            
            // Reset description
            _metaData.SetEntityDescription(ent.Owner, Loc.GetString("ent-BorgChassisSyndicateSaboteur.desc"));
            
            Dirty(ent);
            UpdateVisuals(ent);
            
            Logger.InfoS("borg.saboteur", $"Disguise disabled for {ent.Owner} due to death");
        }
    }

    private void OnChassisSelected(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref SaboteurSelectChassisMessage args)
    {
        if (!Prototypes.TryIndex(args.Subtype, out var subtypePrototype))
        {
            Logger.ErrorS("borg.saboteur", $"Failed to find borg subtype prototype: {args.Subtype}");
            return;
        }

        // Get the parent BorgTypePrototype for functional data
        if (!Prototypes.TryIndex(subtypePrototype.ParentBorgType, out var borgTypePrototype))
        {
            Logger.ErrorS("borg.saboteur", $"Failed to find borg type prototype: {subtypePrototype.ParentBorgType}");
            return;
        }

        ent.Comp.CurrentBorgSubtype = args.Subtype;

        // Apply full disguise (visual + functional)
        ApplyDisguise(ent, subtypePrototype, borgTypePrototype);

        // Handle custom name if provided
        if (!string.IsNullOrEmpty(args.CustomName))
        {
            _metaData.SetEntityName(ent.Owner, args.CustomName);
            ent.Comp.CustomName = args.CustomName;
        }
        else
        {
            // Reset to default name
            _metaData.SetEntityName(ent.Owner, string.Empty);
            ent.Comp.CustomName = string.Empty;
        }

        // Update entity description to match the disguised borg type
        string description;
        if (args.Subtype == "syndicate_saboteur")
        {
            // Restore original syndicate saboteur description
            description = Loc.GetString("borg-subtype-syndicate_saboteur-desc");
        }
        else
        {
            // Use borg type description for disguises
            description = Loc.GetString($"borg-type-{borgTypePrototype.ID}-desc");
        }
        _metaData.SetEntityDescription(ent.Owner, description);

        Dirty(ent);
        UpdateVisuals(ent);
        _userInterface.CloseUi((ent.Owner, null), SaboteurChassisSwitchUiKey.SelectChassis);
    }

    private void ApplyDisguise(
        Entity<SyndicateSaboteurChassisSwitchComponent> ent,
        BorgSubtypePrototype subtype,
        BorgTypePrototype borgType,
        bool forceReset = false)
    {
        var uid = ent.Owner;

        // === 1. Update BorgChassis component (modules) ===
        if (TryComp<BorgChassisComponent>(uid, out var borgChassis))
        {
            var chassisEnt = (uid, borgChassis);

            // Count existing syndicate modules (they don't count towards maxModules)
            var syndicateModuleCount = borgChassis.ModuleContainer.ContainedEntities
                .Count(m =>
                {
                    var protoId = Comp<MetaDataComponent>(m).EntityPrototype?.ID;
                    return protoId != null && protoId.Contains("Syndicate");
                });

            // Update max modules (extra + default modules + syndicate modules for hidden storage)
            // Syndicate modules are hidden and don't count towards the visible limit
            _borgSystem.SetMaxModules(chassisEnt, borgType.ExtraModuleCount + borgType.DefaultModules.Length + syndicateModuleCount);

            // Update module whitelist
            if (borgType.ModuleWhitelist != null)
            {
                _borgSystem.SetModuleWhitelist(chassisEnt, borgType.ModuleWhitelist);
            }

            // ONLY remove non-syndicate modules (preserve syndicate modules!)
            var modulesToRemove = borgChassis.ModuleContainer.ContainedEntities
                .Where(m =>
                {
                    var protoId = Comp<MetaDataComponent>(m).EntityPrototype?.ID;
                    // Keep syndicate modules
                    return protoId == null || !protoId.Contains("Syndicate");
                })
                .ToList();
            
            foreach (var module in modulesToRemove)
            {
                _borgSystem.UninstallModule(uid, module, borgChassis);
                EntityManager.DeleteEntity(module);
            }

            // Add default modules for this borg type (only if not already present)
            var existingModules = borgChassis.ModuleContainer.ContainedEntities
                .Select(m => Comp<MetaDataComponent>(m).EntityPrototype?.ID)
                .ToHashSet();

            foreach (var moduleProto in borgType.DefaultModules)
            {
                if (!existingModules.Contains(moduleProto.Id))
                {
                    var moduleEntity = Spawn(moduleProto);
                    var borgModule = Comp<BorgModuleComponent>(moduleEntity);
                    _borgSystem.SetBorgModuleDefault((moduleEntity, borgModule), true);
                    _borgSystem.InsertModule(chassisEnt, moduleEntity);
                }
            }

            Dirty(uid, borgChassis);
        }

        // === 2. Update inventory template ===
        if (!string.IsNullOrEmpty(borgType.InventoryTemplateId.Id))
        {
            if (TryComp<InventoryComponent>(uid, out var inventory))
            {
                _inventory.SetTemplateId((uid, inventory), borgType.InventoryTemplateId);
            }
        }

        // === 3. Update radio channels ===
        UpdateRadioChannels(uid, borgType.RadioChannels);

        // === 4. Update Access ===
        UpdateAccess(uid, borgType, subtype.ID == "syndicate_saboteur");

        // === 5. Update footstep sounds ===
        if (borgType.FootstepCollection != null)
        {
            if (TryComp<FootstepModifierComponent>(uid, out var footstep))
            {
                footstep.FootstepSoundCollection = borgType.FootstepCollection;
                Dirty(uid, footstep);
            }
        }

        // === 6. Update TTS voice ===
        if (!string.IsNullOrEmpty(borgType.VoicePrototypeId))
        {
            if (TryComp<TTSComponent>(uid, out var tts))
            {
                tts.VoicePrototypeId = borgType.VoicePrototypeId;
                Dirty(uid, tts);
            }
        }

        // === 7. Update pet strings ===
        if (!string.IsNullOrEmpty(borgType.PetSuccessString) || !string.IsNullOrEmpty(borgType.PetFailureString))
        {
            if (TryComp<InteractionPopupComponent>(uid, out var interaction))
            {
                if (!string.IsNullOrEmpty(borgType.PetSuccessString))
                    _interactionPopup.SetInteractSuccessString((uid, interaction), borgType.PetSuccessString);
                if (!string.IsNullOrEmpty(borgType.PetFailureString))
                    _interactionPopup.SetInteractFailureString((uid, interaction), borgType.PetFailureString);
            }
        }
    }

    private void UpdateRadioChannels(EntityUid uid, ProtoId<RadioChannelPrototype>[] channels)
    {
        // Always keep Binary channel
        var binaryChannel = Prototypes.Index<RadioChannelPrototype>("Binary");
        var newChannels = new HashSet<ProtoId<RadioChannelPrototype>> { binaryChannel };
        
        // Add channels from borg type
        foreach (var channel in channels)
        {
            newChannels.Add(channel);
        }

        if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
        {
            transmitter.Channels = newChannels;
            Dirty(uid, transmitter);
        }

        if (TryComp<ActiveRadioComponent>(uid, out var activeRadio))
        {
            activeRadio.Channels = newChannels;
            Dirty(uid, activeRadio);
        }
    }

    private void UpdateAccess(EntityUid uid, BorgTypePrototype borgType, bool isSaboteur)
    {
        // Update AccessComponent tags and groups
        if (TryComp<AccessComponent>(uid, out var access))
        {
            if (isSaboteur)
            {
                // Restore syndicate access tags
                var syndicateTags = new HashSet<ProtoId<AccessLevelPrototype>>
                {
                    Prototypes.Index<AccessLevelPrototype>("SyndicateAgent"),
                    Prototypes.Index<AccessLevelPrototype>("NuclearOperative")
                };
                access.Tags.Clear();
                access.Tags.UnionWith(syndicateTags);
                Dirty(uid, access);
            }
            else
            {
                // Set standard borg access
                var borgTags = new HashSet<ProtoId<AccessLevelPrototype>>
                {
                    Prototypes.Index<AccessLevelPrototype>("Borg")
                };
                access.Tags.Clear();
                access.Tags.UnionWith(borgTags);
                Dirty(uid, access);
            }
        }

        // Also update AccessReader if present
        if (TryComp<AccessReaderComponent>(uid, out var accessReader))
        {
            if (isSaboteur)
            {
                accessReader.AccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>>
                {
                    new HashSet<ProtoId<AccessLevelPrototype>>
                    {
                        Prototypes.Index<AccessLevelPrototype>("SyndicateAgent"),
                        Prototypes.Index<AccessLevelPrototype>("NuclearOperative")
                    }
                };
                Dirty(uid, accessReader);
            }
            else
            {
                accessReader.AccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>>
                {
                    new HashSet<ProtoId<AccessLevelPrototype>>
                    {
                        Prototypes.Index<AccessLevelPrototype>("Borg")
                    }
                };
                Dirty(uid, accessReader);
            }
        }
    }
}
