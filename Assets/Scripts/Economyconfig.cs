using UnityEngine;

[CreateAssetMenu(fileName = "EconomyConfig", menuName = "TopEndWar/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Slice Feature Toggles")]
    public bool enableOfflineEarnings = false;

    [Tooltip("false ise slot upgrade sadece Gold harcar")]
    public bool useTechCoreForSlotUpgrades = false;

    public bool enablePitySystem = false;

    [Header("Slot Yukseltme — Altin Maliyeti")]
    public float slotGoldCostBase = 180f;
    public float slotGoldCostGrowth = 1.22f;

    [Header("Slot Yukseltme — Tech Core Maliyeti (Bantli)")]
    public int[] tcBandFromLevel = { 1, 6, 11, 16, 21, 31 };
    public int[] tcBandToLevel   = { 5, 10, 15, 20, 30, 50 };
    public int[] tcBandCost      = { 1, 2, 3, 4, 5, 7 };

    [Header("Stage Odulu Formulü")]
    public float goldBase = 120f;
    public float goldPerStage = 10f;
    public float goldDpsFactor = 0.20f;

    [Range(0f, 1f)]
    public float midLootFraction = 0.35f;

    [Header("Offline Gelir")]
    public int baseOfflineRate = 50;
    [Range(8f, 24f)]
    public float offlineCapHours = 15f;

    [Header("Reklam Politikasi")]
    public int reviveAdsPerRun = 1;
    public int doubleGoldAdsDaily = 3;
    public int bonusChestAdsDaily = 4;

    [Header("Pity Timer (Acima Sayaci)")]
    public int pityStagThreshold = 20;

    public int GetSlotGoldCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        return Mathf.RoundToInt(slotGoldCostBase * Mathf.Pow(slotGoldCostGrowth, level - 1));
    }

    public int GetSlotTechCoreCost(int level)
    {
        if (!useTechCoreForSlotUpgrades)
            return 0;

        level = Mathf.Clamp(level, 1, 50);
        for (int i = 0; i < tcBandFromLevel.Length; i++)
            if (level >= tcBandFromLevel[i] && level <= tcBandToLevel[i])
                return tcBandCost[i];

        return tcBandCost.Length > 0 ? tcBandCost[tcBandCost.Length - 1] : 0;
    }

    public int GetGoldReward(int stageNumber, float targetDps)
        => Mathf.RoundToInt(goldBase + goldPerStage * stageNumber + goldDpsFactor * targetDps);

    public int GetMidLootGold(int stageNumber, float targetDps)
        => Mathf.RoundToInt(GetGoldReward(stageNumber, targetDps) * midLootFraction);

#if UNITY_EDITOR
    void OnValidate()
    {
        slotGoldCostBase = Mathf.Max(1f, slotGoldCostBase);
        slotGoldCostGrowth = Mathf.Max(1.01f, slotGoldCostGrowth);
        goldBase = Mathf.Max(0f, goldBase);
        goldPerStage = Mathf.Max(0f, goldPerStage);
        goldDpsFactor = Mathf.Max(0f, goldDpsFactor);
        midLootFraction = Mathf.Clamp01(midLootFraction);
        baseOfflineRate = Mathf.Max(0, baseOfflineRate);
        offlineCapHours = Mathf.Clamp(offlineCapHours, 8f, 24f);
        pityStagThreshold = Mathf.Max(1, pityStagThreshold);

        if (tcBandFromLevel == null) tcBandFromLevel = new int[0];
        if (tcBandToLevel == null) tcBandToLevel = new int[0];
        if (tcBandCost == null) tcBandCost = new int[0];
    }
#endif
}