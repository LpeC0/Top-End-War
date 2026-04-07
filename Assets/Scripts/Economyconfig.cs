using UnityEngine;

/// <summary>
/// Top End War — Ekonomi Konfigurasyonu v1 (Claude)
///
/// Tum ekonomi formullerini tek bir SO'da toplar.
/// ChatGPT canonical JSON'undan turetildi.
///
/// FORMÜLLER:
///   SlotGoldCost(level)     = round(180 * 1.22^(level-1))
///   GoldReward(stage,dps)   = round(120 + 10*stage + 0.20*targetDps)
///   MidLootGold             = round(goldReward * 0.35)
///
/// TECH CORE BANTLARI:
///   Level 1-5   → 1 TC
///   Level 6-10  → 2 TC
///   Level 11-15 → 3 TC
///   Level 16-20 → 4 TC
///   Level 21-30 → 5 TC
///   Level 31-50 → 7 TC
///
/// ASSETS: Create > TopEndWar > EconomyConfig
/// EconomyManager bu SO'yu okur.
/// </summary>
[CreateAssetMenu(fileName = "EconomyConfig", menuName = "TopEndWar/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    // ── Slot Gold Maliyeti ────────────────────────────────────────────────
    [Header("Slot Yukseltme — Altin Maliyeti")]
    [Tooltip("Temel maliyet (level 1). Her seviye 1.22x artar.")]
    public float slotGoldCostBase    = 180f;

    [Tooltip("Buyume katsayisi. 1.22 = her seviye %22 pahali.")]
    public float slotGoldCostGrowth  = 1.22f;

    // ── Slot Tech Core Maliyeti (Bantli) ─────────────────────────────────
    [Header("Slot Yukseltme — Tech Core Maliyeti (Bantli)")]
    [Tooltip("Level aralik baslangici")]
    public int[] tcBandFromLevel     = { 1,  6, 11, 16, 21, 31 };
    [Tooltip("Level aralik bitis (kapsamli)")]
    public int[] tcBandToLevel       = { 5, 10, 15, 20, 30, 50 };
    [Tooltip("Her banttaki Tech Core maliyeti")]
    public int[] tcBandCost          = { 1,  2,  3,  4,  5,  7 };

    // ── Stage Altin Odulu ─────────────────────────────────────────────────
    [Header("Stage Odulu Formulü")]
    [Tooltip("Baz altin: round(goldBase + goldPerStage*stage + goldDpsFactor*targetDps)")]
    public float goldBase            = 120f;
    public float goldPerStage        = 10f;
    public float goldDpsFactor       = 0.20f;

    [Tooltip("Stage ortasi micro-loot orani (0.35 = odulun %35'i)")]
    [Range(0f, 1f)]
    public float midLootFraction     = 0.35f;

    // ── Offline Gelir ─────────────────────────────────────────────────────
    [Header("Offline Gelir")]
    public int   baseOfflineRate     = 50;     // Altin / saat (baslangic)
    [Range(8f, 24f)]
    public float offlineCapHours     = 15f;

    // ── Reklam Sinirlamalari ──────────────────────────────────────────────
    [Header("Reklam Politikasi")]
    public int   reviveAdsPerRun     = 1;
    public int   doubleGoldAdsDaily  = 3;
    public int   bonusChestAdsDaily  = 4;
    // techCoreAds ve hardCurrencyAds kapalı — kod seviyesinde bypass yok

    // ── Pity Timer ────────────────────────────────────────────────────────
    [Header("Pity Timer (Acima Sayaci)")]
    [Tooltip("Kac bos stage sonra Basic Scroll garantilenir")]
    public int   pityStagThreshold   = 20;

    // ── API ───────────────────────────────────────────────────────────────

    /// <summary>Belirtilen seviyenin slot yükseltme altin maliyetini dondurur.</summary>
    public int GetSlotGoldCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        return Mathf.RoundToInt(slotGoldCostBase * Mathf.Pow(slotGoldCostGrowth, level - 1));
    }

    /// <summary>Belirtilen seviyenin Tech Core maliyetini dondurur.</summary>
    public int GetSlotTechCoreCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        for (int i = 0; i < tcBandFromLevel.Length; i++)
            if (level >= tcBandFromLevel[i] && level <= tcBandToLevel[i])
                return tcBandCost[i];
        return tcBandCost[tcBandCost.Length - 1];
    }

    /// <summary>Stage altin odulunu hesaplar.</summary>
    public int GetGoldReward(int stageNumber, float targetDps)
        => Mathf.RoundToInt(goldBase + goldPerStage * stageNumber + goldDpsFactor * targetDps);

    /// <summary>Stage ortasi micro-loot altinini hesaplar.</summary>
    public int GetMidLootGold(int stageNumber, float targetDps)
        => Mathf.RoundToInt(GetGoldReward(stageNumber, targetDps) * midLootFraction);
}