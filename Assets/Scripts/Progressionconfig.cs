using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Konfigurasyonu (Claude)
/// Assets → Create → TopEndWar → Progression Config
/// DifficultyManager'a bagla. Baglamazsan DifficultyManager dahili sabitlerle calisir.
/// NAMESPACE YOK — eski GPT kodlari namespace kullaniyordu, biz kullanmiyoruz.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Ilerleme")]
    [Range(1.05f, 1.5f)] public float growthRate          = 1.15f;
    [Range(1.0f,  3.0f)] public float difficultyExponent  = 1.3f;
    public int baseStartCP = 200;

    [Header("Dusman")]
    public int   baseEnemyHealth       = 100;
    public int   baseEnemyDamage       = 25;
    public float baseEnemySpeed        = 4.0f;
    public float enemyMaxSpeed         = 7.5f;
    [Range(0.5f, 1.5f)] public float playerCPScalingFactor = 0.9f;

    [Header("Kapi")]
    public float gateValueGrowthRate   = 1.12f;
    public int   minGateValue          = 20;
    public int   maxGateValue          = 500;
    public float noBadGateZoneBeforeBoss = 200f;

    [Header("Tier Eslikleri")]
    public int[] tierThresholds = { 0, 300, 800, 2000, 5000 };

    public int CalculateExpectedCP(float d)
        => Mathf.RoundToInt(baseStartCP * Mathf.Pow(growthRate, d / 100f));

    public float CalculateDifficultyMultiplier(float d)
        => 1f + Mathf.Pow(d / 1000f, difficultyExponent);

    public int ScaleGateValue(int v, float d)
    {
        int s = Mathf.RoundToInt(v * Mathf.Pow(gateValueGrowthRate, d / 150f));
        if (s < minGateValue) return minGateValue;
        if (s > maxGateValue) return maxGateValue;
        return s;
    }
}
