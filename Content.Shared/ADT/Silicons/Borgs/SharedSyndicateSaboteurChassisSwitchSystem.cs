using Robust.Shared.Prototypes;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedSyndicateSaboteurChassisSwitchSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    public static readonly EntProtoId ActionId = "ActionSaboteurChassisSwitch";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, SaboteurToggleChassisSwitchEvent>(OnChassisSwitchAction);
        SubscribeLocalEvent<SyndicateSaboteurChassisSwitchComponent, ComponentInit>(OnComponentInit);
    }

    private void OnMapInit(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.SwitchChassisAction, ActionId);
        Dirty(ent);
        // Don't update visuals here - let the client system handle it on ComponentStartup
    }

    private void OnComponentInit(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref ComponentInit args)
    {
        // Initialize visuals on component init
        if (ent.Comp.CurrentBorgSubtype == null)
        {
            // Set default subtype if not set
            ent.Comp.CurrentBorgSubtype = "syndicate_saboteur";
            Dirty(ent);
        }
    }

    private void OnShutdown(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SwitchChassisAction);
    }

    private void OnChassisSwitchAction(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ref SaboteurToggleChassisSwitchEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        _userInterface.TryToggleUi((ent.Owner, null), SaboteurChassisSwitchUiKey.SelectChassis, actor.PlayerSession);
    }

    protected virtual void SetAppearanceFromSubtype(Entity<SyndicateSaboteurChassisSwitchComponent> ent, ProtoId<BorgSubtypePrototype> subtype) { }

    protected void UpdateVisuals(Entity<SyndicateSaboteurChassisSwitchComponent> ent)
    {
        if (ent.Comp.CurrentBorgSubtype == null)
            return;
        SetAppearanceFromSubtype(ent, ent.Comp.CurrentBorgSubtype.Value);
    }
}
