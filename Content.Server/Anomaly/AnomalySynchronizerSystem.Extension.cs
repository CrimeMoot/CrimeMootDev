using Content.Server.ADT.Anomaly;
using Content.Server.Anomaly;
using Content.Shared.ADT.Anomaly.Components;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Anomaly;

/// <summary>
/// Серверное расширение для системы синхронизатора аномалий.
/// Добавляет поддержку автоматической стабилизации.
/// </summary>
public sealed class AnomalySynchronizerSystem_Extension : EntitySystem
{
    [Dependency] private readonly AnomalySynchronizerAutoStabilizeSystem _autoStabilizeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на обновление синхронизатора
        SubscribeLocalEvent<AnomalySynchronizerComponent, ComponentStartup>(OnSynchronizerStartup);
    }

    private void OnSynchronizerStartup(Entity<AnomalySynchronizerComponent> ent, ref ComponentStartup args)
    {
        // Проверяем, есть ли компонент автоматической стабилизации
        if (HasComp<AnomalySynchronizerAutoStabilizeComponent>(ent))
        {
            // Компонент уже есть
        }
        else
        {
            // Добавляем компонент автоматической стабилизации для улучшенных синхронизаторов
            if (HasComp<AnomalySynchronizerAdvancedComponent>(ent))
            {
                AddComp<AnomalySynchronizerAutoStabilizeComponent>(ent);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Обновляем автоматическую стабилизацию для всех синхронизаторов
        var query = EntityQueryEnumerator<AnomalySynchronizerAutoStabilizeComponent, AnomalySynchronizerComponent>();
        while (query.MoveNext(out var uid, out var autoStab, out var sync))
        {
            _autoStabilizeSystem.UpdateAutoStabilization(uid, autoStab);
        }
    }
}

/// <summary>
/// Маркерный компонент для улучшенного синхронизатора.
/// </summary>
[RegisterComponent]
public sealed partial class AnomalySynchronizerAdvancedComponent : Component
{
}
