using UnityEngine;

/// <summary>
/// Top End War — Ekipman Verisi v4
///
/// NOTLAR:
///   - Rifle = Assault rolü
///   - Automatic = SMG rolü
///   - Sniper = Sniper rolü
///   - Boots su an loadout / PlayerStats tarafinda aktif degil
///   - ArmorType.Shield AYRI shield katmani degil; sadece agir savunma etiketi
/// </summary>

public enum EquipmentSlot
{
    Weapon,      // Silah
    Armor,       // Zirh
    Shoulder,    // Omuzluk
    Knee,        // Dizlik
    Boots,       // Su an aktif DEGIL — loadout/player tarafinda slotu yok
    Necklace,    // Kolye
    Ring,        // Yuzuk
}

public enum WeaponType
{
    None,
    Pistol,      // Slice disi / opsiyonel
    Rifle,       // Assault rolu
    Automatic,   // SMG rolu
    Sniper,      // Sniper rolu
    Shotgun,     // Slice disi
}

public enum ArmorType
{
    None,
    Light,
    Medium,
    Heavy,
    Shield,      // AYRI shield bar degil; sadece savunma etiketi
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Kimlik")]
    public string equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot = EquipmentSlot.Weapon;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description = "";

    [Header("Silah Turu (sadece Weapon slot)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Turu (sadece Armor/Shoulder/Knee)")]
    public ArmorType armorType = ArmorType.None;

    [Header("CP Gear Score Bonusu")]
    public int baseCPBonus = 0;

    [Header("CP Carpani (kolye/yuzuk — Gear Score icin)")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    [Header("Atis Hizi Carpani (sadece silahlar)")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silahlar)")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    [Header("Global Hasar Carpani (yuzuk/kolye — DPS'e etki eder)")]
    [Range(1f, 2f)]
    public float globalDmgMultiplier = 1f;

    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    public int commanderHPBonus = 0;

    [Header("Mermi Spread Bonusu (sadece silahlar)")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    // DEĞİŞİKLİK
    [Header("Vertical Slice Combat (sadece silahlar)")]
    [Min(0)] public int armorPen = 0;
    [Min(0)] public int pierceCount = 0;
    [Range(1f, 3f)] public float eliteDamageMultiplier = 1f;

    [Header("Nadir (rarity) 1=Gri 2=Yesil 3=Mavi 4=Mor 5=Altin")]
    [Range(1, 5)]
    public int rarity = 1;

    public string GetTypeDescription()
    {
        return weaponType switch
        {
            WeaponType.Pistol    => "Tabanca: Hizli, kisa menzilli",
            WeaponType.Rifle     => "Assault: Dengeli, cok yonlu",
            WeaponType.Automatic => "SMG: Hizli, swarm odakli",
            WeaponType.Sniper    => "Sniper: Tek hedef, armor odakli",
            WeaponType.Shotgun   => "Pompa: Yakin mesafe",
            _ => armorType switch
            {
                ArmorType.Light  => "Hafif Zirh",
                ArmorType.Medium => "Orta Zirh",
                ArmorType.Heavy  => "Agir Zirh",
                ArmorType.Shield => "Savunma odakli zirh etiketi",
                _                => "Aksesuar",
            }
        };
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        bool isWeapon = slot == EquipmentSlot.Weapon;
        bool isArmorLike = slot == EquipmentSlot.Armor ||
                           slot == EquipmentSlot.Shoulder ||
                           slot == EquipmentSlot.Knee;

        bool isNecklaceOrRing = slot == EquipmentSlot.Necklace || slot == EquipmentSlot.Ring;

        if (!isWeapon)
        {
            weaponType = WeaponType.None;
            fireRateMultiplier = 1f;
            damageMultiplier = 1f;
            spreadBonus = 0f;
            armorPen = 0;
            pierceCount = 0;
            eliteDamageMultiplier = 1f;
        }

        if (!isArmorLike)
            armorType = ArmorType.None;

        if (!isNecklaceOrRing)
        {
            cpMultiplier = 1f;
            globalDmgMultiplier = 1f;
        }

        if (eliteDamageMultiplier < 1f)
            eliteDamageMultiplier = 1f;

        if (pierceCount < 0)
            pierceCount = 0;

        if (armorPen < 0)
            armorPen = 0;
    }
#endif
}