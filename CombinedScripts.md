# Combined Assets/Scripts C# Sources

This file contains all `.cs` files from `Assets/Scripts/` merged into a single Markdown document. Each file is included with its path and content in a fenced code block.

## ArmyManager.cs

```csharp
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
            ordered[i].formationOffset = slots[i];
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
```

## BiomeManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Biyom Yoneticisi (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "BiomeManager" -> bu scripti ekle.
///   Inspector'da currentBiome'u sahneye gore ayarla:
///     Sivas=Tas  Tokat=Orman  Kayseri=Cul  Erzurum=Karli  Malatya=Tarim
///
/// Kullanim:
///   float mult = BiomeManager.Instance.GetMultiplier(SoldierPath.Teknoloji);
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("Bu Sahnenin Biyomu")]
    [Tooltip("Tas / Orman / Cul / Karli / Tarim")]
    public string currentBiome = "Tas";

    // Biyom x Path hasar matrisi — dogru path x1.25, yanlis x0.85 ceza
    static readonly Dictionary<string, Dictionary<string, float>> _matrix
        = new Dictionary<string, Dictionary<string, float>>
    {
        ["Tas"]   = new Dictionary<string, float> { ["Piyade"]=0.90f, ["Mekanik"]=1.10f, ["Teknoloji"]=1.25f },
        ["Orman"] = new Dictionary<string, float> { ["Piyade"]=1.20f, ["Mekanik"]=1.00f, ["Teknoloji"]=0.85f },
        ["Cul"]   = new Dictionary<string, float> { ["Piyade"]=1.10f, ["Mekanik"]=1.20f, ["Teknoloji"]=1.00f },
        ["Karli"] = new Dictionary<string, float> { ["Piyade"]=1.15f, ["Mekanik"]=0.85f, ["Teknoloji"]=1.15f },
        ["Tarim"] = new Dictionary<string, float> { ["Piyade"]=1.25f, ["Mekanik"]=1.10f, ["Teknoloji"]=0.80f },
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => GameEvents.OnBiomeChanged?.Invoke(currentBiome);

    /// <summary>Asker pathine gore biyom hasar carpanini dondurur.</summary>
    public float GetMultiplier(SoldierPath path)
    {
        string key = path.ToString();
        if (_matrix.TryGetValue(currentBiome, out var row) &&
            row.TryGetValue(key, out float mult))
            return mult;
        Debug.LogWarning($"[BiomeManager] Bilinmeyen biome/path: {currentBiome}/{key}");
        return 1f;
    }

    /// <summary>Runtime biyom degistir (yeni bolum gecislerinde).</summary>
    public void SetBiome(string biome)
    {
        if (!_matrix.ContainsKey(biome))
        { Debug.LogWarning($"[BiomeManager] Bilinmeyen biome: {biome}"); return; }
        currentBiome = biome;
        GameEvents.OnBiomeChanged?.Invoke(currentBiome);
        Debug.Log($"[Biome] -> {biome}");
    }

    public string GetBossName() => currentBiome switch
    {
        "Tas"   => "Gokmedrese Muhafizi",
        "Orman" => "Orman Canavarı",
        "Cul"   => "Kum Devigi",
        "Karli" => "Buz Muhafizi",
        "Tarim" => "Tarla Ruhu",
        _       => "Bilinmeyen Boss"
    };

    void OnDestroy() { if (Instance == this) Instance = null; }
}
```

## Biomevisuals.cs

```csharp
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Biyom Görsel Sistemi (Claude)
///
/// BiomeManager'ın OnBiomeChanged eventini dinler.
/// Her biyom geçişinde:
///   - Kamera arkaplan rengi değişir (DOTween ile yumuşak)
///   - Directional Light rengi değişir
///   - Fog rengi değişir (opsiyonel)
///
/// UNITY KURULUM:
///   Hierarchy → Create Empty → "BiomeVisuals" → bu scripti ekle
///   mainCamera   → Main Camera
///   mainLight    → Directional Light
///
/// BİYOM RENKLERİ:
///   Taş  (Sivas)  → gri/mavi soğuk
///   Orman(Tokat)  → yeşil/sıcak
///   Çöl  (Kayser) → turuncu/sarı kuru
///   Karlı(Erzrum) → beyaz/mavi buz
///   Tarım(Mlatya) → yeşil/sarı yumuşak
/// </summary>
public class BiomeVisuals : MonoBehaviour
{
    [Header("Referanslar")]
    public Camera    mainCamera;
    public Light     mainLight;

    [Header("Geçiş Süresi")]
    public float transitionDuration = 2.5f;

    // ── Biyom renk tanımları ──────────────────────────────────────────────
    static readonly System.Collections.Generic.Dictionary<string, BiomeColors> COLORS
        = new System.Collections.Generic.Dictionary<string, BiomeColors>
    {
        ["Tas"]   = new BiomeColors(
            sky:   new Color(0.28f, 0.33f, 0.42f),   // soğuk gri-mavi
            light: new Color(0.90f, 0.88f, 0.80f),   // soluk beyaz
            fog:   new Color(0.60f, 0.62f, 0.68f),
            fogDensity: 0.008f
        ),
        ["Orman"] = new BiomeColors(
            sky:   new Color(0.18f, 0.28f, 0.20f),   // koyu yeşil
            light: new Color(1.00f, 0.95f, 0.75f),   // sıcak sarı
            fog:   new Color(0.45f, 0.55f, 0.40f),
            fogDensity: 0.012f
        ),
        ["Cul"]   = new BiomeColors(
            sky:   new Color(0.55f, 0.40f, 0.20f),   // turuncu çöl
            light: new Color(1.00f, 0.88f, 0.60f),   // sıcak altın
            fog:   new Color(0.70f, 0.58f, 0.35f),
            fogDensity: 0.015f
        ),
        ["Karli"] = new BiomeColors(
            sky:   new Color(0.70f, 0.78f, 0.90f),   // açık buz mavisi
            light: new Color(0.85f, 0.92f, 1.00f),   // soğuk beyaz-mavi
            fog:   new Color(0.80f, 0.85f, 0.95f),
            fogDensity: 0.018f
        ),
        ["Tarim"] = new BiomeColors(
            sky:   new Color(0.35f, 0.45f, 0.25f),   // tarım yeşili
            light: new Color(1.00f, 0.96f, 0.78f),   // güneşli
            fog:   new Color(0.55f, 0.60f, 0.42f),
            fogDensity: 0.006f
        ),
    };

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainLight  == null) mainLight  = FindFirstObjectByType<Light>();

        GameEvents.OnBiomeChanged += OnBiomeChanged;

        // Başlangıç biyomunu uygula (animasyonsuz)
        string startBiome = BiomeManager.Instance?.currentBiome ?? "Tas";
        ApplyImmediate(startBiome);
    }

    void OnDestroy() => GameEvents.OnBiomeChanged -= OnBiomeChanged;

    void OnBiomeChanged(string biome) => ApplyTransition(biome);

    void ApplyImmediate(string biome)
    {
        if (!COLORS.TryGetValue(biome, out var c)) return;
        if (mainCamera) { mainCamera.backgroundColor = c.sky; mainCamera.clearFlags = CameraClearFlags.SolidColor; }
        if (mainLight)  mainLight.color = c.light;
        RenderSettings.fogColor   = c.fog;
        RenderSettings.fogDensity = c.fogDensity;
        RenderSettings.fog        = true;
    }

    void ApplyTransition(string biome)
    {
        if (!COLORS.TryGetValue(biome, out var c)) return;

        // Kamera arkaplan
        if (mainCamera)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            DOTween.To(
                () => mainCamera.backgroundColor,
                x  => mainCamera.backgroundColor = x,
                c.sky, transitionDuration
            ).SetEase(Ease.InOutSine);
        }

        // Işık rengi
        if (mainLight)
        {
            DOTween.To(
                () => mainLight.color,
                x  => mainLight.color = x,
                c.light, transitionDuration
            ).SetEase(Ease.InOutSine);
        }

        // Fog
        DOTween.To(
            () => RenderSettings.fogColor,
            x  => RenderSettings.fogColor = x,
            c.fog, transitionDuration
        ).SetEase(Ease.InOutSine);

        RenderSettings.fog = true;
        DOTween.To(
            () => RenderSettings.fogDensity,
            x  => RenderSettings.fogDensity = x,
            c.fogDensity, transitionDuration
        );
    }

    // ── İç tip ─────────────────────────────────────────────────────────────
    struct BiomeColors
    {
        public Color sky, light, fog;
        public float fogDensity;
        public BiomeColors(Color sky, Color light, Color fog, float fogDensity)
        { this.sky=sky; this.light=light; this.fog=fog; this.fogDensity=fogDensity; }
    }
}
```

## Bossconfig.cs

```csharp
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
```

## Bosshitreceiver.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Boss Isabet Alici v2
///
/// DEĞİŞİKLİK:
///   - ArmorPen / BossDamageMult tasir
///   - Eski TakeDamage(int) korunur
/// </summary>
public class BossHitReceiver : MonoBehaviour
{
    [Tooltip("BossManager objesi. Bos birakılırsa BossManager.Instance kullanilir.")]
    public BossManager bossManager;

    void Awake()
    {
        if (bossManager == null)
            bossManager = BossManager.Instance;
    }

    public void TakeDamage(int dmg)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(dmg);
    }

    // DEĞİŞİKLİK
    public void TakeDamage(int rawDamage, int armorPen, float bossDamageMult)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(rawDamage, armorPen, bossDamageMult);
    }
}
```

## BossManager.cs

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Boss Yoneticisi v7 (Claude & Patch Integrated)
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Ayarlari")]
    public int bossMaxHP = 41000;

    [Header("Vertical Slice Mini-Boss")]
    public float transitionLockDuration = 1.6f;
    [Range(0.1f, 0.9f)] public float phase2Threshold = 0.50f;

    [Header("Legacy (Slice'ta kapali tutulur)")]
    public bool enableMinionPhase = false;
    public bool enableEnragePhase = false;

    [Header("Minyon Spawn (Legacy)")]
    [Tooltip("ObjectPooler 'Enemy' havuzundan cekilir. Pool bos ise spawn edilmez.")]
    public int minionsPerWave = 4;
    public float minionInterval = 8f;
    [Tooltip("Minyon spawn pozisyonu icin bos referans noktalari (opsiyonel)")]
    public Transform[] minionSpawnPoints;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] private int _currentHP;
    [SerializeField] private int _currentPhase;   // 1=normal, 2=phase2
    [SerializeField] private bool _invulnerable;
    [SerializeField] private bool _phase2Triggered;
    [SerializeField] private bool _active;

    Coroutine _minionCoroutine;
    Coroutine _shieldCoroutine;

    // --- YENİ ALANLAR ---
    BossConfig _currentBossConfig;
    int _currentBossArmor = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // --- YENİ START OVERLOAD ---
    public void StartBoss(BossConfig config, float targetDps)
    {
        _currentBossConfig = config;

        if (config != null)
        {
            bossMaxHP = config.GetHP(targetDps);
            _currentBossArmor = config.armor;
            transitionLockDuration = config.GetFirstTransitionLock(transitionLockDuration);
            phase2Threshold = config.GetFirstTransitionRatio(phase2Threshold);
        }
        else
        {
            _currentBossArmor = 0;
        }

        StartBoss(bossMaxHP);
    }

    public void StartBoss(int hp = -1)
    {
        if (hp > 0) bossMaxHP = hp;
        _currentHP = bossMaxHP;
        _currentPhase = 1;
        _invulnerable = false;
        _phase2Triggered = false;
        _active = true;

        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);
        GameEvents.OnBossEncountered?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(true);

        Debug.Log($"[BossManager] Basliyor. HP: {bossMaxHP}");
    }

    // --- HASAR UYGULAMA (GÜNCELLENDİ) ---
    void ApplyFinalDamage(int finalDamage)
    {
        if (!_active || _invulnerable || _currentHP <= 0) return;

        _currentHP = Mathf.Max(0, _currentHP - finalDamage);
        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);

        CheckPhaseTransitions();

        if (_currentHP <= 0) OnBossDefeated();
    }

    public void TakeDamage(int dmg)
    {
        ApplyFinalDamage(dmg);
    }

    public void TakeDamage(int rawDamage, int armorPen, float bossDamageMult)
    {
        int effectiveArmor = Mathf.Max(0, _currentBossArmor - Mathf.Max(0, armorPen));
        int finalDamage = Mathf.Max(1, rawDamage - effectiveArmor);
        finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, bossDamageMult)));

        ApplyFinalDamage(finalDamage);
    }

    void CheckPhaseTransitions()
    {
        float ratio = (float)_currentHP / bossMaxHP;
        
        if (!_phase2Triggered && ratio <= phase2Threshold)
        {
            _phase2Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(TransitionLockRoutine());
        }
    }

    IEnumerator TransitionLockRoutine()
    {
        _invulnerable = true;
        GameEvents.OnBossPhaseShield?.Invoke(2); 

        yield return new WaitForSeconds(transitionLockDuration);

        _invulnerable = false;
        EnterPhase2();
    }

    void EnterPhase2()
    {
        _currentPhase = 2;
        GameEvents.OnBossPhaseChanged?.Invoke(2);

        if (enableMinionPhase)
        {
            if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
            _minionCoroutine = StartCoroutine(MinionWaveRoutine());
        }
    }

    IEnumerator MinionWaveRoutine()
    {
        while (_active && _currentPhase == 2)
        {
            SpawnMinions();
            yield return new WaitForSeconds(minionInterval);
        }
    }

    void SpawnMinions()
    {
        if (!_active || ObjectPooler.Instance == null) return;
        for (int i = 0; i < minionsPerWave; i++)
        {
            Vector3 spawnPos;
            if (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
                spawnPos = minionSpawnPoints[i % minionSpawnPoints.Length].position;
            else
                spawnPos = transform.position + new Vector3(
                    Random.Range(-5f, 5f), 0f, Random.Range(-3f, 3f));
            
            ObjectPooler.Instance.SpawnFromPool("Enemy", spawnPos, Quaternion.identity);
        }
    }

    void OnBossDefeated()
    {
        _active = false;
        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);

        GameEvents.OnBossDefeated?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        Debug.Log("[BossManager] Boss yenildi!");
    }

    public float GetHPRatio() => bossMaxHP > 0 ? (float)_currentHP / bossMaxHP : 0f;
    public bool IsActive() => _active;
    public bool IsInvulnerable() => _invulnerable;
}
/// KURULUM:
///   1. Hierarchy'de bir Boss GameObject olustur.
///   2. BossHitReceiver.cs'i bu objeye ekle (Bullet.cs bunu arar).
///   3. BossManager.cs ayri bir sahne objesine (BossManager) ekle.
///   4. Inspector'dan bossMaxHP ayarla veya StageManager.SetupBoss() kullan.
/// </summary>
```

## Bullet.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);

    [HideInInspector] public string hitterPath = "Commander"; 
    [HideInInspector] public int   armorPen = 0;
    [HideInInspector] public int   pierceCount = 0;
    [HideInInspector] public float eliteDamageMult = 1f;
    [HideInInspector] public float bossDamageMult = 1f;

    const float HIT_RADIUS = 0.4f;
    const float LIFETIME   = 1.8f;

    Renderer _rend;
    bool _hit = false;

    int _remainingPierce = 0;
    readonly HashSet<int> _hitTargets = new HashSet<int>();

    // DEĞİŞİKLİK
    Vector3 _lastPos;

    void Awake() => _rend = GetComponentInChildren<Renderer>();

    void OnEnable()
    {
        _hit = false;
        _remainingPierce = Mathf.Max(0, pierceCount);
        _hitTargets.Clear();
        ApplyColor();

        // DEĞİŞİKLİK
        _lastPos = transform.position;

        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
        _remainingPierce = 0;
        _hitTargets.Clear();
    }

    public void SetDamage(int d) => damage = d;

    public void SetCombatStats(int newDamage, int newArmorPen = 0, int newPierceCount = 0, float newEliteDamageMult = 1f, float newBossDamageMult = 1f)
    {
        damage = newDamage;
        armorPen = Mathf.Max(0, newArmorPen);
        pierceCount = Mathf.Max(0, newPierceCount);
        eliteDamageMult = Mathf.Max(1f, newEliteDamageMult);
        bossDamageMult = Mathf.Max(1f, newBossDamageMult);
        _remainingPierce = pierceCount;
    }

    void Update()
    {
        if (_hit) return;

        // DEĞİŞİKLİK
        Collider[] cols = Physics.OverlapCapsule(_lastPos, transform.position, HIT_RADIUS);

        foreach (Collider col in cols)
        {
            BossHitReceiver bossRecv = col.GetComponent<BossHitReceiver>() ?? col.GetComponentInParent<BossHitReceiver>();
            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();

            if (bossRecv == null && enemy == null)
                continue;

            int targetId = bossRecv != null ? bossRecv.gameObject.GetInstanceID()
                                            : enemy.gameObject.GetInstanceID();

            if (_hitTargets.Contains(targetId))
                continue;

            if (PlayerStats.Instance != null)
            {
                float playerZ = PlayerStats.Instance.transform.position.z;
                Transform t = bossRecv != null ? bossRecv.transform : enemy.transform;
                if (t.position.z < playerZ - 2f) continue;
            }

            if (bossRecv != null)
            {
                bossRecv.TakeDamage(damage, armorPen, bossDamageMult);
                DamagePopup.Show(col.transform.position, damage,
                    DamagePopup.GetColor(hitterPath), damage > 500);
            }
            else if (enemy != null)
            {
                enemy.TakeDamage(
                    rawDamage: damage,
                    armorPenValue: armorPen,
                    eliteMultiplier: eliteDamageMult,
                    hitColor: DamagePopup.GetColor(hitterPath));
            }

            _hitTargets.Add(targetId);

            if (_remainingPierce > 0)
            {
                _remainingPierce--;
                continue;
            }

            Hit();
            return;
        }

        // DEĞİŞİKLİK
        _lastPos = transform.position;
    }

    void Hit()
    {
        if (_hit) return;
        _hit = true;
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    void ApplyColor()
    {
        if (_rend == null) return;
        if (_rend.material.HasProperty("_BaseColor"))
            _rend.material.SetColor("_BaseColor", bulletColor);
        else
            _rend.material.color = bulletColor;
    }
}
```

## ChunkManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Sonsuz Yol (Gemini)
/// RoadChunk prefabini Inspector'dan bagla.
/// RoadChunk Scale X = 1.6 (genislik 16 birim = xLimit*2)
/// chunkLength = 50
/// </summary>
public class ChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Transform  playerTransform;
    public int        initialChunks = 5;
    public float      chunkLength   = 50f;

    float spawnZ = 0f;
    Queue<GameObject> activeChunks = new Queue<GameObject>();

    void Start()
    {
        for (int i = 0; i < initialChunks; i++) SpawnChunk();
    }

    void Update()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.z - (chunkLength * 1.5f) > (spawnZ - (initialChunks * chunkLength)))
        {
            SpawnChunk();
            DeleteOldChunk();
        }
    }

    void SpawnChunk()
    {
        GameObject c = Instantiate(chunkPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        c.transform.SetParent(this.transform);
        activeChunks.Enqueue(c);
        spawnZ += chunkLength;
    }

    void DeleteOldChunk()
    {
        Destroy(activeChunks.Dequeue());
    }
}
```

## Commanderdata.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Komutan Verisi v1 (Claude)
///
/// Her komutan bir ScriptableObject'tir.
/// Assets > Create > TopEndWar > CommanderData
///
/// PlayerStats bu dosyadan stat okur.
/// Tier tabloları burada tutulur — PlayerController'daki sabit diziler kaldırıldı.
/// </summary>
[CreateAssetMenu(fileName = "Commander_", menuName = "TopEndWar/CommanderData")]
public class CommanderData : ScriptableObject
{
    [Header("Kimlik")]
    public string commanderName   = "Gonullu Er";
    public Sprite portrait;
    [TextArea(2, 4)]
    public string lore            = "";

    [Header("Tier Bazli Istatistikler (5 deger = Tier 1-5)")]
    [Tooltip("Tier 1'den 5'e temel hasar degerleri")]
    public float[] baseDMG        = { 60f, 95f, 145f, 210f, 300f };

    [Tooltip("Tier 1'den 5'e atisHizi (atis/saniye)")]
    public float[] baseFireRate   = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };

    [Tooltip("Tier 1'den 5'e temel HP")]
    public int[]   baseHP         = { 500, 700, 950, 1200, 1500 };

    [Header("Ozel Mekanik")]
    public CommanderSpecialty specialty = CommanderSpecialty.Assault;
    public ArmySynergy armySynergy     = ArmySynergy.Hybrid;

    [Tooltip("Komutan sinerjisi: asker turune gore hasar carpani")]
    [Range(1f, 1.5f)]
    public float armyDamageMultiplier  = 1.0f;

    [Header("Kilit Kosulu")]
    [Tooltip("Hangi dunya bitmeli? 0 = baslangictan acik")]
    public int requiredWorldID         = 0;

    [Header("Gorsel Evrim (Tier basi model/aura)")]
    public GameObject[] tierModels;        // 5 eleman, Tier 1-5
    public ParticleSystem[] tierAuras;     // 5 eleman

    /// <summary>Verilen tier icin guveli temel hasar degerini dondurur.</summary>
    public float GetBaseDMG(int tier)
        => baseDMG[Mathf.Clamp(tier - 1, 0, 4)];

    /// <summary>Verilen tier icin temel atis hizini dondurur.</summary>
    public float GetBaseFireRate(int tier)
        => baseFireRate[Mathf.Clamp(tier - 1, 0, 4)];

    /// <summary>Verilen tier icin temel HP'yi dondurur.</summary>
    public int GetBaseHP(int tier)
        => baseHP[Mathf.Clamp(tier - 1, 0, 4)];
}

public enum CommanderSpecialty
{
    Assault,    // Dengeli — baslangic komutani
    Sniper,     // Yuksek hasar, yavash atis
    Support,    // Yuksek HP, dusuk hasar, ordu guclendirir
    Swarm,      // Cok mermi, dusuk tek mermi hasari
}

public enum ArmySynergy
{
    Piyade,
    Mekanik,
    Teknoloji,
    Hybrid,
}
```

## Damagepopup.cs

```csharp
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Top End War — Hasar Popup (Claude)
///
/// UNITY KURULUM:
///   ObjectPooler → Pools listesine "DamagePopup" tag'i ekle, size=30, prefab=null.
///   (Bu script kendi GameObject'ini yönetiyor — prefab gerekmez, DamagePopupPool.cs halleder.)
///
///   VEYA: DamagePopupPool.cs'yi Hierarchy'e ekle, o ObjectPooler'ı bypass eder.
///
/// KULLANIM (Enemy.TakeDamage içinden):
///   DamagePopupPool.Show(transform.position, dmg, hitColor);
///
/// ÖZELLİKLER:
///   - Renk kodlu: Komutan=mor, Piyade=yeşil, Mekanik=gri, Teknoloji=mavi, Boss hit=kırmızı
///   - Hızlı ateşlerde üst üste gelmez: random X offset
///   - DOTween ile yukarı kayar + fade
///   - Büyük hasar (crit) daha büyük font
/// </summary>
public class DamagePopup : MonoBehaviour
{
    TextMeshPro _tmp;
    Canvas      _canvas;

    // Singleton pool — ObjectPooler yerine basit stack
    static DamagePopup[] _pool;
    static int           _poolHead = 0;
    const  int           POOL_SIZE = 30;
    static bool          _initialized = false;

    // ── Başlatma ─────────────────────────────────────────────────────────
    public static void Init()
    {
        if (_initialized) return;
        _pool = new DamagePopup[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var go = new GameObject("DmgPopup_" + i);
            DontDestroyOnLoad(go);
            go.SetActive(false);
            _pool[i] = go.AddComponent<DamagePopup>();
            _pool[i].Build();
        }
        _initialized = true;
    }

    // ── Göster ───────────────────────────────────────────────────────────
    /// <summary>
    /// worldPos: düşmanın dünya konumu.
    /// damage: gösterilecek sayı.
    /// color: vuranın rengi (Bullet.bulletColor veya sabit renk).
    /// isCrit: büyük hasar mı? (çarpan ≥ 2.0 ise true gönder)
    /// </summary>
    public static void Show(Vector3 worldPos, int damage, Color color, bool isCrit = false)
    {
        if (!_initialized) Init();

        DamagePopup p = _pool[_poolHead % POOL_SIZE];
        _poolHead++;

        p.gameObject.SetActive(false); // önce kapat (aktif tweeni durdur)
        p.gameObject.SetActive(true);

        // Konum: random X offset — üst üste binmesin
        Vector3 pos = worldPos + new Vector3(
            Random.Range(-0.6f, 0.6f), 1.5f, Random.Range(-0.3f, 0.3f));
        p.transform.position = pos;

        // Kameraya bak
        if (Camera.main != null)
            p.transform.LookAt(p.transform.position + Camera.main.transform.forward);

        // Metin ve görünüm
        p._tmp.text      = damage.ToString();
        p._tmp.color     = color;
        p._tmp.fontSize  = isCrit ? 5.5f : 3.5f;
        p._tmp.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;

        // Animasyon: yukarı kayma + fade
        p.transform.DOKill();
        p._tmp.DOKill();
        p.transform.DOMove(pos + Vector3.up * (isCrit ? 2.2f : 1.5f), isCrit ? 0.9f : 0.7f)
            .SetEase(Ease.OutCubic);
        p._tmp.DOFade(0f, isCrit ? 0.9f : 0.7f)
            .SetEase(Ease.InCubic)
            .OnComplete(() => p.gameObject.SetActive(false));
    }

    // ── Renk Yardımcısı (dışarıdan çağrılır) ─────────────────────────────
    public static Color GetColor(string hitter)
    {
        return hitter switch
        {
            "Commander" => new Color(0.6f, 0.1f, 1.0f),   // mor
            "Piyade"    => new Color(0.2f, 0.85f, 0.2f),   // yeşil
            "Mekanik"   => new Color(0.65f, 0.65f, 0.65f), // gri
            "Teknoloji" => new Color(0.2f, 0.5f, 1.0f),    // mavi
            "Boss"      => new Color(1f, 0.2f, 0.1f),      // kırmızı
            _           => Color.white
        };
    }

    // ── İç yapı ──────────────────────────────────────────────────────────
    void Build()
    {
        // WorldSpace Canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.WorldSpace;
        _canvas.sortingOrder = 20;
        var cr = GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(3f, 1f);

        // TextMeshPro
        var go = new GameObject("T");
        go.transform.SetParent(transform, false);
        _tmp = go.AddComponent<TextMeshPro>();
        _tmp.alignment      = TextAlignmentOptions.Center;
        _tmp.fontSize       = 3.5f;
        _tmp.fontStyle      = FontStyles.Bold;
        _tmp.color          = Color.white;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta         = new Vector2(3f, 1f);
        r.localPosition     = Vector3.zero;
    }
}
```

## DifficultyManager.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Zorluk Yoneticisi v4
///
/// Vertical slice icin compatibility shim:
/// - API korunur
/// - compile uyumu bozulmaz
/// - player gucune gore gizli scaling KAPALI
/// - fixed difficulty varsayilan
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    public struct EnemyStats
    {
        public int   Health;
        public int   Damage;
        public float Speed;
        public int   CPReward;

        public EnemyStats(int health, int damage, float speed, int cpReward)
        {
            Health   = health;
            Damage   = damage;
            Speed    = speed;
            CPReward = cpReward;
        }
    }

    [Header("Konfigurasyon (ProgressionConfig SO)")]
    public ProgressionConfig config;

    [Header("Temel Dusman Degerleri (Legacy fallback)")]
    public float baseEnemyHP     = 100f;
    public float baseEnemySpeed  = 4.5f;
    public float baseEnemyDamage = 25f;
    public int   baseEnemyReward = 15;

    [Header("Legacy Helper")]
    [Range(0f, 0.3f)]
    public float pityZoneFraction = 0.15f;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] private float _currentMultiplier  = 1f;
    [SerializeField] private float _smoothedPowerRatio = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Vertical slice: fixed difficulty.
    /// Bu metod API uyumu icin korunur ama her zaman 1 dondurur.
    /// </summary>
    public float CalculateMultiplier(float z, int playerCP, float expectedCP)
    {
        _smoothedPowerRatio = 1f;
        _currentMultiplier  = 1f;

        // Event imzasi bozulmasin
        GameEvents.OnDifficultyChanged?.Invoke(_currentMultiplier, _smoothedPowerRatio);
        return _currentMultiplier;
    }

    /// <summary>
    /// Legacy fallback stats.
    /// Config-driven spawn varsa zaten kullanilmamali.
    /// </summary>
    public EnemyStats GetScaledEnemyStats()
    {
        return new EnemyStats(
            health:   Mathf.RoundToInt(baseEnemyHP),
            damage:   Mathf.RoundToInt(baseEnemyDamage),
            speed:    baseEnemySpeed,
            cpReward: baseEnemyReward
        );
    }

    /// <summary>
    /// Slice'ta risk gate/pity zone aktif degil.
    /// API uyumu icin false doner.
    /// </summary>
    public bool IsInPityZone(float bossDistance)
    {
        return false;
    }

    public float PlayerPowerRatio => 1f;

    public float GetCurrentMultiplier()  => _currentMultiplier;
    public float GetSmoothedPowerRatio() => _smoothedPowerRatio;

    /// <summary>
    /// API uyumu icin korunur. Fixed difficulty'de aktif etkisi yoktur.
    /// </summary>
    public void SetExpectedCP(float expected)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.SetExpectedCP(Mathf.Max(1f, expected));
    }
}
```

## Economyconfig.cs

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "EconomyConfig", menuName = "TopEndWar/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Slice Feature Toggles")]
    public bool enableOfflineEarnings = false;

    [Tooltip("false ise slot upgrade sadece Gold harcar")]
    public bool useTechCoreForSlotUpgrades = false;

    public bool enablePitySystem = false;

    [Header("Slot Yukseltme — Altin Maliyeti")]
    public float slotGoldCostBase = 180f;
    public float slotGoldCostGrowth = 1.22f;

    [Header("Slot Yukseltme — Tech Core Maliyeti (Bantli)")]
    public int[] tcBandFromLevel = { 1, 6, 11, 16, 21, 31 };
    public int[] tcBandToLevel   = { 5, 10, 15, 20, 30, 50 };
    public int[] tcBandCost      = { 1, 2, 3, 4, 5, 7 };

    [Header("Stage Odulu Formulü")]
    public float goldBase = 120f;
    public float goldPerStage = 10f;
    public float goldDpsFactor = 0.20f;

    [Range(0f, 1f)]
    public float midLootFraction = 0.35f;

    [Header("Offline Gelir")]
    public int baseOfflineRate = 50;
    [Range(8f, 24f)]
    public float offlineCapHours = 15f;

    [Header("Reklam Politikasi")]
    public int reviveAdsPerRun = 1;
    public int doubleGoldAdsDaily = 3;
    public int bonusChestAdsDaily = 4;

    [Header("Pity Timer (Acima Sayaci)")]
    public int pityStagThreshold = 20;

    public int GetSlotGoldCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        return Mathf.RoundToInt(slotGoldCostBase * Mathf.Pow(slotGoldCostGrowth, level - 1));
    }

    public int GetSlotTechCoreCost(int level)
    {
        if (!useTechCoreForSlotUpgrades)
            return 0;

        level = Mathf.Clamp(level, 1, 50);
        for (int i = 0; i < tcBandFromLevel.Length; i++)
            if (level >= tcBandFromLevel[i] && level <= tcBandToLevel[i])
                return tcBandCost[i];

        return tcBandCost.Length > 0 ? tcBandCost[tcBandCost.Length - 1] : 0;
    }

    public int GetGoldReward(int stageNumber, float targetDps)
        => Mathf.RoundToInt(goldBase + goldPerStage * stageNumber + goldDpsFactor * targetDps);

    public int GetMidLootGold(int stageNumber, float targetDps)
        => Mathf.RoundToInt(GetGoldReward(stageNumber, targetDps) * midLootFraction);

#if UNITY_EDITOR
    void OnValidate()
    {
        slotGoldCostBase = Mathf.Max(1f, slotGoldCostBase);
        slotGoldCostGrowth = Mathf.Max(1.01f, slotGoldCostGrowth);
        goldBase = Mathf.Max(0f, goldBase);
        goldPerStage = Mathf.Max(0f, goldPerStage);
        goldDpsFactor = Mathf.Max(0f, goldDpsFactor);
        midLootFraction = Mathf.Clamp01(midLootFraction);
        baseOfflineRate = Mathf.Max(0, baseOfflineRate);
        offlineCapHours = Mathf.Clamp(offlineCapHours, 8f, 24f);
        pityStagThreshold = Mathf.Max(1, pityStagThreshold);

        if (tcBandFromLevel == null) tcBandFromLevel = new int[0];
        if (tcBandToLevel == null) tcBandToLevel = new int[0];
        if (tcBandCost == null) tcBandCost = new int[0];
    }
#endif
}
```

## EconomyManager.cs

```csharp
using UnityEngine;
using System;

/// <summary>
/// Top End War — Ekonomi Yoneticisi v2 (Claude)
///
/// v2: EconomyConfig SO entegre edildi.
///   SlotUpgrade() — Gold + TechCore harcar, basarili ise true dondurur.
///   Pity timer — N bos stage sonra Basic Scroll garantisi.
///   Reklam politikasi — TechCore ve Hard Currency reklamla bypass edilemez.
///
/// Para birimleri: Altin (Soft) | TechCore (Skill-based) | Kristal (Hard)
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Konfigurasyon")]
    public EconomyConfig config;

    // ── Para Birimleri ────────────────────────────────────────────────────
    public int Gold      { get; private set; }
    public int TechCore  { get; private set; }
    public int Crystal   { get; private set; }

    // ── Offline Gelir ─────────────────────────────────────────────────────
    private int _bonusOfflineRate = 0;

    // ── Pity Sayaci ───────────────────────────────────────────────────────
    private int _emptyStageCount = 0;  // Scroll dusmeyen stage sayisi

    // ── Gunluk Reklam Sayaclari ───────────────────────────────────────────
    private int  _doubleGoldAdsToday = 0;
    private int  _bonusChestAdsToday = 0;
    private string _lastAdResetDate  = "";

    // ── PlayerPrefs Anahtarlari ───────────────────────────────────────────
    const string KEY_GOLD         = "Economy_Gold";
    const string KEY_TECHCORE     = "Economy_TechCore";
    const string KEY_CRYSTAL      = "Economy_Crystal";
    const string KEY_BONUS_RATE   = "Economy_BonusRate";
    const string KEY_LAST_SAVE    = "Economy_LastSaveTime";
    const string KEY_PITY         = "Economy_PityCount";
    const string KEY_AD_DATE      = "Economy_AdResetDate";
    const string KEY_AD_DGOLD     = "Economy_DoubleGoldAds";
    const string KEY_AD_CHEST     = "Economy_BonusChestAds";

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
        ResetDailyAdsIfNeeded();
        CollectOfflineEarnings();
    }

    void OnApplicationPause(bool paused) { if (paused) SaveLastTime(); }
    void OnApplicationQuit()             { SaveLastTime(); }

    // ── Altin ─────────────────────────────────────────────────────────────
    public void AddGold(int amount)
    {
        Gold = Mathf.Max(0, Gold + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── TechCore ─────────────────────────────────────────────────────────
    public void AddTechCore(int amount)
    {
        TechCore = Mathf.Max(0, TechCore + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendTechCore(int amount)
    {
        if (TechCore < amount) return false;
        TechCore -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── Kristal ───────────────────────────────────────────────────────────
    public void AddCrystal(int amount)
    {
        Crystal = Mathf.Max(0, Crystal + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendCrystal(int amount)
    {
        if (Crystal < amount) return false;
        Crystal -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── Slot Yukseltme ────────────────────────────────────────────────────
    /// <summary>
    /// Belirtilen slot seviyesi icin Gold + TechCore harcar.
    /// EconomyConfig formulune gore maliyet hesaplanir.
    /// Basarili ise true, yetersiz kaynak ise false dondurur.
    ///
    /// currentLevel: MEVCUT seviye. Yeni seviye = currentLevel + 1.
    /// </summary>
    public bool TryUpgradeSlot(int currentLevel, out string failReason)
{
    failReason = "";
    if (config == null) { failReason = "EconomyConfig atanmamis."; return false; }

    int nextLevel = currentLevel + 1;
    if (nextLevel > 50) { failReason = "Maksimum slot seviyesi."; return false; }

    int goldCost = config.GetSlotGoldCost(currentLevel);
    int tcCost   = config.GetSlotTechCoreCost(currentLevel);

    if (Gold < goldCost)
    {
        failReason = $"Yetersiz altin. Gerekli: {goldCost}, Mevcut: {Gold}";
        return false;
    }

    if (config.useTechCoreForSlotUpgrades && TechCore < tcCost)
    {
        failReason = $"Yetersiz TechCore. Gerekli: {tcCost}, Mevcut: {TechCore}";
        return false;
    }

    Gold -= goldCost;

    if (config.useTechCoreForSlotUpgrades)
        TechCore -= tcCost;

    Save();
    OnEconomyChanged?.Invoke();
    Debug.Log($"[Economy] Slot Lv{currentLevel}→{nextLevel} | -{goldCost}G" +
              (config.useTechCoreForSlotUpgrades ? $" -{tcCost}TC" : ""));
    return true;
}

    /// <summary>Bir sonraki slot yukseltmesinin maliyetini dondurur (bilgi icin).</summary>
    public (int gold, int tc) GetUpgradeCost(int currentLevel)
    {
        if (config == null) return (0, 0);
        return (config.GetSlotGoldCost(currentLevel), config.GetSlotTechCoreCost(currentLevel));
    }

    // ── Pity Timer ────────────────────────────────────────────────────────
    /// <summary>
    /// Scroll dusmeyen her stage'de cagirilir.
    /// Esige ulasilirsa Basic Scroll garantisi tetiklenir.
    /// </summary>
    public void RegisterEmptyStage()
{
    if (config != null && !config.enablePitySystem)
        return;

    _emptyStageCount++;
    int threshold = config != null ? config.pityStagThreshold : 20;

    if (_emptyStageCount >= threshold)
    {
        _emptyStageCount = 0;
        OnGuaranteedScroll?.Invoke();
        Debug.Log("[Economy] Pity Timer: Guaranteed Basic Scroll!");
    }

    PlayerPrefs.SetInt(KEY_PITY, _emptyStageCount);
    PlayerPrefs.Save();
}

    public void ResetPityCounter()
    {
        _emptyStageCount = 0;
        PlayerPrefs.SetInt(KEY_PITY, 0);
    }

    // ── Reklam ───────────────────────────────────────────────────────────
    public bool TryDoubleGoldAd()
    {
        int limit = config != null ? config.doubleGoldAdsDaily : 3;
        if (_doubleGoldAdsToday >= limit) return false;
        _doubleGoldAdsToday++;
        SaveAds();
        return true;
    }

    public bool TryBonusChestAd()
    {
        int limit = config != null ? config.bonusChestAdsDaily : 4;
        if (_bonusChestAdsToday >= limit) return false;
        _bonusChestAdsToday++;
        SaveAds();
        return true;
    }

    // TechCore reklamla alinamaz — bu metot kasitli olarak yok.

    // ── Offline Gelir ─────────────────────────────────────────────────────
    public void AddOfflineRate(int amountPerHour)
{
    if (config != null && !config.enableOfflineEarnings)
        return;

    _bonusOfflineRate += amountPerHour;
    PlayerPrefs.SetInt(KEY_BONUS_RATE, _bonusOfflineRate);
    PlayerPrefs.Save();
}

    public int GetTotalOfflineRate()
    {
        int baseRate = config != null ? config.baseOfflineRate : 50;
        return baseRate + _bonusOfflineRate;
    }

   void CollectOfflineEarnings()
{
    if (config != null && !config.enableOfflineEarnings)
        return;

    string savedTime = PlayerPrefs.GetString(KEY_LAST_SAVE, "");
    if (string.IsNullOrEmpty(savedTime)) return;
    if (!DateTime.TryParse(savedTime, out DateTime lastSave)) return;

    float capHours = config != null ? config.offlineCapHours : 15f;
    double hoursGone = Mathf.Min((float)(DateTime.UtcNow - lastSave).TotalHours, capHours);
    if (hoursGone < 0.01f) return;

    int earned = Mathf.RoundToInt((float)hoursGone * GetTotalOfflineRate());
    if (earned <= 0) return;

    Gold += earned;
    Save();
    Debug.Log($"[Economy] Offline: +{earned} Altin ({hoursGone:F1} saat)");
    OnOfflineEarningCollected?.Invoke(earned);
}

    void SaveLastTime()
    {
        PlayerPrefs.SetString(KEY_LAST_SAVE, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    // ── Gunluk Reklam Sifirla ─────────────────────────────────────────────
    void ResetDailyAdsIfNeeded()
    {
        string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        if (_lastAdResetDate != today)
        {
            _doubleGoldAdsToday = 0;
            _bonusChestAdsToday = 0;
            _lastAdResetDate    = today;
            SaveAds();
        }
    }

    void SaveAds()
    {
        PlayerPrefs.SetString(KEY_AD_DATE,  _lastAdResetDate);
        PlayerPrefs.SetInt(KEY_AD_DGOLD,    _doubleGoldAdsToday);
        PlayerPrefs.SetInt(KEY_AD_CHEST,    _bonusChestAdsToday);
        PlayerPrefs.Save();
    }

    // ── Save / Load ───────────────────────────────────────────────────────
    void Save()
    {
        PlayerPrefs.SetInt(KEY_GOLD,       Gold);
        PlayerPrefs.SetInt(KEY_TECHCORE,   TechCore);
        PlayerPrefs.SetInt(KEY_CRYSTAL,    Crystal);
        PlayerPrefs.SetInt(KEY_BONUS_RATE, _bonusOfflineRate);
        PlayerPrefs.Save();
    }

    void Load()
    {
        Gold              = PlayerPrefs.GetInt(KEY_GOLD,       0);
        TechCore          = PlayerPrefs.GetInt(KEY_TECHCORE,   0);
        Crystal           = PlayerPrefs.GetInt(KEY_CRYSTAL,    0);
        _bonusOfflineRate = PlayerPrefs.GetInt(KEY_BONUS_RATE, 0);
        _emptyStageCount  = PlayerPrefs.GetInt(KEY_PITY,       0);
        _lastAdResetDate  = PlayerPrefs.GetString(KEY_AD_DATE, "");
        _doubleGoldAdsToday = PlayerPrefs.GetInt(KEY_AD_DGOLD, 0);
        _bonusChestAdsToday = PlayerPrefs.GetInt(KEY_AD_CHEST, 0);
    }

    // ── Olaylar ───────────────────────────────────────────────────────────
    public static Action      OnEconomyChanged;
    public static Action<int> OnOfflineEarningCollected;
    public static Action      OnGuaranteedScroll;
}
```

## Enemy.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dusman v7
///
/// PATCH OZETI:
/// - Eski calisan davranislar KORUNDU
/// - Reservation / Threat eklendi
/// - ConfigureCombat dolduruldu
/// - Elite görsel tonu eklendi
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    [Header("Combat Flags (Debug / Fallback)")]
    [SerializeField] int _armor = 0;
    [SerializeField] bool _isElite = false;

    int _maxHealth;
    int _currentHealth;
    int _contactDamage;
    float _moveSpeed;
    int _cpReward;
    bool _initialized = false;
    bool _isDead = false;
    bool _hasDamagedPlayer = false;

    Renderer _bodyRenderer;
    EnemyHealthBar _hpBar;

    float _lastSepTime = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    // Reservation / threat
    int _reservationCount = 0;
    int _reservationCap = 2;
    float _threatWeight = 1f;
    Color _baseColor = Color.white;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();

        UseDefaults();
    }

    void OnEnable()
    {
        _isDead = false;
        _hasDamagedPlayer = false;
        _separationVec = Vector3.zero;
        _reservationCount = 0;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        if (!_initialized)
            AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth = stats.Health;
        _currentHealth = _maxHealth;
        _contactDamage = stats.Damage;
        _moveSpeed = stats.Speed;
        _cpReward = stats.CPReward;
        _initialized = true;
        _isDead = false;
        _hasDamagedPlayer = false;
        _reservationCount = 0;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats, int armor, bool isElite)
    {
        Initialize(stats);
        ConfigureCombat(armor, isElite);
    }

    void AutoInit()
    {
        UseDefaults();
        _initialized = true;
    }

    void UseDefaults()
    {
        _maxHealth = _currentHealth = 100;
        _contactDamage = 25;
        _moveSpeed = 4.5f;
        _cpReward = 15;
        _armor = 0;
        _isElite = false;
        _reservationCap = 2;
        _threatWeight = 1f;
        _baseColor = Color.white;
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float pZ = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime = Time.time;
        }

        pos += _separationVec * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f)
            gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep = Vector3.zero;
        int count = 0;

        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;

            Vector3 away = pos - col.transform.position;
            away.y = 0f;

            if (away.magnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, 0f).normalized * 0.1f;

            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        return count > 0 ? (sep / count) * 3.5f : Vector3.zero;
    }

    public void TakeDamage(int rawDamage, int armorPenValue = 0, float eliteMultiplier = 1f, Color? hitColor = null)
    {
        if (_isDead) return;

        int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPenValue));
        int finalDamage = Mathf.Max(1, rawDamage - effectiveArmor);

        if (_isElite)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, eliteMultiplier)));

        _currentHealth -= finalDamage;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.red;

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), 0.1f);

        bool isCrit = finalDamage > 200;
        Color popupColor = hitColor ?? DamagePopup.GetColor("Commander");
        DamagePopup.Show(transform.position, finalDamage, popupColor, isCrit);

        if (_currentHealth <= 0)
            Die();
    }

    public void ConfigureCombat(int armor, bool isElite)
    {
        _armor = Mathf.Max(0, armor);
        _isElite = isElite;

        _reservationCap = _isElite ? 3 : 2;
        _threatWeight = _isElite ? 1.35f : 1f;
        _baseColor = _isElite ? new Color(1f, 0.92f, 0.35f) : Color.white;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;
    }

    public bool TryReserve()
    {
        if (_reservationCount >= _reservationCap) return false;
        _reservationCount++;
        return true;
    }

    public void ReleaseReservation()
    {
        _reservationCount = Mathf.Max(0, _reservationCount - 1);
    }

    void ResetColor()
    {
        if (!_isDead && _bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;
    }

    void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _initialized = false;
        _reservationCount = 0;
        CancelInvoke();

        PlayerStats.Instance?.AddCPFromKill(_cpReward);
        SaveManager.Instance?.RegisterKill();
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isDead) return;

        SoldierUnit soldier = other.GetComponent<SoldierUnit>();
        if (soldier != null)
        {
            soldier.TakeDamage(_contactDamage);
            Die();
            return;
        }

        if (!other.CompareTag("Player") || _hasDamagedPlayer) return;

        _hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

    void OnDisable()
    {
        CancelInvoke();
        _initialized = false;
        _reservationCount = 0;
    }

    public int Armor => _armor;
    public bool IsElite => _isElite;
    public int ReservationCount => _reservationCount;
    public bool CanAcceptReservation => _reservationCount < _reservationCap;
    public float ThreatWeight => _threatWeight;
    public float HealthRatio => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 1f;
}
```

## Enemyarchetypeconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dusman Arketip Konfigurasyonu v2
///
/// Runtime baglantisi ileride WaveConfig/SpawnEntry tarafindan verilir.
/// Simdilik guvenli data asset'i olarak clamp'lenir.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TopEndWar/EnemyArchetypeConfig")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string enemyId = "trooper";
    public string enemyName = "Trooper";
    public EnemyClass enemyClass = EnemyClass.Normal;

    [Header("Stat Faktörleri")]
    [Tooltip("HP = StageConfig.targetDps * hpFactor")]
    public float hpFactor = 0.90f;

    [Tooltip("Zirh degeri. Silah ArmorPen bu degerden dusulur.")]
    public int armor = 0;

    [Tooltip("Hareket hizi (birim/saniye).")]
    public float moveSpeed = 4.5f;

    [Tooltip("Temas hasari")]
    public int contactDamage = 30;

    [Header("Davranis")]
    public EnemyThreatType threatType = EnemyThreatType.Standard;
    public bool canSpawnInRunner = true;
    public bool canSpawnInAnchor = true;

    [Header("Odül")]
    [Tooltip("CP reward = targetDps * cpRewardFactor")]
    public float cpRewardFactor = 0.06f;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public RuntimeAnimatorController animatorOverride;

    public int GetHP(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * hpFactor));

    public int GetCPReward(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * cpRewardFactor));

    // DEĞİŞİKLİK
    public bool IsEliteLike =>
        enemyClass == EnemyClass.Elite ||
        threatType == EnemyThreatType.ElitePressure;

#if UNITY_EDITOR
    void OnValidate()
    {
        hpFactor = Mathf.Max(0.1f, hpFactor);
        armor = Mathf.Max(0, armor);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        contactDamage = Mathf.Max(0, contactDamage);
        cpRewardFactor = Mathf.Max(0f, cpRewardFactor);

        if (!string.IsNullOrEmpty(enemyId))
            name = $"Enemy_{enemyClass}_{enemyId}";
    }
#endif
}

public enum EnemyClass
{
    Normal,
    Elite,
    MiniBoss,
    BossSupport,
}

public enum EnemyThreatType
{
    Standard,
    PackPressure,
    Priority,
    Durable,
    ElitePressure,
    Backline,
}
```

## EnemyHealthBar.cs

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top End War — Dusman HP Bari (Claude)
/// Enemy prefabina ekle — veya Enemy.Awake() otomatik ekler.
/// Kod kendi Canvas'ini olusturur, elle kurulum yok.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public float barWidth   = 1.2f;
    public float barHeight  = 0.15f;
    public float barYOffset = 1.8f;

    public Color fullColor = new Color(0.15f, 0.85f, 0.15f);
    public Color halfColor = new Color(0.95f, 0.75f, 0.05f);
    public Color lowColor  = new Color(0.9f,  0.15f, 0.15f);

    Canvas    _canvas;
    Image     _fillImage;
    int       _maxHP;
    Transform _camTransform;

    void Awake()
    {
        BuildBar();
        _camTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        if (_canvas == null || _camTransform == null) return;
        _canvas.transform.position = transform.position + Vector3.up * barYOffset;
        _canvas.transform.LookAt(_canvas.transform.position + _camTransform.forward);
    }

    public void Init(int maxHP)
    {
        _maxHP = Mathf.Max(1, maxHP);
        UpdateBar(maxHP);
    }

    public void UpdateBar(int currentHP)
    {
        if (_fillImage == null) return;
        float ratio = (float)Mathf.Max(0, currentHP) / _maxHP;
        _fillImage.fillAmount = ratio;

        if      (ratio > 0.6f) _fillImage.color = fullColor;
        else if (ratio > 0.3f) _fillImage.color = Color.Lerp(halfColor, fullColor, (ratio - 0.3f) / 0.3f);
        else                   _fillImage.color = Color.Lerp(lowColor,  halfColor,  ratio / 0.3f);

        if (_canvas != null) _canvas.gameObject.SetActive(currentHP > 0);
    }

    void BuildBar()
    {
        GameObject cObj = new GameObject("HPBarCanvas");
        cObj.transform.SetParent(transform);
        cObj.transform.localPosition = Vector3.up * barYOffset;

        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 10;

        RectTransform cr = cObj.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(barWidth, barHeight * 2f);

        // Arka plan
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(cObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;

        // Dolucu
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(cObj.transform, false);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.color = fullColor;
        RectTransform fillR = fillObj.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one;
        fillR.offsetMin = Vector2.zero; fillR.offsetMax = Vector2.zero;
    }
}
```

## EquipmentData.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ekipman Verisi v5
///
/// AYRIM:
///   - WeaponArchetypeConfig = silah ailesinin dogasi
///   - EquipmentData         = oyuncunun sahip oldugu item/equip
///
/// Ornek:
///   Archetype: Assault
///   Item:      Rare Assault Rifle +10% fire rate
/// </summary>

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Shoulder,
    Knee,
    Boots,
    Necklace,
    Ring,
}

public enum WeaponType
{
    None,
    Pistol,
    Rifle,
    Automatic,
    Sniper,
    Shotgun,
    Launcher,
    Beam,
}

public enum ArmorType
{
    None,
    Light,
    Medium,
    Heavy,
    Shield,
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Kimlik")]
    public string equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot = EquipmentSlot.Weapon;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description = "";

    [Header("Silah Referansi (yeni sistem)")]
    public WeaponArchetypeConfig weaponArchetype;

    [Header("Legacy Silah Turu (geriye uyumluluk icin)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Turu (sadece Armor/Shoulder/Knee)")]
    public ArmorType armorType = ArmorType.None;

    [Header("CP Gear Score Bonusu")]
    public int baseCPBonus = 0;

    [Header("CP Carpani (kolye/yuzuk — Gear Score icin)")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    [Header("Atis Hizi Carpani (sadece silah itemleri)")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silah itemleri)")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    [Header("Global Hasar Carpani (yuzuk/kolye — DPS'e etki eder)")]
    [Range(1f, 2f)]
    public float globalDmgMultiplier = 1f;

    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    public int commanderHPBonus = 0;

    [Header("Mermi Spread Bonusu (sadece silah itemleri)")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    [Header("Ek Combat Bonuslari (item modifier)")]
    [Min(0)] public int armorPen = 0;
    [Min(0)] public int pierceCount = 0;
    [Range(1f, 3f)] public float eliteDamageMultiplier = 1f;

    [Header("Nadir (rarity) 1=Gri 2=Yesil 3=Mavi 4=Mor 5=Altin")]
    [Range(1, 5)]
    public int rarity = 1;

    public bool IsWeapon => slot == EquipmentSlot.Weapon;

    public string GetTypeDescription()
    {
        if (slot == EquipmentSlot.Weapon && weaponArchetype != null)
        {
            return weaponArchetype.family switch
            {
                WeaponFamily.Assault => "Assault: Dengeli, cok yonlu",
                WeaponFamily.SMG => "SMG: Hizli, swarm odakli",
                WeaponFamily.Sniper => "Sniper: Tek hedef, armor odakli",
                WeaponFamily.Shotgun => "Pompa: Yakin mesafe patlayici baski",
                WeaponFamily.Launcher => "Launcher: Alan hasari ve pack temizleme",
                WeaponFamily.Beam => "Beam: Surekli baski ve elite/boss odagi",
                _ => "Silah",
            };
        }

        return weaponType switch
        {
            WeaponType.Pistol => "Tabanca: Hizli, kisa menzilli",
            WeaponType.Rifle => "Assault: Dengeli, cok yonlu",
            WeaponType.Automatic => "SMG: Hizli, swarm odakli",
            WeaponType.Sniper => "Sniper: Tek hedef, armor odakli",
            WeaponType.Shotgun => "Pompa: Yakin mesafe",
            WeaponType.Launcher => "Launcher: Alan hasari",
            WeaponType.Beam => "Beam: Surekli enerji baskisi",
            _ => armorType switch
            {
                ArmorType.Light => "Hafif Zirh",
                ArmorType.Medium => "Orta Zirh",
                ArmorType.Heavy => "Agir Zirh",
                ArmorType.Shield => "Savunma odakli zirh etiketi",
                _ => "Aksesuar",
            }
        };
    }

    WeaponType MapFamilyToLegacyType(WeaponFamily family)
    {
        return family switch
        {
            WeaponFamily.Assault => WeaponType.Rifle,
            WeaponFamily.SMG => WeaponType.Automatic,
            WeaponFamily.Sniper => WeaponType.Sniper,
            WeaponFamily.Shotgun => WeaponType.Shotgun,
            WeaponFamily.Launcher => WeaponType.Launcher,
            WeaponFamily.Beam => WeaponType.Beam,
            _ => WeaponType.None,
        };
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        bool isWeapon = slot == EquipmentSlot.Weapon;
        bool isArmorLike = slot == EquipmentSlot.Armor ||
                           slot == EquipmentSlot.Shoulder ||
                           slot == EquipmentSlot.Knee;

        bool isNecklaceOrRing = slot == EquipmentSlot.Necklace || slot == EquipmentSlot.Ring;

        if (!isWeapon)
        {
            weaponArchetype = null;
            weaponType = WeaponType.None;
            fireRateMultiplier = 1f;
            damageMultiplier = 1f;
            spreadBonus = 0f;
            armorPen = 0;
            pierceCount = 0;
            eliteDamageMultiplier = 1f;
        }
        else if (weaponArchetype != null)
        {
            weaponType = MapFamilyToLegacyType(weaponArchetype.family);
        }

        if (!isArmorLike)
            armorType = ArmorType.None;

        if (!isNecklaceOrRing)
        {
            cpMultiplier = 1f;
            globalDmgMultiplier = 1f;
        }

        if (eliteDamageMultiplier < 1f)
            eliteDamageMultiplier = 1f;

        if (pierceCount < 0)
            pierceCount = 0;

        if (armorPen < 0)
            armorPen = 0;
    }
#endif
}
```

## Equipmentloadout.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ekipman Seti ScriptableObject v2
///
/// DEĞİŞİKLİK:
///   - Yanlis slot item'lari runtime'da equip edilmez
///   - Inspector'da OnValidate ile temizlenir
///   - TotalCPBonus, PlayerStats.CP mantigina daha yakin hesaplanir
/// </summary>
[CreateAssetMenu(fileName = "NewLoadout", menuName = "TopEndWar/Equipment Loadout")]
public class EquipmentLoadout : ScriptableObject
{
    [Header("Silah")]
    public EquipmentData weapon;

    [Header("Zırh")]
    public EquipmentData armor;

    [Header("Aksesuarlar")]
    public EquipmentData shoulder;
    public EquipmentData knee;
    public EquipmentData necklace;
    public EquipmentData ring;

    [Header("Pet")]
    public PetData pet;

    public void ApplyTo(PlayerStats ps)
    {
        if (ps == null) return;

        ps.equippedWeapon   = ValidateForSlot(weapon,   EquipmentSlot.Weapon,   "weapon");
        ps.equippedArmor    = ValidateForSlot(armor,    EquipmentSlot.Armor,    "armor");
        ps.equippedShoulder = ValidateForSlot(shoulder, EquipmentSlot.Shoulder, "shoulder");
        ps.equippedKnee     = ValidateForSlot(knee,     EquipmentSlot.Knee,     "knee");
        ps.equippedNecklace = ValidateForSlot(necklace, EquipmentSlot.Necklace, "necklace");
        ps.equippedRing     = ValidateForSlot(ring,     EquipmentSlot.Ring,     "ring");
        ps.equippedPet      = pet;
    }

    public void ReadFrom(PlayerStats ps)
    {
        if (ps == null) return;
        weapon   = ps.equippedWeapon;
        armor    = ps.equippedArmor;
        shoulder = ps.equippedShoulder;
        knee     = ps.equippedKnee;
        necklace = ps.equippedNecklace;
        ring     = ps.equippedRing;
        pet      = ps.equippedPet;
    }

    /// <summary>
    /// UI preview icin PlayerStats.CP mantigina yakin hesap.
    /// </summary>
    public int TotalCPBonus()
    {
        int total = 0;
        total += weapon   != null ? weapon.baseCPBonus   : 0;
        total += armor    != null ? armor.baseCPBonus    : 0;
        total += shoulder != null ? shoulder.baseCPBonus : 0;
        total += knee     != null ? knee.baseCPBonus     : 0;
        total += necklace != null ? necklace.baseCPBonus : 0;
        total += ring     != null ? ring.baseCPBonus     : 0;
        total += pet      != null ? pet.cpBonus          : 0;

        float mult = 1f;
        if (necklace != null) mult *= necklace.cpMultiplier;
        if (ring != null)     mult *= ring.cpMultiplier;

        return Mathf.RoundToInt(total * mult);
    }

    EquipmentData ValidateForSlot(EquipmentData item, EquipmentSlot expected, string label)
    {
        if (item == null) return null;

        if (item.slot == expected)
            return item;

        Debug.LogWarning(
            $"[EquipmentLoadout] {label} alaninda yanlis item var. " +
            $"Beklenen={expected}, Gelen={item.slot}, Item={item.equipmentName}");

        return null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        weapon   = Sanitize(weapon,   EquipmentSlot.Weapon);
        armor    = Sanitize(armor,    EquipmentSlot.Armor);
        shoulder = Sanitize(shoulder, EquipmentSlot.Shoulder);
        knee     = Sanitize(knee,     EquipmentSlot.Knee);
        necklace = Sanitize(necklace, EquipmentSlot.Necklace);
        ring     = Sanitize(ring,     EquipmentSlot.Ring);
    }

    EquipmentData Sanitize(EquipmentData item, EquipmentSlot expected)
    {
        if (item == null) return null;
        return item.slot == expected ? item : null;
    }
#endif
}
```

## Equipmentui.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top End War — Ekipman Menüsü (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "EquipmentUIManager" -> bu scripti ekle.
///   Kod kendi Canvas'ini oluşturur — elle kurulum yok.
///
/// KONTROL:
///   Klavye: E tuşu aç/kapat
///   Mobil: Sağ alttaki buton (GameHUD'a "EKİPMAN" butonu eklendi)
///
/// ÇALIŞMA PRENSİBİ:
///   Inspector'dan equippableItems listesine EquipmentData ScriptableObject'leri sürükle.
///   Slot'a tıklayınca o slot'un ekipmanı değişir.
///   Değişiklik anında PlayerStats'a yansır (Inspector referansı üzerinden).
///
/// NOT:
///   Şimdilik sadece Inspector'daki PlayerStats.equippedXxx alanlarını gösterir.
///   Gelecek: Chest/Summon sisteminden gelen envanter listesi buraya bağlanacak.
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [Header("Ekipmanlanabilir Itemlar (Inspector'dan ata)")]
    public EquipmentData[] availableWeapons;
    public EquipmentData[] availableArmors;
    public EquipmentData[] availableAccessories; // omuzluk, dizlik, kolye, yüzük

    bool   _open   = false;
    Canvas _canvas;
    GameObject _panel;

    // Slot butonları
    Button _weaponBtn, _armorBtn, _shoulderBtn, _kneeBtn, _necklaceBtn, _ringBtn;
    TextMeshProUGUI _weaponTxt, _armorTxt, _shoulderTxt, _kneeTxt, _necklaceTxt, _ringTxt;
    TextMeshProUGUI _statsText;

    // Seçim paneli
    GameObject      _pickPanel;
    EquipmentSlot   _currentSlot;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        BuildUI();
        _panel.SetActive(false);
    }

    void Update()
    {
        // E tuşu toggle
        if (Input.GetKeyDown(KeyCode.E))
            Toggle();
    }

    public void Toggle()
    {
        _open = !_open;
        _panel.SetActive(_open);
        Time.timeScale = _open ? 0f : 1f; // menü açıkken oyun durur
        if (_open) RefreshAll();
    }

    // ── UI Kurulumu ────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        var cObj = new GameObject("EquipmentCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 80;
        var cs = cObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cObj.AddComponent<GraphicRaycaster>();

        // Arkaplan panel
        _panel = new GameObject("EqPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        Stretch(_panel.GetComponent<RectTransform>());

        // Başlık
        MakeLabel(_panel, "EKIPMAN", new Vector2(0.5f, 1f), new Vector2(0, -80), 40,
            new Color(1f, 0.85f, 0f), FontStyles.Bold);

        // Kapat butonu
        MakeCloseBtn(_panel);

        // 6 slot - iki sütun 3er tane
        float startY = -180f;
        float stepY  = -155f;
        float leftX  = -240f;
        float rightX =  240f;

        (_weaponBtn,   _weaponTxt)   = MakeSlot(_panel, "SILAH",     new Vector2(leftX,  startY + stepY * 0), EquipmentSlot.Weapon);
        (_armorBtn,    _armorTxt)    = MakeSlot(_panel, "ZIRH",      new Vector2(leftX,  startY + stepY * 1), EquipmentSlot.Armor);
        (_shoulderBtn, _shoulderTxt) = MakeSlot(_panel, "OMUZLUK",   new Vector2(leftX,  startY + stepY * 2), EquipmentSlot.Shoulder);
        (_necklaceBtn, _necklaceTxt) = MakeSlot(_panel, "KOLYE",     new Vector2(rightX, startY + stepY * 0), EquipmentSlot.Necklace);
        (_kneeBtn,     _kneeTxt)     = MakeSlot(_panel, "DIZLIK",    new Vector2(rightX, startY + stepY * 1), EquipmentSlot.Knee);
        (_ringBtn,     _ringTxt)     = MakeSlot(_panel, "YUZUK",     new Vector2(rightX, startY + stepY * 2), EquipmentSlot.Ring);

        // Stat özeti
        var statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(_panel.transform, false);
        _statsText = statsObj.AddComponent<TextMeshProUGUI>();
        _statsText.alignment = TextAlignmentOptions.Center;
        _statsText.fontSize  = 22;
        _statsText.color     = new Color(0.8f, 0.8f, 0.8f);
        var sr = statsObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.1f, 0f); sr.anchorMax = new Vector2(0.9f, 0f);
        sr.anchoredPosition = new Vector2(0, 80); sr.sizeDelta = new Vector2(0, 120);

        // Seçim alt-paneli
        BuildPickPanel();
    }

    // Slot butonu oluştur
    (Button, TextMeshProUGUI) MakeSlot(GameObject parent, string label, Vector2 pos, EquipmentSlot slot)
    {
        var obj = new GameObject("Slot_" + label);
        obj.transform.SetParent(parent.transform, false);

        var bg = obj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.25f, 1f);
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Hover rengi
        var cols = btn.colors;
        cols.highlightedColor = new Color(0.25f, 0.25f, 0.45f);
        btn.colors = cols;

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(360, 130);

        // Slot ismi (üstte küçük)
        MakeLabel(obj, label, new Vector2(0.5f, 1f), new Vector2(0, -18), 18,
            new Color(0.6f, 0.6f, 0.8f), FontStyles.Normal);

        // Ekipman ismi (ortada büyük)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(obj.transform, false);
        var tmp = nameObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = "— BOŞ —";
        tmp.fontSize  = 22;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        var tr = nameObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(8, 30); tr.offsetMax = new Vector2(-8, -10);

        btn.onClick.AddListener(() => OpenPick(slot));
        return (btn, tmp);
    }

    // ── Seçim paneli ───────────────────────────────────────────────────────
    void BuildPickPanel()
    {
        _pickPanel = new GameObject("PickPanel");
        _pickPanel.transform.SetParent(_panel.transform, false);
        var bg = _pickPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.15f, 0.98f);
        var r = _pickPanel.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, 0.15f); r.anchorMax = new Vector2(0.95f, 0.85f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        _pickPanel.SetActive(false);
    }

    void OpenPick(EquipmentSlot slot)
    {
        _currentSlot = slot;
        _pickPanel.SetActive(true);

        // Eski içeriği temizle
        foreach (Transform t in _pickPanel.transform) Destroy(t.gameObject);

        EquipmentData[] pool = GetFilteredPool(slot);
        if (pool == null || pool.Length == 0)
        {
            MakeLabel(_pickPanel, "Bu slot icin ekipman yok.\nInspector'dan ekle.",
                new Vector2(0.5f, 0.5f), Vector2.zero, 24, Color.gray, FontStyles.Normal);
            MakeClosePickBtn();
            return;
        }

        // Geri butonu
        MakeClosePickBtn();

        // İtem listesi
        float startY = -60f;
        for (int i = 0; i < pool.Length && i < 8; i++)
        {
            var item = pool[i];
            if (item == null) continue;
            int idx = i;

            var row = new GameObject("Item_" + i);
            row.transform.SetParent(_pickPanel.transform, false);
            var rowBg = row.AddComponent<Image>();
            rowBg.color = new Color(0.18f, 0.18f, 0.3f);
            var rowBtn = row.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            var rr = row.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.05f, 1f); rr.anchorMax = new Vector2(0.95f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0, startY - idx * 100f);
            rr.sizeDelta = new Vector2(0, 90);

            string rarityStr = item.rarity switch { 5 => "[EFSANE]", 4 => "[EPİK]", 3 => "[NADİR]", 2 => "[SIK]", _ => "[YAYGIN]" };
            Color  rarityCol = item.rarity switch { 5 => new Color(1,0.7f,0), 4 => new Color(0.6f,0,1), 3 => Color.cyan, _ => Color.white };
            string desc = $"{rarityStr}  +{item.baseCPBonus}CP  {item.GetTypeDescription()}";

            MakeLabel(row, item.equipmentName, new Vector2(0.02f, 0.8f), Vector2.zero, 24, rarityCol, FontStyles.Bold);
            MakeLabel(row, desc, new Vector2(0.02f, 0.25f), Vector2.zero, 18, Color.gray, FontStyles.Normal);

            rowBtn.onClick.AddListener(() => EquipItem(item));
        }
    }

    void MakeClosePickBtn()
    {
        var cb = new GameObject("ClosePick");
        cb.transform.SetParent(_pickPanel.transform, false);
        var img = cb.AddComponent<Image>(); img.color = new Color(0.6f, 0.1f, 0.1f);
        var btn = cb.AddComponent<Button>(); btn.targetGraphic = img;
        var r = cb.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.7f, 0.92f); r.anchorMax = new Vector2(0.95f, 0.99f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        btn.onClick.AddListener(() => _pickPanel.SetActive(false));
        MakeLabel(cb, "GERİ", new Vector2(0.5f, 0.5f), Vector2.zero, 20, Color.white, FontStyles.Bold);
    }

    void EquipItem(EquipmentData item)
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        switch (_currentSlot)
        {
            case EquipmentSlot.Weapon:   ps.equippedWeapon   = item; break;
            case EquipmentSlot.Armor:    ps.equippedArmor    = item; break;
            case EquipmentSlot.Shoulder: ps.equippedShoulder = item; break;
            case EquipmentSlot.Knee:     ps.equippedKnee     = item; break;
            case EquipmentSlot.Necklace: ps.equippedNecklace = item; break;
            case EquipmentSlot.Ring:     ps.equippedRing     = item; break;
        }

        _pickPanel.SetActive(false);
        RefreshAll();

        // Loadout SO varsa değişikliği oraya da yaz (save için)
        ps.equippedLoadout?.ReadFrom(ps);

        GameEvents.OnCPUpdated?.Invoke(ps.CP);
        GameEvents.OnCommanderHPChanged?.Invoke(ps.CommanderHP, ps.CommanderMaxHP);
    }

    // ── Refresh ─────────────────────────────────────────────────────────────
    void RefreshAll()
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        _weaponTxt.text   = ps.equippedWeapon   != null ? ps.equippedWeapon.equipmentName   : "— BOŞ —";
        _armorTxt.text    = ps.equippedArmor     != null ? ps.equippedArmor.equipmentName    : "— BOŞ —";
        _shoulderTxt.text = ps.equippedShoulder  != null ? ps.equippedShoulder.equipmentName : "— BOŞ —";
        _kneeTxt.text     = ps.equippedKnee      != null ? ps.equippedKnee.equipmentName     : "— BOŞ —";
        _necklaceTxt.text = ps.equippedNecklace  != null ? ps.equippedNecklace.equipmentName : "— BOŞ —";
        _ringTxt.text     = ps.equippedRing      != null ? ps.equippedRing.equipmentName     : "— BOŞ —";

        float dr     = ps.TotalDamageReduction() * 100f;
        int   hpBon  = ps.TotalEquipmentHPBonus();
        float fireMul= ps.equippedWeapon != null ? ps.equippedWeapon.fireRateMultiplier : 1f;
        float dmgMul = ps.equippedWeapon != null ? ps.equippedWeapon.damageMultiplier   : 1f;

        _statsText.text =
            $"CP: {ps.CP:N0}  |  MaxHP: {ps.CommanderMaxHP} (+{hpBon})\n" +
            $"Hasar Azaltma: %{dr:N0}  |  Ates: x{fireMul:N2}  |  Hasar: x{dmgMul:N2}";
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────
    EquipmentData[] GetFilteredPool(EquipmentSlot slot)
{
    EquipmentData[] raw = GetPool(slot);
    if (raw == null || raw.Length == 0) return raw;

    var list = new System.Collections.Generic.List<EquipmentData>(raw.Length);
    foreach (var item in raw)
    {
        if (item == null) continue;
        if (item.slot == slot)
            list.Add(item);
    }
    return list.ToArray();
}
    
    EquipmentData[] GetPool(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Weapon   => availableWeapons,
        EquipmentSlot.Armor    => availableArmors,
        _                      => availableAccessories,
    };

    TextMeshProUGUI MakeLabel(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("Lbl");
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(500, 50);
        return t;
    }

    void MakeCloseBtn(GameObject parent)
    {
        var obj = new GameObject("CloseBtn");
        obj.transform.SetParent(parent.transform, false);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.7f, 0.1f, 0.1f);
        var btn = obj.AddComponent<Button>(); btn.targetGraphic = img;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.85f, 0.95f); r.anchorMax = new Vector2(0.97f, 0.99f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        btn.onClick.AddListener(Toggle);
        MakeLabel(obj, "X", new Vector2(0.5f, 0.5f), Vector2.zero, 26, Color.white, FontStyles.Bold);
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void OnDisable()
{
    if (_open)
    {
        _open = false;
        Time.timeScale = 1f;
    }
}

void OnDestroy()
{
    Time.timeScale = 1f;
}
}
```

## GameEvents.cs

```csharp
using System;

/// <summary>
/// Top End War — Oyun Olaylari v5 (Claude)
/// Tum v4 eventleri korundu + Boss/Dunya eventleri eklendi.
/// KURAL: Raise() yok — dogrudan ?.Invoke() kullan.
/// </summary>
public static class GameEvents
{
    // ── Oyuncu / Komutan ─────────────────────────────────────────────────
    public static Action<int>        OnCPUpdated;
    public static Action<int>        OnBulletCountChanged;
    public static Action<int>        OnTierChanged;
    public static Action<int, int>   OnCommanderHPChanged;    // (current, max)
    public static Action<int, int>   OnCommanderDamaged;      // (finalDmg, currentHP)
    public static Action<int>        OnCommanderHealed;
    public static Action<int>        OnPlayerDamaged;

    // ── Ordu ────────────────────────────────────────────────────────────
    public static Action<int>        OnSoldierAdded;          // (toplam asker sayisi)
    public static Action<int>        OnSoldierRemoved;        // (toplam asker sayisi)
    public static Action<string,int> OnSoldierMerged;         // (path adı, yeni level) ← DUZELTILDI
    public static Action<int>        OnSoldierHPRestored;
    public static Action<int>        OnSoldierCountChanged;

    // ── Yol / Sinerji ────────────────────────────────────────────────────
    public static Action             OnMergeTriggered;
    public static Action<string>     OnPathBoosted;
    public static Action<string>     OnSynergyFound;

    // ── Kapi / Risk ──────────────────────────────────────────────────────
    public static Action<int>        OnRiskBonusActivated;

    // ── Zorluk / Spawn ───────────────────────────────────────────────────
    // SpawnManager (float multiplier, float powerRatio) olarak kullaniyor
    public static Action<float,float> OnDifficultyChanged;    // ← DUZELTILDI (2 param)
    public static Action              OnBossEncountered;

    // ── Anchor / Boss ────────────────────────────────────────────────────
    public static Action<bool>       OnAnchorModeChanged;
    public static Action<int, int>   OnBossHPChanged;         // (current, max)
    public static Action<int>        OnBossPhaseShield;       // (gelen faz: 2 veya 3)
    public static Action<int>        OnBossPhaseChanged;
    public static Action<float>      OnBossEnraged;
    public static Action             OnBossDefeated;

    // ── Oyun Akisi ────────────────────────────────────────────────────────
    public static Action             OnGameOver;
    public static Action             OnVictory;

    // ── Biyom / Dunya ────────────────────────────────────────────────────
    public static Action<string>     OnBiomeChanged;
    public static Action<int>        OnWorldChanged;
    public static Action<int, int>   OnStageChanged;          // (worldID, stageID)
}
```

## GameHUD.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD v8 (Claude)
///
/// v8 DÜZELTMELER:
///   - CommanderHP Slider fill rect düzgün oluşturuluyor (v7'de bozuktu)
///   - Slider hierarchy: Bar BG → FillArea → Fill (Unity standart yapısı)
///   - SoldierCountText sol üstte, net okunur
///
/// UNITY KURULUM:
///   Canvas → HUDPanel → GameHUD bileşeni zaten bağlı.
///   Inspector'da commanderHPSlider / commanderHPText / soldierCountText
///   referanslarını bağlayabilirsin VEYA boş bırak (auto-build çalışır).
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("CP / Tier")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barlari")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup / Sinerji")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash")]
    public Image damageFlashImage;

    [Header("Komutan HP (opsiyonel — bos birakilabilir)")]
    public Slider          commanderHPSlider;
    public TextMeshProUGUI commanderHPText;

    [Header("Asker Sayisi (opsiyonel)")]
    public TextMeshProUGUI soldierCountText;

    bool _autoBuilt = false;
    int  _lastCP    = 0;

    void Start()
    {
        if (PlayerStats.Instance == null)
        { Debug.LogError("GameHUD: PlayerStats yok!"); return; }

        if (cpText == null || tierText == null) AutoBuildHUD();

        GameEvents.OnCPUpdated          += OnCPUpdated;
        GameEvents.OnTierChanged        += OnTierChanged;
        GameEvents.OnSynergyFound       += OnSynergy;
        GameEvents.OnPlayerDamaged      += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated += OnRiskBonus;
        GameEvents.OnBulletCountChanged += OnBulletCount;
        GameEvents.OnCommanderHPChanged += OnCommanderHP;
        GameEvents.OnSoldierAdded       += OnSoldierCount;
        GameEvents.OnSoldierRemoved     += OnSoldierCount;

        _lastCP = PlayerStats.Instance.CP;
        if (cpText)   cpText.text   = PlayerStats.Instance.CP.ToString("N0");
        if (tierText) tierText.text = "TIER 1 | " + PlayerStats.Instance.GetTierName();
        if (damageFlashImage) damageFlashImage.color = new Color(1,0,0,0);

        // Komutan HP bar ilk değer
        OnCommanderHP(PlayerStats.Instance.CommanderHP, PlayerStats.Instance.CommanderMaxHP);
        if (soldierCountText) soldierCountText.text = "Asker: 0/20";
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated          -= OnCPUpdated;
        GameEvents.OnTierChanged        -= OnTierChanged;
        GameEvents.OnSynergyFound       -= OnSynergy;
        GameEvents.OnPlayerDamaged      -= OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated -= OnRiskBonus;
        GameEvents.OnBulletCountChanged -= OnBulletCount;
        GameEvents.OnCommanderHPChanged -= OnCommanderHP;
        GameEvents.OnSoldierAdded       -= OnSoldierCount;
        GameEvents.OnSoldierRemoved     -= OnSoldierCount;
    }

    // ── AUTO BUILD ────────────────────────────────────────────────────────
    void AutoBuildHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
        }

        if (cpText   == null) cpText   = MakeText(canvas.gameObject, "CP", new Vector2(0.5f,1f), new Vector2(0,-50),  52, Color.white);
        if (tierText == null) tierText = MakeText(canvas.gameObject, "TIER 1", new Vector2(0.5f,1f), new Vector2(0,-105), 32, Color.yellow);
        if (popupText== null) popupText= MakeText(canvas.gameObject, "", new Vector2(0.5f,0.5f), new Vector2(0,80), 52, Color.cyan);

        // ── Komutan HP Bar ────────────────────────────────────────────────
        // Unity Slider standart yapısı: Slider → Background + Fill Area → Fill
        if (commanderHPSlider == null)
            commanderHPSlider = BuildHPBar(canvas,
                new Vector2(0.03f, 0.90f), new Vector2(0.72f, 0.96f),
                new Color(0.2f, 0.8f, 0.2f), "KomutanHP");

        // HP text (slider'ın yanında)
        if (commanderHPText == null)
            commanderHPText = MakeText(canvas.gameObject, "HP",
                new Vector2(0.74f, 0.93f), Vector2.zero, 24, Color.white);

        // ── Asker Sayısı ──────────────────────────────────────────────────
        if (soldierCountText == null)
            soldierCountText = MakeText(canvas.gameObject, "Asker: 0/20",
                new Vector2(0.0f, 0.88f), new Vector2(100, 0), 28, new Color(0.9f,0.9f,0.9f));

        // ── Hasar Flash ───────────────────────────────────────────────────
        if (damageFlashImage == null)
        {
            var fg = new GameObject("DamageFlash");
            fg.transform.SetParent(canvas.transform, false);
            damageFlashImage = fg.AddComponent<Image>();
            damageFlashImage.color = new Color(1,0,0,0);
            damageFlashImage.raycastTarget = false;
            var fr = fg.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
        }

        _autoBuilt = true;
        Debug.Log("[GameHUD v8] AutoBuild tamamlandi.");
    }

    /// <summary>
    /// Unity Slider standart hiyerarşisini elle oluşturur:
    ///   Slider root → Background → Fill Area → Fill → Handle Slide Area → Handle
    /// Fill Rect doğru şekilde atanır — bu v7'deki hatanın düzeltmesi.
    /// </summary>
    Slider BuildHPBar(Canvas canvas, Vector2 anchorMin, Vector2 anchorMax,
                      Color fillColor, string name)
    {
        // Root
        var root = new GameObject(name);
        root.transform.SetParent(canvas.transform, false);
        var sl = root.AddComponent<Slider>();
        sl.interactable = false;
        sl.minValue = 0f; sl.maxValue = 1f; sl.value = 1f;
        var rootR = root.GetComponent<RectTransform>();
        rootR.anchorMin = anchorMin; rootR.anchorMax = anchorMax;
        rootR.offsetMin = rootR.offsetMax = Vector2.zero;

        // Background
        var bg = new GameObject("Background"); bg.transform.SetParent(root.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.08f,0.08f,0.08f,0.88f);
        StretchRect(bg.GetComponent<RectTransform>());

        // Fill Area
        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(root.transform, false);
        var faR = fillArea.GetComponent<RectTransform>() ?? fillArea.AddComponent<RectTransform>();
        faR.anchorMin = new Vector2(0,0.25f); faR.anchorMax = new Vector2(1,0.75f);
        faR.offsetMin = new Vector2(5,0); faR.offsetMax = new Vector2(-5,0);

        // Fill
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
        var fillR = fill.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = new Vector2(0,1);
        fillR.sizeDelta  = new Vector2(10,0); fillR.anchoredPosition = Vector2.zero;

        // Slider referanslari
        sl.fillRect       = fillR;           // ← kritik satır, v7'de eksikti
        sl.targetGraphic  = bgImg;

        return sl;
    }

    // ── EVENT HANDLER'LAR ─────────────────────────────────────────────────
    void OnCPUpdated(int cp)
    {
        var s = PlayerStats.Instance; if (s == null) return;
        if (cpText) cpText.text = cp.ToString("N0");

        float total = s.PiyadePath + s.MekanizePath + s.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = s.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = s.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = s.TeknolojiPath / total;
        }

        int delta = cp - _lastCP;
        if (delta != 0)
            ShowPopup(delta > 0 ? "+" + delta : "" + delta, delta > 0 ? Color.cyan : Color.red);
        _lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        var s = PlayerStats.Instance;
        if (tierText && s != null) tierText.text = $"TIER {tier} | {s.GetTierName()}";
        ShowPopup($"TIER {tier}!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) { ShowPopup(name, new Color(1,0.84f,0)); return; }
        StopCoroutine("HideSynergy");
        synergyText.text = name; synergyText.color = new Color(1,0.84f,0);
        StartCoroutine("HideSynergy");
    }

    void OnRiskBonus(int r) => ShowPopup($"RISK! +{r}", new Color(1,0.85f,0));

    void OnPlayerDamaged(int _)
    {
        if (!damageFlashImage) return;
        StopCoroutine("FlashDamage"); StartCoroutine("FlashDamage");
    }

    void OnBulletCount(int c) => ShowPopup($"+MERMI {c}", new Color(0.5f,0,0.9f));

    // ── KOMUTAN HP ────────────────────────────────────────────────────────
    void OnCommanderHP(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;

        if (commanderHPSlider)
        {
            commanderHPSlider.value = ratio;

            // Fill rengini güncelle
            Image fillImg = commanderHPSlider.fillRect?.GetComponent<Image>();
            if (fillImg)
                fillImg.color = ratio > 0.6f ? new Color(0.2f,0.8f,0.2f)
                              : ratio > 0.3f ? new Color(1f,0.7f,0f)
                              :                new Color(0.9f,0.1f,0.1f);
        }

        if (commanderHPText) commanderHPText.text = $"{current}/{max}";
    }

    // ── ASKER SAYISI ─────────────────────────────────────────────────────
    void OnSoldierCount(int count)
    {
        if (soldierCountText) soldierCountText.text = $"Asker: {count}/20";
    }

    // ── POPUP ─────────────────────────────────────────────────────────────
    void ShowPopup(string msg, Color color)
    {
        if (!popupText) return;
        StopCoroutine("HidePopup");
        popupText.text = msg; popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator FlashDamage()
    {
        damageFlashImage.color = new Color(1,0,0,0.55f);
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            damageFlashImage.color = new Color(1,0,0, Mathf.Lerp(0.55f,0,t/0.4f));
            yield return null;
        }
        damageFlashImage.color = new Color(1,0,0,0);
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text   = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }

    // ── YARDIMCI ─────────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string txt, Vector2 anchor,
                             Vector2 pos, float size, Color color)
    {
        var obj = new GameObject("HUD_" + txt.Substring(0, Mathf.Min(8, txt.Length)));
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(500, 60);
        return t;
    }

    void StretchRect(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
```

## GameOverUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top End War — Game Over Arayuzu v3 (Claude)
/// Revive (reklam, run basina 1x) + Retreat (%20 loot koruma)
/// SaveManager.CurrentRunKills kullanilir (SessionKills degil).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject gameOverPanel;

    [Header("Skor Gostergeleri")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI highScoreText;
    public GameObject      newRecordBadge;

    [Header("Revive")]
    public Button          reviveButton;
    public TextMeshProUGUI reviveInfoText;

    [Header("Retreat")]
    public Button          retreatButton;
    public TextMeshProUGUI retreatRewardText;

    [Header("Tekrar Oyna / Ana Menu")]
    public Button restartButton;
    public Button mainMenuButton;

    bool _reviveUsed    = false;
    int  _runGoldEarned = 0;
    int  _runTechEarned = 0;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        reviveButton?.onClick.AddListener(OnReviveClicked);
        retreatButton?.onClick.AddListener(OnRetreatClicked);
        restartButton?.onClick.AddListener(OnRestartClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
    }

    void OnEnable()  => GameEvents.OnGameOver += ShowGameOver;
    void OnDisable() => GameEvents.OnGameOver -= ShowGameOver;

    // ── Run Takibi ────────────────────────────────────────────────────────
    public void RegisterRunGold(int amount)     => _runGoldEarned += amount;
    public void RegisterRunTechCore(int amount)  => _runTechEarned += amount;

    public void ResetRunTracking()
    {
        _runGoldEarned = 0;
        _runTechEarned = 0;
        _reviveUsed    = false;
    }

    // ── Game Over ─────────────────────────────────────────────────────────
    void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        UpdateScoreDisplay();
        UpdateReviveButton();
        UpdateRetreatButton();
    }

    void UpdateScoreDisplay()
    {
        int dist  = Mathf.RoundToInt(
            PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f);
        int cp    = PlayerStats.Instance != null ? PlayerStats.Instance.CP : 0;

        // SaveManager.CurrentRunKills  ← dogru property adi
        int kills = SaveManager.Instance != null ? SaveManager.Instance.CurrentRunKills : 0;

        if (distanceText != null) distanceText.text = $"{dist} m";
        if (killText     != null) killText.text      = $"{kills}";
        if (cpText       != null) cpText.text        = $"{cp}";

        int  prevBest = PlayerPrefs.GetInt("HighScore_CP", 0);
        bool isRecord = cp > prevBest;
        if (isRecord) { PlayerPrefs.SetInt("HighScore_CP", cp); PlayerPrefs.Save(); }

        if (highScoreText  != null) highScoreText.text = $"{Mathf.Max(cp, prevBest)}";
        if (newRecordBadge != null) newRecordBadge.SetActive(isRecord);
    }

    void UpdateReviveButton()
    {
        if (reviveButton == null) return;
        reviveButton.interactable = !_reviveUsed;
        if (reviveInfoText != null)
            reviveInfoText.text = _reviveUsed ? "Kullanildi" : "Reklam izle";
    }

    void UpdateRetreatButton()
{
    if (retreatButton == null) return;

    int goldBack = Mathf.RoundToInt(_runGoldEarned * 0.20f);

    if (retreatRewardText != null)
        retreatRewardText.text = $"Gold +{goldBack}";
}

    // ── Revive ────────────────────────────────────────────────────────────
    void OnReviveClicked()
    {
        if (_reviveUsed) return;
        _reviveUsed = true;
        UpdateReviveButton();
        // TODO: Gercek reklam SDK buraya
        Debug.Log("[GameOverUI] Reklam placeholder — Revive verildi.");
        OnReviveGranted();
    }

    void OnReviveGranted()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        PlayerStats.Instance?.HealCommander(PlayerStats.Instance.CommanderMaxHP);
        Time.timeScale = 1f;
        Debug.Log("[GameOverUI] Oyuncu diriltildi.");
    }

    // ── Retreat ───────────────────────────────────────────────────────────
    void OnRetreatClicked()
{
    int goldBack = Mathf.RoundToInt(_runGoldEarned * 0.20f);
    EconomyManager.Instance?.AddGold(goldBack);

    Debug.Log($"[GameOverUI] Retreat: +{goldBack} Gold.");
    OnRestartClicked();
}

    // ── Tekrar / Menu ─────────────────────────────────────────────────────
    void OnRestartClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void OnMainMenuClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        Debug.Log("[GameOverUI] Ana menu sahne adi henuz tanimsiz.");
    }
}
```

## Gamestartup.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyun Baslangic Ayarlari (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "GameStartup" -> bu scripti ekle.
///   Baska hicbir sey yapma. Kod her seferinde calısır.
///
/// Ne yapar:
///   - Hedef FPS: 60 (mobil pil dostu)
///   - Shadows: Kapat (mobil performans)
///   - Quality Level: Medium (mobil icin uygun)
///   - Screen uyku: Kapalı (oyun sirasinda ekran kararmasin)
/// </summary>
public class GameStartup : MonoBehaviour
{
    [Header("Performans")]
    public int  targetFPS          = 60;
    public bool disableShadows     = true;
    public bool preventScreenSleep = true;

    [Header("Quality (0=VeryLow 1=Low 2=Medium 3=High 4=VeryHigh 5=Ultra)")]
    [Range(0, 5)]
    public int mobileQualityLevel  = 2; // Medium

    void Awake()
    {
        // FPS kilidi
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount  = 0; // VSyncCount=0 → targetFrameRate etkin olur

        // Quality level (mobil=Medium yeterli)
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.SetQualityLevel(mobileQualityLevel, true);
        Debug.Log($"[Startup] Mobil kalite: Level {mobileQualityLevel}");
#else
        // Editor / PC'de dokunsun ama cok dusurusun
        Debug.Log("[Startup] PC/Editor modu — kalite degistirilmedi.");
#endif

        // Shadows
        if (disableShadows)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        // Ekran uyku
        if (preventScreenSleep)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log($"[Startup] FPS={targetFPS} | Shadows={!disableShadows} | Sleep=Kapali");
    }
}
```

## Gate.cs

```csharp
using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v2
///
/// Yeni kanonik veri tipi: GateConfig
/// </summary>
public class Gate : MonoBehaviour
{
    public GateConfig  gateConfig;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool _triggered = false;

    void Start()
    {
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable() { _triggered = false; }

    public void Refresh()
    {
        ApplyVisuals();
        FitBoxCollider();
    }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) Destroy(col);
    }

    void ApplyVisuals()
    {
        if (gateConfig == null) return;

        if (labelText != null)
        {
            string sub = string.IsNullOrWhiteSpace(gateConfig.tag2)
                ? gateConfig.tag1
                : $"{gateConfig.tag1} • {gateConfig.tag2}";

            labelText.text               = $"{gateConfig.title}\n<size=55%>{sub}</size>";
            labelText.fontSize           = 5f;
            labelText.color              = Color.white;
            labelText.alignment          = TextAlignmentOptions.Center;
            labelText.fontStyle          = FontStyles.Bold;
            labelText.overflowMode       = TextOverflowModes.Overflow;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c = gateConfig.gateColor;
            c.a = 0.72f;
            mat.color = c;
            panelRenderer.material = mat;
        }
    }

    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;

        Vector3 s = panelRenderer.transform.localScale;
        box.size = new Vector3(s.x * 0.95f, s.y, 1.2f);
        box.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        PlayerStats ps = other.GetComponent<PlayerStats>();
        ps?.ApplyGateConfig(gateConfig);

        other.GetComponent<GateFeedback>()?.PlayGatePop();

        Debug.Log($"[Gate] {gateConfig.title}");
        Destroy(gameObject);
    }
}
```

## Gateconfig.cs

```csharp
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
```

## Gatefeedback.cs

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Kapi Gecis Efekti v2
/// Player objesine ekle. Coroutine ile calisir (DOTween'e gerek yok).
/// </summary>
public class GateFeedback : MonoBehaviour
{
    [Header("Gate Gecis")]
    public float gatePopDuration = 0.25f;
    public float gatePopScale    = 1.25f;

    [Header("Tier Atlama")]
    public float tierPopDuration = 0.4f;
    public float tierPopScale    = 1.5f;

    [Header("Kamera Sallama")]
    public Camera mainCamera;
    public float  shakeStrength = 0.15f;
    public float  shakeDuration = 0.2f;

    Vector3 _originalScale;
    Vector3 _cameraOriginalPos;
    Coroutine _scaleRoutine;
    Coroutine _shakeRoutine;

    void Start()
    {
        _originalScale = transform.localScale;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _cameraOriginalPos = mainCamera.transform.localPosition;

        GameEvents.OnTierChanged += OnTierChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    public void PlayGatePop()
    {
        StartScalePop(gatePopScale, gatePopDuration);
    }

    public void PlayTierPop()
    {
        StartScalePop(tierPopScale, tierPopDuration);
        if (mainCamera != null)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(CameraShakeRoutine());
        }
    }

    void OnTierChanged(int tier)
    {
        PlayTierPop();
    }

    void StartScalePop(float peak, float duration)
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScalePopRoutine(peak, duration));
    }

    IEnumerator ScalePopRoutine(float peak, float duration)
    {
        float upTime = duration * 0.4f;
        float downTime = duration * 0.6f;

        transform.localScale = _originalScale;
        Vector3 peakScale = _originalScale * peak;

        float t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / upTime);
            transform.localScale = Vector3.Lerp(_originalScale, peakScale, k);
            yield return null;
        }

        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / downTime);
            transform.localScale = Vector3.Lerp(peakScale, _originalScale, k);
            yield return null;
        }

        transform.localScale = _originalScale;
    }

    IEnumerator CameraShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * shakeStrength;
            offset.z = 0f;
            mainCamera.transform.localPosition = _cameraOriginalPos + offset;
            yield return null;
        }

        mainCamera.transform.localPosition = _cameraOriginalPos;
    }
}
```

## Gatepoolconfig.cs

```csharp
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
```

## Inventorymanager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Envanter Yoneticisi v1 (Claude)
///
/// SLOT LEVELING (Senin Kararin):
///   Oyuncu "silah"i degil "silah slotunu" gellistirir.
///   Yeni silah takinca slot seviyesi SIFIRLANMAZ.
///   SlotLevelMult = 1 + azalan_verim_formulü (PlayerStats.GetSlotLevelMult)
///
/// MERGE (Birlestime):
///   itemID ile karsilastirilir — string itemName KULLANILMAZ (localization sonrasi patlar).
///   3x ayni itemID + ayni rarity → 1x (rarity + 1) item.
///
/// SLOT YÜKSELTME:
///   TryUpgradeSlot(slot) → EconomyManager.TryUpgradeSlot() cagirir.
///   Basarili ise PlayerStats'i günceller.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ── Slot Seviyeleri ───────────────────────────────────────────────────
    // PlayerStats zaten slot level tutuyor (weaponSlotLevel vb.)
    // InventoryManager bu degerleri okur/yazar.

    // ── Sahip Olunan Esyalar ─────────────────────────────────────────────
    // ItemID bazli liste. Her esyanin benzersiz bir int ID'si var.
    // EquipmentData.itemID alani olacak (su an rarity kullaniliyor, ileride genisletilecek).
    [Header("Sahip Olunan Esyalar (Runtime)")]
    public List<EquipmentData> ownedItems = new List<EquipmentData>(50);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Esya Ekle ─────────────────────────────────────────────────────────
    public void AddItem(EquipmentData item)
    {
        if (item == null) return;
        ownedItems.Add(item);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] +{item.equipmentName} (rarity {item.rarity})");
    }

    // ── Slot Yükselt ─────────────────────────────────────────────────────
    /// <summary>
    /// Verilen slot icin seviye atlamayı dener.
    /// EconomyManager.TryUpgradeSlot() Gold ve TechCore dusurur.
    /// Basarili ise PlayerStats'taki slot levelini 1 arttirir.
    /// </summary>
    public bool TryUpgradeWeaponSlot()
    {
        int cur = PlayerStats.Instance != null ? PlayerStats.Instance.weaponSlotLevel : 1;
        if (!EconomyManager.Instance.TryUpgradeSlot(cur, out string fail))
        {
            Debug.Log($"[Inventory] Slot upgrade basarisiz: {fail}");
            return false;
        }
        if (PlayerStats.Instance != null) PlayerStats.Instance.weaponSlotLevel++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryUpgradeArmorSlot()
    {
        int cur = PlayerStats.Instance != null ? PlayerStats.Instance.armorSlotLevel : 1;
        if (!EconomyManager.Instance.TryUpgradeSlot(cur, out string fail))
        {
            Debug.Log($"[Inventory] Armor slot upgrade basarisiz: {fail}");
            return false;
        }
        if (PlayerStats.Instance != null) PlayerStats.Instance.armorSlotLevel++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── Merge (Birlestime) ────────────────────────────────────────────────
    /// <summary>
    /// ownedItems listesinde verilen esyanin tipinde (ayni weaponType/armorType + rarity)
    /// 3 kopya varsa bilestirir: 3x Lv R → 1x Lv (R+1).
    /// Basarili ise true dondurur.
    ///
    /// NOT: itemName STRING ile degil, weaponType + armorType + rarity ile karsilastirilir.
    /// </summary>
    public bool TryMergeItem(EquipmentData targetItem)
    {
        if (targetItem == null) return false;
        if (targetItem.rarity >= 5) { Debug.Log("[Inventory] Maksimum rarity, merge yapilamaz."); return false; }

        var duplicates = FindDuplicates(targetItem, 3);
        if (duplicates.Count < 3)
        {
            Debug.Log($"[Inventory] Merge icin 3 kopya gerekli, bulunan: {duplicates.Count}");
            return false;
        }

        // 3 eskiyi kaldir
        for (int i = 0; i < 3; i++) ownedItems.Remove(duplicates[i]);

        // Yeni (rarity+1) esyayi bul veya klonla
        EquipmentData upgraded = FindUpgradedVersion(targetItem);
        if (upgraded != null)
        {
            ownedItems.Add(upgraded);
            Debug.Log($"[Inventory] MERGE: {targetItem.equipmentName} R{targetItem.rarity} x3 → R{upgraded.rarity}");
        }
        else
        {
            Debug.LogWarning($"[Inventory] Merge: R{targetItem.rarity + 1} versiyonu bulunamadi.");
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Ayni weapon/armor tipi ve rarity'de kopya esyalari dondurur.
    /// String degil enum/int karsilastirmasi.
    /// </summary>
    List<EquipmentData> FindDuplicates(EquipmentData target, int maxCount)
    {
        var result = new List<EquipmentData>(maxCount);
        foreach (var item in ownedItems)
        {
            if (result.Count >= maxCount) break;
            if (item == null) continue;
            if (item.rarity    != target.rarity)    continue;
            if (item.slot      != target.slot)      continue;
            if (item.weaponType != target.weaponType) continue;
            if (item.armorType  != target.armorType)  continue;
            result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Ayni tipe sahip 1 rarity yukari versiyonu ownedItems veya
    /// Resources klasöründen arar.
    /// Yoksa mevcut esyanin kopyasini olusturup rarity arttirir (fallback).
    /// </summary>
    EquipmentData FindUpgradedVersion(EquipmentData source)
    {
        int targetRarity = source.rarity + 1;

        // Once mevcut listede ara
        foreach (var item in ownedItems)
        {
            if (item == null) continue;
            if (item.rarity     == targetRarity &&
                item.slot       == source.slot &&
                item.weaponType == source.weaponType &&
                item.armorType  == source.armorType)
                return item;
        }

        // Fallback: mevcut SO'yu kopyala, rarity artir
        // (Gercek projede Database'den cektirilmeli)
        var clone = Instantiate(source);
        clone.rarity = targetRarity;
        clone.equipmentName = $"{source.equipmentName} +{targetRarity}";
        return clone;
    }

    // ── Esya Kus ─────────────────────────────────────────────────────────
    public void EquipItem(EquipmentData item)
{
    if (item == null || PlayerStats.Instance == null) return;

    switch (item.slot)
    {
        case EquipmentSlot.Weapon:   PlayerStats.Instance.equippedWeapon   = item; break;
        case EquipmentSlot.Armor:    PlayerStats.Instance.equippedArmor    = item; break;
        case EquipmentSlot.Shoulder: PlayerStats.Instance.equippedShoulder = item; break;
        case EquipmentSlot.Knee:     PlayerStats.Instance.equippedKnee     = item; break;
        case EquipmentSlot.Necklace: PlayerStats.Instance.equippedNecklace = item; break;
        case EquipmentSlot.Ring:     PlayerStats.Instance.equippedRing     = item; break;
    }

    PlayerStats.Instance.equippedLoadout?.ReadFrom(PlayerStats.Instance);
    GameEvents.OnCPUpdated?.Invoke(PlayerStats.Instance.CP);
    GameEvents.OnCommanderHPChanged?.Invoke(PlayerStats.Instance.CommanderHP, PlayerStats.Instance.CommanderMaxHP);

    OnInventoryChanged?.Invoke();
    Debug.Log($"[Inventory] Kusanildi: {item.equipmentName} ({item.slot})");
}

    // ── Slot Carpan Bilgisi (UI icin) ─────────────────────────────────────
    public float GetWeaponSlotMult()
    {
        int lv = PlayerStats.Instance != null ? PlayerStats.Instance.weaponSlotLevel : 1;
        return PlayerStats.GetSlotLevelMult(lv);
    }

    public float GetArmorSlotMult()
    {
        int lv = PlayerStats.Instance != null ? PlayerStats.Instance.armorSlotLevel : 1;
        return PlayerStats.GetSlotLevelMult(lv);
    }

    // ── Olaylar ───────────────────────────────────────────────────────────
    public static System.Action OnInventoryChanged;
}
```

## Mainmenuui.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Ana Menü v2
///
/// Vertical slice icin sade ve compile-safe surum.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Sahne")]
    public string gameSceneName = "SampleScene";

    [Header("Arkaplan Rengi")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.12f);

    Canvas _canvas;
    TextMeshProUGUI _bestCPText;
    TextMeshProUGUI _bestDistText;
    TextMeshProUGUI _totalRunsText;

    void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = bgColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }

        BuildUI();
        RefreshStats();
    }

    void BuildUI()
    {
        var cObj = new GameObject("MainMenuCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        cObj.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("BG");
        bg.transform.SetParent(_canvas.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        Stretch(bg.GetComponent<RectTransform>());

        MakeText(_canvas.gameObject, "TOP END WAR",
            new Vector2(0.5f, 1f), new Vector2(0, -160),
            72, new Color(1f, 0.85f, 0.1f), FontStyles.Bold);

        // DEĞİŞİKLİK
        MakeText(_canvas.gameObject, "Forge the front. Break the line.",
            new Vector2(0.5f, 1f), new Vector2(0, -250),
            30, new Color(0.7f, 0.7f, 0.9f), FontStyles.Italic);

        _bestCPText = MakeText(_canvas.gameObject, "Best CP: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 200),
            32, Color.white, FontStyles.Normal);

        _bestDistText = MakeText(_canvas.gameObject, "Best Distance: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 155),
            28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        _totalRunsText = MakeText(_canvas.gameObject, "Total Runs: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0, 110),
            24, new Color(0.6f, 0.6f, 0.6f), FontStyles.Normal);

        MakeButton(_canvas.gameObject, "PLAY",
            new Vector2(0.5f, 0.5f), new Vector2(0, -30),
            new Vector2(400, 110),
            new Color(0.15f, 0.75f, 0.25f),
            () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(gameSceneName);
            });

        MakeButton(_canvas.gameObject, "Reset Save",
            new Vector2(0f, 0f), new Vector2(130, 60),
            new Vector2(220, 55),
            new Color(0.4f, 0.1f, 0.1f, 0.7f),
            () =>
            {
                SaveManager.Instance?.ResetAll();
                RefreshStats();
            });

        MakeText(_canvas.gameObject, "v0.5 — Vertical Slice",
            new Vector2(1f, 0f), new Vector2(-110, 35),
            18, new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);
    }

    void RefreshStats()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

        _bestCPText.text = $"Best CP: {save.HighScoreCP:N0}";
        _bestDistText.text = $"Best Distance: {save.HighScoreDistance:N0}m";
        _totalRunsText.text = $"Total Runs: {save.TotalRuns}";
    }

    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("T_" + text.Substring(0, Mathf.Min(8, text.Length)));
        obj.transform.SetParent(parent.transform, false);

        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(900, 80);

        return t;
    }

    void MakeButton(GameObject parent, string label, Vector2 anchor,
        Vector2 pos, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent.transform, false);

        var img = obj.AddComponent<Image>();
        img.color = bg;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f;
        cols.pressedColor = bg * 0.7f;
        btn.colors = cols;
        btn.onClick.AddListener(onClick);

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(obj.transform, false);

        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = size.y * 0.35f;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;

        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
```

## MorphController.cs

```csharp
using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Top End War — Tier Morph (Claude)
/// CRASH FIX: Destroy yerine SetActive. Tum modeller baslangicta olusturulur.
/// DOTween ile scale pop animasyonu.
/// Player objesine ekle. Tier prefablari 0=Tier1...4=Tier5.
/// </summary>
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefablari (0=Tier1 .. 4=Tier5)")]
    public GameObject[] tierPrefabs;

    [Header("VFX")]
    public GameObject morphParticlePrefab;

    [Header("Animasyon")]
    public float shrinkDuration = 0.15f;
    public float popDuration    = 0.35f;
    public float popPeak        = 1.35f;

    [Header("Shader Renkleri (Commander_BiomeShader icin)")]
    [SerializeField] Renderer characterRenderer;

    // Tier renkleri (T1=gri → T5=altin)
    static readonly Color[] TIER_COLORS =
    {
        new Color(0.55f, 0.55f, 0.55f), // T1 Gri
        new Color(0.20f, 0.45f, 0.90f), // T2 Mavi
        new Color(0.90f, 0.50f, 0.10f), // T3 Turuncu
        new Color(0.65f, 0.10f, 0.90f), // T4 Mor
        new Color(1.00f, 0.80f, 0.10f), // T5 Altin
    };

    // Biyom renkleri (BiomeManager currentBiome ile eslenik)
    static readonly System.Collections.Generic.Dictionary<string, Color> BIOME_COLORS =
        new System.Collections.Generic.Dictionary<string, Color>
    {
        ["Tas"]   = new Color(0.55f, 0.52f, 0.48f),
        ["Orman"] = new Color(0.20f, 0.60f, 0.20f),
        ["Cul"]   = new Color(0.85f, 0.70f, 0.30f),
        ["Karli"] = new Color(0.80f, 0.90f, 1.00f),
        ["Tarim"] = new Color(0.60f, 0.80f, 0.30f),
    };

    MaterialPropertyBlock _propBlock;
    static readonly int TierColorID = Shader.PropertyToID("_TierColor");
    static readonly int BiomeTintID = Shader.PropertyToID("_BiomeTint");

    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged  += HandleTierChange;
        GameEvents.OnBiomeChanged += HandleBiomeChange;
        ActivateTier(0);
        RefreshShader(1, BiomeManager.Instance?.currentBiome ?? "Tas");
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged  -= HandleTierChange;
        GameEvents.OnBiomeChanged -= HandleBiomeChange;
    }

    // ── Shader Guncelleme ─────────────────────────────────────────────────
    /// <summary>Tier ve biyom degistiginde Commander_BiomeShader'i gunceller.</summary>
    public void RefreshShader(int tier, string biome)
    {
        if (characterRenderer == null) return;

        Color tc = TIER_COLORS[Mathf.Clamp(tier - 1, 0, TIER_COLORS.Length - 1)];
        Color bc = BIOME_COLORS.TryGetValue(biome, out Color found) ? found : Color.white;

        characterRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(TierColorID, tc);
        _propBlock.SetColor(BiomeTintID, bc);
        characterRenderer.SetPropertyBlock(_propBlock);
    }

    void HandleTierChange(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, _spawnedModels.Length - 1);
        RefreshShader(newTier, BiomeManager.Instance?.currentBiome ?? "Tas");
        if (index == _currentTierIndex || _isMorphing) return;
        StartCoroutine(MorphCoroutine(index));
    }

    void HandleBiomeChange(string biome)
    {
        RefreshShader(PlayerStats.Instance?.CurrentTier ?? 1, biome);
    }

    // ── Model Yonetimi ────────────────────────────────────────────────────
    void PrewarmModels()
    {
        int count = tierPrefabs != null ? tierPrefabs.Length : 5;
        _spawnedModels = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            GameObject model;
            if (tierPrefabs != null && i < tierPrefabs.Length && tierPrefabs[i] != null)
                model = Instantiate(tierPrefabs[i], transform);
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.transform.SetParent(transform);
                Destroy(model.GetComponent<Collider>());
            }

            model.transform.localPosition = Vector3.zero;
            model.transform.localScale    = Vector3.one;

            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Destroy(c);

            model.SetActive(false);
            _spawnedModels[i] = model;
        }
    }

    IEnumerator MorphCoroutine(int targetIndex)
    {
        _isMorphing = true;

        if (_currentTierIndex >= 0 && _currentTierIndex < _spawnedModels.Length)
        {
            GameObject prev = _spawnedModels[_currentTierIndex];
            if (prev != null)
            {
                yield return prev.transform.DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(Ease.InBack).WaitForCompletion();
                prev.SetActive(false);
                prev.transform.localScale = Vector3.one;
            }
        }

        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

        ActivateTier(targetIndex);
        _isMorphing = false;
    }

    void ActivateTier(int index)
    {
        if (_spawnedModels == null || index >= _spawnedModels.Length) return;
        GameObject model = _spawnedModels[index];
        if (model == null) return;

        model.transform.localScale = Vector3.zero;
        model.SetActive(true);

        model.transform.DOScale(Vector3.one * popPeak, popDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (model != null)
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f)
                        .SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
}
```

## ObjectPooler.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Nesne Havuzu (Gemini)
/// Instantiate/Destroy yerine SetActive(true/false) ile performans.
/// PoolManager objesine ekle.
/// Inspector'da Pools listesine: tag, prefab, size gir.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string     tag;
        public GameObject prefab;
        public int        size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (Pool pool in pools)
        {
            Queue<GameObject> q = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.parent = this.transform;
                q.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, q);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;
        GameObject obj = poolDictionary[tag].Dequeue();
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        poolDictionary[tag].Enqueue(obj);
        return obj;
    }
}
```

## Petcontroller.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v7 (Patch 4 Entegrasyonu)
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      detectRange = 35f;

    static readonly float[][] SPREAD =
    {
        new[] {  0f },
        new[] { -8f,  8f },
        new[] { -12f, 0f, 12f },
        new[] { -18f, -6f, 6f, 18f },
        new[] { -22f, -11f, 0f, 11f, 22f },
    };

    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
    }

    void OnDestroy() => GameEvents.OnAnchorModeChanged -= OnAnchorMode;

    void OnAnchorMode(bool active)
    {
        _anchorMode  = active;
        forwardSpeed = active ? 0f : 10f;
        if (active) Debug.Log("[Player] Anchor modu aktif.");
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height = 2f;
        c.radius = 0.4f;
        c.isTrigger = false;
    }

    void Update()
    {
        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    void HandleDrag()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            _targetX = Mathf.Clamp(_targetX - 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetKey(KeyCode.RightArrow))
            _targetX = Mathf.Clamp(_targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (_dragging)
        {
            _targetX = Mathf.Clamp(
                _targetX + (Input.mousePosition.x - _lastMouseX) * dragSensitivity,
                -xLimit, xLimit);

            _lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null)
            return;

        // DEĞİŞİKLİK 4: Kapı bonusları hesaba dahil ediliyor
        float finalFireRate = PlayerStats.Instance.GetBaseFireRate()
                            * (1f + PlayerStats.Instance.RunFireRatePercent / 100f);

        float totalDPS = PlayerStats.Instance.GetTotalDPS()
                       * (1f + PlayerStats.Instance.RunWeaponPowerPercent / 100f);

        int bCount = PlayerStats.Instance.BulletCount;
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * bCount)));

        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        int armorPen = GetCurrentArmorPen();
        int pierceCount = GetCurrentPierceCount();
        float eliteDamageMult = GetCurrentEliteDamageMultiplier();

        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(
                firePoint.position,
                dir.normalized,
                bulletDamage,
                armorPen,
                pierceCount,
                eliteDamageMult);
        }

        _nextFire = Time.time + 1f / finalFireRate;
    }

    void FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount, float eliteDamageMult)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b == null) return;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = "Commander";
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }

    // DEĞİŞİKLİK 4: Helper'lar kapı değerlerini okuyor
    int GetCurrentArmorPen()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        int equipValue = w != null ? w.armorPen : 0;
        int gateValue  = PlayerStats.Instance != null ? PlayerStats.Instance.RunArmorPenFlat : 0;
        return equipValue + gateValue;
    }

    int GetCurrentPierceCount()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        int equipValue = w != null ? w.pierceCount : 0;
        int gateValue  = PlayerStats.Instance != null ? PlayerStats.Instance.RunPierceCount : 0;
        return equipValue + gateValue;
    }

    float GetCurrentEliteDamageMultiplier()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        float equipMult = w != null ? w.eliteDamageMultiplier : 1f;
        float gateMult  = PlayerStats.Instance != null
            ? (1f + PlayerStats.Instance.RunEliteDamagePercent / 100f)
            : 1f;

        return equipMult * gateMult;
    }

    Transform FindTarget()
    {
        if (_anchorMode)
        {
            float bestDist = 70f * 70f;
            Transform best = null;

            foreach (Collider c in Physics.OverlapSphere(transform.position, 70f))
            {
                bool isEnemy = c.GetComponent<Enemy>() != null || c.GetComponentInParent<Enemy>() != null;
                bool isBoss  = c.GetComponent<BossHitReceiver>() != null || c.GetComponentInParent<BossHitReceiver>() != null;

                if (!isEnemy && !isBoss) continue;

                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c.transform;
                }
            }
            return best;
        }
        else
        {
            RaycastHit hit;
            bool found = Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
                Vector3.forward,
                out hit,
                Quaternion.identity,
                detectRange);

            if (!found) return null;

            bool isEnemy = hit.collider.GetComponent<Enemy>() != null || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null || hit.collider.GetComponentInParent<BossHitReceiver>() != null;

            return (isEnemy || isBoss) ? hit.transform : null;
        }
    }

    public void ResumeRun() => OnAnchorMode(false);
}
```

## PetData.cs

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewPet", menuName = "TopEndWar/Pet")]
public class PetData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string petName;
    public GameObject petPrefab; // Oyunda karakterin arkasından koşacak 3D model
    public Sprite icon;

    [Header("Anchor & Combat Bonusları")]
    public int cpBonus;
    public float anchorDamageReduction = 0.1f; // Anchor modunda iken ekstra %10 hasar emme
}
```

## PlayerController.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v8 (Boss Damage Mult Eklendi)
/// </summary>
public class Playercontroller : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 6.8f;

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      detectRange = 35f;

    static readonly float[][] SPREAD =
    {
        new[] {  0f },
        new[] { -8f,  8f },
        new[] { -12f, 0f, 12f },
        new[] { -18f, -6f, 6f, 18f },
        new[] { -22f, -11f, 0f, 11f, 22f },
    };

    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
    }

    void OnDestroy() => GameEvents.OnAnchorModeChanged -= OnAnchorMode;

    void OnAnchorMode(bool active)
    {
        _anchorMode  = active;
        forwardSpeed = active ? 0f : 10f;
        if (active) Debug.Log("[Player] Anchor modu aktif.");
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height = 2f;
        c.radius = 0.4f;
        c.isTrigger = false;
    }

    void Update()
    {
        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    void HandleDrag()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            _targetX = Mathf.Clamp(_targetX - 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetKey(KeyCode.RightArrow))
            _targetX = Mathf.Clamp(_targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (_dragging)
        {
            _targetX = Mathf.Clamp(
                _targetX + (Input.mousePosition.x - _lastMouseX) * dragSensitivity,
                -xLimit, xLimit);

            _lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null)
            return;

        float finalFireRate = PlayerStats.Instance.GetBaseFireRate()
                            * (1f + PlayerStats.Instance.RunFireRatePercent / 100f);

        float totalDPS = PlayerStats.Instance.GetTotalDPS()
                       * (1f + PlayerStats.Instance.RunWeaponPowerPercent / 100f);

        int bCount = PlayerStats.Instance.BulletCount;
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * bCount)));

        // YENİ: BossDamageMult alındı
        float bossDamageMult = GetCurrentBossDamageMultiplier();

        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        int armorPen = GetCurrentArmorPen();
        int pierceCount = GetCurrentPierceCount();
        float eliteDamageMult = GetCurrentEliteDamageMultiplier();

        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            // YENİ: bossDamageMult FireOne'a eklendi
            FireOne(firePoint.position, dir.normalized, bulletDamage, armorPen, pierceCount, eliteDamageMult, bossDamageMult);
        }

        _nextFire = Time.time + 1f / finalFireRate;
    }

    // YENİ İMZA
    void FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount, float eliteDamageMult, float bossDamageMult)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b == null) return;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = "Commander";
            // YENİ SETCOMBATSTATS
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult, bossDamageMult);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 50f;
    }

    int GetCurrentArmorPen()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        int equipValue = w != null ? w.armorPen : 0;
        int gateValue  = PlayerStats.Instance != null ? PlayerStats.Instance.RunArmorPenFlat : 0;
        return equipValue + gateValue;
    }

    int GetCurrentPierceCount()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        int equipValue = w != null ? w.pierceCount : 0;
        int gateValue  = PlayerStats.Instance != null ? PlayerStats.Instance.RunPierceCount : 0;
        return equipValue + gateValue;
    }

    float GetCurrentEliteDamageMultiplier()
    {
        EquipmentData w = PlayerStats.Instance != null ? PlayerStats.Instance.equippedWeapon : null;
        float equipMult = w != null ? w.eliteDamageMultiplier : 1f;
        float gateMult  = PlayerStats.Instance != null
            ? (1f + PlayerStats.Instance.RunEliteDamagePercent / 100f)
            : 1f;

        return equipMult * gateMult;
    }

    // YENİ HELPER
    float GetCurrentBossDamageMultiplier()
    {
        return PlayerStats.Instance != null
            ? 1f + (PlayerStats.Instance.RunBossDamagePercent / 100f)
            : 1f;
    }

    Transform FindTarget()
    {
        if (_anchorMode)
        {
            float bestDist = 70f * 70f;
            Transform best = null;

            foreach (Collider c in Physics.OverlapSphere(transform.position, 70f))
            {
                bool isEnemy = c.GetComponent<Enemy>() != null || c.GetComponentInParent<Enemy>() != null;
                bool isBoss  = c.GetComponent<BossHitReceiver>() != null || c.GetComponentInParent<BossHitReceiver>() != null;

                if (!isEnemy && !isBoss) continue;

                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c.transform;
                }
            }
            return best;
        }
        else
        {
            RaycastHit hit;
            bool found = Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
                Vector3.forward,
                out hit,
                Quaternion.identity,
                detectRange);

            if (!found) return null;

            bool isEnemy = hit.collider.GetComponent<Enemy>() != null || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null || hit.collider.GetComponentInParent<BossHitReceiver>() != null;

            return (isEnemy || isBoss) ? hit.transform : null;
        }
    }

    public void ResumeRun() => OnAnchorMode(false);
}
```

## PlayerStats.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Oyuncu Istatistikleri v8 (Claude & Patch)
///
/// DEGISIKLIKLER:
///   + GateConfig ve Modifier destegi (Slice kural seti)
///   + Run-time kapı bonus alanları eklendi
///   + Eski ApplyGateEffect(GateData) destekli ama yeni yapıya adapte edildi
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ── Komutan ───────────────────────────────────────────────────────────
    [Header("Aktif Komutan (CommanderData SO)")]
    public CommanderData activeCommander;

    // ── Ekipman ───────────────────────────────────────────────────────────
    [Header("Ekipman Seti (EquipmentLoadout SO)")]
    public EquipmentLoadout equippedLoadout;

    [Header("Tekil Ekipmanlar")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public EquipmentData equippedShoulder;
    public EquipmentData equippedKnee;
    public EquipmentData equippedNecklace;
    public EquipmentData equippedRing;
    public PetData       equippedPet;

    // ── Slot Seviyeleri ───────────────────────────────────────────────────
    [Header("Slot Seviyeleri (max 50)")]
    [Range(1, 50)] public int weaponSlotLevel   = 1;
    [Range(1, 50)] public int armorSlotLevel    = 1;
    [Range(1, 50)] public int shoulderSlotLevel = 1;
    [Range(1, 50)] public int kneeSlotLevel     = 1;
    [Range(1, 50)] public int necklaceSlotLevel = 1;
    [Range(1, 50)] public int ringSlotLevel     = 1;

    // ── Diger Ayarlar ─────────────────────────────────────────────────────
    [Header("Baslangic Ayarlari")]
    public float invincibilityDuration = 0.8f;

    // ── Dahili Durum ──────────────────────────────────────────────────────
    private int   _baseCP        = 0;
    private int   _riskBonusLeft = 0;
    private float _expectedCP    = 200f;
    private float _lastDmgTime   = -99f;

    // ── RUN-TIME GATE BONUSLARI (Vertical Slice) ─────────────────────────
    float _runWeaponPowerPercent = 0f;
    float _runFireRatePercent    = 0f;
    float _runEliteDamagePercent = 0f;
    float _runBossDamagePercent  = 0f;
    int   _runArmorPenFlat       = 0;
    int   _runPierceCount        = 0;

    public float RunWeaponPowerPercent => _runWeaponPowerPercent;
    public float RunFireRatePercent    => _runFireRatePercent;
    public float RunEliteDamagePercent => _runEliteDamagePercent;
    public float RunBossDamagePercent  => _runBossDamagePercent;
    public int   RunArmorPenFlat       => _runArmorPenFlat;
    public int   RunPierceCount        => _runPierceCount;

    // ── CP Property ───────────────────────────────────────────────────────
    public int CP
    {
        get
        {
            int total = _baseCP;
            total += equippedWeapon   != null ? equippedWeapon.baseCPBonus   : 0;
            total += equippedArmor    != null ? equippedArmor.baseCPBonus    : 0;
            total += equippedShoulder != null ? equippedShoulder.baseCPBonus : 0;
            total += equippedKnee     != null ? equippedKnee.baseCPBonus     : 0;
            total += equippedNecklace != null ? equippedNecklace.baseCPBonus : 0;
            total += equippedRing     != null ? equippedRing.baseCPBonus     : 0;
            total += equippedPet      != null ? equippedPet.cpBonus          : 0;

            float mult = equippedNecklace != null ? equippedNecklace.cpMultiplier : 1f;
            if (equippedRing != null) mult *= equippedRing.cpMultiplier;
            return Mathf.RoundToInt(total * mult);
        }
    }

    public float GetTotalDPS()
    {
        if (activeCommander == null) return 60f;
        float baseDMG    = activeCommander.GetBaseDMG(CurrentTier);
        float weaponMult = equippedWeapon != null ? equippedWeapon.damageMultiplier    : 1f;
        float slotMult   = GetSlotLevelMult(weaponSlotLevel);
        float rarityMult = GetRarityMult(equippedWeapon != null ? equippedWeapon.rarity : 1);
        float globalMult = 1f;

        if (equippedNecklace != null) globalMult *= equippedNecklace.globalDmgMultiplier;
        if (equippedRing     != null) globalMult *= equippedRing.globalDmgMultiplier;

        return baseDMG * weaponMult * slotMult * rarityMult * globalMult;
    }

    public float GetBaseFireRate()
    {
        if (activeCommander == null) return 1.5f;
        float baseRate  = activeCommander.GetBaseFireRate(CurrentTier);
        float equipMult = equippedWeapon != null ? equippedWeapon.fireRateMultiplier : 1f;
        return baseRate * equipMult;
    }

    public static float GetSlotLevelMult(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        float bonus = 0f;
        int   tier1 = Mathf.Min(level, 10);
        bonus += tier1 * 0.05f;
        if (level > 10) { int tier2 = Mathf.Min(level - 10, 20); bonus += tier2 * 0.03f; }
        if (level > 30) { int tier3 = level - 30; bonus += tier3 * 0.015f; }
        return 1f + bonus;
    }

    public static float GetRarityMult(int rarity)
    {
        return rarity switch { 1 => 1.0f, 2 => 1.3f, 3 => 1.7f, 4 => 2.2f, 5 => 3.0f, _ => 1.0f };
    }

    public float TotalDamageReduction()
    {
        float dr = 0f;
        dr += equippedArmor    != null ? equippedArmor.damageReduction    : 0f;
        dr += equippedShoulder != null ? equippedShoulder.damageReduction : 0f;
        dr += equippedKnee     != null ? equippedKnee.damageReduction     : 0f;
        dr += equippedRing     != null ? equippedRing.damageReduction     : 0f;
        dr += equippedPet      != null ? equippedPet.anchorDamageReduction: 0f;
        return Mathf.Clamp(dr, 0f, 0.60f);
    }

    public int TotalEquipmentHPBonus()
    {
        int bonus = 0;
        bonus += equippedArmor    != null ? equippedArmor.commanderHPBonus    : 0;
        bonus += equippedShoulder != null ? equippedShoulder.commanderHPBonus : 0;
        bonus += equippedKnee     != null ? equippedKnee.commanderHPBonus     : 0;
        return bonus;
    }

    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    static readonly int[] TIER_CP = { 0, 300, 900, 2500, 6000 };
    const int MAX_BULLETS = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        equippedLoadout?.ApplyTo(this);
        _baseCP = 0;

        CommanderMaxHP = (activeCommander != null ? activeCommander.GetBaseHP(1) : 500)
                       + TotalEquipmentHPBonus();
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        float dr         = TotalDamageReduction();
        int finalAmount  = Mathf.RoundToInt(amount * (1f - dr));

        CommanderHP = Mathf.Max(0, CommanderHP - finalAmount);
        GameEvents.OnCommanderDamaged?.Invoke(finalAmount, CommanderHP);
        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

        if (CommanderHP <= 0) GameEvents.OnGameOver?.Invoke();
    }

    public void HealCommander(int amount)
    {
        CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + amount);
        GameEvents.OnCommanderHealed?.Invoke(CommanderHP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        _baseCP = Mathf.Min(_baseCP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── YENİ SİSTEM: GATE CONFIG ──────────────────────────────────────────
    public void ResetRunGateBonuses()
    {
        _runWeaponPowerPercent = 0f;
        _runFireRatePercent    = 0f;
        _runEliteDamagePercent = 0f;
        _runBossDamagePercent  = 0f;
        _runArmorPenFlat       = 0;
        _runPierceCount        = 0;
    }

    public void ApplyGateConfig(GateConfig gate)
    {
        if (gate == null) return;

        ApplyModifierList(gate.modifiers);
        if (gate.IsRisk && gate.penaltyModifiers != null && gate.penaltyModifiers.Count > 0)
            ApplyModifierList(gate.penaltyModifiers);

        Debug.Log($"[PlayerStats] Gate applied: {gate.title}");
    }

    void ApplyModifierList(List<GateModifier2> list)
    {
        if (list == null) return;
        foreach (var mod in list)
            ApplyModifier(mod);
    }

    void ApplyModifier(GateModifier2 mod)
    {
        if (mod == null) return;
        switch (mod.statType)
        {
            case GateStatType2.WeaponPowerPercent:
                _runWeaponPowerPercent += mod.value;
                break;
            case GateStatType2.FireRatePercent:
                _runFireRatePercent += mod.value;
                break;
            case GateStatType2.EliteDamagePercent:
                _runEliteDamagePercent += mod.value;
                break;
            case GateStatType2.BossDamagePercent:
                _runBossDamagePercent += mod.value;
                break;
            case GateStatType2.ArmorPenFlat:
                _runArmorPenFlat += Mathf.RoundToInt(mod.value);
                break;
            case GateStatType2.PierceCount:
                _runPierceCount += Mathf.RoundToInt(mod.value);
                break;
            case GateStatType2.AddSoldierCount:
            {
                int count = Mathf.RoundToInt(mod.value);
                switch (mod.targetType)
                {
                    case GateTargetType2.PiyadeSoldiers:
                        ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: count);
                        break;
                    case GateTargetType2.MekanikSoldiers:
                        ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: count);
                        break;
                    case GateTargetType2.TeknolojiSoldiers:
                        ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: count);
                        break;
                    case GateTargetType2.AllSoldiers:
                    default:
                        ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: count);
                        break;
                }
                break;
            }
            case GateStatType2.HealCommanderPercent:
            {
                int heal = Mathf.RoundToInt(CommanderMaxHP * (mod.value / 100f));
                HealCommander(heal);
                break;
            }
            case GateStatType2.HealSoldiersPercent:
            {
                float pct = mod.value / 100f;
                ArmyManager.Instance?.HealAll(pct);
                break;
            }
            default:
                Debug.Log($"[PlayerStats] Unsupported gate stat for slice now: {mod.statType}");
                break;
        }
    }

    // ── ESKİ SİSTEM: (Legacy uyumluluk) ──────────────────────────────────

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged()
    {
        if (activeCommander == null) return;
        int oldMax = CommanderMaxHP;
        CommanderMaxHP = activeCommander.GetBaseHP(CurrentTier) + TotalEquipmentHPBonus();

        if (CommanderMaxHP > oldMax)
        {
            int bonusHP = CommanderMaxHP - oldMax;
            CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + bonusHP);
        }
        GameEvents.OnTierChanged?.Invoke(CurrentTier);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Yardimci ─────────────────────────────────────────────────────────
    public void SetExpectedCP(float e)
    {
        _expectedCP = Mathf.Max(1f, e);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
        => SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, (float)CP / _expectedCP, 0.08f);

    void RefreshTier()
    {
        for (int i = TIER_CP.Length - 1; i >= 0; i--)
            if (CP >= TIER_CP[i]) { CurrentTier = i + 1; return; }
        CurrentTier = 1;
    }

    void ShowPopupMessage(string msg) => GameEvents.OnSynergyFound?.Invoke(msg);

    public string GetTierName()
    {
        if (activeCommander != null) return activeCommander.commanderName;
        string[] fallback = { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
        return fallback[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    }

    public int GetRiskBonus() => _riskBonusLeft;
}
```

## Progressionconfig.cs

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/ProgressionConfig")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Zorluk Egrisi")]
    [Range(0.8f, 2.0f)]
    public float difficultyExponent = 1.0f;

    public float distanceScale = 1000f;

    [Header("Oyuncu Gucu Uyumu")]
    [Range(0f, 1f)]
    public float playerCPScalingFactor = 0f;

    [Range(0.5f, 1f)]
    public float minPowerAdjust = 1f;

    [Range(1f, 2f)]
    public float maxPowerAdjust = 1f;

    [Header("Beklenen CP (Legacy / opsiyonel)")]
    public float expectedCPGrowthPerKm = 150f;

#if UNITY_EDITOR
    void OnValidate()
    {
        difficultyExponent = Mathf.Max(0.8f, difficultyExponent);
        distanceScale = Mathf.Max(1f, distanceScale);

        // Vertical slice: fixed difficulty varsayilan
        playerCPScalingFactor = Mathf.Clamp01(playerCPScalingFactor);

        if (playerCPScalingFactor <= 0f)
        {
            minPowerAdjust = 1f;
            maxPowerAdjust = 1f;
        }
        else
        {
            minPowerAdjust = Mathf.Clamp(minPowerAdjust, 0.5f, 1f);
            maxPowerAdjust = Mathf.Clamp(maxPowerAdjust, 1f, 2f);
        }

        expectedCPGrowthPerKm = Mathf.Max(0f, expectedCPGrowthPerKm);
    }
#endif
}
```

## Runstate.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Run Durumu v1 (Claude)
///
/// Bir run sirasindaki gecici state. MonoBehaviour degil — servis sinifi.
/// Run bittikten sonra sifirlanir; PlayerPrefs'e yazilmaz.
///
/// NEDEN AYRI?
///   PlayerStats  → hesaplama motorlari + baslangic degerleri
///   RunState     → "su an ne durumda?" sorusunun cevabi
///   SaveData     → oyuncunun kalici ilerlemesi
///
/// KULLANIM:
///   RunState.Instance.AddGateEffect(modifier);
///   RunState.Instance.CommanderCurrentHp;
///   RunState.Instance.Reset();
/// </summary>
public class RunState
{
    // ── Singleton ─────────────────────────────────────────────────────────
    private static RunState _instance;
    public static RunState Instance => _instance ??= new RunState();

    // ── Komutan ───────────────────────────────────────────────────────────
    public int CommanderCurrentHp  { get; set; }
    public int CommanderMaxHp      { get; set; }

    // ── Para ──────────────────────────────────────────────────────────────
    public int CurrentRunGold      { get; private set; }
    public int CurrentRunTechCore  { get; private set; }

    public void AddRunGold(int amount)    => CurrentRunGold     += amount;
    public void AddRunTechCore(int amount) => CurrentRunTechCore += amount;

    // ── Ordu ──────────────────────────────────────────────────────────────
    public int PiyadeCount         { get; set; }
    public int MekanikCount        { get; set; }
    public int TeknolojiCount      { get; set; }

    // ── Gate Efektleri ────────────────────────────────────────────────────
    // Run boyunca biriken aktif gate efektlerinin listesi.
    // GateEffectApplier bu listeyi okuyarak stat carpanlarini hesaplar.
    public List<ActiveGateEffect> ActiveGateEffects { get; } = new List<ActiveGateEffect>();

    public void AddGateEffect(GateConfig source, GateModifier2 mod)
    {
        ActiveGateEffects.Add(new ActiveGateEffect { SourceGateId = source.gateId, Modifier = mod });
    }

    // ── Stat Toplama Yardimcilari ─────────────────────────────────────────
    /// <summary>Silah gücü toplam % bonus (ornegin 16 = +%16).</summary>
    public float GetWeaponPowerBonus()     => SumPercent(GateStatType2.WeaponPowerPercent);
    public float GetFireRateBonus()        => SumPercent(GateStatType2.FireRatePercent);
    public int   GetArmorPenBonus()        => SumFlat(GateStatType2.ArmorPenFlat);
    public float GetEliteDamageBonus()     => SumPercent(GateStatType2.EliteDamagePercent);
    public float GetBossDamageBonus()      => SumPercent(GateStatType2.BossDamagePercent);
    public float GetArmoredDamageBonus()   => SumPercent(GateStatType2.ArmoredTargetDamagePercent);
    public int   GetPierceBonus()          => SumFlat(GateStatType2.PierceCount);
    public int   GetBounceBonus()          => SumFlat(GateStatType2.BounceCount);

    float SumPercent(GateStatType2 stat)
    {
        float total = 0f;
        foreach (var e in ActiveGateEffects)
            if (e.Modifier.statType == stat && e.Modifier.operation == GateOperation2.AddPercent)
                total += e.Modifier.value;
        return total;
    }

    int SumFlat(GateStatType2 stat)
    {
        int total = 0;
        foreach (var e in ActiveGateEffects)
            if (e.Modifier.statType == stat && e.Modifier.operation == GateOperation2.AddFlat)
                total += Mathf.RoundToInt(e.Modifier.value);
        return total;
    }

    // ── Boss ──────────────────────────────────────────────────────────────
    public int  BossPhase          { get; set; }
    public bool BossActive         { get; set; }

    // ── Istatistik ────────────────────────────────────────────────────────
    public int  KillCount          { get; set; }
    public float DistanceTravelled { get; set; }

    // ── Sifirla ───────────────────────────────────────────────────────────
    public void Reset()
    {
        CommanderCurrentHp  = 0;
        CommanderMaxHp      = 0;
        CurrentRunGold      = 0;
        CurrentRunTechCore  = 0;
        PiyadeCount         = 0;
        MekanikCount        = 0;
        TeknolojiCount      = 0;
        ActiveGateEffects.Clear();
        BossPhase           = 0;
        BossActive          = false;
        KillCount           = 0;
        DistanceTravelled   = 0f;
    }
}

public class ActiveGateEffect
{
    public string        SourceGateId;
    public GateModifier2 Modifier;
}
```

## Savemanager.cs

```csharp
using UnityEngine;
using System.IO;

/// <summary>
/// Top End War — Kayit/Yukle v2 (Claude)
///
/// v2: PlayerPrefs → JSON dosyası.
///   Kalıcı veri: highCP, highDist, totalRuns, totalKills
///   Ekipman seti: EquipmentLoadout SO adını kaydeder (isim bazlı)
///
/// DOSYA KONUMU: Application.persistentDataPath/tew_save.json
///   Android: /data/data/<package>/files/
///   PC:      %APPDATA%/../LocalLow/<company>/<product>/
///
/// KURULUM:
///   Hierarchy → Create Empty → "SaveManager" → ekle. Bitti.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // ── Save yapısı ───────────────────────────────────────────────────────
    [System.Serializable]
    class SaveData
    {
        public int   highScoreCP       = 0;
        public float highScoreDistance = 0f;
        public int   totalRuns         = 0;
        public int   totalKills        = 0;
        public int   bestSoldierCount  = 0;
        public string loadoutName      = ""; // EquipmentLoadout SO adı
    }

    SaveData _data = new SaveData();
    string   _savePath;

    // Mevcut oyun
    public int   CurrentRunKills     { get; private set; } = 0;
    public float CurrentRunStartTime { get; private set; }

    // Okunabilir özellikler
    public int   HighScoreCP       => _data.highScoreCP;
    public float HighScoreDistance => _data.highScoreDistance;
    public int   TotalRuns         => _data.totalRuns;
    public int   TotalKills        => _data.totalKills;
    public int   BestSoldierCount  => _data.bestSoldierCount;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath         = Path.Combine(Application.persistentDataPath, "tew_save.json");
        CurrentRunStartTime = Time.time;
        Load();
        Debug.Log($"[Save] Yukle OK | Best CP: {_data.highScoreCP:N0} | Runs: {_data.totalRuns}");
    }

    void Start()
    {
        GameEvents.OnGameOver += OnGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver -= OnGameOver;
        if (Instance == this) Instance = null;
    }

    // ── Game Over ────────────────────────────────────────────────────────
    void OnGameOver()
    {
        int   cp   = PlayerStats.Instance?.CP ?? 0;
        float dist = PlayerStats.Instance?.transform.position.z ?? 0f;
        int   sol  = ArmyManager.Instance?.SoldierCount ?? 0;

        bool newCP   = cp   > _data.highScoreCP;
        bool newDist = dist > _data.highScoreDistance;

        if (newCP)   _data.highScoreCP       = cp;
        if (newDist) _data.highScoreDistance = dist;
        if (sol > _data.bestSoldierCount) _data.bestSoldierCount = sol;

        _data.totalRuns++;
        _data.totalKills += CurrentRunKills;

        // Loadout adını kaydet
        if (PlayerStats.Instance?.equippedLoadout != null)
            _data.loadoutName = PlayerStats.Instance.equippedLoadout.name;

        Save();

        if (newCP || newDist)
            GameEvents.OnSynergyFound?.Invoke($"YENİ REKOR: {cp:N0} CP!");

        Debug.Log($"[Save] Run bitti | CP={cp} | Dist={dist:N0}m | Runs={_data.totalRuns}");
    }

    // ── Kill sayacı ───────────────────────────────────────────────────────
    public void RegisterKill() => CurrentRunKills++;

    // ── IO ────────────────────────────────────────────────────────────────
    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(_savePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Save] Kayıt başarısız: " + e.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                _data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
        }
        catch
        {
            _data = new SaveData();
        }
    }

    public void ResetAll()
    {
        _data = new SaveData();
        Save();
        Debug.Log("[Save] Sıfırlandı.");
    }
    public void BeginRun()
{
    CurrentRunKills = 0;
    CurrentRunStartTime = Time.time;
}
}
```

## SimpleCameraFollow.cs

```csharp
using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Pozisyon")]
    public float heightOffset = 10.5f;
    public float backOffset   = 14f;
    public float followSpeed  = 8f;

    [Header("Açı")]
    [Range(10f, 50f)]
    public float pitchAngle = 28f;

    // DEĞİŞİKLİK
    [Header("X Takip")]
    [Range(0f, 1f)] public float xFollowStrength = 0.42f;
    public float xMaxOffset = 2.6f;

    Quaternion _fixedRotation;

    void Start()
    {
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = _fixedRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // DEĞİŞİKLİK
        float camX = Mathf.Clamp(target.position.x * xFollowStrength, -xMaxOffset, xMaxOffset);

        Vector3 desired = new Vector3(
            camX,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * followSpeed
        );

        transform.rotation = _fixedRotation;
    }

    public void SetPitch(float angle)
    {
        pitchAngle     = Mathf.Clamp(angle, 10f, 50f);
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}
```

## Soliderunit.cs

```csharp
using UnityEngine;

/// <summary>
/// Asker path tipleri — GateData ve ArmyManager ile eslesik olmali.
/// </summary>
public enum SoldierPath
{
    Piyade,
    Mekanik,
    Teknoloji
}

/// <summary>
/// Top End War — Bireysel Asker v4
///
/// PATCH OZETI:
/// - Eski follow + ates akisi korundu
/// - WeaponArchetypeConfig entegrasyonu eklendi
/// - Reservation hedef sistemi eklendi
/// - weaponConfig yoksa guvenli fallback ile calisir
/// </summary>
public class SoldierUnit : MonoBehaviour
{
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string biome = "Tas";
    [HideInInspector] public int mergeLevel = 1;

    [HideInInspector] public int maxHP;
    [HideInInspector] public int currentHP;

    [HideInInspector] public float chassisDamageMult = 1f;
    [HideInInspector] public float chassisFireRateMult = 1f;
    [HideInInspector] public int formationRank = 1;

    [HideInInspector] public WeaponArchetypeConfig weaponConfig;
    [HideInInspector] public int affinityPercent = 100;

    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED = 14f;
    const float RETARGET_INTERVAL = 0.20f;
    const float KEEP_TARGET_MULT = 1.15f;
    const float FALLBACK_RANGE = 18f;
    const float FALLBACK_PROJECTILE_SPEED = 32f;

    Renderer _rend;
    float _nextFire;
    bool _dead;

    Enemy _reservedEnemy;
    float _nextRetargetTime;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        _dead = false;
        _reservedEnemy = null;
        _nextRetargetTime = 0f;

        float fireRate = GetFinalFireRate();
        _nextFire = Time.time + Random.value / Mathf.Max(fireRate, 0.1f);
    }

    void Update()
    {
        if (_dead || PlayerStats.Instance == null) return;

        FollowPlayer();

        if (Time.time >= _nextFire)
            TryShoot();
    }

    void FollowPlayer()
    {
        Vector3 target = PlayerStats.Instance.transform.position + formationOffset;
        target.y = 1.2f;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_SPEED);
    }

    float GetAttackRange()
    {
        return weaponConfig != null ? weaponConfig.attackRange : FALLBACK_RANGE;
    }

    float GetProjectileSpeed()
    {
        return weaponConfig != null ? weaponConfig.projectileSpeed : FALLBACK_PROJECTILE_SPEED;
    }

    float GetFinalFireRate()
    {
        float baseRate = weaponConfig != null ? weaponConfig.fireRate : 1.5f;
        return Mathf.Max(0.05f, baseRate * chassisFireRateMult);
    }

    int GetFinalDamage()
    {
        float baseDamage = weaponConfig != null ? weaponConfig.baseDamage : 15f;

        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path)
            : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        {
            1 => 0f,
            2 => 0.10f,
            3 => 0.20f,
            4 => 0.30f,
            _ => 0.40f
        };

        float mergeMult = mergeLevel switch
        {
            2 => 1.8f,
            3 => 3.5f,
            4 => 7.0f,
            _ => 1.0f
        };

        float affinityMult = affinityPercent / 100f;

        float raw = baseDamage
            * chassisDamageMult
            * biomeMultiplier
            * (1f + cmdAura)
            * mergeMult
            * affinityMult;

        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    bool IsTargetStillValid(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return false;

        float keepRange = GetAttackRange() * KEEP_TARGET_MULT;
        Vector3 delta = enemy.transform.position - transform.position;

        if (delta.z < -1f) return false;
        return delta.sqrMagnitude <= keepRange * keepRange;
    }

    float ScoreTarget(Enemy enemy)
    {
        Vector3 pos = enemy.transform.position;
        Vector3 delta = pos - transform.position;

        float dist = delta.magnitude;
        float xOffset = Mathf.Abs(pos.x - transform.position.x);
        float zForward = Mathf.Max(0f, pos.z - transform.position.z);

        TargetProfile profile = weaponConfig != null
            ? weaponConfig.defaultTargetProfile
            : TargetProfile.Balanced;

        float score = dist + (enemy.ReservationCount * 2.5f);

        switch (profile)
        {
            case TargetProfile.NearestThreat:
                score += xOffset * 0.8f;
                score -= zForward * 0.35f;
                break;

            case TargetProfile.EliteHunter:
                score += enemy.IsElite ? -10f : 8f;
                score -= enemy.Armor * 0.15f;
                break;

            case TargetProfile.Finisher:
                score += enemy.HealthRatio * 8f;
                break;

            case TargetProfile.ClusterFocus:
                score += xOffset * 0.5f;
                break;

            case TargetProfile.Balanced:
            default:
                score += xOffset * 1.1f;
                score += zForward * 0.25f;
                break;
        }

        score -= enemy.ThreatWeight;
        return score;
    }

    Enemy AcquireTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, GetAttackRange());

        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Collider col in cols)
        {
            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            Vector3 delta = enemy.transform.position - transform.position;
            if (delta.z < -1f) continue;

            bool allowed = enemy == _reservedEnemy || enemy.CanAcceptReservation;
            if (!allowed) continue;

            float score = ScoreTarget(enemy);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    void RefreshReservedTarget()
    {
        if (IsTargetStillValid(_reservedEnemy) && Time.time < _nextRetargetTime)
            return;

        Enemy next = AcquireTarget();
        _nextRetargetTime = Time.time + RETARGET_INTERVAL;

        if (next == _reservedEnemy) return;

        if (_reservedEnemy != null)
            _reservedEnemy.ReleaseReservation();

        _reservedEnemy = null;

        if (next != null && next.TryReserve())
            _reservedEnemy = next;
    }

    void TryShoot()
    {
        float fireRate = GetFinalFireRate();
        _nextFire = Time.time + 1f / Mathf.Max(fireRate, 0.01f);

        RefreshReservedTarget();
        if (_reservedEnemy == null) return;

        int finalDmg = GetFinalDamage();
        Collider targetCol = _reservedEnemy.GetComponent<Collider>() ?? _reservedEnemy.GetComponentInChildren<Collider>();
        FireBullet(targetCol, finalDmg);
    }

    void FireBullet(Collider target, int dmg)
    {
        if (target == null) return;

        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 aimPoint = target.bounds.center;
        Vector3 dir = (aimPoint - pos).normalized;

        GameObject b = null;
        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null) return;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = path.ToString();
            bullet.bulletColor = GetPathColor() * 0.85f;

            int armorPen = weaponConfig != null ? weaponConfig.armorPen : 0;
            int pierceCount = weaponConfig != null ? weaponConfig.pierceCount : 0;

            bullet.SetCombatStats(
                dmg,
                armorPen,
                pierceCount,
                1f,
                1f
            );
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * GetProjectileSpeed();
    }

    public void TakeDamage(int dmg)
    {
        if (_dead) return;

        currentHP -= dmg;

        if (_rend) StartCoroutine(FlashRed());

        if (currentHP <= 0)
            Die();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (!_rend) yield break;

        Color orig = _rend.material.color;
        _rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);

        if (_rend && !_dead)
            _rend.material.color = orig;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        if (_reservedEnemy != null)
        {
            _reservedEnemy.ReleaseReservation();
            _reservedEnemy = null;
        }

        ArmyManager.Instance?.RemoveSoldier(this);
        gameObject.SetActive(false);
    }

    public void HealPercent(float pct)
    {
        currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * pct));
    }

    public Color GetPathColor() => path switch
    {
        SoldierPath.Piyade => new Color(0.2f, 0.85f, 0.2f),
        SoldierPath.Mekanik => new Color(0.65f, 0.65f, 0.65f),
        SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),
        _ => Color.white
    } * (mergeLevel switch { 2 => 1.2f, 3 => 1.5f, 4 => 2.0f, _ => 1.0f });
}
```

## SpawnManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v14 (BossConfig Patch)
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;

    [Header("Gate Havuzlari")]
    public GatePoolConfig poolStage1To5;
    public GatePoolConfig poolStage6To10;

    [Header("Spawn")]
    public float spawnAhead = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int minEnemies = 2;
    public int maxEnemies = 8;

    float _nextGateZ = 40f;
    float _nextWaveZ = 55f;
    bool _bossSpawned = false;

    int _waveCursor = 0; // Dalga sırasını takip eder

public void ResetForStage()
{
    _nextGateZ = 40f;
    _nextWaveZ = 55f;
    _bossSpawned = false;
    _waveCursor = 0; // Yeni stage başladığında sıfırlanmalı
}

    DifficultyManager.EnemyStats _stats;
    bool _statsReady = false;

    float _overrideNormalHP = 0f;
    float _overrideEliteHP = 0f;
    float _densityMult = 1f;
    bool _hpOverrideActive = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        RefreshStats();
        GameEvents.OnDifficultyChanged += (m, r) => RefreshStats();
    }

    void RefreshStats()
    {
        _stats = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetScaledEnemyStats()
            : FallbackStats();
        _statsReady = true;
    }

    DifficultyManager.EnemyStats FallbackStats()
    {
        float z = playerTransform != null ? playerTransform.position.z : 0f;
        float m = 1f + Mathf.Pow(z / 1000f, 1.3f);
        return new DifficultyManager.EnemyStats(
            Mathf.RoundToInt(100f * m), Mathf.RoundToInt(25f * m),
            Mathf.Min(4f + (m - 1f) * 1.4f, 7.5f), Mathf.RoundToInt(15f * m));
    }

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        // YENİ: Boss Spawn Mantığı
        if (!_bossSpawned && pz >= bossDistance)
        {
            _bossSpawned = true;

            StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;

            if (BossManager.Instance != null && stage != null && stage.bossConfig != null)
                BossManager.Instance.StartBoss(stage.bossConfig, stage.targetDps);
            else if (BossManager.Instance != null && stage != null)
                BossManager.Instance.StartBoss(stage.GetBossHP());
            else if (BossManager.Instance != null)
                BossManager.Instance.StartBoss();

            return;
        }

        while (pz + spawnAhead >= _nextGateZ)
        {
            SpawnNextGateGroup(_nextGateZ);
            _nextGateZ += gateSpacing;
        }

        while (pz + spawnAhead >= _nextWaveZ)
        {
            SpawnEnemyWave(_nextWaveZ);
            _nextWaveZ += waveSpacing;
        }
    }

    void TryFindPlayer()
    { 
        if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform;
    }

    GatePoolConfig GetActiveGatePool()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return stage <= 5 ? poolStage1To5 : poolStage6To10;
    }

    GateConfig PickGateFromPoolDistinct(GateConfig exclude)
{
    for (int i = 0; i < 8; i++)
    {
        GateConfig picked = PickGateFromPool();
        if (picked != null && picked != exclude)
            return picked;
    }

    return PickGateFromPool();
}

    GateConfig PickGateFromPool()
    {
        GatePoolConfig pool = GetActiveGatePool();
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return pool != null ? pool.PickRandom(stage) : null;
    }

void SpawnEnemyWave(float zPos)
{
    if (TrySpawnConfiguredWave(zPos))
        return;

    // Fallback: eski procedural davranış
    float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
    int cnt = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));

    switch (PickWaveType(prog))
    {
        case 0: NormalWave(zPos, cnt); break;
        case 1: HeavyWave(zPos, cnt);  break;
        case 2: FlankWave(zPos, cnt);  break;
    }
}

    void SpawnNextGateGroup(float zPos)
    {
        SpawnNormalPair(zPos, pity: false);
    }

    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;

    GateConfig leftGate = PickGateFromPool();
    GateConfig rightGate = PickGateFromPoolDistinct(leftGate);

    SpawnGate(leftGate,  new Vector3(-offset, 1.5f, zPos), scale: 1f);
    SpawnGate(rightGate, new Vector3( offset, 1.5f, zPos), scale: 1f);
    }

    void SpawnGate(GateConfig data, Vector3 pos, float scale = 1f)
    {
        if (data == null) return;

        GameObject obj;
        if (gatePrefab != null)
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(0.95f, 1f, 1.5f);

            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }

        Gate gate = obj.GetComponent<Gate>();
        if (gate != null)
        {
            gate.gateConfig = data;
            gate.Refresh();
        }

        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);
            
        Destroy(obj, 45f);
    }


    int PickWaveType(float p)
    { 
        if (p < 0.25f) return 0;
        float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2;
    }

    void NormalWave(float z, int n)
    {
        int cols = Mathf.Min(n, 4), rows = Mathf.CeilToInt((float)n / cols), pl = 0;
        float gap = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx = -(gap * (cols - 1)) * 0.5f;
        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            { 
                PlaceEnemy(new Vector3(Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f), 1.2f, z + r * 3f));
                pl++; 
            }
    }

    void HeavyWave(float z, int n)
    { 
        for (int i = 0; i < n; i++) PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
    }

    void FlankWave(float z, int n)
    {
        int h = n / 2;
        for (int i = 0; i < h; i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
        }
        if (n % 2 == 1) PlaceEnemy(new Vector3(0f, 1.2f, z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        foreach (Collider c in Physics.OverlapSphere(pos, 1.2f))
            if (c.CompareTag("Enemy")) { pos.x += 2.4f; break; }
            
        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);
        GameObject obj;
        if (enemyPrefab != null) obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy"; obj.AddComponent<Enemy>();
        }

        var stats = GetEnemyStatsForSpawn();
        obj.GetComponent<Enemy>()?.Initialize(stats);
    }

    public void SetMobHP(int normalHP, int eliteHP, float density = 1f)
    {
        _overrideNormalHP = normalHP;
        _overrideEliteHP = eliteHP;
        _densityMult = density;
        _hpOverrideActive = true;
        Debug.Log($"[SpawnManager] Mob HP override: Normal={normalHP}, Elite={eliteHP}, Density={density}");
    }

    DifficultyManager.EnemyStats GetEnemyStatsForSpawn()
    {
        if (_hpOverrideActive)
        {
            float speed = _stats.Speed;
            int reward = _stats.CPReward;
            return new DifficultyManager.EnemyStats(
                health:   Mathf.RoundToInt(_overrideNormalHP),
                damage:   _stats.Damage,
                speed:    speed,
                cpReward: reward);
        }
        return _stats;
    }

    // --- SPAWN MANAGER YARDIMCI METOTLARI (PATCH 3 - C) ---

bool TrySpawnConfiguredWave(float zPos)
{
    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    if (stage == null || stage.waveSequence == null || stage.waveSequence.Count == 0)
        return false;

    int safeIndex = Mathf.Clamp(_waveCursor, 0, stage.waveSequence.Count - 1);
    WaveConfig wave = stage.waveSequence[safeIndex];
    if (wave == null)
        return false;

    // Boss wave'leri simdilik normal enemy spawn akisina sokma
    if (wave.waveRole == WaveRole.Boss)
        return false;

    SpawnWaveFromConfig(wave, zPos);
    _waveCursor++;
    return true;
}

void SpawnWaveFromConfig(WaveConfig wave, float baseZ)
{
    if (wave == null || wave.groups == null || wave.groups.Count == 0)
        return;

    // Runner mesafesi ile zaman arasındaki köprü:
    // Saniye değerlerini (time) yaklaşık z-offset'e çeviriyoruz.
    float groupZStep = Mathf.Max(4f, wave.spawnGroupDelay * 3f);
    float intraZStep = Mathf.Max(1.5f, wave.intraGroupDelay * 3f);

    for (int g = 0; g < wave.groups.Count; g++)
    {
        WaveGroup group = wave.groups[g];
        if (group == null || group.archetype == null) continue;

        for (int i = 0; i < group.count; i++)
        {
            float z = baseZ + (g * groupZStep) + (i * intraZStep);
            Vector3 pos = GetLaneBiasedSpawnPos(group.laneBias, i, group.count, z);
            SpawnArchetypeEnemy(group.archetype, pos);
        }
    }
}

void SpawnArchetypeEnemy(EnemyArchetypeConfig archetype, Vector3 pos)
{
    if (archetype == null) return;

    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    float targetDps = stage != null ? stage.targetDps : 100f;

    // HP ve Ödül, Archetype içindeki formüle göre targetDps üzerinden hesaplanır
    int hp = archetype.GetHP(targetDps);
    int cpReward = archetype.GetCPReward(targetDps);

    var stats = new DifficultyManager.EnemyStats(
        health: hp,
        damage: archetype.contactDamage,
        speed: archetype.moveSpeed,
        cpReward: cpReward
    );

    GameObject obj;
    if (enemyPrefab != null)
    {
        obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
    else
    {
        // Prefab yoksa görsel bir placeholder yaratır
        obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.transform.position = pos;
        Destroy(obj.GetComponent<Collider>());
        var cc = obj.AddComponent<CapsuleCollider>();
        cc.isTrigger = true;
        var rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        obj.tag = "Enemy";
        obj.AddComponent<Enemy>();
    }

    Enemy enemy = obj.GetComponent<Enemy>();
    if (enemy != null)
    {
        enemy.Initialize(stats);
        // Zırh ve elitlik bilgisini buraya işler
        enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
    }
}

Vector3 GetLaneBiasedSpawnPos(LaneBias bias, int index, int total, float z)
{
    float x = 0f;
    float left  = -ROAD_HALF_WIDTH * 0.72f;
    float right =  ROAD_HALF_WIDTH * 0.72f;

    switch (bias)
    {
        case LaneBias.Center:
            x = Random.Range(-1.25f, 1.25f);
            break;
        case LaneBias.Left:
            x = Random.Range(left - 0.8f, left + 0.8f);
            break;
        case LaneBias.Right:
            x = Random.Range(right - 0.8f, right + 0.8f);
            break;
        case LaneBias.Random:
            x = Random.Range(-ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f);
            break;
        case LaneBias.Spread:
        default:
            if (total <= 1) x = 0f;
            else
            {
                float t = (float)index / (total - 1);
                x = Mathf.Lerp(left, right, t);
            }
            break;
    }

    x = Mathf.Clamp(x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);
    return new Vector3(x, 1.2f, z);
}
}
```

## Stageconfig.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v3
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID        = 1;
    public int    stageID        = 1;
    public string locationName = "Frontier Pass";

    // ── Denge ─────────────────────────────────────────────────────────────
    [Header("Denge — Temel Deger")]
    [Tooltip(
        "Bu stage icin hedeflenen oyuncu DPS'i.\n" +
        "HP formulleri bu degere gore hesaplanir:\n" +
        "  Normal mob   = targetDps x 1.0\n" +
        "  Elite mob    = targetDps x 4.0\n" +
        "  Mini-boss HP = targetDps x 18\n" +
        "  Final boss   = targetDps x 36")]
    public float targetDps = 70f;

    [Header("Kapi Butcesi")]
    [Tooltip(
        "Bu stage'deki kapilarin verebilecegi max DPS buyume katsayisi.\n" +
        "entryDps = round(targetDps / gateBudgetMult)\n" +
        "Stage 1-5: 1.40 | 6-9: 1.50 | 10: 1.55 | 11-19: 1.65 | 20: 1.70\n" +
        "Stage 21-29: 1.80 | 30-34: 1.88 | 35: 1.95")]
    [Range(1f, 2.5f)]
    public float gateBudgetMult  = 1.40f;

    // ── Boss Turu ─────────────────────────────────────────────────────────
    [Header("Boss")]
    public BossType bossType     = BossType.None;
    
    // YENİ EKLENEN
    public BossConfig bossConfig;

    [Header("Wave Sequence")]
    [Tooltip("Bu stage boyunca oynatilacak dalga sirasi. Bos birakılırsa eski procedural fallback kullanilir.")]
public List<WaveConfig> waveSequence = new List<WaveConfig>();

    // ── Spawn Yogunlugu ───────────────────────────────────────────────────
    [Header("Spawn")]
    [Tooltip("1.0 = normal. DifficultyManager carpaniyla carpilir.")]
    [Range(0.5f, 3f)]

    
    public float spawnDensity    = 1f;

    // ── Odüller ───────────────────────────────────────────────────────────
    [Header("Odüller")]
    [Tooltip("Bos birakılırsa EconomyConfig formulunden hesaplanir.")]
    public int    goldRewardOverride   = 0;  // 0 = formul kullan
    public bool   hasMidStageLoot      = true;
    [Range(0f, 1f)]
    public float  techCoreDropChance   = 0.15f;
    [Tooltip("Stage tamamlaninca saatlik altina eklenen miktar")]
    public int    offlineBoostPerHour  = 5;

    // ── Tutorial ──────────────────────────────────────────────────────────
    [Header("Ozel")]
    public bool   isTutorialStage    = false;

    // ── HP Formul Metotlari (StageManager kullanir) ───────────────────────

    public int GetNormalMobHP()   => Mathf.RoundToInt(targetDps * 1.0f);
    public int GetEliteHP()       => Mathf.RoundToInt(targetDps * 4.0f);
    public int GetMiniBossHP()    => Mathf.RoundToInt(targetDps * 13f);
    public int GetFinalBossHP()   => Mathf.RoundToInt(targetDps * 36f);

    public int GetBossHP()
    {
        return bossType switch
        {
            BossType.MiniBoss   => GetMiniBossHP(),
            BossType.FinalBoss  => GetFinalBossHP(),
            _                   => 0,
        };
    }

    public int GetEntryDps() => Mathf.RoundToInt(targetDps / gateBudgetMult);

    public bool IsBossStage => bossType != BossType.None;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(name))
            name = $"Stage_W{worldID}_{stageID:D2}";
    }
#endif
}

public enum BossType
{
    None,
    MiniBoss,
    FinalBoss,
}
```

## Stagemanager.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Stage Yoneticisi v2 (Claude) + Vertical Slice Patch
///
/// v2: StageConfig.targetDps formullerine gore HP degerlerini
///     SpawnManager ve BossManager'a iletir.
///     EconomyManager'a altin ekler (Offline boost slice'ta kapali).
///     Dunya bitisinde WorldConfig'deki komutani acar.
///
/// KURULUM:
///   Hierarchy > Create Empty > "StageManager" > bu scripti ekle.
///   worlds[]      : WorldConfig SO'lari sur (World 1, 2, 3...).
///   stageConfigs[]: StageConfig SO'lari sur (veya Resources/Stages/'a koy).
///   economyConfig : EconomyConfig SO'sunu sur.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Dunya Listesi (sirali — World 1, 2, 3...)")]
    public WorldConfig[] worlds;

    [Header("Stage Verileri")]
    [Tooltip("Bos birakılırsa Resources/Stages/Stage_W{w}_{s:D2} yolundan yuklenir")]
    public StageConfig[] stageConfigs;

    [Header("Ekonomi Formulü")]
    public EconomyConfig economyConfig;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] int _currentWorldID = 1;
    [SerializeField] int _currentStageID = 1;

    StageConfig _activeStage;
    WorldConfig _activeWorld;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => LoadStage(_currentWorldID, _currentStageID);

    // ── Stage Yukle ───────────────────────────────────────────────────────
   
    public void LoadStage(int worldID, int stageID)
    {
        RunState.Instance.Reset();
        SaveManager.Instance?.BeginRun();
        _currentWorldID = worldID;
        _currentStageID = stageID;

        _activeWorld = FindWorld(worldID);
        _activeStage = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

        // Stage başında gate bonuslarını sıfırla
        PlayerStats.Instance?.ResetRunGateBonuses();

        // Biyomu guncelle
        if (_activeWorld != null)
            BiomeManager.Instance?.SetBiome(_activeWorld.biome);

        // SpawnManager'a mob HP'yi ilet
        ApplyMobHP();

        // Boss stage ise BossManager'a HP'yi ilet
        if (_activeStage.IsBossStage)
            ApplyBossHP();

        GameEvents.OnStageChanged?.Invoke(worldID, stageID);
        Debug.Log($"[StageManager] W{worldID}-{stageID} | targetDps={_activeStage.targetDps} " +
                  $"| mobHP={_activeStage.GetNormalMobHP()} | bossHP={_activeStage.GetBossHP()}");
    }

    // ── HP Dagitimi ───────────────────────────────────────────────────────
    void ApplyMobHP()
    {
        if (SpawnManager.Instance == null || _activeStage == null) return;

        SpawnManager.Instance.SetMobHP(
            normalHP: _activeStage.GetNormalMobHP(),
            eliteHP:  _activeStage.GetEliteHP(),
            density:  _activeStage.spawnDensity);
    }

    void ApplyBossHP()
    {
        if (BossManager.Instance == null || _activeStage == null) return;
        BossManager.Instance.bossMaxHP = _activeStage.GetBossHP();
        Debug.Log($"[StageManager] Boss HP set: {_activeStage.GetBossHP()} ({_activeStage.bossType})");
    }

    // ── Stage Tamamlandi ─────────────────────────────────────────────────
    public void OnStageComplete()
    {
        

        if (_activeStage == null) return;

        // Altin odulu
        int gold = _activeStage.goldRewardOverride > 0
            ? _activeStage.goldRewardOverride
            : economyConfig != null
                ? economyConfig.GetGoldReward(_activeStage.stageID, _activeStage.targetDps)
                : 150;
        RunState.Instance.AddRunGold(gold);
        EconomyManager.Instance?.AddGold(gold);
        
        // Vertical slice'ta offline boost kapali.
        // EconomyManager.Instance?.AddOfflineRate(_activeStage.offlineBoostPerHour);

        Debug.Log($"[StageManager] Stage tamamlandi. Altin: +{gold}");

        // Sadece FinalBoss world complete sayilsin.
        if (_activeStage.bossType == BossType.FinalBoss)
        {
            OnWorldComplete();
            return;
        }

        // MiniBoss ise normal stage gibi ilerlesin; sonraki stage yoksa slice biter.
        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        
        if (nextStage != null)
        {
            LoadStage(_currentWorldID, _currentStageID + 1);
            return;
        }

        Debug.Log("[StageManager] Vertical slice tamamlandi.");
        GameEvents.OnWorldChanged?.Invoke(_currentWorldID); // mevcut event sistemi bozulmasin
    }

    // ── Stage Ortasi Micro-Loot ───────────────────────────────────────────
    public void OnMidStageReached()
    {
        if (_activeStage == null || !_activeStage.hasMidStageLoot) return;

        int midGold = economyConfig != null
            ? economyConfig.GetMidLootGold(_activeStage.stageID, _activeStage.targetDps)
            : 50;

        EconomyManager.Instance?.AddGold(midGold);
        Debug.Log($"[StageManager] Micro-loot: +{midGold} Altin");

        // Vertical slice economy: TechCore kapali.
        // if (Random.value < _activeStage.techCoreDropChance)
        // {
        //     EconomyManager.Instance?.AddTechCore(1);
        // }
    }

    // ── Dunya Tamamlandi ─────────────────────────────────────────────────
    void OnWorldComplete()
    {
        if (_activeWorld != null)
        {
            EconomyManager.Instance?.AddOfflineRate(_activeWorld.offlineIncomeBoost);
            if (_activeWorld.unlockedCommander != null)
                Debug.Log($"[StageManager] Komutan acildi: {_activeWorld.unlockedCommander.commanderName}");
            
            // TODO: Komutan unlock UI
        }

        GameEvents.OnWorldChanged?.Invoke(_currentWorldID);
        LoadStage(_currentWorldID + 1, stageID: 1);
    }

    // ── Yardimcilar ───────────────────────────────────────────────────────
    WorldConfig FindWorld(int id)
    {
        if (worlds != null)
            foreach (var w in worlds)
                if (w != null && w.worldID == id) return w;
        return null;
    }

    StageConfig FindStage(int worldID, int stageID)
    {
        if (stageConfigs != null)
            foreach (var s in stageConfigs)
                if (s != null && s.worldID == worldID && s.stageID == stageID) return s;
                
        return Resources.Load<StageConfig>($"Stages/Stage_W{worldID}_{stageID:D2}");
    }

    // ── Getter'lar ────────────────────────────────────────────────────────
    public StageConfig ActiveStage => _activeStage;
    public WorldConfig ActiveWorld => _activeWorld;
    public int CurrentWorldID => _currentWorldID;
    public int CurrentStageID => _currentStageID;
}
```

## Tiervisualizer.cs

```csharp
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Tier Gorsel Evrimi v1 (Claude)
///
/// Tier atladikca boyut DEGISMEZ (eski morph sistemi).
/// Bunun yerine:
///   - Aktif model degisir (CommanderData.tierModels[tier-1])
///   - Aura degisir      (CommanderData.tierAuras[tier-1])
///   - Mermi VFX rengi degisir
///   - Tier-up mini event: DOTween scale punch + kisa slow-mo
///
/// KURULUM:
///   Player objesine ekle.
///   CommanderData SO'daki tierModels ve tierAuras dizilerini doldur
///   (bos birakilabilir — dizi yoksa sadece event tetiklenir).
/// </summary>
public class TierVisualizer : MonoBehaviour
{
    [Header("Baglanti (opsiyonel — CommanderData'dan da okunur)")]
    [Tooltip("Bos birakılırsa PlayerStats.activeCommander'dan alinir")]
    public CommanderData commanderOverride;

    [Header("Tier-Up Event Ayarlari")]
    [Tooltip("Scale punch siddeti")]
    public float punchStrength  = 0.25f;
    [Tooltip("Scale punch suresi (saniye)")]
    public float punchDuration  = 0.4f;
    [Tooltip("Slow-motion carpani (0.3 = %30 hiz)")]
    public float slowMoScale    = 0.3f;
    [Tooltip("Slow-motion suresi (saniye, gercek zaman)")]
    public float slowMoDuration = 0.5f;

    // ── Dahili ────────────────────────────────────────────────────────────
    int              _currentTier     = 0;
    CommanderData    _commander;
    ParticleSystem   _activeAura;

    void Start()
    {
        _commander = commanderOverride != null
            ? commanderOverride
            : PlayerStats.Instance?.activeCommander;

        GameEvents.OnTierChanged += OnTierChanged;

        // Baslangic tier'ini uygula (animasyonsuz)
        int startTier = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        ApplyTierVisuals(startTier, animated: false);
    }

    void OnDestroy() => GameEvents.OnTierChanged -= OnTierChanged;

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged(int newTier)
    {
        if (newTier <= _currentTier) return;   // Sadece yukari tier
        _currentTier = newTier;
        ApplyTierVisuals(newTier, animated: true);
    }

    void ApplyTierVisuals(int tier, bool animated)
    {
        _currentTier = tier;
        int idx      = Mathf.Clamp(tier - 1, 0, 4);

        // ── Model degisimi ────────────────────────────────────────────────
        if (_commander != null && _commander.tierModels != null &&
            _commander.tierModels.Length > 0)
        {
            for (int i = 0; i < _commander.tierModels.Length; i++)
            {
                if (_commander.tierModels[i] != null)
                    _commander.tierModels[i].SetActive(i == idx);
            }
        }

        // ── Aura degisimi ─────────────────────────────────────────────────
        if (_commander != null && _commander.tierAuras != null &&
            _commander.tierAuras.Length > 0)
        {
            // Onceki aurayi durdur
            _activeAura?.Stop(withChildren: true);

            if (idx < _commander.tierAuras.Length && _commander.tierAuras[idx] != null)
            {
                _activeAura = _commander.tierAuras[idx];
                _activeAura.Play();
            }
        }

        // ── Tier-up animasyon (sadece ilk kez atlandikta) ─────────────────
        if (animated) TierUpEvent();
    }

    // ── Tier-Up Mini Event ────────────────────────────────────────────────
    void TierUpEvent()
    {
        // Scale punch (DOTween)
        transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 6, 0.5f);

        // Kisa slow-motion (gercek zamanda geri doner)
        if (slowMoScale > 0f && slowMoDuration > 0f)
            SlowMo();

        Debug.Log($"[TierVisualizer] Tier {_currentTier} evrimi!");
    }

    void SlowMo()
    {
        Time.timeScale = slowMoScale;
        // UnscaledTime ile geri yukle
        DOVirtual.DelayedCall(slowMoDuration, ResetTimeScale, ignoreTimeScale: true);
    }

    static void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    // ── Getter ────────────────────────────────────────────────────────────
    public int CurrentTier => _currentTier;
}
```

## Waveconfig.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Dalga Konfigurasyonu v2
///
/// DEĞİŞİKLİK:
///   - OnValidate eklendi
///   - Slice icin guvenli clamp'ler eklendi
/// </summary>
[CreateAssetMenu(fileName = "Wave_", menuName = "TopEndWar/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string waveId = "W1";
    public string waveCode = "W1_TrooperLine";
    public WaveRole waveRole = WaveRole.Normal;

    [Header("Spawn Gruplari")]
    [Tooltip("Her grup ayri bir burst. Gruplar arasi delay spawnGroupDelay saniye.")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Header("Zamanlama")]
    [Tooltip("Gruplar arasi bekleme suresi")]
    public float spawnGroupDelay = 1.2f;

    [Tooltip("Ayni grup icinde dusman spawn araliginda saniye")]
    public float intraGroupDelay = 0.25f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (groups == null)
            groups = new List<WaveGroup>();

        spawnGroupDelay = Mathf.Max(0f, spawnGroupDelay);
        intraGroupDelay = Mathf.Max(0f, intraGroupDelay);

        if (!string.IsNullOrEmpty(waveId))
            name = $"Wave_{waveId}";
    }
#endif
}

[System.Serializable]
public class WaveGroup
{
    [Tooltip("Bu gruptan hangi dusman arketipi?")]
    public EnemyArchetypeConfig archetype;

    [Tooltip("Kac adet ciksin?")]
    [Range(1, 15)]
    public int count = 4;

    [Tooltip("Bu grup icinde tekrar kullanilan lane dagilimi")]
    public LaneBias laneBias = LaneBias.Spread;
}

public enum WaveRole
{
    Normal,
    Elite,
    Boss,
    MixedExam,
}

public enum LaneBias
{
    Spread,
    Center,
    Left,
    Right,
    Random,
}
```

## Weaponarchetypeconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Silah Arketip Konfigurasyonu v2
///
/// Bu dosya SILAH AILESININ TEMEL DOGASIDIR.
/// Runtime state veya item rarity tasimaz.
///
/// Ornek:
///   - Assault nasil ates eder?
///   - SMG kac menzilden vurur?
///   - Sniper hangi hedefi sever?
///
/// Item bonuslari EquipmentData tarafinda kalir.
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "TopEndWar/WeaponArchetypeConfig")]
public class WeaponArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string weaponId = "assault";
    public string weaponName = "Assault Rifle";
    public WeaponFamily family = WeaponFamily.Assault;

    [Header("Combat Kimligi")]
    public TargetProfile defaultTargetProfile = TargetProfile.Balanced;

    [Tooltip("Tek mermi taban hasari")]
    public float baseDamage = 14f;

    [Tooltip("Saniyede atis sayisi")]
    public float fireRate = 3.6f;

    [Tooltip("Etkili menzil")]
    public float attackRange = 20f;

    [Tooltip("Mermi/proje hizi")]
    public float projectileSpeed = 38f;

    [Tooltip("Zirh delme degeri")]
    public int armorPen = 6;

    [Tooltip("Ayni atista cikan mermi sayisi (1 = tek mermi, 6 = shotgun pellet)")]
    public int projectileCount = 1;

    [Tooltip("Delme: 0 = delmez, 1 = ek 1 dusmani deler")]
    public int pierceCount = 0;

    [Tooltip("Sekme: 0 = sekmez")]
    public int bounceCount = 0;

    [Tooltip("Patlama yaricapi (0 = yok)")]
    public float splashRadius = 0f;

    [Header("Denge Skoru (Tasarim referansi)")]
    [Range(0f, 2f)]
    public float packFactor = 1.05f;

    [Range(0, 10)] public int runnerScore = 8;
    [Range(0, 10)] public int bossScore = 7;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public GameObject modelPrefab;

    public float RawSingleDPS => baseDamage * fireRate;

    public float GetArmorDamageMultiplier(int targetArmor)
    {
        int effectiveArmor = Mathf.Max(0, targetArmor - armorPen);
        return 100f / (100f + effectiveArmor);
    }

    public float GetEffectiveDPS(int targetArmor)
        => RawSingleDPS * GetArmorDamageMultiplier(targetArmor);

#if UNITY_EDITOR
    void OnValidate()
    {
        baseDamage = Mathf.Max(1f, baseDamage);
        fireRate = Mathf.Max(0.05f, fireRate);
        attackRange = Mathf.Max(1f, attackRange);
        projectileSpeed = Mathf.Max(1f, projectileSpeed);
        armorPen = Mathf.Max(0, armorPen);
        projectileCount = Mathf.Max(1, projectileCount);
        pierceCount = Mathf.Max(0, pierceCount);
        bounceCount = Mathf.Max(0, bounceCount);
        splashRadius = Mathf.Max(0f, splashRadius);

        if (!string.IsNullOrEmpty(weaponId))
            name = $"Weapon_{family}";
    }
#endif
}

public enum WeaponFamily
{
    Assault,
    SMG,
    Sniper,
    Shotgun,
    Launcher,
    Beam
}

public enum TargetProfile
{
    Balanced,
    NearestThreat,
    EliteHunter,
    Finisher,
    ClusterFocus
}
```

## Worldconfig.cs

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "World_", menuName = "TopEndWar/WorldConfig")]
public class WorldConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int worldID = 1;
    public string worldName = "Frontier One";

    [Header("Biyom")]
    [Tooltip("Global, kurgusal veya evrensel biome etiketi kullan.")]
    public string biome = "Temperate Frontier";

    [Header("Stage Yapisi")]
    public int stageCount = 35;

    [Header("Rarity Esigi")]
    [Range(1, 5)]
    public int maxRarity = 2;

    [Header("Komutan Kilidi")]
    public CommanderData unlockedCommander;

    [Header("Offline Kazanc")]
    public int offlineIncomeBoost = 0;

    [Header("Gorunumler")]
    public Color mapColor = Color.green;

#if UNITY_EDITOR
    void OnValidate()
    {
        worldID = Mathf.Max(1, worldID);
        stageCount = Mathf.Max(1, stageCount);
        maxRarity = Mathf.Clamp(maxRarity, 1, 5);
        offlineIncomeBoost = Mathf.Max(0, offlineIncomeBoost);

        if (!string.IsNullOrEmpty(worldName))
            name = $"World_{worldID}_{worldName}";
    }
#endif
}
```
