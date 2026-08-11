using Content.Server.ADT.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Station.Systems;

public sealed class EmergencyAccessInitSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        EnsureComp<EmergencyAccessComponent>(ev.Station.Owner);
    }
}
