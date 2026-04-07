using UnityEngine;

/// <summary>
/// Top End War — Dusman Arketip Konfigurasyonu v1 (Claude)
///
/// Bir dusman sinifinin tasarim verisi. Runtime state icermez.
/// Enemy.cs, SpawnManager'dan gelen ArchetypeConfig + StageConfig.targetDps
/// ikilisiyle kendini baslatir:
///   HP = targetDps * hpFactor
///
/// SLICE DEGERLERI (Final Pack v1):
///   Trooper:      hpFactor=0.90, armor=0,  speed=Orta
///   Swarm:        hpFactor=0.35, armor=0,  speed=Yuksek
///   Charger:      hpFactor=0.65, armor=0,  speed=Yuksek
///   ArmoredBrute: hpFactor=1.25, armor=28, speed=Dusuk
///   EliteCharger: hpFactor=3.40, armor=8,  speed=CokYuksek
///
/// ASSETS: Create > TopEndWar > EnemyArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TopEndWar/EnemyArchetypeConfig")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string  enemyId    = "trooper";
    public string  enemyName  = "Trooper";
    public EnemyClass enemyClass = EnemyClass.Normal;

    [Header("Stat Faktörleri")]
    [Tooltip("HP = StageConfig.targetDps * hpFactor")]
    public float hpFactor     = 0.90f;

    [Tooltip("Zirh degeri. Silah ArmorPen bu degerden dusulur.")]
    public int   armor        = 0;

    [Tooltip("Hareket hizi (birim/saniye). SpawnManager bu degeri kullanir.")]
    public float moveSpeed    = 4.5f;

    [Tooltip("Temas hasari (Komutan HP'ye vurur)")]
    public int   contactDamage = 30;

    [Header("Davranis")]
    public EnemyThreatType threatType = EnemyThreatType.Standard;

    [Tooltip("Runner modunda spawnlanabilir mi?")]
    public bool canSpawnInRunner  = true;

    [Tooltip("Anchor modunda spawnlanabilir mi? (ileri asamalar icin)")]
    public bool canSpawnInAnchor  = true;

    [Header("Odül")]
    [Tooltip("Olunce verilen CP miktarinin temel carpani")]
    public float cpRewardFactor   = 0.06f;   // targetDps * 0.06 civarinda

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public RuntimeAnimatorController animatorOverride;

    // ── Hesaplamalar ──────────────────────────────────────────────────────

    /// <summary>
    /// Verilen stage targetDps degerine gore bu dusmanin HP'sini dondurur.
    /// Formul: round(targetDps * hpFactor)
    /// </summary>
    public int GetHP(float targetDps)
        => Mathf.RoundToInt(targetDps * hpFactor);

    /// <summary>Bu stage icin CP odulunu hesaplar.</summary>
    public int GetCPReward(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * cpRewardFactor));

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(enemyId))
            name = $"Enemy_{enemyClass}_{enemyId}";
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
    Standard,       // Trooper — referans dusman
    PackPressure,   // Swarm  — kalabalik baskisi
    Priority,       // Charger — once vurulmali
    Durable,        // Armored Brute — zirh kontrolu
    ElitePressure,  // Elite Charger — panik + oncelik
    Backline,       // Operator — arkadan tehdit (ileride)
}