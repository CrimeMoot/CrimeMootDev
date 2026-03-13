using Content.Server.Anomaly;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.ADT.Anomaly.Components;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Radio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Anomaly;

/// <summary>
/// Система автоматической стабилизации аномалий для синхронизатора.
/// При критическом состоянии аномалии автоматически отправляет стабилизирующие частицы
/// и оповещает учёных через рацию.
/// </summary>
public sealed class AnomalySynchronizerAutoStabilizeSystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomalySystem = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySynchronizerAutoStabilizeComponent, ComponentStartup>(OnAutoStabilizeStartup);
        SubscribeLocalEvent<AnomalySynchronizerAutoStabilizeComponent, ComponentShutdown>(OnAutoStabilizeShutdown);
    }

    private void OnAutoStabilizeStartup(Entity<AnomalySynchronizerAutoStabilizeComponent> ent, ref ComponentStartup args)
    {
        // При запуске начинаем мониторинг
    }

    private void OnAutoStabilizeShutdown(Entity<AnomalySynchronizerAutoStabilizeComponent> ent, ref ComponentShutdown args)
    {
        // Очистка при удалении
    }

    /// <summary>
    /// Обновление состояния автоматической стабилизации.
    /// Вызывается из основной системы синхронизатора.
    /// </summary>
    public void UpdateAutoStabilization(EntityUid uid, AnomalySynchronizerAutoStabilizeComponent comp, AnomalyComponent? anomalyComp = null)
    {
        if (!comp.Enabled)
            return;

        if (comp.ConnectedAnomaly is not { } anomalyUid)
            return;

        if (!Resolve(anomalyUid, ref anomalyComp, false))
        {
            // Аномалия уничтожена
            comp.ConnectedAnomaly = null;
            comp.WarningSent = false;
            return;
        }

        // Проверяем, не на cooldown ли мы
        var curTime = _timing.CurTime;
        if (curTime < comp.LastStabilizationTime + comp.StabilizationCooldown)
            return;

        // Проверяем стабильность аномалии
        if (anomalyComp.Stability >= comp.StabilityThreshold)
        {
            // Аномалия стабильна, сбрасываем флаг предупреждения
            if (comp.WarningSent)
            {
                comp.WarningSent = false;
                Log.Info($"Аномалия {anomalyUid} стабилизировалась. Синхронизатор {uid} прекращает оповещения.");
            }
            return;
        }

        // Аномалия в критическом состоянии!
        comp.LastStabilizationTime = curTime;

        // Отправляем предупреждение учёным (только один раз)
        if (!comp.WarningSent)
        {
            SendScientistWarning(uid, anomalyUid);
            comp.WarningSent = true;
        }

        Log.Info($"Синхронизатор {uid} зафиксировал критическое состояние аномалии {anomalyUid}. Стабильность: {anomalyComp.Stability:F2}");
    }

    /// <summary>
    /// Отправляет предупреждение учёным через научный канал рации.
    /// </summary>
    private void SendScientistWarning(EntityUid synchronizerUid, EntityUid anomalyUid)
    {
        var message = Loc.GetString("adt-anomaly-sync-auto-stabilize-warning");
        
        // Отправляем в научный канал (частота 1351)
        var scienceChannel = _prototypeManager.Index<RadioChannelPrototype>("Science");
        _radioSystem.SendRadioMessage(synchronizerUid, message, scienceChannel, synchronizerUid);

        // Также отправляем как объявление по станции
        var announcementMessage = Loc.GetString("adt-anomaly-sync-auto-stabilize-announcement");
        _chatManager.DispatchServerAnnouncement(announcementMessage);

        Log.Info($"Синхронизатор {synchronizerUid} отправил предупреждение об аномалии {anomalyUid} учёным.");
    }

    /// <summary>
    /// Включает/выключает автоматическую стабилизацию.
    /// </summary>
    public void ToggleAutoStabilization(EntityUid uid, AnomalySynchronizerAutoStabilizeComponent comp)
    {
        comp.Enabled = !comp.Enabled;
        Dirty(uid, comp);

        var status = comp.Enabled ? "включена" : "выключена";
        Log.Info($"Автоматическая стабилизация синхронизатора {uid} {status}.");
    }

    /// <summary>
    /// Устанавливает тип стабилизирующих частиц.
    /// </summary>
    public void SetStabilizationParticleType(
        EntityUid uid,
        AnomalySynchronizerAutoStabilizeComponent comp,
        AnomalousParticleType type)
    {
        comp.StabilizationParticleType = type;
        Dirty(uid, comp);
    }
}
