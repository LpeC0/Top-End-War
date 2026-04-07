using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Envanter Yoneticisi v1 (Claude)
///
/// SLOT LEVELING (Senin Kararin):
///   Oyuncu "silah"i degil "silah slotunu" gellistirir.
///   Yeni silah takinca slot seviyesi SIFIRLANMAZ.
///   SlotLevelMult = 1 + azalan_verim_formulü (PlayerStats.GetSlotLevelMult)
///
/// MERGE (Birlestime):
///   itemID ile karsilastirilir — string itemName KULLANILMAZ (localization sonrasi patlar).
///   3x ayni itemID + ayni rarity → 1x (rarity + 1) item.
///
/// SLOT YÜKSELTME:
///   TryUpgradeSlot(slot) → EconomyManager.TryUpgradeSlot() cagirir.
///   Basarili ise PlayerStats'i günceller.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ── Slot Seviyeleri ───────────────────────────────────────────────────
    // PlayerStats zaten slot level tutuyor (weaponSlotLevel vb.)
    // InventoryManager bu degerleri okur/yazar.

    // ── Sahip Olunan Esyalar ─────────────────────────────────────────────
    // ItemID bazli liste. Her esyanin benzersiz bir int ID'si var.
    // EquipmentData.itemID alani olacak (su an rarity kullaniliyor, ileride genisletilecek).
    [Header("Sahip Olunan Esyalar (Runtime)")]
    public List<EquipmentData> ownedItems = new List<EquipmentData>(50);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Esya Ekle ─────────────────────────────────────────────────────────
    public void AddItem(EquipmentData item)
    {
        if (item == null) return;
        ownedItems.Add(item);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] +{item.equipmentName} (rarity {item.rarity})");
    }

    // ── Slot Yükselt ─────────────────────────────────────────────────────
    /// <summary>
    /// Verilen slot icin seviye atlamayı dener.
    /// EconomyManager.TryUpgradeSlot() Gold ve TechCore dusurur.
    /// Basarili ise PlayerStats'taki slot levelini 1 arttirir.
    /// </summary>
    public bool TryUpgradeWeaponSlot()
    {
        int cur = PlayerStats.Instance != null ? PlayerStats.Instance.weaponSlotLevel : 1;
        if (!EconomyManager.Instance.TryUpgradeSlot(cur, out string fail))
        {
            Debug.Log($"[Inventory] Slot upgrade basarisiz: {fail}");
            return false;
        }
        if (PlayerStats.Instance != null) PlayerStats.Instance.weaponSlotLevel++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryUpgradeArmorSlot()
    {
        int cur = PlayerStats.Instance != null ? PlayerStats.Instance.armorSlotLevel : 1;
        if (!EconomyManager.Instance.TryUpgradeSlot(cur, out string fail))
        {
            Debug.Log($"[Inventory] Armor slot upgrade basarisiz: {fail}");
            return false;
        }
        if (PlayerStats.Instance != null) PlayerStats.Instance.armorSlotLevel++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── Merge (Birlestime) ────────────────────────────────────────────────
    /// <summary>
    /// ownedItems listesinde verilen esyanin tipinde (ayni weaponType/armorType + rarity)
    /// 3 kopya varsa bilestirir: 3x Lv R → 1x Lv (R+1).
    /// Basarili ise true dondurur.
    ///
    /// NOT: itemName STRING ile degil, weaponType + armorType + rarity ile karsilastirilir.
    /// </summary>
    public bool TryMergeItem(EquipmentData targetItem)
    {
        if (targetItem == null) return false;
        if (targetItem.rarity >= 5) { Debug.Log("[Inventory] Maksimum rarity, merge yapilamaz."); return false; }

        var duplicates = FindDuplicates(targetItem, 3);
        if (duplicates.Count < 3)
        {
            Debug.Log($"[Inventory] Merge icin 3 kopya gerekli, bulunan: {duplicates.Count}");
            return false;
        }

        // 3 eskiyi kaldir
        for (int i = 0; i < 3; i++) ownedItems.Remove(duplicates[i]);

        // Yeni (rarity+1) esyayi bul veya klonla
        EquipmentData upgraded = FindUpgradedVersion(targetItem);
        if (upgraded != null)
        {
            ownedItems.Add(upgraded);
            Debug.Log($"[Inventory] MERGE: {targetItem.equipmentName} R{targetItem.rarity} x3 → R{upgraded.rarity}");
        }
        else
        {
            Debug.LogWarning($"[Inventory] Merge: R{targetItem.rarity + 1} versiyonu bulunamadi.");
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Ayni weapon/armor tipi ve rarity'de kopya esyalari dondurur.
    /// String degil enum/int karsilastirmasi.
    /// </summary>
    List<EquipmentData> FindDuplicates(EquipmentData target, int maxCount)
    {
        var result = new List<EquipmentData>(maxCount);
        foreach (var item in ownedItems)
        {
            if (result.Count >= maxCount) break;
            if (item == null) continue;
            if (item.rarity    != target.rarity)    continue;
            if (item.slot      != target.slot)      continue;
            if (item.weaponType != target.weaponType) continue;
            if (item.armorType  != target.armorType)  continue;
            result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Ayni tipe sahip 1 rarity yukari versiyonu ownedItems veya
    /// Resources klasöründen arar.
    /// Yoksa mevcut esyanin kopyasini olusturup rarity arttirir (fallback).
    /// </summary>
    EquipmentData FindUpgradedVersion(EquipmentData source)
    {
        int targetRarity = source.rarity + 1;

        // Once mevcut listede ara
        foreach (var item in ownedItems)
        {
            if (item == null) continue;
            if (item.rarity     == targetRarity &&
                item.slot       == source.slot &&
                item.weaponType == source.weaponType &&
                item.armorType  == source.armorType)
                return item;
        }

        // Fallback: mevcut SO'yu kopyala, rarity artir
        // (Gercek projede Database'den cektirilmeli)
        var clone = Instantiate(source);
        clone.rarity = targetRarity;
        clone.equipmentName = $"{source.equipmentName} +{targetRarity}";
        return clone;
    }

    // ── Esya Kus ─────────────────────────────────────────────────────────
    public void EquipItem(EquipmentData item)
    {
        if (item == null || PlayerStats.Instance == null) return;

        switch (item.slot)
        {
            case EquipmentSlot.Weapon:   PlayerStats.Instance.equippedWeapon   = item; break;
            case EquipmentSlot.Armor:    PlayerStats.Instance.equippedArmor    = item; break;
            case EquipmentSlot.Shoulder: PlayerStats.Instance.equippedShoulder = item; break;
            case EquipmentSlot.Knee:     PlayerStats.Instance.equippedKnee     = item; break;
            case EquipmentSlot.Necklace: PlayerStats.Instance.equippedNecklace = item; break;
            case EquipmentSlot.Ring:     PlayerStats.Instance.equippedRing     = item; break;
        }
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] Kusanildi: {item.equipmentName} ({item.slot})");
    }

    // ── Slot Carpan Bilgisi (UI icin) ─────────────────────────────────────
    public float GetWeaponSlotMult()
    {
        int lv = PlayerStats.Instance != null ? PlayerStats.Instance.weaponSlotLevel : 1;
        return PlayerStats.GetSlotLevelMult(lv);
    }

    public float GetArmorSlotMult()
    {
        int lv = PlayerStats.Instance != null ? PlayerStats.Instance.armorSlotLevel : 1;
        return PlayerStats.GetSlotLevelMult(lv);
    }

    // ── Olaylar ───────────────────────────────────────────────────────────
    public static System.Action OnInventoryChanged;
}