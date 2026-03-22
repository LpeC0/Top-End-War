using UnityEngine;

public enum EquipmentType { Weapon, Armor, Accessory }

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string equipmentName;
    public EquipmentType type;
    public Sprite icon; // UI'da göstermek için

    [Header("İstatistikler")]
    public int baseCPBonus; // Karakterin CP'sine eklenecek düz değer
    public float fireRateMultiplier = 1f; // Sadece silahlara özel atış hızı çarpanı
}