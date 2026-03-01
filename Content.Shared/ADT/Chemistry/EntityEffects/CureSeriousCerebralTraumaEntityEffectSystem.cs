using Content.Shared.ADT.CerebralTrauma;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.ADT.Chemistry.EntityEffects;

namespace Content.Shared.ADT.Chemistry.EntityEffects;

/// <summary>
/// Система лечения серьёзной церебральной травмы (Psicodine).
/// </summary>
public sealed partial class CureSeriousCerebralTraumaEntityEffectSystem : EntityEffectSystem<CerebralTraumaComponent, CureSeriousCerebralTrauma>
{
    protected override void Effect(Entity<CerebralTraumaComponent> entity, ref EntityEffectEvent<CureSeriousCerebralTrauma> args)
    {
        if (entity.Comp.Severity == CerebralTraumaSeverity.Serious)
        {
            Log.Info($"[Psicodine] Лечение травмы у {entity}, квирков: {entity.Comp.GrantedQuirkIds.Count}");
            
            // Сохраняем квирки перед удалением
            var grantedQuirkIds = new List<string>(entity.Comp.GrantedQuirkIds);
            
            // Удаляем компонент травмы
            EntityManager.RemoveComponent<CerebralTraumaComponent>(entity);
            
            // Вызываем событие для удаления квирков (доходит до сервера)
            var ev = new CerebralTraumaCuredEvent(EntityManager.GetNetEntity(entity.Owner), grantedQuirkIds);
            RaiseLocalEvent(entity, ev);
        }
    }
}

/// <summary>
/// Эффект лечения серьёзной церебральной травмы.
/// </summary>
public sealed partial class CureSeriousCerebralTrauma : EntityEffectBase<CureSeriousCerebralTrauma>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-cure-serious-cerebral-trauma");
}
