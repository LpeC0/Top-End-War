using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/ProgressionConfig")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Zorluk Egrisi")]
    [Range(0.8f, 2.0f)]
    public float difficultyExponent = 1.0f;

    public float distanceScale = 1000f;

    [Header("Oyuncu Gucu Uyumu")]
    [Range(0f, 1f)]
    public float playerCPScalingFactor = 0f;

    [Range(0.5f, 1f)]
    public float minPowerAdjust = 1f;

    [Range(1f, 2f)]
    public float maxPowerAdjust = 1f;

    [Header("Beklenen CP (Legacy / opsiyonel)")]
    public float expectedCPGrowthPerKm = 150f;

#if UNITY_EDITOR
    void OnValidate()
    {
        difficultyExponent = Mathf.Max(0.8f, difficultyExponent);
        distanceScale = Mathf.Max(1f, distanceScale);

        // Vertical slice: fixed difficulty varsayilan
        playerCPScalingFactor = Mathf.Clamp01(playerCPScalingFactor);

        if (playerCPScalingFactor <= 0f)
        {
            minPowerAdjust = 1f;
            maxPowerAdjust = 1f;
        }
        else
        {
            minPowerAdjust = Mathf.Clamp(minPowerAdjust, 0.5f, 1f);
            maxPowerAdjust = Mathf.Clamp(maxPowerAdjust, 1f, 2f);
        }

        expectedCPGrowthPerKm = Mathf.Max(0f, expectedCPGrowthPerKm);
    }
#endif
}