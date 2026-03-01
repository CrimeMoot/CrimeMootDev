using Content.Server.Chat.Systems;
using Content.Shared.ADT.CerebralTrauma;
using Content.Shared.ADT.Chemistry.EntityEffects;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Chat;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System;
using System.Linq;

namespace Content.Server.ADT.CerebralTrauma;

/// <summary>
/// Система церебральных травм.
/// Игрок с травмой периодически кричит случайные фразы.
/// Лёгкая травма (50%) - лечится Mannitol
/// Серьёзная травма (50%) - лечится Psicodine, выдаёт случайный отрицательный квирк
/// </summary>
public sealed class CerebralTraumaSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    private List<string> _defaultTraumaPhrases = new();
    private List<string> _defaultNegativeQuirks = new();
    private float _updateAccumulator;
    private const float UpdateInterval = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CerebralTraumaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CerebralTraumaComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<CerebralTraumaCuredEvent>(OnCerebralTraumaCured);

        if (_proto.TryIndex<EntityPrototype>("CerebralTrauma", out var proto) &&
            proto.TryGetComponent<CerebralTraumaComponent>(out var comp))
        {
            _defaultTraumaPhrases = new List<string>(comp.TraumaPhraseKeys);
            _defaultNegativeQuirks = new List<string>(comp.NegativeQuirkIds);
        }
    }

    private void OnStartup(EntityUid uid, CerebralTraumaComponent component, ComponentStartup args)
    {
        component.NextShoutTime = _random.NextFloat(component.MinShoutInterval, component.MaxShoutInterval);
    }

    private void OnRejuvenate(EntityUid uid, CerebralTraumaComponent component, RejuvenateEvent args)
    {
        RemoveTraumaWithQuirks(uid, component);
    }

    private void OnCerebralTraumaCured(CerebralTraumaCuredEvent args)
    {
        var uid = EntityManager.GetEntity(args.Target);
        Log.Info($"[CerebralTrauma] Лечение: {ToPrettyString(uid)}, квирков: {args.GrantedQuirkIds.Count}");
        RemoveGrantedQuirksDirect(uid, args.GrantedQuirkIds);
    }

    public override void Update(float frameTime)
    {
        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        _updateAccumulator -= UpdateInterval;

        var query = EntityQueryEnumerator<CerebralTraumaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Severity == CerebralTraumaSeverity.None)
                continue;

            comp.NextShoutTime -= UpdateInterval;

            if (comp.NextShoutTime <= 0f)
            {
                ShoutRandomPhrase(uid, comp);
                comp.NextShoutTime = _random.NextFloat(comp.MinShoutInterval, comp.MaxShoutInterval);
            }
        }
    }

    private void ShoutRandomPhrase(EntityUid uid, CerebralTraumaComponent comp)
    {
        var phraseKey = _random.Pick(comp.TraumaPhraseKeys);
        var phrase = Loc.GetString(phraseKey);

        if (!TrySpeakRadio(uid, phrase))
        {
            _chat.TrySendInGameICMessage(uid, phrase, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }
    }

    private bool TrySpeakRadio(EntityUid entity, string message)
    {
        HashSet<ProtoId<RadioChannelPrototype>> potentialChannels = [];

        foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
        {
            if (!TryComp<ActiveRadioComponent>(item, out var radio))
                continue;

            potentialChannels.UnionWith(radio.Channels);
        }

        if (potentialChannels.Count == 0)
            return false;

        var channel = _random.Pick(potentialChannels);
        var channelPrefix = _proto.Index<RadioChannelPrototype>(channel).KeyCode;

        _chat.TrySendInGameICMessage(
            entity,
            $"{SharedChatSystem.RadioChannelPrefix}{channelPrefix} {message}",
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);

        return true;
    }

    /// <summary>
    /// Добавляет церебральную травму игроку.
    /// Синты (IPC, киборги) не получают травму.
    /// </summary>
    public void AddTrauma(EntityUid uid, CerebralTraumaSeverity severity = CerebralTraumaSeverity.None)
    {
        if (HasComp<SiliconComponent>(uid))
            return;

        var comp = EnsureComp<CerebralTraumaComponent>(uid);

        if (severity == CerebralTraumaSeverity.None)
        {
            severity = _random.Prob(0.5f) ? CerebralTraumaSeverity.Light : CerebralTraumaSeverity.Serious;
        }

        comp.Severity = severity;
        comp.NextShoutTime = _random.NextFloat(comp.MinShoutInterval, comp.MaxShoutInterval);

        if (_defaultTraumaPhrases.Count > 0)
        {
            comp.TraumaPhraseKeys = new List<string>(_defaultTraumaPhrases);
        }

        if (severity == CerebralTraumaSeverity.Serious && _defaultNegativeQuirks.Count > 0)
        {
            var existingQuirks = new HashSet<string>(comp.GrantedQuirkIds);
            string? newQuirkId = null;
            var shuffledQuirks = _defaultNegativeQuirks.OrderBy(x => Guid.NewGuid()).ToList();

            Log.Info($"[CerebralTrauma] Выдача квирка, доступно: {_defaultNegativeQuirks.Count}");

            foreach (var quirkId in shuffledQuirks)
            {
                if (existingQuirks.Contains(quirkId))
                {
                    Log.Info($"[CerebralTrauma] Пропуск {quirkId} - уже выдан");
                    continue;
                }

                if (_proto.TryIndex<TraitPrototype>(quirkId, out var traitProto))
                {
                    var hasAnyComponent = false;
                    foreach (var compEntry in traitProto.Components)
                    {
                        var compType = compEntry.Value.Component.GetType();
                        if (HasComp(uid, compType))
                        {
                            Log.Info($"[CerebralTrauma] Пропуск {quirkId} - есть компонент {compType.Name}");
                            hasAnyComponent = true;
                            break;
                        }
                    }

                    if (!hasAnyComponent)
                    {
                        newQuirkId = quirkId;
                        Log.Info($"[CerebralTrauma] Выбран квирк {quirkId}");
                        break;
                    }
                }
                else
                {
                    Log.Info($"[CerebralTrauma] Квирк {quirkId} не найден");
                }
            }

            if (!string.IsNullOrEmpty(newQuirkId) && _proto.TryIndex<TraitPrototype>(newQuirkId, out var newQuirkProto))
            {
                if (!_whitelistSystem.IsWhitelistFail(newQuirkProto.Whitelist, uid) &&
                    !_whitelistSystem.IsBlacklistPass(newQuirkProto.Blacklist, uid))
                {
                    EntityManager.AddComponents(uid, newQuirkProto.Components, false);
                    comp.GrantedQuirkIds.Add(newQuirkId);
                    Log.Info($"[CerebralTrauma] Добавлен квирк {newQuirkId}");
                }
                else
                {
                    Log.Info($"[CerebralTrauma] Квирк {newQuirkId} не прошёл whitelist/blacklist");
                }
            }
            else
            {
                Log.Info($"[CerebralTrauma] Не удалось найти квирк {newQuirkId}");
            }
        }

        Dirty(uid, comp);
    }

    /// <summary>
    /// Удаляет церебральную травму и все выданные ею квирки.
    /// </summary>
    public void RemoveTraumaWithQuirks(EntityUid uid, CerebralTraumaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        RemoveGrantedQuirks(uid, comp.GrantedQuirkIds);
        comp.GrantedQuirkIds.Clear();
        RemComp<CerebralTraumaComponent>(uid);
    }

    /// <summary>
    /// Удаляет квирки выданные травмой (прямой вызов из shared кода).
    /// </summary>
    public void RemoveGrantedQuirksDirect(EntityUid uid, List<string> grantedQuirkIds)
    {
        Log.Info($"[CerebralTrauma] Удаление квирков: {string.Join(", ", grantedQuirkIds)}");
        
        foreach (var quirkId in grantedQuirkIds)
        {
            if (_proto.TryIndex<TraitPrototype>(quirkId, out var traitProto))
            {
                Log.Info($"[CerebralTrauma] Квирк {quirkId}, компонентов: {traitProto.Components.Count}");
                
                foreach (var compEntry in traitProto.Components)
                {
                    // Используем имя компонента для поиска типа
                    if (EntityManager.ComponentFactory.TryGetRegistration(compEntry.Key, out var compReg))
                    {
                        Log.Info($"[CerebralTrauma] Проверка {compEntry.Key} ({compReg.Type.Name})");
                        
                        if (HasComp(uid, compReg.Type))
                        {
                            Log.Info($"[CerebralTrauma] Удаление {compEntry.Key}");
                            EntityManager.RemoveComponent(uid, compReg.Type);
                        }
                        else
                        {
                            Log.Info($"[CerebralTrauma] Компонент {compEntry.Key} не найден");
                        }
                    }
                    else
                    {
                        Log.Info($"[CerebralTrauma] Регистрация компонента {compEntry.Key} не найдена");
                    }
                }
            }
            else
            {
                Log.Info($"[CerebralTrauma] Квирк {quirkId} не найден в прототипах");
            }
        }
    }

    /// <summary>
    /// Удаляет квирки выданные травмой.
    /// </summary>
    private void RemoveGrantedQuirks(EntityUid uid, List<string> grantedQuirkIds)
    {
        RemoveGrantedQuirksDirect(uid, grantedQuirkIds);
    }
}
