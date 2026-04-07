using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Kapi Havuzu Konfigurasyonu v1 (Claude)
///
/// Stage'ler tek tek gate secmez; havuz cagirip SpawnManager secer.
/// Bu SO, hangi stage'de hangi gate'lerin ne agirlikla cikacagini tanimlar.
///
/// SLICE HAVUZLARI:
///   GP_BasicPowerTempo  (Stage 1-5):  Hardline, Overclock, Reinforce_Piyade, Medkit, FieldRepair
///   GP_FullSlice        (Stage 6-10): + Breacher, PiercingRound, ExecutionLine
///
/// ASSETS: Create > TopEndWar > GatePoolConfig
/// </summary>
[CreateAssetMenu(fileName = "GatePool_", menuName = "TopEndWar/GatePoolConfig")]
public class GatePoolConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string poolId   = "GP_BasicPowerTempo";
    public string poolName = "Basic Power/Tempo (Stage 1-5)";

    [Header("Havuz Icerik")]
    [Tooltip("Bu havuzdaki kapılar ve agirliklari")]
    public List<GatePoolEntry> entries = new List<GatePoolEntry>();

    [Header("Spawn Kurallari")]
    [Tooltip("Risk kapilari bu havuzda acikça isaretlenmedikce cikmasin")]
    public bool allowRisk         = false;
    [Tooltip("Boss prep kapilari oncelik alsin mi?")]
    public bool bossPrepBias      = false;

    // ── Weighted Random ───────────────────────────────────────────────────

    /// <summary>
    /// Bu havuzdan agirlikla rastgele bir GateConfig dondurur.
    /// stageIndex: minStage/maxStage filtrelemesi icin.
    /// </summary>
    public GateConfig PickRandom(int stageIndex)
    {
        var valid  = new List<GatePoolEntry>();
        float total = 0f;

        foreach (var e in entries)
        {
            if (e.gate == null) continue;
            if (stageIndex < e.gate.minStage || stageIndex > e.gate.maxStage) continue;
            if (e.gate.IsRisk && !allowRisk) continue;
            valid.Add(e);
            total += e.overrideWeight > 0f ? e.overrideWeight : e.gate.spawnWeight;
        }

        if (valid.Count == 0) return null;

        float r = Random.value * total, cum = 0f;
        foreach (var e in valid)
        {
            float w = e.overrideWeight > 0f ? e.overrideWeight : e.gate.spawnWeight;
            cum += w;
            if (r <= cum) return e.gate;
        }
        return valid[valid.Count - 1].gate;
    }
}

[System.Serializable]
public class GatePoolEntry
{
    public GateConfig gate;

    [Tooltip("0 = gate.spawnWeight kullan, >0 = bu havuza ozel override agirlik")]
    [Range(0f, 1f)]
    public float overrideWeight = 0f;
}