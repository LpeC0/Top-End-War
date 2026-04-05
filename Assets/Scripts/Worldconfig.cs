using UnityEngine;

/// <summary>
/// Top End War — Dunya Konfigurasyonu v1 (Claude)
///
/// Her dunya icin ayri bir SO olustur:
///   Assets > Create > TopEndWar > WorldConfig
///
/// WorldConfig nedir?
///   Bir dunya (ornegin Sivas, Tokat) kac stage'den olusuyor,
///   hangi biyom, hangi rarity esigi ve hangi komutan aciliyor.
///
/// StageManager bu SO'yu okur, BiomeManager biyomu buradan alir.
/// </summary>
[CreateAssetMenu(fileName = "World_", menuName = "TopEndWar/WorldConfig")]
public class WorldConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID   = 1;
    public string worldName = "Sivas";

    [Header("Biyom")]
    [Tooltip("BiomeManager'in taniydigi biyom adi: Tas, Orman, Col, Karli, Tarim")]
    public string biome     = "Tas";

    [Header("Stage Yapisi")]
    [Tooltip("Bu dunyada kac stage var (ornegin 15)")]
    public int stageCount   = 15;

    [Header("Rarity Esigi")]
    [Tooltip(
        "Bu dunyada drop edilebilecek max rarity.\n" +
        "Dunya 1 = 2 (Yesil), Dunya 3 = 4 (Mor), Dunya 5+ = 5 (Altin)")]
    [Range(1, 5)]
    public int maxRarity    = 2;

    [Header("Komutan Kilidi")]
    [Tooltip("Bu dunya bittikten sonra acilan CommanderData SO. Bos = komutan acilmaz.")]
    public CommanderData unlockedCommander;

    [Header("Offline Kazanc")]
    [Tooltip("Bu dunya temizlenince saatlik altina eklenen miktar")]
    public int offlineIncomeBoost = 30;

    [Header("Gorunumler")]
    [Tooltip("Haritada bu dunya icin gosterilecek ikon veya renk (gelecek)")]
    public Color mapColor   = Color.green;
}