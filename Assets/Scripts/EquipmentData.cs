using UnityEngine;

/// <summary>
/// Top End War — Ekipman Verisi v2 (Claude)
///
/// SILAH TÜRLERİ VE GERÇEKÇİ ÖZELLİKLER:
///
///   Tabanca     — Hızlı atış, kısa menzil, hafif    
///   Tüfek       — Dengeli, orta menzil, tek atış
///   Otomatik    — Yüksek DPS, kısa/orta menzil, düşük isabet
///   Keskin nişancı — Yüksek hasar, uzun menzil, çok yavaş atış
///   Pompalı     — Çok hasar yakın, düşük menzil, yavaş
///
/// ZIRH TÜRLERİ:
///   Hafif zırh  — Az koruma, yüksek mobilite (gelecek: hız bonusu)
///   Orta zırh   — Dengeli
///   Ağır zırh   — Yüksek koruma, yavaş (gelecek: hız cezası)
///   Kalkan      — Hasar azaltma bonusu
///
/// AKSESUAR (kolye, yüzük, omuzluk, vb.):
///   Çeşitli bonuslar — CP, ateş hızı, hasar, çoğaltıcı vb.
///
/// KURULUM:
///   Assets → Create → TopEndWar → Equipment
///   Tipi seç, değerleri doldur → PlayerStats'e sürükle.
/// </summary>

public enum EquipmentSlot
{
    Weapon,         // Silah — ateş hızı + hasar
    Armor,          // Zırh — HP + hasar azaltma
    Shoulder,       // Omuzluk — CP bonus + küçük hasar
    Knee,           // Dizlik — hafif HP + hareket
    Boots,          // Ayakkabı — hareket bonusu (gelecek)
    Necklace,       // Kolye — CP çarpanı
    Ring,           // Yüzük — genel buff
}

public enum WeaponType
{
    None,           // Silah değil
    Pistol,         // Tabanca: atış/s ×1.5, hasar ×0.7, spread dar
    Rifle,          // Tüfek: atış/s ×1.0 (base), hasar ×1.0
    Automatic,      // Otomatik: atış/s ×2.2, hasar ×0.6, spread geniş
    Sniper,         // Keskin: atış/s ×0.35, hasar ×3.5, tek mermi
    Shotgun,        // Pompalı: atış/s ×0.5, hasar ×2.0, spread çok geniş yakında
}

public enum ArmorType
{
    None,
    Light,          // Hafif: HP +%20, hasar azaltma +%5
    Medium,         // Orta: HP +%40, hasar azaltma +%12
    Heavy,          // Ağır: HP +%70, hasar azaltma +%22
    Shield,         // Kalkan: HP +%30, hasar azaltma +%30 (en iyi DR)
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Kimlik")]
    public string        equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot          = EquipmentSlot.Weapon;
    public Sprite        icon;
    [TextArea(2,4)]
    public string        description   = "";

    [Header("Silah Ayarlari (slot=Weapon ise doldur)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Ayarlari (slot=Armor/Shoulder/Knee ise)")]
    public ArmorType armorType = ArmorType.None;

    // ── Temel Bonuslar ───────────────────────────────────────────────────
    [Header("CP Bonusu")]
    [Tooltip("Kuşanılınca CP'ye düz eklenir")]
    public int baseCPBonus = 0;

    [Header("Ates Hizi Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 1.5 = %50 hızlı, 0.5 = %50 yavaş")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 1.5 = %50 daha fazla hasar")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Tooltip("0.0 - 0.5 arası. 0.2 = düşman hasarı %20 azalır")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    [Tooltip("Maks HP'ye eklenen değer")]
    public int commanderHPBonus = 0;

    [Header("CP Carpani (kolye/yuzuk)")]
    [Tooltip("1.0 = etki yok, 1.1 = CP %10 daha fazla")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    [Header("Mermi Spread Bonusu (sadece silahlar)")]
    [Tooltip("Ek mermi yayılma açısı: 0 = yok, 10 = +10 derece")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    // ── Hesaplanmış özellikler (oyun içinde salt okunur) ─────────────────
    [Header("Nadir (rarity) 1=Common 2=Uncommon 3=Rare 4=Epic 5=Legendary)")]
    [Range(1,5)]
    public int rarity = 1;

    /// <summary>Silah tipine göre gerçekçi önerilen değerler için açıklama döndürür.</summary>
    public string GetTypeDescription()
    {
        return weaponType switch
        {
            WeaponType.Pistol    => "Tabanca: Hızlı, kısa menzilli",
            WeaponType.Rifle     => "Tüfek: Dengeli, çok yönlü",
            WeaponType.Automatic => "Otomatik: Yüksek DPS, geniş spread",
            WeaponType.Sniper    => "Keskin Nişancı: Dev hasar, yavaş",
            WeaponType.Shotgun   => "Pompalı: Yakın mesafe katili",
            _ => armorType switch
            {
                ArmorType.Light  => "Hafif Zırh: Hızlı ama az korumalı",
                ArmorType.Medium => "Orta Zırh: Dengeli savunma",
                ArmorType.Heavy  => "Ağır Zırh: Max korumalı",
                ArmorType.Shield => "Kalkan: Hasar azaltmada uzman",
                _ => "Aksesuar: Özel bonus"
            }
        };
    }
}