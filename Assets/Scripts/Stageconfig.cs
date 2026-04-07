using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v2 (Claude)
///
/// v2: targetDps eklendi. HP degerleri artik formule gore hesaplanir:
///   normalMobHP = targetDps * 1.0
///   eliteHP     = targetDps * 4.0
///   miniBossHP  = targetDps * 18
///   finalBossHP = targetDps * 36
///
/// gateBudgetMult: Bu stage'de kapilarin verebilecegi max DPS artis cokeni.
/// BandliBütçe (ChatGPT canonical JSON):
///   Stage 1-5:  1.40 | 6-9:   1.50 | 10: 1.55
///   Stage 11-19: 1.65 | 20:   1.70
///   Stage 21-29: 1.80 | 30-34: 1.88 | 35: 1.95
///
/// HP degerleri Inspector'dan ELLE YAZILMAZ — GetXxxHP() metotlari kullanilir.
/// StageManager bu metotlari cagirip SpawnManager ve BossManager'a iletir.
///
/// ASSETS: Create > TopEndWar > StageConfig
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID        = 1;
    public int    stageID        = 1;
    public string locationName   = "Sivas - Sinir Boyu";

    // ── Denge ─────────────────────────────────────────────────────────────
    [Header("Denge — Temel Deger")]
    [Tooltip(
        "Bu stage icin hedeflenen oyuncu DPS'i.\n" +
        "HP formulleri bu degere gore hesaplanir:\n" +
        "  Normal mob   = targetDps x 1.0\n" +
        "  Elite mob    = targetDps x 4.0\n" +
        "  Mini-boss HP = targetDps x 18\n" +
        "  Final boss   = targetDps x 36")]
    public float targetDps = 70f;

    [Header("Kapi Butcesi")]
    [Tooltip(
        "Bu stage'deki kapilarin verebilecegi max DPS buyume katsayisi.\n" +
        "entryDps = round(targetDps / gateBudgetMult)\n" +
        "Stage 1-5: 1.40 | 6-9: 1.50 | 10: 1.55 | 11-19: 1.65 | 20: 1.70\n" +
        "Stage 21-29: 1.80 | 30-34: 1.88 | 35: 1.95")]
    [Range(1f, 2.5f)]
    public float gateBudgetMult  = 1.40f;

    // ── Boss Turu ─────────────────────────────────────────────────────────
    [Header("Boss")]
    public BossType bossType     = BossType.None;

    // ── Spawn Yogunlugu ───────────────────────────────────────────────────
    [Header("Spawn")]
    [Tooltip("1.0 = normal. DifficultyManager carpaniyla carpilir.")]
    [Range(0.5f, 3f)]
    public float spawnDensity    = 1f;

    // ── Odüller ───────────────────────────────────────────────────────────
    [Header("Odüller")]
    [Tooltip("Bos birakılırsa EconomyConfig formulunden hesaplanir.")]
    public int    goldRewardOverride   = 0;  // 0 = formul kullan
    public bool   hasMidStageLoot      = true;
    [Range(0f, 1f)]
    public float  techCoreDropChance   = 0.15f;
    [Tooltip("Stage tamamlaninca saatlik altina eklenen miktar")]
    public int    offlineBoostPerHour  = 5;

    // ── Tutorial ──────────────────────────────────────────────────────────
    [Header("Ozel")]
    public bool   isTutorialStage    = false;

    // ── HP Formul Metotlari (StageManager kullanir) ───────────────────────

    /// <summary>Normal mob HP = targetDps x 1.0</summary>
    public int GetNormalMobHP()   => Mathf.RoundToInt(targetDps * 1.0f);

    /// <summary>Elite mob HP = targetDps x 4.0</summary>
    public int GetEliteHP()       => Mathf.RoundToInt(targetDps * 4.0f);

    /// <summary>Mini-boss HP = targetDps x 18. BossType.MiniBoss icin kullanilir.</summary>
    public int GetMiniBossHP()    => Mathf.RoundToInt(targetDps * 18f);

    /// <summary>Final boss HP = targetDps x 36. BossType.FinalBoss icin kullanilir.</summary>
    public int GetFinalBossHP()   => Mathf.RoundToInt(targetDps * 36f);

    /// <summary>BossType'a gore dogru HP degerini dondurur.</summary>
    public int GetBossHP()
    {
        return bossType switch
        {
            BossType.MiniBoss   => GetMiniBossHP(),
            BossType.FinalBoss  => GetFinalBossHP(),
            _                   => 0,
        };
    }

    /// <summary>entryDps = round(targetDps / gateBudgetMult)</summary>
    public int GetEntryDps() => Mathf.RoundToInt(targetDps / gateBudgetMult);

    public bool IsBossStage => bossType != BossType.None;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(name))
            name = $"Stage_W{worldID}_{stageID:D2}";
    }
#endif
}

public enum BossType
{
    None,
    MiniBoss,
    FinalBoss,
}