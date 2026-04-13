using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Boss Konfigurasyonu v2
///
/// Vertical slice icin BossManager'a minimum bridge helper'lari eklendi.
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
    public float hpFactor = 13f;

    [Tooltip("Zirh degeri")]
    public int armor = 10;

    [Tooltip("Encounter suresi hedefi (saniye, tasarim referansi)")]
    public float targetEncounterSec = 13.5f;

    [Header("Faz Yapisi")]
    public List<BossPhaseData> phases = new List<BossPhaseData>();

    [Header("Tasarim Notu")]
    [TextArea(2, 4)]
    public string teachingFocus = "";

    [TextArea(2, 4)]
    public string skillsTested = "";

    public int GetHP(float targetDps) => Mathf.RoundToInt(targetDps * hpFactor);

    // DEĞİŞİKLİK
    public float GetFirstTransitionRatio(float fallback = 0.50f)
    {
        if (phases == null || phases.Count < 2) return fallback;
        return Mathf.Clamp01(phases[1].startHpRatio);
    }

    // DEĞİŞİKLİK
    public float GetFirstTransitionLock(float fallback = 1.6f)
    {
        if (phases == null || phases.Count < 2) return fallback;
        return Mathf.Max(0f, phases[1].transitionLockSec);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        hpFactor = Mathf.Max(0.1f, hpFactor);
        armor = Mathf.Max(0, armor);
        targetEncounterSec = Mathf.Max(1f, targetEncounterSec);

        if (phases == null)
            phases = new List<BossPhaseData>();

        if (!string.IsNullOrEmpty(bossId))
            name = $"Boss_{bossTier}_{bossId}";
    }
#endif
}

[System.Serializable]
public class BossPhaseData
{
    [Header("Faz Tanimi")]
    [Range(0f, 1f)]
    public float startHpRatio = 1.0f;

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
    public string attackId = "line_shot";
    public string attackName = "Line Shot";
    public BossAttackType attackType = BossAttackType.Strike;

    public float telegraphSec = 0.7f;
    public float cooldownSec = 2.8f;
    public float damageScalar = 0.8f;
    public float areaRadius = 0f;

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
    Strike,
    Sweep,
    Charge,
    AreaMark,
    SummonPulse,
    WeakpointWindow,
}