using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.Silicons.Borgs.Components;

/// <summary>
/// Component for syndicate saboteur borgs that can switch their chassis appearance
/// to mimic other borg types and rename themselves.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SyndicateSaboteurChassisSwitchComponent : Component
{
    /// <summary>
    /// Action entity used by players to switch their chassis.
    /// </summary>
    [DataField]
    public EntityUid? SwitchChassisAction;

    /// <summary>
    /// The currently selected borg subtype for appearance, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BorgSubtypePrototype>? CurrentBorgSubtype;

    /// <summary>
    /// The custom name set by the saboteur borg, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CustomName;
}

/// <summary>
/// UI message used by a saboteur borg to select their chassis subtype.
/// </summary>
[Serializable, NetSerializable]
public sealed class SaboteurSelectChassisMessage(ProtoId<BorgSubtypePrototype> subtype, string? customName = null) : BoundUserInterfaceMessage
{
    public ProtoId<BorgSubtypePrototype> Subtype = subtype;
    public string? CustomName = customName;
}

/// <summary>
/// UI key used by the chassis selection menu for saboteur borgs.
/// </summary>
[NetSerializable, Serializable]
public enum SaboteurChassisSwitchUiKey : byte
{
    SelectChassis,
}

/// <summary>
/// Action event used to open the chassis selection menu of a <see cref="SyndicateSaboteurChassisSwitchComponent"/>.
/// </summary>
public sealed partial class SaboteurToggleChassisSwitchEvent : InstantActionEvent;
