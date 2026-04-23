using UnityEngine;

/// <summary>
/// Top End War — Dusman Arketip Konfigurasyonu v2.1
///
/// v2 → v2.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: displayNameKey, descriptionKey, roleKey, threatTag1Key, threatTag2Key
///   • DisplayName, DisplayDescription, DisplayRole, DisplayThreatTag1, DisplayThreatTag2 property'leri eklendi
///   • Mevcut enemyName ve tüm stat alanları DOKUNULMADI
///
/// Eski alanlar:
///   enemyName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > EnemyArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TopEndWar/EnemyArchetypeConfig")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string enemyId = "trooper";
    public string enemyName = "Trooper";
    public EnemyClass enemyClass = EnemyClass.Normal;

    // ── Localization Keys ──────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Display property'leri fallback olarak enemyName vb. döner.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Düşman görünen adı anahtarı  ör: enemy_trooper_name")]
    public string displayNameKey  = "";
    [Tooltip("Kısa açıklama / flavor text anahtarı  ör: enemy_trooper_desc")]
    public string descriptionKey  = "";
    [Tooltip("Rol / davranış etiketi anahtarı  ör: enemy_trooper_role  →  'Standart Piyade'")]
    public string roleKey         = "";
    [Tooltip("Tehdit UI sol tag anahtarı  ör: enemy_trooper_threat1  →  'HIZLI'")]
    public string threatTag1Key   = "";
    [Tooltip("Tehdit UI sağ tag anahtarı  ör: enemy_trooper_threat2  →  'SÜRÜ'")]
    public string threatTag2Key   = "";

    // ── Display Properties (Localization-ready fallback) ───────────────────
    public string DisplayName        => string.IsNullOrEmpty(displayNameKey)  ? enemyName : displayNameKey;
    public string DisplayDescription => string.IsNullOrEmpty(descriptionKey)  ? ""         : descriptionKey;
    public string DisplayRole        => string.IsNullOrEmpty(roleKey)         ? ""         : roleKey;
    public string DisplayThreatTag1  => string.IsNullOrEmpty(threatTag1Key)   ? ""         : threatTag1Key;
    public string DisplayThreatTag2  => string.IsNullOrEmpty(threatTag2Key)   ? ""         : threatTag2Key;

    [Header("Stat Faktörleri")]
    [Tooltip("HP = StageConfig.targetDps * hpFactor")]
    public float hpFactor = 0.90f;

    [Tooltip("Zirh degeri. Silah ArmorPen bu degerden dusulur.")]
    public int armor = 0;

    [Tooltip("Hareket hizi (birim/saniye).")]
    public float moveSpeed = 4.5f;

    [Tooltip("Temas hasari")]
    public int contactDamage = 30;

    [Header("Davranis")]
    public EnemyThreatType threatType = EnemyThreatType.Standard;
    public bool canSpawnInRunner = true;
    public bool canSpawnInAnchor = true;

    [Header("Odül")]
    [Tooltip("CP reward = targetDps * cpRewardFactor")]
    public float cpRewardFactor = 0.06f;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public RuntimeAnimatorController animatorOverride;

    public int GetHP(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * hpFactor));

    public int GetCPReward(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * cpRewardFactor));

    public bool IsEliteLike =>
        enemyClass == EnemyClass.Elite ||
        threatType == EnemyThreatType.ElitePressure;

#if UNITY_EDITOR
    void OnValidate()
    {
        hpFactor = Mathf.Max(0.1f, hpFactor);
        armor = Mathf.Max(0, armor);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        contactDamage = Mathf.Max(0, contactDamage);
        cpRewardFactor = Mathf.Max(0f, cpRewardFactor);
    }
#endif
}

public enum EnemyClass
{
    Normal,
    Elite,
    MiniBoss,
    BossSupport,
}

public enum EnemyThreatType
{
    Standard,
    PackPressure,
    Priority,
    Durable,
    ElitePressure,
    Backline,
}
