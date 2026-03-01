using Content.Shared.ADT.CerebralTrauma;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Chemistry.EntityEffects;

/// <summary>
/// Система лечения лёгкой церебральной травмы (Mannitol).
/// </summary>
public sealed partial class CureCerebralTraumaEntityEffectSystem : EntityEffectSystem<CerebralTraumaComponent, CureCerebralTrauma>
{
    protected override void Effect(Entity<CerebralTraumaComponent> entity, ref EntityEffectEvent<CureCerebralTrauma> args)
    {
        if (entity.Comp.Severity == CerebralTraumaSeverity.Light)
        {
            Log.Info($"[Mannitol] Лечение травмы у {entity}, квирков: {entity.Comp.GrantedQuirkIds.Count}");
            
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
/// Эффект лечения лёгкой церебральной травмы.
/// </summary>
public sealed partial class CureCerebralTrauma : EntityEffectBase<CureCerebralTrauma>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-cure-cerebral-trauma");
}

/// <summary>
/// Событие для удаления квирков.
/// </summary>
[Serializable, NetSerializable]
public sealed class CerebralTraumaCuredEvent : EntityEventArgs
{
    public NetEntity Target;
    public List<string> GrantedQuirkIds;
    
    public CerebralTraumaCuredEvent(NetEntity target, List<string> grantedQuirkIds)
    {
        Target = target;
        GrantedQuirkIds = grantedQuirkIds;
    }
}
