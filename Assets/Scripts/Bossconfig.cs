using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Boss Konfigurasyonu v1 (Claude)
///
/// Boss'un tasarim verisi. Runtime state icermez.
/// BossManager bu SO'dan HP, faz ve saldiri bilgisini okur.
///
/// SLICE BOSS: Gatekeeper Walker (Stage 10)
///   HP = targetDps * 13 (Stage 10 targetDps=232 => HP=3016)
///   Armor = 10
///   Encounter hedefi: 12-15 sn
///   Faz gecis kilit: 1.6 sn
///
/// ASSETS: Create > TopEndWar > BossConfig
/// </summary>
[CreateAssetMenu(fileName = "Boss_", menuName = "TopEndWar/BossConfig")]
public class BossConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string   bossId      = "gatekeeper_walker";
    public string   bossName    = "Gatekeeper Walker";
    public BossTier bossTier    = BossTier.MiniBoss1;

    [Header("Stat Faktörleri")]
    [Tooltip("HP = StageConfig.targetDps * hpFactor")]
    public float hpFactor       = 13f;

    [Tooltip("Zirh degeri")]
    public int   armor          = 10;

    [Tooltip("Encounter suresi hedefi (saniye, tasarim referansi)")]
    public float targetEncounterSec = 13.5f;

    [Header("Faz Yapisi")]
    public List<BossPhaseData> phases = new List<BossPhaseData>();

    [Header("Tasarim Notu")]
    [TextArea(2, 4)]
    public string teachingFocus = "";
    [TextArea(2, 4)]
    public string skillsTested  = "";

    // ── Yardimcilar ───────────────────────────────────────────────────────
    public int GetHP(float targetDps) => Mathf.RoundToInt(targetDps * hpFactor);
}

[System.Serializable]
public class BossPhaseData
{
    [Header("Faz Tanimi")]
    [Tooltip("Bu faz hangi HP oraninda baslar? (1.0 = %100, 0.5 = %50)")]
    [Range(0f, 1f)]
    public float startHpRatio   = 1.0f;

    [Tooltip("Faz gecisi oncesi transition lock suresi (saniye)")]
    public float transitionLockSec = 1.6f;

    [Header("Saldirilar")]
    public List<BossAttackData> attacks = new List<BossAttackData>();

    [Header("Durum Notu")]
    [TextArea(1, 3)]
    public string phaseNote = "";
}

[System.Serializable]
public class BossAttackData
{
    public string attackId      = "line_shot";
    public string attackName    = "Line Shot";
    public BossAttackType attackType = BossAttackType.Strike;

    [Tooltip("Telegraph suresi (saniye)")]
    public float telegraphSec   = 0.7f;

    [Tooltip("Cooldown (saniye)")]
    public float cooldownSec    = 2.8f;

    [Tooltip("Hasar skalasi (targetDps ile carpilir)")]
    public float damageScalar   = 0.8f;

    [Tooltip("Alan saldirisi yaricapi (0 = tek hedef)")]
    public float areaRadius     = 0f;

    [TextArea(1, 2)]
    public string note = "";
}

public enum BossTier
{
    MiniBoss1,
    MiniBoss2,
    FinalBoss,
}

public enum BossAttackType
{
    Strike,     // Tekil darbe
    Sweep,      // Alan tarama
    Charge,     // Hizli ilerleme
    AreaMark,   // Yer isaretleme
    SummonPulse,// Yardimci cagirma
    WeakpointWindow, // Hassas nokta acilmasi
}