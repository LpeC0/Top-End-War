using UnityEngine;

public enum GateEffectType
{
    AddCP,
    MultiplyCP,
    Merge,
    PathBoost_Piyade,
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
    NegativeCP
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Görsel")]
    public string gateText  = "+60";
    public Color  gateColor = new Color(0f, 0.7f, 1f, 0.6f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 60f;
}
