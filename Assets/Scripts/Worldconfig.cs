using UnityEngine;

[CreateAssetMenu(fileName = "World_", menuName = "TopEndWar/WorldConfig")]
public class WorldConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int worldID = 1;
    public string worldName = "Frontier One";

    [Header("Biyom")]
    [Tooltip("Global, kurgusal veya evrensel biome etiketi kullan.")]
    public string biome = "Temperate Frontier";

    [Header("Stage Yapisi")]
    public int stageCount = 35;

    [Header("Rarity Esigi")]
    [Range(1, 5)]
    public int maxRarity = 2;

    [Header("Komutan Kilidi")]
    public CommanderData unlockedCommander;

    [Header("Offline Kazanc")]
    public int offlineIncomeBoost = 0;

    [Header("Gorunumler")]
    public Color mapColor = Color.green;

#if UNITY_EDITOR
    void OnValidate()
    {
        worldID = Mathf.Max(1, worldID);
        stageCount = Mathf.Max(1, stageCount);
        maxRarity = Mathf.Clamp(maxRarity, 1, 5);
        offlineIncomeBoost = Mathf.Max(0, offlineIncomeBoost);

        if (!string.IsNullOrEmpty(worldName))
            name = $"World_{worldID}_{worldName}";
    }
#endif
}