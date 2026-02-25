using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Corvax.TTS;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.ADT.Silicons.Borgs;

public sealed partial class SyndicateSaboteurChassisSwitchSystem : SharedSyndicateSaboteurChassisSwitchSystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, AfterAutoHandleStateEvent>(AfterStateHandler);
    }

    private void OnComponentStartup(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
        ApplyDisguiseClient(ent);
    }

    private void AfterStateHandler(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.CurrentBorgSubtype.HasValue)
        {
            UpdateVisuals(ent);
            ApplyDisguiseClient(ent);
        }
    }

    private void ApplyDisguiseClient(Entity<SyndicateSaboteurChassisSwitchComponent> ent)
    {
        if (!ent.Comp.CurrentBorgSubtype.HasValue ||
            !_prototypeManager.TryIndex(ent.Comp.CurrentBorgSubtype.Value, out var subtypePrototype) ||
            !_prototypeManager.TryIndex(subtypePrototype.ParentBorgType, out var borgTypePrototype))
        {
            return;
        }

        var uid = ent.Owner;

        // Update footstep sounds
        if (borgTypePrototype.FootstepCollection != null)
        {
            if (TryComp<FootstepModifierComponent>(uid, out var footstep))
            {
                footstep.FootstepSoundCollection = borgTypePrototype.FootstepCollection;
                Dirty(uid, footstep);
            }
        }

        // Update TTS voice
        if (!string.IsNullOrEmpty(borgTypePrototype.VoicePrototypeId))
        {
            if (TryComp<TTSComponent>(uid, out var tts))
            {
                tts.VoicePrototypeId = borgTypePrototype.VoicePrototypeId;
                Dirty(uid, tts);
            }
        }
    }

    protected override void SetAppearanceFromSubtype(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!_prototypeManager.TryIndex(subtype, out var subtypePrototype))
        {
            Logger.ErrorS("borg.saboteur", $"Failed to find borg subtype prototype: {subtype}");
            return;
        }

        if (!TryComp(ent, out SpriteComponent? sprite))
        {
            Logger.ErrorS("borg.saboteur", $"Entity {ent.Owner} has no sprite component");
            return;
        }

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / subtypePrototype.Sprite;

        if (!_resourceCache.TryGetResource<RSIResource>(rsiPath, out var resource))
        {
            Logger.ErrorS("borg.saboteur", $"Failed to load RSI {rsiPath} for subtype {subtype}");
            return;
        }

        if (!_appearance.TryGetData<bool>(ent, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        sprite.LayerSetState(BorgVisualLayers.Body, subtypePrototype.SpriteBodyState);
        sprite.LayerSetState(BorgVisualLayers.Light, hasPlayer ? subtypePrototype.SpriteHasMindState : subtypePrototype.SpriteNoMindState);
        sprite.LayerSetState(BorgVisualLayers.LightStatus, subtypePrototype.SpriteToggleLightState);

        sprite.LayerSetRSI(BorgVisualLayers.Body.GetHashCode(), resource.RSI);
        sprite.LayerSetRSI(BorgVisualLayers.Light.GetHashCode(), resource.RSI);
        sprite.LayerSetRSI(BorgVisualLayers.LightStatus.GetHashCode(), resource.RSI);
    }
}
