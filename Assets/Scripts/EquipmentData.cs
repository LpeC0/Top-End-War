using UnityEngine;

/// <summary>
/// Top End War — Ekipman Verisi v3 (Claude)
///
/// v3 degisiklikleri:
///   + globalDmgMultiplier eklendi (Yuzuk/Kolye icin DPS carpani)
///   - cpMultiplier artik SADECE CP Gear Score icin kullanilir, DPS hesabinda YOK
///   ArmorType enum yorumlari duzeltildi (enum sadece kategori, gercek deger fieldlarda)
///
/// KURULUM:
///   Assets > Create > TopEndWar > Equipment
///   Slot sec, degerleri doldur, PlayerStats'e surukle.
/// </summary>

public enum EquipmentSlot
{
    Weapon,      // Silah    — atisHizi + hasar
    Armor,       // Zirh     — HP + hasarAzaltma
    Shoulder,    // Omuzluk  — CP bonus + kucuk DR
    Knee,        // Dizlik   — hafif HP bonus
    Boots,       // Ayakkabi — hareket bonusu (gelecek)
    Necklace,    // Kolye    — CP carpani + globalDmg
    Ring,        // Yuzuk    — globalDmgMultiplier
}

public enum WeaponType
{
    None,
    Pistol,      // Tabanca:    atisHizi x1.5, hasar x0.7
    Rifle,       // Tufek:      atisHizi x1.0, hasar x1.0  (standart)
    Automatic,   // Otomatik:   atisHizi x2.2, hasar x0.6
    Sniper,      // Nishanci:   atisHizi x0.35, hasar x3.5
    Shotgun,     // Pompa:      atisHizi x0.5,  hasar x2.0
}

public enum ArmorType
{
    None,
    Light,       // Genelde dusuk DR, dusuk HP bonusu
    Medium,      // Dengeli
    Heavy,       // Yuksek HP bonusu, orta DR
    Shield,      // En yuksek DR odakli
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string        equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot          = EquipmentSlot.Weapon;
    public Sprite        icon;
    [TextArea(2, 4)]
    public string        description   = "";

    // ── Tur ───────────────────────────────────────────────────────────────
    [Header("Silah Turu (sadece Weapon slot)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Turu (sadece Armor/Shoulder/Knee)")]
    public ArmorType armorType = ArmorType.None;

    // ── CP Gear Score (Meta-Hub gostergesi) ───────────────────────────────
    [Header("CP Gear Score Bonusu")]
    [Tooltip("Kusanilinca CP Gear Score'una duz eklenir. DPS ile ilgisi yoktur.")]
    public int baseCPBonus = 0;

    /// <summary>
    /// CP carpani — SADECE Gear Score icin. DPS hesabinda KULLANILMAZ.
    /// DPS icin globalDmgMultiplier kullan.
    /// </summary>
    [Header("CP Carpani (kolye/yuzuk — Gear Score icin)")]
    [Tooltip("1.0 = etki yok. DPS etkilemez, sadece CP puanini carpar.")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    // ── Silah Statistikleri ────────────────────────────────────────────────
    [Header("Atis Hizi Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 2.2 = %120 daha hizli")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 3.5 = keskin nisanci")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    // ── Global DPS Carpani (Yuzuk / Kolye) ───────────────────────────────
    [Header("Global Hasar Carpani (yuzuk/kolye — DPS'e etki eder)")]
    [Tooltip(
        "1.0 = etki yok. Bu alan DPS formulundeki GlobalMult'tur.\n" +
        "cpMultiplier'dan FARKLIDIR — o sadece Gear Score icindir.\n" +
        "Ornekler: Yuzuk 1.1 = DPS %10 artar. Necklace 1.05 = DPS %5 artar.")]
    [Range(1f, 2f)]
    public float globalDmgMultiplier = 1f;

    // ── Zirh / Savunma ─────────────────────────────────────────────────────
    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Tooltip("0.0-0.5. Toplam max %60 (PlayerStats.TotalDamageReduction() ile sinirli)")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    [Tooltip("Maks HP'ye duz eklenir")]
    public int commanderHPBonus = 0;

    // ── Diger ────────────────────────────────────────────────────────────
    [Header("Mermi Spread Bonusu (sadece silahlar)")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    [Header("Nadir (rarity) 1=Gri 2=Yesil 3=Mavi 4=Mor 5=Altin")]
    [Range(1, 5)]
    public int rarity = 1;

    // ── Yardimci ─────────────────────────────────────────────────────────
    /// <summary>Silah/zirh turune gore kisa aciklama dondurur (Inspector icin).</summary>
    public string GetTypeDescription()
    {
        return weaponType switch
        {
            WeaponType.Pistol    => "Tabanca: Hizli, kisa menzilli",
            WeaponType.Rifle     => "Tufek: Dengeli, cok yonlu",
            WeaponType.Automatic => "Otomatik: Yuksek DPS, genis spread",
            WeaponType.Sniper    => "Keskin Nisanci: Dev hasar, yavash",
            WeaponType.Shotgun   => "Pompa: Yakin mesafe katili",
            _ => armorType switch
            {
                ArmorType.Light  => "Hafif Zirh: Dusuk DR, dusuk HP",
                ArmorType.Medium => "Orta Zirh: Dengeli savunma",
                ArmorType.Heavy  => "Agir Zirh: Yuksek HP, orta DR",
                ArmorType.Shield => "Kalkan: En yuksek DR",
                _                => "Aksesuar",
            }
        };
    }
}