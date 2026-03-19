using UnityEngine;

/// <summary>
/// Top End War — Kapi Verisi v3
/// v3: AddSoldier (Piyade/Mekanik/Teknoloji) + HealCommander + HealSoldiers eklendi.
/// Eski degerler (0-8) korundu — sahne .asset dosyalari bozulmaz.
/// </summary>
public enum GateEffectType
{
    // ── Mevcut (v1-v2) ─ degerler degismedi ──────────────────────────────
    AddCP             = 0,
    MultiplyCP        = 1,
    AddBullet         = 2,   // Eski isim korundu, AddSoldier_Piyade gibi davranir
    Merge             = 3,
    PathBoost_Piyade  = 4,   // Eski — CP + path skoru verir (hala calisir)
    PathBoost_Mekanize= 5,
    PathBoost_Teknoloji=6,
    NegativeCP        = 7,
    RiskReward        = 8,

    // ── v3 Yeni ───────────────────────────────────────────────────────────
    AddSoldier_Piyade    = 9,   // +1 Piyade Lv1 asker + kucuk CP
    AddSoldier_Mekanik   = 10,  // +1 Mekanik Lv1 asker + kucuk CP
    AddSoldier_Teknoloji = 11,  // +1 Teknoloji Lv1 asker + kucuk CP
    HealCommander        = 12,  // Komutan HP +300 (effectValue ile ayarlanabilir)
    HealSoldiers         = 13,  // Tum askerler %50 HP geri kazanir (effectValue=yuzde)
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")]
    public string gateText  = "+80";
    public Color  gateColor = new Color(0.2f, 0.85f, 0.2f, 0.7f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    [Tooltip("AddCP: miktar | MultiplyCP: carpan | AddSoldier: CP bonus | HealCommander: HP miktar | HealSoldiers: yuzdesi (0-1)")]
    public float effectValue = 80f;

    [Header("Spawn Agirligi (SpawnManager icin)")]
    [Range(0f, 1f)]
    public float spawnWeight = 0.12f;
}