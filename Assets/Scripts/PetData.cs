using UnityEngine;

[CreateAssetMenu(fileName = "NewPet", menuName = "TopEndWar/Pet")]
public class PetData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string petName;
    public GameObject petPrefab; // Oyunda karakterin arkasından koşacak 3D model
    public Sprite icon;

    [Header("Anchor & Combat Bonusları")]
    public int cpBonus;
    public float anchorDamageReduction = 0.1f; // Anchor modunda iken ekstra %10 hasar emme
}