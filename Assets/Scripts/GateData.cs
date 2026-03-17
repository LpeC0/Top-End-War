using UnityEngine;

/// <summary>
/// Top End War — Kapi Verisi
/// Namespace YOK — Unity'de en kolay kullanim.
/// </summary>
public enum GateEffectType
{
    AddCP,
    MultiplyCP,
    Merge,
    PathBoost_Piyade,
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
    NegativeCP,
    RiskReward   // -30% CP + sonraki 3 kapiya +50% bonus
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")]
    public string gateText  = "+60";
    public Color  gateColor = new Color(0.2f, 0.85f, 0.2f, 0.7f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 60f;
    // Renk rehberi (Inspector'da referans):
    // Yesil  = AddCP        new Color(0.2f, 0.85f, 0.2f, 0.7f)
    // Mavi   = MultiplyCP   new Color(0.1f, 0.4f,  1.0f, 0.7f)
    // Mor    = Merge        new Color(0.6f, 0.1f,  0.9f, 0.7f)
    // Turuncu= PathBoost    new Color(1.0f, 0.5f,  0.0f, 0.7f)
    // Kirmizi= NegativeCP   new Color(0.9f, 0.1f,  0.1f, 0.7f)
    // Sari   = RiskReward   new Color(1.0f, 0.85f, 0.0f, 0.7f)
}
