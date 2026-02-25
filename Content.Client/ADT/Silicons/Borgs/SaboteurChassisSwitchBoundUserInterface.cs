using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.ADT.Silicons.Borgs;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Client.ADT.Silicons.Borgs;

[UsedImplicitly]
public sealed class SaboteurChassisSwitchBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SaboteurChassisSwitchMenu? _window;

    public SaboteurChassisSwitchBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SaboteurChassisSwitchMenu>();

        _window.ConfirmedChassisSelection += OnChassisSelectionConfirmed;
    }

    private void OnChassisSelectionConfirmed(ProtoId<BorgSubtypePrototype> subtype, string? customName)
    {
        SendMessage(new SaboteurSelectChassisMessage(subtype, customName));
    }
}
