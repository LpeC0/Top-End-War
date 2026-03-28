using UnityEngine;

/// <summary>
/// Top End War — Ekipman Seti ScriptableObject (Claude)
///
/// 6 slotun tamamını tek bir .asset dosyasında tutar.
/// Ana menü ve save sistemi için ideal: 1 referans = tüm ekipman.
///
/// KULLANIM:
///   Assets → Create → TopEndWar → Equipment Loadout
///   PlayerStats Inspector'da equippedLoadout alanına sürükle.
///   Oyun içinde EquipmentUI değişiklikleri hem tek slota hem de
///   Loadout'a yazar (SaveManager bunu JSON'a kaydeder).
///
/// GELECEK: SaveManager bu SO'yu JSON'a serialize edecek.
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

    /// <summary>Bu loadout'u PlayerStats'a uygular.</summary>
    public void ApplyTo(PlayerStats ps)
    {
        if (ps == null) return;
        ps.equippedWeapon   = weapon;
        ps.equippedArmor    = armor;
        ps.equippedShoulder = shoulder;
        ps.equippedKnee     = knee;
        ps.equippedNecklace = necklace;
        ps.equippedRing     = ring;
        ps.equippedPet      = pet;
    }

    /// <summary>PlayerStats'taki mevcut ekipmanı bu loadout'a kaydeder.</summary>
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

    /// <summary>Toplam CP bonusunu hesaplar (UI önizleme için).</summary>
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
        return total;
    }
}