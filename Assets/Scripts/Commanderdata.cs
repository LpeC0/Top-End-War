using UnityEngine;

/// <summary>
/// Top End War — Komutan Verisi v1 (Claude)
///
/// Her komutan bir ScriptableObject'tir.
/// Assets > Create > TopEndWar > CommanderData
///
/// PlayerStats bu dosyadan stat okur.
/// Tier tabloları burada tutulur — PlayerController'daki sabit diziler kaldırıldı.
/// </summary>
[CreateAssetMenu(fileName = "Commander_", menuName = "TopEndWar/CommanderData")]
public class CommanderData : ScriptableObject
{
    [Header("Kimlik")]
    public string commanderName   = "Gonullu Er";
    public Sprite portrait;
    [TextArea(2, 4)]
    public string lore            = "";

    [Header("Tier Bazli Istatistikler (5 deger = Tier 1-5)")]
    [Tooltip("Tier 1'den 5'e temel hasar degerleri")]
    public float[] baseDMG        = { 60f, 95f, 145f, 210f, 300f };

    [Tooltip("Tier 1'den 5'e atisHizi (atis/saniye)")]
    public float[] baseFireRate   = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };

    [Tooltip("Tier 1'den 5'e temel HP")]
    public int[]   baseHP         = { 500, 700, 950, 1200, 1500 };

    [Header("Ozel Mekanik")]
    public CommanderSpecialty specialty = CommanderSpecialty.Assault;
    public ArmySynergy armySynergy     = ArmySynergy.Hybrid;

    [Tooltip("Komutan sinerjisi: asker turune gore hasar carpani")]
    [Range(1f, 1.5f)]
    public float armyDamageMultiplier  = 1.0f;

    [Header("Kilit Kosulu")]
    [Tooltip("Hangi dunya bitmeli? 0 = baslangictan acik")]
    public int requiredWorldID         = 0;

    [Header("Gorsel Evrim (Tier basi model/aura)")]
    public GameObject[] tierModels;        // 5 eleman, Tier 1-5
    public ParticleSystem[] tierAuras;     // 5 eleman

    /// <summary>Verilen tier icin guveli temel hasar degerini dondurur.</summary>
    public float GetBaseDMG(int tier)
        => baseDMG[Mathf.Clamp(tier - 1, 0, 4)];

    /// <summary>Verilen tier icin temel atis hizini dondurur.</summary>
    public float GetBaseFireRate(int tier)
        => baseFireRate[Mathf.Clamp(tier - 1, 0, 4)];

    /// <summary>Verilen tier icin temel HP'yi dondurur.</summary>
    public int GetBaseHP(int tier)
        => baseHP[Mathf.Clamp(tier - 1, 0, 4)];
}

public enum CommanderSpecialty
{
    Assault,    // Dengeli — baslangic komutani
    Sniper,     // Yuksek hasar, yavash atis
    Support,    // Yuksek HP, dusuk hasar, ordu guclendirir
    Swarm,      // Cok mermi, dusuk tek mermi hasari
}

public enum ArmySynergy
{
    Piyade,
    Mekanik,
    Teknoloji,
    Hybrid,
}