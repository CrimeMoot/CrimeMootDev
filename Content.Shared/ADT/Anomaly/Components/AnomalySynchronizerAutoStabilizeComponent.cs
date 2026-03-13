using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Anomaly;

namespace Content.Shared.ADT.Anomaly.Components;

/// <summary>
/// Компонент автоматической стабилизации аномалии для синхронизатора.
/// При критическом состоянии аномалии автоматически отправляет стабилизирующие частицы.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnomalySynchronizerAutoStabilizeComponent : Component
{
    /// <summary>
    /// Включена ли автоматическая стабилизация.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    /// <summary>
    /// Порог стабильности, при котором активируется автоматическая стабилизация.
    /// По умолчанию 0.2 (немного выше порога распада 0.15).
    /// </summary>
    [DataField]
    public float StabilityThreshold = 0.2f;

    /// <summary>
    /// Тип частиц для стабилизации (по умолчанию Дельта - стабилизирующие).
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType StabilizationParticleType = AnomalousParticleType.Delta;

    /// <summary>
    /// Задержка между попытками стабилизации (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan StabilizationCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Время последней попытки стабилизации.
    /// </summary>
    [DataField]
    public TimeSpan LastStabilizationTime = TimeSpan.Zero;

    /// <summary>
    /// Ссылка на сущность аномалии, к которой подключен синхронизатор.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedAnomaly = null;

    /// <summary>
    /// Было ли отправлено предупреждение учёным.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WarningSent = false;
}
