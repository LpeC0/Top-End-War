using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Ordu Yoneticisi v4
///
/// PATCH OZETI:
/// - Eski calisan ArmyManager akisi korundu
/// - WeaponArchetypeConfig entegrasyonu eklendi
/// - Class/chassis mantigi eklendi
/// - Dinamik ama basit formasyon eklendi
/// - Merge sonrasi silah korunur
/// </summary>
public class ArmyManager : MonoBehaviour
{
    public static ArmyManager Instance { get; private set; }

    [Header("Asker Prefab (bos birakilabilir)")]
    public GameObject soldierPrefab;

    [Header("Sinirlar")]
    public int maxSoldiers = 20;

    [Header("Gorsel")]
    [Range(0.8f, 1f)] public float soldierVisualScale = 0.88f;

    [Header("Silah Veritabani")]
    public List<WeaponArchetypeConfig> weaponConfigs = new List<WeaponArchetypeConfig>();

    [Header("Formasyon")]
    public float rowSpacing = 1.15f;
    public float colSpacing = 1.25f;
    public float edgeCompression = 0.78f;
    public float sideBiasStrength = 0.65f;

    [Header("Mode Tuning")]
    [Range(0.5f, 1.5f)] public float runnerSoldierDpsMultiplier = 1.20f;
    [Range(0.5f, 1.5f)] public float anchorSoldierDpsMultiplier = 0.76f; // DEĞİŞİKLİK: Soldier build güçlü kalır, ama anchor testini tek başına çözmez.

    // (hp, dmgMult, fireRateMult, formationRank)
    static readonly Dictionary<SoldierPath, (int hp, float dmgMult, float fireRateMult, int formationRank)> SOLDIER_BASE
        = new Dictionary<SoldierPath, (int, float, float, int)>
    {
        [SoldierPath.Piyade]    = (90,  1.00f, 1.00f, 1),
        [SoldierPath.Mekanik]   = (125, 0.95f, 1.05f, 0),
        [SoldierPath.Teknoloji] = (65,  1.10f, 0.92f, 2),
    };

    static readonly float[] MERGE_MULT = { 1f, 1.8f, 3.5f, 7.0f };

    readonly List<SoldierUnit> _soldiers = new List<SoldierUnit>(20);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// weaponOverride verilmezse class icin default silah secilir.
    /// </summary>
    public bool AddSoldier(SoldierPath path, string biome = null, int mergeLevel = 1, int count = 1, WeaponArchetypeConfig weaponOverride = null)
    {
        bool added = false;

        for (int i = 0; i < count; i++)
        {
            if (_soldiers.Count >= maxSoldiers) break;

            string actualBiome = biome ?? BiomeManager.Instance?.currentBiome ?? "Tas";
            WeaponArchetypeConfig weapon = weaponOverride ?? GetDefaultWeaponForPath(path);

            SoldierUnit unit = SpawnSoldierUnit(path, actualBiome, mergeLevel, weapon);
            if (unit == null) continue;

            _soldiers.Add(unit);
            AssignFormationOffsets();
            added = true;
        }

        if (added)
        {
            GameEvents.OnSoldierAdded?.Invoke(_soldiers.Count);
            Debug.Log($"[Army] +{count} {path} asker | Toplam: {_soldiers.Count}");
        }

        return added;
    }

    public void RemoveSoldier(SoldierUnit unit)
    {
        if (!_soldiers.Contains(unit)) return;

        _soldiers.Remove(unit);
        AssignFormationOffsets();
        GameEvents.OnSoldierRemoved?.Invoke(_soldiers.Count);
        Debug.Log($"[Army] Asker dustu | Kalan: {_soldiers.Count}");
    }

    public bool TryMerge()
    {
        bool anyMerge = false;

        bool found;
        int safetyLimit = 10;

        do
        {
            found = false;
            if (safetyLimit-- <= 0) break;

            foreach (SoldierPath path in System.Enum.GetValues(typeof(SoldierPath)))
            {
                for (int lv = 1; lv <= 3; lv++)
                {
                    List<SoldierUnit> group = FindGroup(path, lv);
                    if (group.Count < 3) continue;

                    SoldierUnit first = group[0];
                    string biome = first.biome;
                    WeaponArchetypeConfig mergedWeapon = first.weaponConfig;

                    for (int i = 0; i < 3; i++)
                    {
                        SoldierUnit u = group[i];
                        _soldiers.Remove(u);
                        u.gameObject.SetActive(false);
                    }

                    SoldierUnit merged = SpawnSoldierUnit(path, biome, lv + 1, mergedWeapon);
                    if (merged != null) _soldiers.Add(merged);

                    AssignFormationOffsets();
                    GameEvents.OnSoldierMerged?.Invoke(path.ToString(), lv + 1);
                    Debug.Log($"[Army] MERGE: {path} Lv{lv} x3 -> Lv{lv + 1}");

                    found = true;
                    anyMerge = true;
                    break;
                }

                if (found) break;
            }

        } while (found);

        return anyMerge;
    }

    public void HealAll(float pct)
    {
        int totalHealed = 0;

        foreach (SoldierUnit u in _soldiers)
        {
            int before = u.currentHP;
            u.HealPercent(pct);
            totalHealed += u.currentHP - before;
        }

        GameEvents.OnSoldierHPRestored?.Invoke(totalHealed);
        Debug.Log($"[Army] HealAll %{pct * 100f:0} | Toplam iyilestirme: {totalHealed}");
    }

    SoldierUnit SpawnSoldierUnit(SoldierPath path, string biome, int mergeLevel, WeaponArchetypeConfig weaponConfig)
    {
        GameObject go;

        if (soldierPrefab != null)
        {
            go = Instantiate(soldierPrefab);
            go.transform.localScale *= soldierVisualScale;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f) * soldierVisualScale;

            Destroy(go.GetComponent<CapsuleCollider>());
            var cc = go.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f;
            cc.height = 1.1f;
            cc.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        if (PlayerStats.Instance != null)
        {
            go.transform.position = PlayerStats.Instance.transform.position
                                    + new Vector3(Random.Range(-1f, 1f), 1.2f, -2f);
        }

        SoldierUnit unit = go.GetComponent<SoldierUnit>() ?? go.AddComponent<SoldierUnit>();

        var (hp, dmgMult, fireRateMult, formationRank) = SOLDIER_BASE[path];
        float mergeStatMult = MERGE_MULT[Mathf.Clamp(mergeLevel - 1, 0, MERGE_MULT.Length - 1)];

        unit.path = path;
        unit.biome = biome;
        unit.mergeLevel = mergeLevel;
        unit.maxHP = Mathf.RoundToInt(hp * mergeStatMult);
        unit.currentHP = unit.maxHP;

        unit.chassisDamageMult = dmgMult;
        unit.chassisFireRateMult = fireRateMult;
        unit.formationRank = formationRank;

        ApplyWeaponToUnit(unit, weaponConfig);

        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color c = unit.GetPathColor();
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", c);
            else
                rend.material.color = c;
        }

        go.name = $"Soldier_{path}_{unit.weaponConfig?.family}_Lv{mergeLevel}";
        go.SetActive(true);
        return unit;
    }

    void ApplyWeaponToUnit(SoldierUnit unit, WeaponArchetypeConfig weaponConfig)
    {
        unit.weaponConfig = weaponConfig;
        unit.affinityPercent = GetAffinity(unit.path, weaponConfig != null ? weaponConfig.family : WeaponFamily.Assault);
    }

    WeaponArchetypeConfig GetDefaultWeaponForPath(SoldierPath path)
    {
        WeaponFamily family = path switch
        {
            SoldierPath.Piyade => WeaponFamily.Assault,
            SoldierPath.Mekanik => WeaponFamily.SMG,
            SoldierPath.Teknoloji => WeaponFamily.Sniper,
            _ => WeaponFamily.Assault
        };

        return weaponConfigs.Find(x => x != null && x.family == family);
    }

    int GetAffinity(SoldierPath path, WeaponFamily family)
    {
        return path switch
        {
            SoldierPath.Piyade => family switch
            {
                WeaponFamily.Assault => 115,
                WeaponFamily.SMG => 105,
                WeaponFamily.Sniper => 90,
                WeaponFamily.Shotgun => 90,
                WeaponFamily.Launcher => 85,
                WeaponFamily.Beam => 100,
                _ => 100
            },

            SoldierPath.Mekanik => family switch
            {
                WeaponFamily.Assault => 95,
                WeaponFamily.SMG => 115,
                WeaponFamily.Sniper => 80,
                WeaponFamily.Shotgun => 115,
                WeaponFamily.Launcher => 105,
                WeaponFamily.Beam => 90,
                _ => 100
            },

            SoldierPath.Teknoloji => family switch
            {
                WeaponFamily.Assault => 90,
                WeaponFamily.SMG => 80,
                WeaponFamily.Sniper => 115,
                WeaponFamily.Shotgun => 75,
                WeaponFamily.Launcher => 105,
                WeaponFamily.Beam => 115,
                _ => 100
            },

            _ => 100
        };
    }

    void AssignFormationOffsets()
    {
        List<SoldierUnit> ordered = BuildFormationOrder();
        List<Vector3> slots = GenerateFormationSlots(ordered.Count);

        for (int i = 0; i < ordered.Count && i < slots.Count; i++)
        {
            ordered[i].formationOffset = slots[i];
            ordered[i].runtimeEfficiency = GetEfficiencyForIndex(i); // DEĞİŞİKLİK: 1/2/3/4/5+ asker azalan verim alır.
        }
    }

    float GetEfficiencyForIndex(int index)
    {
        // DEĞİŞİKLİK: Soldier DPS defaultta toplam hasarı boğmasın diye yumuşak diminishing return.
        return index switch
        {
            0 => 1.00f,
            1 => 0.82f,
            2 => 0.68f,
            3 => 0.58f,
            _ => 0.50f,
        };
    }

    List<SoldierUnit> BuildFormationOrder()
    {
        var ordered = new List<SoldierUnit>(_soldiers);

        ordered.Sort((a, b) =>
        {
            if (a.formationRank != b.formationRank)
                return a.formationRank.CompareTo(b.formationRank);

            if (a.mergeLevel != b.mergeLevel)
                return b.mergeLevel.CompareTo(a.mergeLevel);

            return 0;
        });

        return ordered;
    }

    List<Vector3> GenerateFormationSlots(int count)
    {
        var list = new List<Vector3>();
        if (count <= 0) return list;

        float xNorm = 0f;
        if (PlayerStats.Instance != null)
            xNorm = Mathf.Clamp(PlayerStats.Instance.transform.position.x / 8f, -1f, 1f);

        float widthMult = Mathf.Lerp(1f, edgeCompression, Mathf.Abs(xNorm));
        float bias = -xNorm * sideBiasStrength;

        int cols = count <= 3 ? count :
                   count <= 6 ? 3 :
                   count <= 12 ? 4 : 5;

        float cSpace = colSpacing * widthMult;

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;
            int col = i % cols;

            int rowCount = Mathf.Min(cols, count - row * cols);
            float width = (rowCount - 1) * cSpace;

            float x = -width * 0.5f + col * cSpace + bias;
            float z = -1.4f - row * rowSpacing;

            list.Add(new Vector3(x, 0f, z));
        }

        return list;
    }

    List<SoldierUnit> FindGroup(SoldierPath path, int level)
    {
        var list = new List<SoldierUnit>();
        foreach (SoldierUnit u in _soldiers)
            if (u.path == path && u.mergeLevel == level)
                list.Add(u);
        return list;
    }

    public int SoldierCount => _soldiers.Count;

    public float GetEstimatedSoldierDps()
    {
        // DEĞİŞİKLİK: Debug panel asker katkısını commander DPS'ten ayrı gösterir.
        float total = 0f;
        foreach (SoldierUnit u in _soldiers)
            if (u != null && u.gameObject.activeInHierarchy)
                total += u.GetEstimatedDps();
        return total;
    }

    public float GetModeSoldierDpsMultiplier()
    {
        // DEĞİŞİKLİK: Runner askerleri nefes aldırır, Anchor askerleri testi tek başına çözmez.
        bool anchorActive = AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive;
        return anchorActive ? anchorSoldierDpsMultiplier : runnerSoldierDpsMultiplier;
    }

    public string GetActiveSoldierTypesText()
    {
        // DEĞİŞİKLİK: Aktif asker rolleri debug panelde kısa okunur.
        int piyade = GetCountByPath(SoldierPath.Piyade);
        int mekanik = GetCountByPath(SoldierPath.Mekanik);
        int teknoloji = GetCountByPath(SoldierPath.Teknoloji);
        if (piyade + mekanik + teknoloji <= 0) return "-";
        return $"P:{piyade} M:{mekanik} T:{teknoloji}";
    }

    public bool IsFull => _soldiers.Count >= maxSoldiers;

    public int GetCountByPath(SoldierPath path)
    {
        int n = 0;
        foreach (SoldierUnit u in _soldiers)
            if (u.path == path) n++;
        return n;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
