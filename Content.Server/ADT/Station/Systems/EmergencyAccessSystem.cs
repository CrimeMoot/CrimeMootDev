using Content.Server.ADT.Station.Components;
using Content.Server.ADT.Station.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Station.Systems;
using Content.Shared.ADT.Doors.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.ADT.Station.Systems;
public sealed class EmergencyAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedAirlockSystem _airlockSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public void ToggleEmergencyAccess(EntityUid station, bool activate)
    {
        if (!TryComp<EmergencyAccessComponent>(station, out var emergencyAccess))
            return;

        emergencyAccess.IsActive = activate;

        var airlockQuery = GetEntityQuery<AirlockComponent>();
        var enumerator = AllEntityQuery<TechnicalAirlockComponent>();

        while (enumerator.MoveNext(out var uid, out _))
        {
            var owningStation = _stationSystem.GetOwningStation(uid);
            if (owningStation != station)
                continue;

            if (!airlockQuery.TryGetComponent(uid, out var airlock))
                continue;

            _airlockSystem.SetEmergencyAccess(new Entity<AirlockComponent>(uid, airlock), activate);
        }
    }
}
