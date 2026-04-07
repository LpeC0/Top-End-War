using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Kapi Konfigurasyonu v2 (Claude)
///
/// Yeni gate sistemi. Eski GateData.cs ile birlikte calisabilir (migration bridge).
/// CP toplama mantigi KALDIRILDI. Her kapi stat/modifier verisi tasir.
///
/// GATE SUNUMU:
///   Kapi ustunde: title  ("+10% Ates Hizi")
///   Kapi altinda: tag1 + tag2 ("TEMPO • SWARM")
///   Oyuncuya "bunu sec" diye yonlendirme yapilmaz, etki alani hissettirilir.
///
/// DELIVERY TIPI:
///   Single   = tek olumlu etki
///   Duel     = iki farkli uzmanlikh kapidan secim (SpawnManager cift yerlesirir)
///   Risk     = buyuk oluml + penaltyModifiers
///   Recovery = toparlanma odakli
///   BossPrep = boss oncesi ogullu, seyrek normal havuzda
///
/// ASSETS: Create > TopEndWar > GateConfig
/// </summary>
[CreateAssetMenu(fileName = "Gate_", menuName = "TopEndWar/GateConfig")]
public class GateConfig : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string gateId   = "gate_hardline";
    public string title    = "+8% Silah Gucu";
    public string tag1     = "BOSS";
    public string tag2     = "ELITE";

    // ── Gorsel ───────────────────────────────────────────────────────────
    [Header("Gorsel")]
    public Color gateColor = new Color(0.15f, 0.80f, 0.15f, 0.80f);
    public Sprite icon;

    // ── Siniflandirma ─────────────────────────────────────────────────────
    [Header("Siniflandirma")]
    public GateFamily2     family       = GateFamily2.Power;
    public GateDeliveryType2 deliveryType = GateDeliveryType2.Single;

    // ── Spawn Kontrol ─────────────────────────────────────────────────────
    [Header("Spawn Kontrol")]
    [Tooltip("Bu kapiyi hangi stage'den itibaren havuza al")]
    public int  minStage         = 1;
    [Tooltip("Bu kapiyi hangi stage'den sonra havuzdan cikar (999 = her zaman)")]
    public int  maxStage         = 999;
    [Range(0f, 1f)]
    [Tooltip("Havuz icindeki goreli spawn agirligi")]
    public float spawnWeight     = 0.12f;
    [Tooltip("Tutorial stage'lerinde de cikabilir mi?")]
    public bool tutorialAllowed  = true;
    [Tooltip("Boss prep stage'lerinde oncelikli mi?")]
    public bool bossPrepPriority = false;

    // ── Etkiler ───────────────────────────────────────────────────────────
    [Header("Modifiers (Ana Etki)")]
    public List<GateModifier2> modifiers = new List<GateModifier2>();

    [Header("Ceza Modifiers (Risk delivery icin)")]
    public List<GateModifier2> penaltyModifiers = new List<GateModifier2>();

    // ── Dengeleme Notu ────────────────────────────────────────────────────
    [Header("Denge (Tasarim referansi, oyuncuya gosterilmez)")]
    [Range(0.5f, 3f)]
    public float gateValueBudget = 1.0f;

    // ── Yardimcilar ───────────────────────────────────────────────────────
    public bool IsRisk     => deliveryType == GateDeliveryType2.Risk;
    public bool IsBossPrep => deliveryType == GateDeliveryType2.BossPrep;
    public bool IsRecovery => deliveryType == GateDeliveryType2.Recovery;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(gateId))
            name = $"Gate_{family}_{gateId}";
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class GateModifier2
{
    [Tooltip("Bu modifier kime uygulanir?")]
    public GateTargetType2 targetType = GateTargetType2.CommanderWeapon;

    [Tooltip("Hangi stat?")]
    public GateStatType2   statType   = GateStatType2.WeaponPowerPercent;

    [Tooltip("Islem: AddFlat=duz ekle, AddPercent=yuzde ekle, Promote=seviye atla, HealPercent=HP orani")]
    public GateOperation2  operation  = GateOperation2.AddPercent;

    public float value = 8f;
}

// ── Enumlar ───────────────────────────────────────────────────────────────

public enum GateFamily2
{
    Power,
    Tempo,
    Geometry,
    Army,
    Sustain,
    Tactical,
}

public enum GateDeliveryType2
{
    Single,
    Duel,
    Risk,
    Recovery,
    BossPrep,
}

public enum GateTargetType2
{
    CommanderWeapon,    // Komutan silahi
    Commander,          // Komutanin kendisi
    AllSoldiers,        // Tum askerler
    PiyadeSoldiers,
    MekanikSoldiers,
    TeknolojiSoldiers,
    WeakestSoldier,     // Field Promotion
}

public enum GateStatType2
{
    // Power
    WeaponPowerPercent,
    EliteDamagePercent,
    BossDamagePercent,
    ArmorPenFlat,
    ArmoredTargetDamagePercent,

    // Tempo
    FireRatePercent,

    // Geometry
    PierceCount,
    BounceCount,
    PelletCount,
    SplashRadiusPercent,

    // Army
    AddSoldierCount,
    SoldierDamagePercent,

    // Sustain
    HealCommanderPercent,
    HealSoldiersPercent,

    // Penalty (Risk icin)
    CommanderMaxHpPercent,      // Negatif value ile ceza
    SoldierDamagePercentMalus,
}

public enum GateOperation2
{
    AddFlat,
    AddPercent,
    Promote,        // Field Promotion: en zayif birlik +1 seviye
    HealPercent,    // Toparlanma: mevcut max HP'nin yuzde X'i
}