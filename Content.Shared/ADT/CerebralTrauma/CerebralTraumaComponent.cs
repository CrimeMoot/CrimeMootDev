using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CerebralTrauma;

/// <summary>
/// Компонент для отслеживания церебральной травмы после реанимации дефибриллятором.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class CerebralTraumaComponent : Component
{
    /// <summary>
    /// Тип травмы: лёгкая или серьёзная.
    /// </summary>
    [DataField("severity")]
    public CerebralTraumaSeverity Severity = CerebralTraumaSeverity.None;

    /// <summary>
    /// Время до следующего крика (в секундах).
    /// </summary>
    [DataField("nextShoutTime")]
    public float NextShoutTime = 0f;

    /// <summary>
    /// Минимальное время между криками (в секундах).
    /// </summary>
    [DataField("minShoutInterval")]
    public float MinShoutInterval = 5f;

    /// <summary>
    /// Максимальное время между криками (в секундах).
    /// </summary>
    [DataField("maxShoutInterval")]
    public float MaxShoutInterval = 30f;

    /// <summary>
    /// Список ключей локализации для фраз церебральной травмы.
    /// </summary>
    [DataField("traumaPhraseKeys")]
    public List<string> TraumaPhraseKeys = new();

    /// <summary>
    /// ADT-Tweak: Список ID выданных квирков при серьёзной травме.
    /// Хранит только квирки, выданные травмой, а не взятые в настройках персонажа.
    /// </summary>
    [DataField("grantedQuirkIds")]
    public List<string> GrantedQuirkIds = new();

    /// <summary>
    /// ADT-Tweak: Список отрицательных квирков которые могут быть выданы при серьёзной травме.
    /// </summary>
    [DataField("negativeQuirkIds")]
    public List<string> NegativeQuirkIds = new();
}

/// <summary>
/// Серьёзность церебральной травмы.
/// </summary>
public enum CerebralTraumaSeverity
{
    None,
    Light,      // Лёгкая травма
    Serious     // Серьёзная травма
}
