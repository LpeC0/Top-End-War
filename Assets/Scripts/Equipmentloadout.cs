using UnityEngine;

/// <summary>
/// Top End War — Ekipman Seti ScriptableObject v2
///
/// DEĞİŞİKLİK:
///   - Yanlis slot item'lari runtime'da equip edilmez
///   - Inspector'da OnValidate ile temizlenir
///   - TotalCPBonus, PlayerStats.CP mantigina daha yakin hesaplanir
/// </summary>
[CreateAssetMenu(fileName = "NewLoadout", menuName = "TopEndWar/Equipment Loadout")]
public class EquipmentLoadout : ScriptableObject
{
    [Header("Silah")]
    public EquipmentData weapon;

    [Header("Zırh")]
    public EquipmentData armor;

    [Header("Aksesuarlar")]
    public EquipmentData shoulder;
    public EquipmentData knee;
    public EquipmentData necklace;
    public EquipmentData ring;

    [Header("Pet")]
    public PetData pet;

    public void ApplyTo(PlayerStats ps)
    {
        if (ps == null) return;

        ps.equippedWeapon   = ValidateForSlot(weapon,   EquipmentSlot.Weapon,   "weapon");
        ps.equippedArmor    = ValidateForSlot(armor,    EquipmentSlot.Armor,    "armor");
        ps.equippedShoulder = ValidateForSlot(shoulder, EquipmentSlot.Shoulder, "shoulder");
        ps.equippedKnee     = ValidateForSlot(knee,     EquipmentSlot.Knee,     "knee");
        ps.equippedNecklace = ValidateForSlot(necklace, EquipmentSlot.Necklace, "necklace");
        ps.equippedRing     = ValidateForSlot(ring,     EquipmentSlot.Ring,     "ring");
        ps.equippedPet      = pet;
        ps.RefreshWeaponDerivedStats();
    }

    public void ReadFrom(PlayerStats ps)
    {
        if (ps == null) return;
        weapon   = ps.equippedWeapon;
        armor    = ps.equippedArmor;
        shoulder = ps.equippedShoulder;
        knee     = ps.equippedKnee;
        necklace = ps.equippedNecklace;
        ring     = ps.equippedRing;
        pet      = ps.equippedPet;
    }

    /// <summary>
    /// UI preview icin PlayerStats.CP mantigina yakin hesap.
    /// </summary>
    public int TotalCPBonus()
    {
        int total = 0;
        total += weapon   != null ? weapon.baseCPBonus   : 0;
        total += armor    != null ? armor.baseCPBonus    : 0;
        total += shoulder != null ? shoulder.baseCPBonus : 0;
        total += knee     != null ? knee.baseCPBonus     : 0;
        total += necklace != null ? necklace.baseCPBonus : 0;
        total += ring     != null ? ring.baseCPBonus     : 0;
        total += pet      != null ? pet.cpBonus          : 0;

        float mult = 1f;
        if (necklace != null) mult *= necklace.cpMultiplier;
        if (ring != null)     mult *= ring.cpMultiplier;

        return Mathf.RoundToInt(total * mult);
    }

    EquipmentData ValidateForSlot(EquipmentData item, EquipmentSlot expected, string label)
    {
        if (item == null) return null;

        if (item.slot == expected)
            return item;

        Debug.LogWarning(
            $"[EquipmentLoadout] {label} alaninda yanlis item var. " +
            $"Beklenen={expected}, Gelen={item.slot}, Item={item.equipmentName}");

        return null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        weapon   = Sanitize(weapon,   EquipmentSlot.Weapon);
        armor    = Sanitize(armor,    EquipmentSlot.Armor);
        shoulder = Sanitize(shoulder, EquipmentSlot.Shoulder);
        knee     = Sanitize(knee,     EquipmentSlot.Knee);
        necklace = Sanitize(necklace, EquipmentSlot.Necklace);
        ring     = Sanitize(ring,     EquipmentSlot.Ring);
    }

    EquipmentData Sanitize(EquipmentData item, EquipmentSlot expected)
    {
        if (item == null) return null;
        return item.slot == expected ? item : null;
    }
#endif
}
