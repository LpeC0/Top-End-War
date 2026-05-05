using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — StageBlueprint v1.0
///
/// Anchor mode'un tüm verisini tutan ScriptableObject.
/// Hem developer'ın elle doldurduğu campaign stage'leri
/// hem de ilerideki level editor'ün ürettiği custom stage'ler
/// bu formattan beslenir.
///
/// Tasarım ilkesi:
///   StageBlueprint hiçbir MonoBehaviour'a doğrudan referans TUTMAZ.
///   Sadece saf veri. AnchorModeManager bu veriyi okur ve sahneye uygular.
///   Bu sayede aynı blueprint hem aynı sahnede hem ayrı sahnede çalışır.
///
/// Level Editor bağlantısı:
///   İleride oyuncu bu SO'nun alanlarını bir UI üzerinden doldurabilir.
///   Runtime'da ScriptableObject.CreateInstance<StageBlueprint>() ile
///   kod tarafından da üretilebilir — sahneye bağımlı değil.
///
/// ASSETS: Create > TopEndWar > StageBlueprint
/// </summary>
[CreateAssetMenu(fileName = "Blueprint_", menuName = "TopEndWar/StageBlueprint")]
public class StageBlueprint : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────

    [Header("Kimlik")]
    public string blueprintId   = "anchor_w1_s01";
    public string displayName   = "Sahil Savunması";

    [Tooltip("Hangi dünya ve stage'e ait. Campaign için zorunlu, custom için boş bırakılabilir.")]
    public int worldID = 1;
    public int stageID = 1;

    // ── Anchor Parametreleri ──────────────────────────────────────────────

    [Header("Anchor HP")]
    [Tooltip("Stage'in temel Anchor HP'si. " +
             "AnchorCore buna PlayerStats.TotalEquipmentHPBonus() ekler.")]
    public int anchorBaseHP = 1000;

    // ── Kazanma / Kaybetme ────────────────────────────────────────────────

    [Header("Win Condition")]
    public AnchorWinCondition winCondition = AnchorWinCondition.ClearAllWaves;

    [Tooltip("WinCondition = SurviveSeconds ise bu süre kullanılır.")]
    public float survivalDuration = 90f;

    // ── Dalga Dizisi ──────────────────────────────────────────────────────

    [Header("Dalga Dizisi")]
    [Tooltip("Sırayla oynanacak dalgalar. Her dalga bir AnchorWaveEntry.")]
    public List<AnchorWaveEntry> waves = new List<AnchorWaveEntry>();

    [Tooltip("Dalgalar arası nefes süresi (saniye). " +
             "0 = anında sonraki dalga. Önerilen: 3–6.")]
    public float waveCooldown = 4f;

    // ── Ödül ──────────────────────────────────────────────────────────────

    [Header("Ödül")]
    public int goldReward   = 200;
    public int bonusGoldOnPerfect = 100;   // Anchor HP tam doluyken bitirirse

    // ── Zorluk Katsayısı ──────────────────────────────────────────────────

    [Header("Zorluk")]
    [Tooltip("Düşman HP ve hasar değerleri bu katsayıyla çarpılır. " +
             "1.0 = StageConfig.targetDps bazlı normal değer.")]
    [Range(0.5f, 3f)]
    public float difficultyMultiplier = 1f;

    // ── Yardımcı ─────────────────────────────────────────────────────────

    /// <summary>
    /// Toplam dalga sayısı. AnchorModeManager ilerleme takibi için kullanır.
    /// </summary>
    public int TotalWaves => waves != null ? waves.Count : 0;

    /// <summary>
    /// Belirtilen indekste dalga var mı?
    /// </summary>
    public bool HasWave(int index)
        => waves != null && index >= 0 && index < waves.Count;

    /// <summary>
    /// Güvenli dalga erişimi. Yoksa null döner.
    /// </summary>
    public AnchorWaveEntry GetWave(int index)
        => HasWave(index) ? waves[index] : null;

#if UNITY_EDITOR
    void OnValidate()
    {
        anchorBaseHP        = Mathf.Max(100, anchorBaseHP);
        survivalDuration    = Mathf.Max(10f, survivalDuration);
        waveCooldown        = Mathf.Max(0f, waveCooldown);
        goldReward          = Mathf.Max(0, goldReward);
        bonusGoldOnPerfect  = Mathf.Max(0, bonusGoldOnPerfect);
        difficultyMultiplier = Mathf.Clamp(difficultyMultiplier, 0.5f, 3f);

        if (!string.IsNullOrEmpty(blueprintId))
            name = $"Blueprint_{blueprintId}";
    }
#endif
}

// ── Dalga Girdisi ─────────────────────────────────────────────────────────

/// <summary>
/// Blueprint'teki tek bir dalga tanımı.
/// Hangi düşmanlar, kaç tane, hangi lane'den, ne kadar aralıkla.
/// </summary>
[System.Serializable]
public class AnchorWaveEntry
{
    [Tooltip("Bu dalganın okunabilir adı (debug ve level editor için).")]
    public string waveLabel = "Dalga";

    [Tooltip("Dalganın tipi — AnchorModeManager spawn stratejisini buna göre seçer.")]
    public AnchorWaveType waveType = AnchorWaveType.Standard;

    [Tooltip("Bu dalgadaki düşman grupları. SpawnPacketConfig veya WaveGroup listesi.")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Tooltip("Gruplar arası spawn gecikmesi (saniye).")]
    [Range(0f, 5f)]
    public float groupDelay = 1.5f;

    [Tooltip("Grup içi düşman spawn aralığı (saniye).")]
    [Range(0f, 2f)]
    public float intraDelay = 0.3f;

    [Tooltip("Bu dalgadan önce oyuncuya gösterilecek uyarı metni. " +
             "Boşsa uyarı gösterilmez.")]
    public string warningText = "";

    [Tooltip("Uyarının kaç saniye önce gösterileceği.")]
    [Range(0f, 5f)]
    public float warningLeadTime = 2f;

    [Tooltip("True = bu wave temizlenmeden sonraki wave başlamaz. False = surge overlap için cooldown sonrası devam eder.")]
    public bool waitForClearBeforeNext = true; // DEĞİŞİKLİK: W1-01 flood-surge ritmi için seçilebilir clear bekleme.
}

// ── Enum'lar ──────────────────────────────────────────────────────────────

public enum AnchorWinCondition
{
    ClearAllWaves,      // Tüm dalgaları temizle
    SurviveSeconds,     // Belirli süre dayan
    DefeatElite,        // Dalga içindeki elite'i öldür
    DefeatBoss,         // Son dalganın boss'unu öldür
}

public enum AnchorWaveType
{
    Standard,           // Normal karışık dalga
    Swarm,              // Yoğun hızlı sürü
    ArmorCheck,         // Zırhlı ön hat
    EliteStrike,        // Elite baskısı
    BossWave,           // Final boss dalgası
    Relief,             // Nefes dalgası — az düşman, oyuncu toparlanır
}
