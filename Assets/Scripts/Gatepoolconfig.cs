using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Kapı Havuzu Konfigürasyonu v2.1 (Claude)
///
/// v2 → v2.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: poolNameKey
///   • DisplayPoolName property'si eklendi
///   • Mevcut tüm havuz mantığı, filtreler ve pick metodları DOKUNULMADI
///
/// Eski alanlar:
///   poolName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > GatePoolConfig
/// </summary>
[CreateAssetMenu(fileName = "GatePool_", menuName = "TopEndWar/GatePoolConfig")]
public class GatePoolConfig : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string poolId   = "GP_BasicPowerTempo";
    public string poolName = "Basic Power/Tempo (Stage 1-5)";

    // ── Localization Keys ──────────────────────────────────────────────────
    // Havuz adı UI'da gösteriliyorsa (debug ekranı, editor araçları vb.) kullanılır.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Havuz adı lokalizasyon anahtarı  ör: gatepool_basic_power_tempo_name")]
    public string poolNameKey = "";

    // ── Display Property ───────────────────────────────────────────────────
    public string DisplayPoolName => string.IsNullOrEmpty(poolNameKey) ? poolName : poolNameKey;

    // ── Havuz İçeriği ─────────────────────────────────────────────────────
    [Header("Havuz İçerik")]
    [Tooltip("Bu havuzdaki kapılar ve ağırlıkları")]
    public List<GatePoolEntry> entries = new List<GatePoolEntry>();

    // ── Spawn Kuralları ───────────────────────────────────────────────────
    [Header("Spawn Kuralları")]
    [Tooltip("Risk kapıları bu havuzda açıkça işaretlenmedikçe çıkmasın")]
    public bool allowRisk    = false;
    [Tooltip("Boss prep kapıları öncelik alsın mı?")]
    public bool bossPrepBias = false;

    // ── Havuz Filtresi (Opsiyonel) ────────────────────────────────────────
    [Header("Havuz Filtresi  (Opsiyonel — None = filtre yok)")]
    [Tooltip("Yalnızca bu aileye ait kapıları döndür  (None = tüm aileler)")]
    public GateFamilyFilter familyFilter = GateFamilyFilter.None;
    [Tooltip("Yalnızca bu tier'a ait kapıları döndür  (None = tüm tier'lar)")]
    public GateTierFilter   tierFilter   = GateTierFilter.None;

    // ── Weighted Random — Mevcut Davranış (KORUNDU) ───────────────────────

    /// <summary>
    /// Bu havuzdan ağırlıklı rastgele bir GateConfig döndürür.
    /// Mevcut SpawnManager bu metodu kullanmaya devam eder.
    /// </summary>
    public GateConfig PickRandom(int stageIndex)
    {
        var   valid = BuildValidList(stageIndex, GateFamilyFilter.None, GateTierFilter.None, true);
        return WeightedPick(valid);
    }

    // ── Yeni Filtreli Pick ────────────────────────────────────────────────

    public GateConfig PickFiltered(int stageIndex)
    {
        var   valid = BuildValidList(stageIndex, familyFilter, tierFilter, true);
        return WeightedPick(valid);
    }

    public GateConfig PickByFamily(int stageIndex, GateFamily targetFamily)
    {
        var valid = new List<GatePoolEntry>();
        var fallback = new List<GatePoolEntry>(); // DEĞİŞİKLİK: Fallback yalnızca silah kimliğini bozmayan gate'leri kullanır.
        foreach (var e in entries)
        {
            if (e.gate == null) continue;
            if (stageIndex < e.gate.minStage || stageIndex > e.gate.maxStage) continue;
            if (e.gate.IsRisk && !allowRisk) continue;
            if (e.gate.family != targetFamily) continue;
            if (!IsGateHardBannedForCurrentWeapon(e.gate))
                fallback.Add(e);
            if (IsGateEligibleForCurrentWeapon(e.gate))
                valid.Add(e);
        }
        return WeightedPick(valid.Count > 0 ? valid : fallback);
    }

    // ── İç Yardımcılar ───────────────────────────────────────────────────

    List<GatePoolEntry> BuildValidList(int stageIndex,
                                       GateFamilyFilter fFilter,
                                       GateTierFilter   tFilter,
                                       bool             useWeaponEligibility)
    {
        var valid = new List<GatePoolEntry>();
        var fallback = new List<GatePoolEntry>(); // DEĞİŞİKLİK: Uygun gate kalmazsa bile Sniper'a yasak gate verilmez.
        foreach (var e in entries)
        {
            if (e.gate == null) continue;
            if (stageIndex < e.gate.minStage || stageIndex > e.gate.maxStage) continue;
            if (e.gate.IsRisk && !allowRisk) continue;

            if (fFilter != GateFamilyFilter.None &&
                (GateFamily)(int)fFilter != e.gate.family) continue;

            if (tFilter != GateTierFilter.None &&
                (GateBalanceTier)(int)tFilter != e.gate.balanceTier) continue;

            if (!useWeaponEligibility || !IsGateHardBannedForCurrentWeapon(e.gate))
                fallback.Add(e);
            if (!useWeaponEligibility || IsGateEligibleForCurrentWeapon(e.gate))
                valid.Add(e);
        }
        return valid.Count > 0 ? valid : fallback;
    }

    bool IsGateEligibleForCurrentWeapon(GateConfig gate)
    {
        // DEĞİŞİKLİK: Silah kimliği korunur; özellikle Sniper'a scatter/pellet/splash spam gelmez.
        if (gate == null || PlayerStats.Instance == null) return true;

        WeaponFamily family = PlayerStats.Instance.GetRuntimeWeaponFamily();
        if (family == WeaponFamily.Assault) return true;

        bool hasPellet = HasModifier(gate, GateStatType2.PelletCount);
        bool hasSplash = HasModifier(gate, GateStatType2.SplashRadiusPercent);
        bool hasBounce = HasModifier(gate, GateStatType2.BounceCount);
        bool hasArmorPen = HasModifier(gate, GateStatType2.ArmorPenFlat);
        bool hasElite = HasModifier(gate, GateStatType2.EliteDamagePercent);
        bool hasBoss = HasModifier(gate, GateStatType2.BossDamagePercent);
        bool hasFireRate = HasModifier(gate, GateStatType2.FireRatePercent);
        bool hasPierce = HasModifier(gate, GateStatType2.PierceCount);
        bool hasArmy = HasModifier(gate, GateStatType2.AddSoldierCount) || HasModifier(gate, GateStatType2.SoldierDamagePercent);

        if (family == WeaponFamily.Sniper)
            return !hasPellet && !hasSplash && !hasBounce;

        if (family == WeaponFamily.SMG)
            return !hasBoss && !hasElite && (hasFireRate || hasPierce || hasArmy || !hasArmorPen || gate.family != GateFamily.Solve);

        return true;
    }

    bool IsGateHardBannedForCurrentWeapon(GateConfig gate)
    {
        // DEĞİŞİKLİK: Fallback havuzu Sniper'ı shotgun/AoE kimliğine geri döndüremez.
        if (gate == null || PlayerStats.Instance == null) return false;
        if (PlayerStats.Instance.GetRuntimeWeaponFamily() != WeaponFamily.Sniper) return false;

        return HasModifier(gate, GateStatType2.PelletCount)
            || HasModifier(gate, GateStatType2.SplashRadiusPercent)
            || HasModifier(gate, GateStatType2.BounceCount);
    }

    bool HasModifier(GateConfig gate, GateStatType2 stat)
    {
        // DEĞİŞİKLİK: Eligibility modifier içeriğine bakar, asset'i değiştirmez.
        if (gate == null || gate.modifiers == null) return false;
        foreach (GateModifier2 mod in gate.modifiers)
            if (mod != null && mod.statType == stat)
                return true;
        return false;
    }

    GateConfig WeightedPick(List<GatePoolEntry> valid)
    {
        if (valid.Count == 0) return null;

        float total = 0f;
        foreach (var e in valid)
            total += e.overrideWeight > 0f ? e.overrideWeight : e.gate.spawnWeight;

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

// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GatePoolEntry
{
    public GateConfig gate;

    [Tooltip("0 = gate.spawnWeight kullan,  >0 = bu havuza özel override ağırlık")]
    [Range(0f, 1f)]
    public float overrideWeight = 0f;
}

// ── Filtre Enum'ları ──────────────────────────────────────────────────────

public enum GateFamilyFilter
{
    None      = -1,
    Power     = 0,
    Tempo     = 1,
    Solve     = 2,
    Geometry  = 3,
    Army      = 4,
    Sustain   = 5,
    Tactical  = 6,
    BossPrep  = 7,
}

public enum GateTierFilter
{
    None      = -1,
    Minor     = 0,
    Standard  = 1,
    Solve     = 2,
    Army      = 3,
    Sustain   = 4,
    BossPrep  = 5,
}
