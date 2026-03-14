using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Dengeleme Konfigurasyonu (Claude + Gemini DDA)
/// Assets/ProgressionConfig klasorunde olustur:
///   Project → Create → TopEndWar → Progression Config
///
/// Bu ScriptableObject oyunun matematiksel dengesinin merkezi.
/// Inspector'dan tweak et, DifficultyManager okur.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Temel Ilerleme")]
    [Tooltip("Her 100 birimdeki buyume carpani. 1.15 = %15")]
    [Range(1.05f, 1.5f)]
    public float growthRate = 1.15f;

    [Tooltip("Zorluk egrisi ussu. 1.3 = dengeli, 2.0 = cok sert")]
    [Range(1.0f, 3.0f)]
    public float difficultyExponent = 1.3f;

    public int baseStartCP = 200;

    [Header("Dusman Olcekleme")]
    public int   baseEnemyHealth    = 100;
    public int   baseEnemyDamage    = 25;
    public float baseEnemySpeed     = 4.5f;
    public float enemyMaxSpeed      = 8f;    // Hiz tavan (Gemini onerisi)

    [Tooltip("Oyuncu CP'sine gore dushman guclenme faktoru")]
    [Range(0.5f, 1.5f)]
    public float playerCPScalingFactor = 0.9f;

    [Header("Kapi Dengeleme")]
    public float gateValueGrowthRate = 1.12f;
    public int   minGateValue        = 20;
    public int   maxGateValue        = 500;

    [Tooltip("Boss oncesi bu mesafede negatif/risk kapi cikmasın (Pity Timer)")]
    public float noBadGateZoneBeforeBoss = 200f;

    [Header("Tier Eslikleri")]
    public int[] tierThresholds = { 0, 300, 800, 2000, 5000 };

    // ── Hesaplama Metodlari (GC-friendly, allocation yok) ────────────────────

    /// <summary>Belirli mesafedeki beklenen CP.</summary>
    public int CalculateExpectedCP(float distance)
    {
        float segments   = distance / 100f;
        float multiplier = Mathf.Pow(growthRate, segments);
        return Mathf.RoundToInt(baseStartCP * multiplier);
    }

    /// <summary>Mesafeye gore zorluk carpani.</summary>
    public float CalculateDifficultyMultiplier(float distance)
    {
        float normalized = distance / 1000f;
        return 1f + Mathf.Pow(normalized, difficultyExponent);
    }

    /// <summary>Kapi degerini mesafeye gore olcekle.</summary>
    public int ScaleGateValue(int baseValue, float distance)
    {
        float segments = distance / 150f;
        float mult     = Mathf.Pow(gateValueGrowthRate, segments);
        int   scaled   = Mathf.RoundToInt(baseValue * mult);
        if (scaled < minGateValue) return minGateValue;
        if (scaled > maxGateValue) return maxGateValue;
        return scaled;
    }
}