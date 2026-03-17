using UnityEngine;

/// <summary>
/// Top End War — Kapi Verisi v2
///
/// DEGİSİKLİK: MultiplyCP x2 kaldirildi (denge bozuyordu).
/// YENİ: AddBullet — +1 mermi + kucuk CP.
/// MultiplyCP kaldi ama x1.3 max ve nadir (agirlik 0.05).
/// </summary>
public enum GateEffectType
{
    AddCP,
    MultiplyCP,             // Artik x1.3 max, agirlik 0.05
    AddBullet,              // +1 mermi + CP bonus
    Merge,
    PathBoost_Piyade,
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
    NegativeCP,
    RiskReward
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")]
    public string gateText  = "+80";
    public Color  gateColor = new Color(0.2f, 0.85f, 0.2f, 0.7f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 80f;

    [Header("Spawn Agirligi")]
    [Range(0f, 1f)]
    public float spawnWeight = 0.12f;
}