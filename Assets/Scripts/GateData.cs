using UnityEngine;

/// <summary>
/// Top End War — Kapi Etki Tipleri
/// RiskReward (YENI): CP -30% ANCAK sonraki 3 kapiya +50% bonus uygulanir.
/// Oyuncu "risk mi alayim?" diye dusunur — oyun derinligi artar.
/// </summary>
public enum GateEffectType
{
    AddCP,               // +deger  — guvenlii
    MultiplyCP,          // xdeger  — risk/odul
    Merge,               // x1.8    — nadir, guclu
    PathBoost_Piyade,    // Strateji yolu
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
    NegativeCP,          // -deger  — saf ceza (az cikacak, %2-3)
    RiskReward           // -30% CP + sonraki 3 kapiya +50% bonus (Claude)
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")]
    public string gateText  = "+60";
    public Color  gateColor = new Color(0f, 0.7f, 1f, 0.6f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 60f;

    // ── Hazir Renkler (Inspector icin referans) ──────────────────────────────
    // Yesil  = AddCP       new Color(0.2f, 0.8f, 0.2f, 0.65f)
    // Mavi   = MultiplyCP  new Color(0.1f, 0.4f, 1.0f, 0.65f)
    // Mor    = Merge       new Color(0.6f, 0.1f, 0.9f, 0.65f)
    // Turuncu= PathBoost   new Color(1.0f, 0.5f, 0.0f, 0.65f)
    // Kirmizi= Negative    new Color(0.9f, 0.1f, 0.1f, 0.65f)
    // Sari   = RiskReward  new Color(1.0f, 0.8f, 0.0f, 0.65f)
}