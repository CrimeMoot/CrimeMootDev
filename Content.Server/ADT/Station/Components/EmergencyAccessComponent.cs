namespace Content.Server.ADT.Station.Components;

[RegisterComponent]
public sealed partial class EmergencyAccessComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive;
}
