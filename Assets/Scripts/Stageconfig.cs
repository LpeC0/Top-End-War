using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v3.2
///
/// DEĞİŞİKLİK:
/// - Stage Flow alanı eklendi.
/// - playMode ile RunnerOnly / RunnerToAnchor / AnchorOnly desteklenir.
/// - anchorBlueprint ile stage'in Anchor savaş verisi bağlanır.
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID      = 1;
    public int    stageID      = 1;
    public string locationName = "Frontier Pass";

    // ── Localization Keys ──────────────────────────────────────────────────

    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Stage adı lokalizasyon anahtarı  ör: stage_w1_01_name")]
    public string stageNameKey = "";

    [Tooltip("Stage kısa açıklaması / briefing anahtarı  ör: stage_w1_01_desc")]
    public string stageDescriptionKey = "";

    [Tooltip("Önerilen build / strateji ipucu anahtarı  ör: stage_w1_01_build_tip")]
    public string recommendedBuildKey = "";

    [Tooltip("Stage tehdit özellikleri etiket anahtarları  ör: [ 'tag_heavy', 'tag_armored' ]")]
    public List<string> threatTagKeys = new List<string>();

    // ── Display Properties ─────────────────────────────────────────────────

    public string DisplayStageName        => string.IsNullOrEmpty(stageNameKey)        ? locationName : stageNameKey;
    public string DisplayDescription      => string.IsNullOrEmpty(stageDescriptionKey) ? ""           : stageDescriptionKey;
    public string DisplayRecommendedBuild => string.IsNullOrEmpty(recommendedBuildKey) ? ""           : recommendedBuildKey;

    // ── Denge ─────────────────────────────────────────────────────────────

    [Header("Denge — Temel Deger")]
    [Tooltip(
        "Bu stage icin hedeflenen oyuncu DPS'i.\n" +
        "HP formulleri bu degere gore hesaplanir:\n" +
        "  Normal mob   = targetDps x 1.0\n" +
        "  Elite mob    = targetDps x 4.0\n" +
        "  Mini-boss HP = targetDps x 13\n" +
        "  Final boss   = targetDps x 36")]
    public float targetDps = 70f;

    [Tooltip(
        "Hedeflenen oyuncu Combat Power'i.\n" +
        "0 = otomatik hesapla (targetDps'ten türet).\n" +
        "Debug/UI'da player vs stage güç karşılaştırması yapılır.")]
    [Range(0f, 10000f)]
    public float targetPower = 0f;

    [Header("Kapi Butcesi")]
    [Tooltip(
        "Bu stage'deki kapilarin verebilecegi max DPS buyume katsayisi.\n" +
        "entryDps = round(targetDps / gateBudgetMult)")]
    [Range(1f, 2.5f)]
    public float gateBudgetMult = 1.40f;

    // ── Stage Flow ─────────────────────────────────────────────────────────

    [Header("Stage Flow")]
    [Tooltip("Bu stage nasıl oynanacak? RunnerOnly, RunnerToAnchor veya AnchorOnly.")]
    public StagePlayMode playMode = StagePlayMode.RunnerToAnchor;

    [Tooltip("playMode = RunnerToAnchor veya AnchorOnly ise kullanılacak Anchor Blueprint.")]
    public StageBlueprint anchorBlueprint;

    // ── Boss Türü ──────────────────────────────────────────────────────────

    [Header("Boss")]
    public BossType   bossType = BossType.None;
    public BossConfig bossConfig;

    [Header("Wave Sequence")]
    [Tooltip("Bu stage boyunca oynatilacak dalga sirasi. Bos birakılırsa eski procedural fallback kullanilir.")]
    public List<WaveConfig> waveSequence = new List<WaveConfig>();

    // ── Spawn Yoğunluğu ───────────────────────────────────────────────────

    [Header("Spawn")]
    [Tooltip("1.0 = normal. DifficultyManager carpaniyla carpilir.")]
    [Range(0.5f, 3f)]
    public float spawnDensity = 1f;

    // ── Ödüller ───────────────────────────────────────────────────────────

    [Header("Odüller")]
    [Tooltip("Bos birakılırsa EconomyConfig formulunden hesaplanir.")]
    public int goldRewardOverride = 0;

    public bool hasMidStageLoot = true;

    [Range(0f, 1f)]
    public float techCoreDropChance = 0.15f;

    [Tooltip("Stage tamamlaninca saatlik altina eklenen miktar")]
    public int offlineBoostPerHour = 5;

    // ── Tutorial ──────────────────────────────────────────────────────────

    [Header("Ozel")]
    public bool isTutorialStage = false;

    // ── HP Formül Metotları ───────────────────────────────────────────────

    public int GetNormalMobHP()  => Mathf.RoundToInt(targetDps * 1.0f);
    public int GetEliteHP()      => Mathf.RoundToInt(targetDps * 4.0f);
    public int GetMiniBossHP()   => Mathf.RoundToInt(targetDps * 13f);
    public int GetFinalBossHP()  => Mathf.RoundToInt(targetDps * 36f);

    public int GetBossHP()
    {
        return bossType switch
        {
            BossType.MiniBoss  => GetMiniBossHP(),
            BossType.FinalBoss => GetFinalBossHP(),
            _                  => 0,
        };
    }

    public int GetEntryDps()
    {
        return Mathf.RoundToInt(targetDps / gateBudgetMult);
    }

    /// <summary>
    /// Etkili Stage Target Power.
    /// PlayerStats.CombatPower ile kıyaslanabilir.
    /// </summary>
    public int GetEffectiveTargetPower()
    {
        if (targetPower > 0f)
            return Mathf.RoundToInt(targetPower);

        float power = 0f;
        power += targetDps * 1.5f;  // DPS ağırlık
        power += 500f * 0.2f;       // Ortalama maxHp katkısı
        power += 5f * 8f;           // DEĞİŞİKLİK: ArmorPen katsayısı 15 değil 8
        power += 0f * 50f;          // Typical pierce = 0
        power += 22f * 2f;          // Typical range = 22

        return Mathf.Max(1, Mathf.RoundToInt(power));
    }

    public bool IsBossStage => bossType != BossType.None;

#if UNITY_EDITOR
    void OnValidate()
    {
        targetDps = Mathf.Max(1f, targetDps);
        targetPower = Mathf.Max(0f, targetPower);
        gateBudgetMult = Mathf.Clamp(gateBudgetMult, 1f, 2.5f);
        spawnDensity = Mathf.Clamp(spawnDensity, 0.5f, 3f);
        goldRewardOverride = Mathf.Max(0, goldRewardOverride);
        techCoreDropChance = Mathf.Clamp01(techCoreDropChance);
        offlineBoostPerHour = Mathf.Max(0, offlineBoostPerHour);

        if (!string.IsNullOrEmpty(locationName))
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

public enum StagePlayMode
{
    RunnerOnly,
    RunnerToAnchor,
    AnchorOnly
}