# Birleştirilmiş Tüm C# Scriptleri

Bu dosya, Assets/Scripts ve _TopEndWar/UI altındaki tüm .cs dosyalarını içermektedir.

### Klasör: Assets\Scripts

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

/// <summary>/// Top End War — Boss Yoneticisi v7 (Claude & Patch Integrated)
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
    //GameEvents.OnAnchorModeChanged?.Invoke(true);

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
using UnityEngine.Rendering;

/// <summary>
/// Top End War — Mermi v1.2 (Gameplay Fix Patch)
///
/// v1.1 → v1.2 Fix Delta:
///   • Physics.OverlapCapsule(): QueryTriggerInteraction.Collide eklendi.
///     Proje Physics Settings'de "Queries Hit Triggers" kapalıysa,
///     enemy'nin isTrigger collider'ı hiç algılanmıyordu → 0 hasar.
///     Bu parametre explicit olarak trigger'ları da yakalar.
///   • HIT_RADIUS: 0.5f (v1.1'den korundu)
/// </summary>
public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);

    [HideInInspector] public string hitterPath = "Commander";
    [HideInInspector] public int   armorPen = 0;
    [HideInInspector] public int   pierceCount = 0;
    [HideInInspector] public float eliteDamageMult = 1f;
    [HideInInspector] public float bossDamageMult = 1f;

    const float HIT_RADIUS = 0.5f;
    const float LIFETIME   = 1.8f;
    const float DEFAULT_MAX_RANGE = 24f;

    Renderer _rend;
    TrailRenderer _trail;
    static Material _trailMaterial;
    bool _hit = false;
    float _maxRange = DEFAULT_MAX_RANGE;

    int _remainingPierce = 0;
    readonly HashSet<int> _hitTargets = new HashSet<int>();

    Vector3 _spawnPos;
    Vector3 _lastPos;
    float _trailTime = 0.10f;
    float _trailStartWidth = 0.08f;
    float _trailEndWidth = 0.008f;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        EnsureTrail();
    }

    void OnEnable()
    {
        _hit = false;
        _remainingPierce = Mathf.Max(0, pierceCount);
        _hitTargets.Clear();
        _maxRange = DEFAULT_MAX_RANGE;
        ConfigureTrail(0.08f, 0.060f, 0.006f);
        EnsureTrail();
        ApplyColor();

        // ObjectPooler artık pozisyonu SetActive'den önce atıyor,
        // bu yüzden _lastPos burada doğru pozisyonu yakalar.
        _spawnPos = transform.position;
        _lastPos = transform.position;

        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
        _remainingPierce = 0;
        _hitTargets.Clear();
        _maxRange = DEFAULT_MAX_RANGE;
        if (_trail != null) _trail.Clear();
    }

    public void SetDamage(int d) => damage = d;

    public void SetMaxRange(float range)
    {
        _maxRange = Mathf.Max(4f, range);
    }

    public void SetTrailProfile(WeaponFamily family)
    {
        switch (family)
        {
            case WeaponFamily.SMG:
                ConfigureTrail(0.06f, 0.045f, 0.004f);
                break;
            case WeaponFamily.Sniper:
                ConfigureTrail(0.10f, 0.075f, 0.008f);
                break;
            default:
                ConfigureTrail(0.08f, 0.060f, 0.006f);
                break;
        }
    }

    public void SetTracerColor(Color color)
    {
        bulletColor = color;
        ApplyColor();
    }

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

        if (Vector3.Distance(_spawnPos, transform.position) >= _maxRange)
        {
            ReturnToPool();
            return;
        }

        // FIX: QueryTriggerInteraction.Collide — isTrigger collider'ları da algıla.
        // "Queries Hit Triggers" project ayarından bağımsız olarak çalışır.
        Collider[] cols = Physics.OverlapCapsule(
            _lastPos,
            transform.position,
            HIT_RADIUS,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider col in cols)
        {
            // FIX: Deaktif objeleri atla.
            if (!col.gameObject.activeInHierarchy) continue;

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
                SpawnImpactVfx(col.transform.position, DamagePopup.GetColor(hitterPath));
            }
            else if (enemy != null)
            {
                enemy.TakeDamage(
                    rawDamage: damage,
                    armorPenValue: armorPen,
                    eliteMultiplier: eliteDamageMult,
                    hitColor: DamagePopup.GetColor(hitterPath));
                SpawnImpactVfx(col.transform.position, DamagePopup.GetColor(hitterPath));
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
        if (_rend != null)
        {
            _rend.enabled = true;

            if (_rend.material.HasProperty("_BaseColor"))
                _rend.material.SetColor("_BaseColor", bulletColor);
            else
                _rend.material.color = bulletColor;
        }

        if (_trail != null)
        {
            _trail.startColor = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0.95f);
            _trail.endColor   = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0f);
            _trail.time       = _trailTime;
            _trail.startWidth = _trailStartWidth;
            _trail.endWidth   = _trailEndWidth;
        }
    }

    void EnsureTrail()
    {
        if (_trail == null)
            _trail = GetComponent<TrailRenderer>() ?? gameObject.AddComponent<TrailRenderer>();

        _trail.minVertexDistance = 0.05f;
        _trail.alignment = LineAlignment.View;
        _trail.shadowCastingMode = ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.generateLightingData = false;
        _trail.material = GetTrailMaterial();
        _trail.emitting = true;
        ApplyColor();
    }

    void ConfigureTrail(float time, float startWidth, float endWidth)
    {
        _trailTime = time;
        _trailStartWidth = startWidth;
        _trailEndWidth = endWidth;
        ApplyColor();
    }

    void SpawnImpactVfx(Vector3 pos, Color color)
    {
        var go = new GameObject("BulletImpactVfx");
        go.transform.position = pos;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 1.5f;
        light.intensity = 1.2f;
        light.color = color;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.12f;
        main.startSpeed = 1.8f;
        main.startSize = 0.12f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ps.Play();
        Destroy(go, 0.35f);
    }

    static Material GetTrailMaterial()
    {
        if (_trailMaterial == null)
            _trailMaterial = new Material(Shader.Find("Sprites/Default"));
        return _trailMaterial;
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
/// Top End War — Dusman v7.2 (Gameplay Fix Patch)
///
/// v7.1 → v7.2 Fix Delta:
///   • playerTouchInterval: 0.20f → 0.50f
///     0.20f = 5 vuruş/saniye, çok agresif ve tutarsız hissettiriyordu.
///     0.50f = 2 vuruş/saniye, kontrollü tick damage hissi verir.
///     OnTriggerStay mekanizması değişmedi; sadece interval güncellendi.
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

    float _nextPlayerDamageTime = 0f;

    // FIX: 0.20f → 0.50f  (5 hit/s → 2 hit/s — kontrollü tick damage)
    [SerializeField] float playerTouchInterval = 0.50f;
    [SerializeField] bool logContactDamage = false;

    [SerializeField] float engageDistance = 14f;
    [SerializeField] float hardEngageDistance = 8f;
    [SerializeField] float preEngageLateralSpeed = 0.45f;
    [SerializeField] float engageLateralSpeed = 1.8f;
    [SerializeField] float hardEngageLateralBoost = 1.35f;
    [SerializeField] float outerLaneInwardFactor = 0.72f;

    float _spawnLaneX = 0f;
    bool _spawnLaneCaptured = false;

    Renderer _bodyRenderer;
    EnemyHealthBar _hpBar;

    float _lastSepTime = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    int _reservationCount = 0;
    int _reservationCap = 2;
    float _threatWeight = 1f;
    Color _baseColor = Color.white;

    float _speedMult = 1f;
    float _engageDistanceMult = 1f;
    float _hardEngageDistanceMult = 1f;
    float _preLateralMult = 1f;
    float _engageLateralMult = 1f;
    float _hardLateralBoostMult = 1f;
    float _outerLaneMult = 1f;
    float _separationMult = 1f;
    Vector3 _baseScale = Vector3.one;
    bool _baseScaleCaptured = false;
    float _spawnIntroEndTime = -1f;
    const float SPAWN_INTRO_DURATION = 0.4f;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();

        CaptureBaseScale();
        UseDefaults();
    }

    void OnEnable()
    {
        _isDead = false;
        _nextPlayerDamageTime = 0f;
        _separationVec = Vector3.zero;
        _reservationCount = 0;
        CaptureSpawnLane();
        BeginSpawnIntro();
        ApplyBehaviorProfile(EnemyThreatType.Standard, EnemyClass.Normal);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        if (!_initialized)
            AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    void OnDisable()
    {
        CancelInvoke();
        _initialized          = false;
        _reservationCount     = 0;
        _isDead               = false;
        _nextPlayerDamageTime = 0f;
        if (_baseScaleCaptured)
            transform.localScale = _baseScale;
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth     = stats.Health;
        _currentHealth = _maxHealth;
        _contactDamage = stats.Damage;
        _moveSpeed     = stats.Speed;
        _cpReward      = stats.CPReward;
        _initialized   = true;
        _isDead        = false;
        _nextPlayerDamageTime = 0f;
        _reservationCount = 0;
        CaptureSpawnLane();

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

    void CaptureSpawnLane()
    {
        _spawnLaneX = transform.position.x;
        _spawnLaneCaptured = true;
    }

    void CaptureBaseScale()
    {
        if (_baseScaleCaptured) return;
        _baseScale = transform.localScale;
        _baseScaleCaptured = true;
    }

    void BeginSpawnIntro()
    {
        CaptureBaseScale();
        _spawnIntroEndTime = Time.time + SPAWN_INTRO_DURATION;
        transform.localScale = _baseScale * 0.75f;
    }

    float UpdateSpawnIntro()
    {
        if (_spawnIntroEndTime <= 0f)
            return 1f;

        float t = 1f - ((_spawnIntroEndTime - Time.time) / SPAWN_INTRO_DURATION);
        t = Mathf.Clamp01(t);
        transform.localScale = Vector3.Lerp(_baseScale * 0.75f, _baseScale, t);
        return t;
    }

    bool IsInSpawnIntro()
    {
        return Time.time < _spawnIntroEndTime;
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
        ApplyBehaviorProfile(EnemyThreatType.Standard, EnemyClass.Normal);
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        if (!_spawnLaneCaptured)
            CaptureSpawnLane();

        float dt = Time.deltaTime;
        float introT = UpdateSpawnIntro();
        float introMoveMult = Mathf.Lerp(0.35f, 1f, introT);

        Transform playerTf = PlayerStats.Instance.transform;
        float pZ = playerTf.position.z;
        float pX = playerTf.position.x;

        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * _speedMult * introMoveMult * dt;

        float distanceAhead = pos.z - pZ;

        float approachTargetX = _spawnLaneX;
        if (Mathf.Abs(_spawnLaneX) > xLimit * 0.45f)
            approachTargetX = _spawnLaneX * Mathf.Clamp01(outerLaneInwardFactor * _outerLaneMult);

        float engageT = Mathf.InverseLerp(
            engageDistance * _engageDistanceMult,
            hardEngageDistance * _hardEngageDistanceMult,
            distanceAhead);
        float targetX = Mathf.Lerp(approachTargetX, pX, engageT);

        float lateralSpeed = Mathf.Lerp(
            preEngageLateralSpeed * _preLateralMult,
            engageLateralSpeed * _engageLateralMult,
            engageT);

        if (distanceAhead <= hardEngageDistance && Mathf.Abs(pos.x - pX) > 2.5f)
            lateralSpeed *= hardEngageLateralBoost * _hardLateralBoostMult;

        pos.x = Mathf.MoveTowards(pos.x, targetX, lateralSpeed * introMoveMult * dt);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime   = Time.time;
        }

        pos.x += _separationVec.x * dt;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f)
            gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep = Vector3.zero;
        int count   = 0;

        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;

            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            away.z = 0f;

            if (away.magnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, 0f).normalized * 0.1f;

            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        return count > 0 ? Vector3.ClampMagnitude(sep / count, 1f) * 1.4f * _separationMult : Vector3.zero;
    }

    public void TakeDamage(int rawDamage, int armorPenValue = 0, float eliteMultiplier = 1f, Color? hitColor = null)
    {
        if (_isDead) return;

        int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPenValue));
        float armorMult    = 100f / (100f + effectiveArmor);

        // FIX: Mathf.Max(1,...) zırh sıfır hasar yapmasını engeller.
        int finalDamage    = Mathf.Max(1, Mathf.RoundToInt(rawDamage * armorMult));

        if (_isElite)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, eliteMultiplier)));

        _currentHealth -= finalDamage;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.red;

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), 0.1f);

        bool isCrit   = finalDamage > 200;
        Color popupColor = hitColor ?? DamagePopup.GetColor("Commander");
        DamagePopup.Show(transform.position, finalDamage, popupColor, isCrit);

        if (_currentHealth <= 0)
            Die();
    }

    public void ConfigureCombat(int armor, bool isElite)
    {
        _armor   = Mathf.Max(0, armor);
        _isElite = isElite;

        _reservationCap = _isElite ? 3 : 2;
        _threatWeight   = _isElite ? 1.35f : 1f;
        _baseColor      = _isElite ? new Color(1f, 0.92f, 0.35f) : Color.white;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;
    }

    public void ConfigureArchetype(EnemyArchetypeConfig archetype)
    {
        if (archetype == null) return;
        ApplyBehaviorProfile(archetype.threatType, archetype.enemyClass);
    }

    void ApplyBehaviorProfile(EnemyThreatType threatType, EnemyClass enemyClass)
    {
        _speedMult = 1f;
        _engageDistanceMult = 1f;
        _hardEngageDistanceMult = 1f;
        _preLateralMult = 1f;
        _engageLateralMult = 1f;
        _hardLateralBoostMult = 1f;
        _outerLaneMult = 1f;
        _separationMult = 1f;

        switch (threatType)
        {
            case EnemyThreatType.PackPressure:
                _speedMult = 1.18f;
                _engageDistanceMult = 1.25f;
                _hardEngageDistanceMult = 1.15f;
                _preLateralMult = 1.15f;
                _engageLateralMult = 1.25f;
                _outerLaneMult = 0.90f;
                _separationMult = 0.75f;
                break;

            case EnemyThreatType.Durable:
                _speedMult = 0.78f;
                _engageDistanceMult = 0.85f;
                _hardEngageDistanceMult = 0.90f;
                _preLateralMult = 0.55f;
                _engageLateralMult = 0.60f;
                _hardLateralBoostMult = 0.70f;
                _outerLaneMult = 1.15f;
                _separationMult = 0.60f;
                break;

            case EnemyThreatType.ElitePressure:
                _speedMult = 1.35f;
                _engageDistanceMult = 1.35f;
                _hardEngageDistanceMult = 1.20f;
                _preLateralMult = 1.10f;
                _engageLateralMult = 1.55f;
                _hardLateralBoostMult = 1.55f;
                _separationMult = 0.70f;
                break;

            case EnemyThreatType.Priority:
            case EnemyThreatType.Backline:
                _speedMult = 0.92f;
                _engageDistanceMult = 0.95f;
                _hardEngageDistanceMult = 0.90f;
                _preLateralMult = 0.65f;
                _engageLateralMult = 0.75f;
                _hardLateralBoostMult = 0.85f;
                _outerLaneMult = 1.10f;
                _separationMult = 0.80f;
                break;
        }

        if (enemyClass == EnemyClass.BossSupport)
        {
            _speedMult *= 0.88f;
            _engageLateralMult *= 0.80f;
            _outerLaneMult *= 1.10f;
        }
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

        if (other.CompareTag("Player"))
            TryDamagePlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (_isDead) return;
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other);
    }

    void TryDamagePlayer(Collider other)
    {
        if (IsInSpawnIntro()) return;
        if (Time.time < _nextPlayerDamageTime) return;

        PlayerStats ps = PlayerStats.Instance
                      ?? other.GetComponent<PlayerStats>()
                      ?? other.GetComponentInParent<PlayerStats>();

        if (ps == null)
        {
            Debug.LogWarning($"[Enemy] PlayerStats bulunamadi — contact damage uygulanamadi. " +
                             $"Player objesinin Tag'i 'Player' ve PlayerStats script'i root'ta olmali.");
            _nextPlayerDamageTime = Time.time + playerTouchInterval;
            return;
        }

        bool applied = ps.TryTakeContactDamage(_contactDamage);

        // FIX: interval 0.5s — kontrollü tick damage, 5 vuruş/s değil 2 vuruş/s
        _nextPlayerDamageTime = Time.time + playerTouchInterval;

        if (!logContactDamage) return;

        if (applied)
            Debug.Log($"[Enemy] Contact damage APPLIED: {_contactDamage}");
        else
            Debug.Log($"[Enemy] Contact damage BLOCKED");
    }

    public int   Armor                => _armor;
    public bool  IsElite              => _isElite;
    public bool  IsAlive              => !_isDead && gameObject.activeInHierarchy && _currentHealth > 0;
    public int   ReservationCount     => _reservationCount;
    public bool  CanAcceptReservation => _reservationCount < _reservationCap;
    public float ThreatWeight         => _threatWeight;
    public float HealthRatio          => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 1f;
}

```

## Enemyarchetypeconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dusman Arketip Konfigurasyonu v2.1
///
/// v2 → v2.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: displayNameKey, descriptionKey, roleKey, threatTag1Key, threatTag2Key
///   • DisplayName, DisplayDescription, DisplayRole, DisplayThreatTag1, DisplayThreatTag2 property'leri eklendi
///   • Mevcut enemyName ve tüm stat alanları DOKUNULMADI
///
/// Eski alanlar:
///   enemyName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > EnemyArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TopEndWar/EnemyArchetypeConfig")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string enemyId = "trooper";
    public string enemyName = "Trooper";
    public EnemyClass enemyClass = EnemyClass.Normal;

    // ── Localization Keys ──────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Display property'leri fallback olarak enemyName vb. döner.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Düşman görünen adı anahtarı  ör: enemy_trooper_name")]
    public string displayNameKey  = "";
    [Tooltip("Kısa açıklama / flavor text anahtarı  ör: enemy_trooper_desc")]
    public string descriptionKey  = "";
    [Tooltip("Rol / davranış etiketi anahtarı  ör: enemy_trooper_role  →  'Standart Piyade'")]
    public string roleKey         = "";
    [Tooltip("Tehdit UI sol tag anahtarı  ör: enemy_trooper_threat1  →  'HIZLI'")]
    public string threatTag1Key   = "";
    [Tooltip("Tehdit UI sağ tag anahtarı  ör: enemy_trooper_threat2  →  'SÜRÜ'")]
    public string threatTag2Key   = "";

    // ── Display Properties (Localization-ready fallback) ───────────────────
    public string DisplayName        => string.IsNullOrEmpty(displayNameKey)  ? enemyName : displayNameKey;
    public string DisplayDescription => string.IsNullOrEmpty(descriptionKey)  ? ""         : descriptionKey;
    public string DisplayRole        => string.IsNullOrEmpty(roleKey)         ? ""         : roleKey;
    public string DisplayThreatTag1  => string.IsNullOrEmpty(threatTag1Key)   ? ""         : threatTag1Key;
    public string DisplayThreatTag2  => string.IsNullOrEmpty(threatTag2Key)   ? ""         : threatTag2Key;

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
        ps.RefreshWeaponDerivedStats();
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
    public struct StageClearInfo
    {
        public int worldID;
        public int stageID;
        public string stageName;
        public int goldReward;
        public bool hasNextStage;
        public bool worldCleared;
    }

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
    public static Action<StageClearInfo> OnStageCleared;

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

    [Header("Runtime Combat Readout (debug)")]
    public TextMeshProUGUI combatReadoutText;

    bool _autoBuilt = false;
    int  _lastCP    = 0;
    float _nextCombatReadoutRefresh = 0f;

    void Start()
    {
        if (PlayerStats.Instance == null)
        { Debug.LogError("GameHUD: PlayerStats yok!"); return; }

        if (cpText == null || tierText == null) AutoBuildHUD();
        EnsureCombatReadout();

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
        RefreshCombatReadout();
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

    void Update()
    {
        if (Time.time < _nextCombatReadoutRefresh) return;
        _nextCombatReadoutRefresh = Time.time + 0.35f;
        if (combatReadoutText == null)
            EnsureCombatReadout();
        RefreshCombatReadout();
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

        EnsureCombatReadout();

        _autoBuilt = true;
        Debug.Log("[GameHUD v8] AutoBuild tamamlandi.");
    }

    void EnsureCombatReadout()
    {
        if (combatReadoutText != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        combatReadoutText = MakeText(canvas.gameObject, "Combat", new Vector2(1f, 1f), new Vector2(-145f, -130f), 20, new Color(0.86f, 0.96f, 1f));
        RectTransform r = combatReadoutText.GetComponent<RectTransform>();
        r.pivot = new Vector2(1f, 1f);
        r.sizeDelta = new Vector2(270f, 190f);
        combatReadoutText.alignment = TextAlignmentOptions.TopRight;
        combatReadoutText.raycastTarget = false;
    }

    void RefreshCombatReadout()
    {
        if (combatReadoutText == null || PlayerStats.Instance == null) return;

        PlayerStats.RuntimeCombatSnapshot c = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        
        string line1 = $"DPS: {c.DisplayedDPS:0} | FR: {c.FireRate:0.0}";
        string line2 = $"Bullet: {c.BulletDamage}x{c.ProjectileCount} | Range: {c.WeaponRange:0}";
        string line3 = $"Pen: {c.ArmorPen} | Pierce: {c.PierceCount}";
        string line4 = $"HP: {c.CurrentHP}/{c.MaxHP} | Pwr: {c.CombatPower:N0}";
        
        // Stage info (optional, tutarlılık check)
        string stageInfo = "";
        StageManager sm = StageManager.Instance;
        if (sm != null && sm.GetActiveStageConfig() != null)
        {
            StageConfig stage = sm.GetActiveStageConfig();
            int targetPower = stage.GetEffectiveTargetPower();
            float targetDps = stage.targetDps;
            
            // Durum: Underpowered / Risky / Ready / Overkill
            string state = "Ready";
            if (c.CombatPower < targetPower * 0.7f)
                state = "Underpowered";
            else if (c.CombatPower < targetPower)
                state = "Risky";
            else if (c.CombatPower >= targetPower * 1.3f)
                state = "Overkill";
            
            stageInfo = $"\nTarget: {targetDps:0} DPS / {targetPower:N0} Pwr\n{state}";
        }
        
        combatReadoutText.text = line1 + "\n" + line2 + "\n" + line3 + "\n" + line4 + stageInfo;
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
        RefreshCombatReadout();

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
        RefreshCombatReadout();
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

    void OnBulletCount(int c)
    {
        RefreshCombatReadout();
        ShowPopup($"+MERMI {c}", new Color(0.5f,0,0.9f));
    }

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
        RefreshCombatReadout();
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
        if (!damageFlashImage) yield break;
        damageFlashImage.color = new Color(1,0,0,0.55f);
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (!damageFlashImage) yield break;
            damageFlashImage.color = new Color(1,0,0, Mathf.Lerp(0.55f,0,t/0.4f));
            yield return null;
        }
        if (!damageFlashImage) yield break;
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
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using TopEndWar.UI.Core;

/// <summary>
/// Top End War — Game Over Arayuzu v4 (Claude)
///
/// v4 → v4.1 Runtime Patch Delta:
///   • _gameOverShown flag eklendi: ShowGameOver() ayni run'da ikinci kez
///     cagrilirsa hic bir sey yapmaz. Bu; PlayerStats._isDead guard'i ve
///     PlayerController._gameOver flag'i ile birlikte uclu savunma hatti olusturur.
///   • ResetRunTracking(): _gameOverShown sifirlanir — yeni run temiz baslar.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel  (Bos birakılırsa kod otomatik olusturur)")]
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

    [Header("Sahne Isimleri")]
    [Tooltip("Ana menu sahnesi — Inspector'dan ata veya default 'MainMenu' kullanilir")]
    public string mainMenuSceneName = "MainMenu";

    bool _reviveUsed    = false;
    int  _runGoldEarned = 0;
    int  _runTechEarned = 0;
    bool _fallbackBuilt = false;

    // PATCH: Ayni run icinde GameOver'in ikinci kez tetiklenmesini engeller.
    bool _gameOverShown = false;

    // ── Yasam Dongusu ─────────────────────────────────────────────────────

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
        _runGoldEarned  = 0;
        _runTechEarned  = 0;
        _reviveUsed     = false;
        _gameOverShown  = false;   // PATCH: yeni run icin sifirla
    }

    // ── Game Over ─────────────────────────────────────────────────────────

    void ShowGameOver()
    {
        // PATCH: Cift tetiklenme korumasi.
        // PlayerStats._isDead ve PlayerController._gameOver zaten engellemeye calisiyor;
        // bu ucuncu hat olarak burada da erken cikis saglar.
        if (_gameOverShown)
        {
            Debug.Log("[GameOverUI] ShowGameOver BLOCKED — zaten gosterildi.");
            return;
        }
        _gameOverShown = true;

        if (TryShowModernDefeatUI())
        {
            Time.timeScale = 0f;
            Debug.Log("[GameOverUI] Yeni Result UI defeat ekrani gosterildi.");
            return;
        }

        if (gameOverPanel == null && !_fallbackBuilt)
            BuildFallbackPanel();

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;

        UpdateScoreDisplay();
        UpdateReviveButton();
        UpdateRetreatButton();

        Debug.Log("[GameOverUI] Game Over ekrani gosterildi.");
    }

    bool TryShowModernDefeatUI()
    {
        UIScreenManager screenManager = FindFirstObjectByType<UIScreenManager>();
        if (screenManager == null)
            screenManager = CreateRuntimeScreenManager();

        if (screenManager == null)
            return false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        screenManager.Bootstrap();
        screenManager.ShowResultDefeat();
        return true;
    }

    UIScreenManager CreateRuntimeScreenManager()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("RuntimeResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = new GameObject("UIRoot", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return root.AddComponent<UIScreenManager>();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    // ── Skor Guncelleme ───────────────────────────────────────────────────

    void UpdateScoreDisplay()
    {
        int dist = Mathf.RoundToInt(
            PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f);
        int cp = PlayerStats.Instance != null ? PlayerStats.Instance.CP : 0;

        int kills = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentRunKills
            : (RunState.Instance != null ? RunState.Instance.KillCount : 0);

        if (distanceText  != null) distanceText.text  = $"{dist} m";
        if (killText      != null) killText.text       = $"{kills}";
        if (cpText        != null) cpText.text         = $"{cp}";

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
        Debug.Log("[GameOverUI] Reklam placeholder — Revive verildi.");
        OnReviveGranted();
    }

    void OnReviveGranted()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        PlayerStats.Instance?.ReviveFromGameOver();
        Object.FindAnyObjectByType<Playercontroller>()?.ResumeRun();

        // PATCH: Revive sonrasi bir sonraki olum tekrar GameOver gosterebilsin.
        _gameOverShown = false;

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

    // ── Restart / Main Menu ───────────────────────────────────────────────

    void OnRestartClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnMainMenuClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[GameOverUI] mainMenuSceneName bos. Inspector'dan ata.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Fallback Panel (Inspector refs yoksa) ─────────────────────────────

    void BuildFallbackPanel()
    {
        _fallbackBuilt = true;

        var canvasGO = new GameObject("GameOver_FallbackCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        gameOverPanel = canvasGO;

        var bg = MakeFBImage(canvasGO, "BG", new Color(0.05f, 0.05f, 0.12f, 0.92f));
        StretchRT(bg.GetComponent<RectTransform>());

        MakeFBText(canvasGO, "GAME OVER",
            new Vector2(0.5f, 0.78f), new Vector2(0, 0), 80,
            new Color(1f, 0.25f, 0.25f), FontStyles.Bold);

        distanceText = MakeFBText(canvasGO, "— m",
            new Vector2(0.5f, 0.62f), new Vector2(0, 0), 34, Color.white, FontStyles.Normal);

        killText = MakeFBText(canvasGO, "0 kill",
            new Vector2(0.5f, 0.56f), new Vector2(0, 0), 30,
            new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        cpText = MakeFBText(canvasGO, "CP: 0",
            new Vector2(0.5f, 0.50f), new Vector2(0, 0), 30,
            new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        restartButton = MakeFBButton(canvasGO, "TEKRAR OYNA",
            new Vector2(0.5f, 0.32f), new Vector2(400, 100),
            new Color(0.15f, 0.70f, 0.20f));
        restartButton.onClick.AddListener(OnRestartClicked);

        mainMenuButton = MakeFBButton(canvasGO, "ANA MENU",
            new Vector2(0.5f, 0.20f), new Vector2(400, 80),
            new Color(0.20f, 0.20f, 0.55f));
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        reviveButton = MakeFBButton(canvasGO, "REKLAM: DEVAM ET",
            new Vector2(0.5f, 0.44f), new Vector2(400, 80),
            new Color(0.70f, 0.55f, 0.10f));
        reviveInfoText = MakeFBText(canvasGO, "Reklam izle",
            new Vector2(0.5f, 0.41f), new Vector2(0, 0), 22,
            new Color(0.7f, 0.7f, 0.7f), FontStyles.Italic);
        reviveButton.onClick.AddListener(OnReviveClicked);

        Debug.Log("[GameOverUI] Fallback panel olusturuldu. Inspector'dan gercek paneli bagla.");
    }

    TextMeshProUGUI MakeFBText(GameObject parent, string text,
        Vector2 anchor, Vector2 offset, float size, Color color, FontStyles style)
    {
        var go = new GameObject("FBText_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = offset; r.sizeDelta = new Vector2(900, 80);
        return t;
    }

    Button MakeFBButton(GameObject parent, string label, Vector2 anchor,
        Vector2 size, Color bgColor)
    {
        var go = new GameObject("FBBtn_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = bgColor;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = size;
        var lbl = new GameObject("Label"); lbl.transform.SetParent(go.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = size.y * 0.32f; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        return btn;
    }

    Image MakeFBImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = color;
        return img;
    }

    void StretchRT(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
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
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Top End War - Gate runtime
/// Spawn aninda config snapshot alir ve sonrasinda degismez.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateConfig gateConfig;
    public Renderer panelRenderer;
    public TextMeshPro labelText;

    static readonly Dictionary<int, int> ConsumedChoiceGroups = new Dictionary<int, int>();

    bool _triggered;
    int _choiceGroupId;
    GateRuntimeData _runtimeData;

    void Start()
    {
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable()
    {
        _triggered = false;
    }

    public static void ResetChoiceState()
    {
        ConsumedChoiceGroups.Clear();
    }

    public static bool TryConsumeGroup(int choiceGroupId, int gateInstanceId)
    {
        if (choiceGroupId <= 0)
        {
            Debug.LogWarning("[Gate] choiceGroupId missing - treating gate as single standalone choice.");
            return true;
        }

        if (ConsumedChoiceGroups.ContainsKey(choiceGroupId))
            return false;

        ConsumedChoiceGroups.Add(choiceGroupId, gateInstanceId);
        return true;
    }

    public void SetChoiceGroup(int choiceGroupId)
    {
        _choiceGroupId = Mathf.Max(0, choiceGroupId);
    }

    public void BindGateConfig(GateConfig config)
    {
        gateConfig = config;
        _runtimeData = GateRuntimeData.FromConfig(config);
        Refresh();
    }

    public void Refresh()
    {
        ApplyVisuals();
        FitBoxCollider();
    }

    GateRuntimeData GetRuntimeData()
    {
        if (_runtimeData != null) return _runtimeData;
        if (gateConfig == null) return null;
        _runtimeData = GateRuntimeData.FromConfig(gateConfig);
        return _runtimeData;
    }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.gameObject != gameObject)
                Destroy(col);
        }
    }

    void ApplyVisuals()
    {
        GateRuntimeData data = GetRuntimeData();
        if (data == null) return;

        if (labelText != null)
        {
            string sub = string.IsNullOrWhiteSpace(data.tag2)
                ? data.tag1
                : $"{data.tag1} • {data.tag2}";

            labelText.text = $"{data.title}\n<size=55%>{sub}</size>";
            labelText.fontSize = 5f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c = data.gateColor;
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
        TryApplyGate(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryApplyGate(other);
    }

    void TryApplyGate(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;

        if (!TryConsumeGroup(_choiceGroupId, GetInstanceID()))
        {
            _triggered = true;
            DisablePassiveGate();
            return;
        }

        _triggered = true;
        DisableOtherGatesInGroup();

        GateRuntimeData data = GetRuntimeData();
        if (data == null)
        {
            DisablePassiveGate();
            return;
        }

        PlayerStats ps = PlayerStats.Instance
                      ?? other.GetComponent<PlayerStats>()
                      ?? other.GetComponentInParent<PlayerStats>();

        if (ps != null)
            ps.ApplyGateConfig(data);
        else
            Debug.LogWarning("[Gate] PlayerStats not found - gate effect skipped.");

        other.GetComponent<GateFeedback>()?.PlayGatePop();

        Debug.Log($"[Gate] {data.title}");
        Destroy(gameObject);
    }

    void DisableOtherGatesInGroup()
    {
        if (_choiceGroupId <= 0) return;

        foreach (Gate gate in FindObjectsByType<Gate>(FindObjectsSortMode.None))
        {
            if (gate == null || gate == this) continue;
            if (gate._choiceGroupId != _choiceGroupId) continue;
            gate.DisarmFromGroup();
        }
    }

    void DisarmFromGroup()
    {
        if (_triggered) return;
        _triggered = true;
        DisablePassiveGate();
    }

    void DisablePassiveGate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (panelRenderer != null)
            panelRenderer.enabled = false;

        if (labelText != null)
            labelText.gameObject.SetActive(false);

        Destroy(gameObject, 0.25f);
    }
}

public class GateRuntimeData
{
    public string gateId;
    public string title;
    public string tag1;
    public string tag2;
    public Color gateColor;
    public bool isRisk;
    public bool isRecovery;
    public List<GateModifier2> modifiers = new List<GateModifier2>();
    public List<GateModifier2> penaltyModifiers = new List<GateModifier2>();

    public static GateRuntimeData FromConfig(GateConfig config)
    {
        if (config == null) return null;

        return new GateRuntimeData
        {
            gateId = config.gateId,
            title = config.title,
            tag1 = config.tag1,
            tag2 = config.tag2,
            gateColor = config.gateColor,
            isRisk = config.IsRisk,
            isRecovery = config.IsRecovery,
            modifiers = CloneModifiers(config.modifiers),
            penaltyModifiers = CloneModifiers(config.penaltyModifiers),
        };
    }

    static List<GateModifier2> CloneModifiers(List<GateModifier2> source)
    {
        var result = new List<GateModifier2>();
        if (source == null) return result;

        foreach (GateModifier2 mod in source)
        {
            if (mod == null) continue;
            result.Add(new GateModifier2
            {
                targetType = mod.targetType,
                statType = mod.statType,
                operation = mod.operation,
                value = mod.value,
            });
        }

        return result;
    }
}

```

## Gateconfig.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Kapı Konfigürasyonu v3 (Claude)
///
/// v2 → v3 Delta:
///   • GateFamily2 → GateFamily  (Solve + BossPrep eklendi, Tactical korundu)
///   • GateBalanceTier eklendi   (Minor | Standard | Solve | Army | Sustain | BossPrep)
///   • Localization key alanları eklendi: titleKey / tag1Key / tag2Key / descriptionKey
///   • title / tag1 / tag2 KORUNDU  → Gate.cs runtime'ı kırılmaz, fallback olarak çalışır
///   • bossPrepPriority → isBossPrepOnly (anlam aynı, isim netleşti)
///   • GateDeliveryType2 / GateModifier2 ve tüm alt enumlar DOKUNULMADI
///
/// GATE UI SÖZLEŞMESİ:
///   Üst satır : title  (veya titleKey → lokalize metin)
///   Alt satır : tag1 • tag2
///
/// ASSETS: Create > TopEndWar > GateConfig
/// </summary>
[CreateAssetMenu(fileName = "Gate_", menuName = "TopEndWar/GateConfig")]
public class GateConfig : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string gateId = "gate_hardline";

    // ── Localization Keys ─────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Gate.cs fallback olarak title/tag1/tag2'yi okur.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Ana etki metni anahtarı  ör: gate_hardline_title")]
    public string titleKey       = "";
    [Tooltip("Alt satır sol tag anahtarı  ör: gate_hardline_tag1")]
    public string tag1Key        = "";
    [Tooltip("Alt satır sağ tag anahtarı  ör: gate_hardline_tag2")]
    public string tag2Key        = "";
    [Tooltip("Detay / tooltip açıklaması anahtarı  ör: gate_hardline_desc")]
    public string descriptionKey = "";

    // ── Runtime / Fallback Görüntü Metinleri ─────────────────────────────
    // Lokalizasyon sistemi aktif değilken Gate.cs bunları doğrudan kullanır.
    // Key alanları doldurulunca bu alanlar tasarım referansı olarak kalır.
    [Header("Görüntü  (Fallback — Lokalizasyon hazır olana kadar)")]
    [Tooltip("Kapi üst satır metni")]
    public string title = "+8% Silah Gücü";
    [Tooltip("Alt satır sol tag")]
    public string tag1  = "POWER";
    [Tooltip("Alt satır sağ tag")]
    public string tag2  = "EARLY";

    // ── Görsel ───────────────────────────────────────────────────────────
    [Header("Görsel")]
    public Color  gateColor = new Color(0.15f, 0.80f, 0.15f, 0.80f);
    public Sprite icon;

    // ── Sınıflandırma ─────────────────────────────────────────────────────
    [Header("Sınıflandırma")]
    [Tooltip("Ailenin içerik kimliği: Power / Tempo / Solve / Geometry / Army / Sustain / Tactical / BossPrep")]
    public GateFamily        family       = GateFamily.Power;
    [Tooltip("Güç bandı: Minor / Standard / Solve / Army / Sustain / BossPrep")]
    public GateBalanceTier   balanceTier  = GateBalanceTier.Standard;
    [Tooltip("Sunum türü: Single / Duel / Risk / Recovery / BossPrep")]
    public GateDeliveryType2 deliveryType = GateDeliveryType2.Single;

    // ── Spawn Kontrol ─────────────────────────────────────────────────────
    [Header("Spawn Kontrol")]
    [Tooltip("Bu kapıyı hangi stage'den itibaren havuza al")]
    public int   minStage        = 1;
    [Tooltip("Bu kapıyı hangi stage'den sonra havuzdan çıkar  (999 = her zaman)")]
    public int   maxStage        = 999;
    [Range(0f, 1f)]
    [Tooltip("Havuz içindeki göreli spawn ağırlığı")]
    public float spawnWeight     = 0.12f;
    [Tooltip("Tutorial stage'lerinde de çıkabilir mi?")]
    public bool  tutorialAllowed = true;
    [Tooltip("Yalnızca boss prep stage'lerinde kullanılabilir; normal havuza eklenmez")]
    public bool  isBossPrepOnly  = false;

    // ── Etkiler ───────────────────────────────────────────────────────────
    [Header("Modifiers  (Ana Etki)")]
    public List<GateModifier2> modifiers = new List<GateModifier2>();

    [Header("Ceza Modifiers  (Risk delivery için)")]
    public List<GateModifier2> penaltyModifiers = new List<GateModifier2>();

    // ── Dengeleme Notu ────────────────────────────────────────────────────
    [Header("Denge  (Tasarım referansı — oyuncuya gösterilmez)")]
    [Range(0.5f, 3f)]
    public float gateValueBudget = 1.0f;

    // ── Yardımcı Property'ler ─────────────────────────────────────────────
    public bool IsRisk     => deliveryType == GateDeliveryType2.Risk;
    public bool IsRecovery => deliveryType == GateDeliveryType2.Recovery;

    /// <summary>
    /// Boss prep alanı: hem family hem de isBossPrepOnly bayrağını kontrol eder.
    /// </summary>
    public bool IsBossPrep => family == GateFamily.BossPrep || isBossPrepOnly;

    /// <summary>
    /// Lokalizasyon sistemi varsa key döner, yoksa fallback display string.
    /// Gate.cs ve UI bu property'leri kullanabilir; doğrudan title/tag1/tag2 yerine.
    /// </summary>
    public string DisplayTitle => string.IsNullOrEmpty(titleKey) ? title : titleKey;
    public string DisplayTag1  => string.IsNullOrEmpty(tag1Key)  ? tag1  : tag1Key;
    public string DisplayTag2  => string.IsNullOrEmpty(tag2Key)  ? tag2  : tag2Key;

#if UNITY_EDITOR
    void OnValidate()
    {
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────
/// <summary>
/// Kapı Ailesi — yeni kanon (v3).
/// Solve: problem-çözücü, burst-power veya niche etki.
/// BossPrep: yalnızca boss öncesi stage'lerde çıkan güçlü hazırlık kapıları.
/// </summary>
public enum GateFamily
{
    Power,
    Tempo,
    Solve,
    Geometry,
    Army,
    Sustain,
    Tactical,
    BossPrep,
}

/// <summary>
/// Güç / etki bandı — spawn havuzlarında gruplama ve dengeleme için.
/// </summary>
public enum GateBalanceTier
{
    Minor,      // Küçük, güvenli etki
    Standard,   // Normal orta etki (en yaygın bant)
    Solve,      // Niche veya problem-çözücü, genelde geç stage
    Army,       // Ordu büyütme odaklı, orta-geç stage
    Sustain,    // Toparlanma odaklı, her aşamada olabilir
    BossPrep,   // Boss öncesi: büyük etki, seyrek çıkar
}

// ─────────────────────────────────────────────────────────────────────────
// Aşağıdaki tipler v2'den DOKUNULMADAN korundu.
// PlayerStats.ApplyGateConfig, SpawnManager ve diğer runtime sistemleri bunlara bağlı.
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GateModifier2
{
    [Tooltip("Bu modifier kime uygulanır?")]
    public GateTargetType2 targetType = GateTargetType2.CommanderWeapon;

    [Tooltip("Hangi stat?")]
    public GateStatType2   statType   = GateStatType2.WeaponPowerPercent;

    [Tooltip("İşlem: AddFlat=düz ekle, AddPercent=yüzde ekle, Promote=seviye atla, HealPercent=HP oranı")]
    public GateOperation2  operation  = GateOperation2.AddPercent;

    public float value = 8f;
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
    CommanderWeapon,    // Komutan silahı
    Commander,          // Komutanın kendisi
    AllSoldiers,        // Tüm askerler
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

    // Penalty (Risk için)
    CommanderMaxHpPercent,      // Negatif value ile ceza
    SoldierDamagePercentMalus,
}

public enum GateOperation2
{
    AddFlat,
    AddPercent,
    Promote,        // Field Promotion: en zayıf birlik +1 seviye
    HealPercent,    // Toparlanma: mevcut max HP'nin yüzde X'i
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
        var   valid = BuildValidList(stageIndex, GateFamilyFilter.None, GateTierFilter.None);
        return WeightedPick(valid);
    }

    // ── Yeni Filtreli Pick ────────────────────────────────────────────────

    public GateConfig PickFiltered(int stageIndex)
    {
        var   valid = BuildValidList(stageIndex, familyFilter, tierFilter);
        return WeightedPick(valid);
    }

    public GateConfig PickByFamily(int stageIndex, GateFamily targetFamily)
    {
        var valid = new List<GatePoolEntry>();
        foreach (var e in entries)
        {
            if (e.gate == null) continue;
            if (stageIndex < e.gate.minStage || stageIndex > e.gate.maxStage) continue;
            if (e.gate.IsRisk && !allowRisk) continue;
            if (e.gate.family != targetFamily) continue;
            valid.Add(e);
        }
        return WeightedPick(valid);
    }

    // ── İç Yardımcılar ───────────────────────────────────────────────────

    List<GatePoolEntry> BuildValidList(int stageIndex,
                                       GateFamilyFilter fFilter,
                                       GateTierFilter   tFilter)
    {
        var valid = new List<GatePoolEntry>();
        foreach (var e in entries)
        {
            if (e.gate == null) continue;
            if (stageIndex < e.gate.minStage || stageIndex > e.gate.maxStage) continue;
            if (e.gate.IsRisk && !allowRisk) continue;

            if (fFilter != GateFamilyFilter.None &&
                (GateFamily)(int)fFilter != e.gate.family) continue;

            if (tFilter != GateTierFilter.None &&
                (GateBalanceTier)(int)tFilter != e.gate.balanceTier) continue;

            valid.Add(e);
        }
        return valid;
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
    PlayerStats.Instance.RefreshWeaponDerivedStats();
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
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Top End War main menu bootstrap.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Color bgColor = default;

    void Awake()
    {
        if (bgColor == default)
        {
            bgColor = UITheme.DeepNavy;
        }

        EnsureSaveManager();
        EnsureEventSystem();
        EnsureScreenManager();
        ApplyCameraTheme();
    }

    void EnsureSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            return;
        }

        // DEGISIKLIK: MainMenu can now preview progression-driven UI even before gameplay loads.
        new GameObject("SaveManager").AddComponent<SaveManager>();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void EnsureScreenManager()
    {
        GameObject canvasObject = GameObject.Find("MainMenuCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform uiRoot = canvasObject.transform.Find("UIRoot");
        if (uiRoot == null)
        {
            GameObject root = new GameObject("UIRoot", typeof(RectTransform));
            uiRoot = root.transform;
            uiRoot.SetParent(canvasObject.transform, false);
        }

        RectTransform rect = (RectTransform)uiRoot;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UIScreenManager screenManager = uiRoot.GetComponent<UIScreenManager>();
        if (screenManager == null)
        {
            screenManager = uiRoot.gameObject.AddComponent<UIScreenManager>();
        }

        screenManager.Bootstrap();
    }

    void ApplyCameraTheme()
    {
        if (Camera.main == null)
        {
            return;
        }

        Camera.main.backgroundColor = bgColor;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
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
/// Top End War — Nesne Havuzu v1.1 (Gameplay Fix Patch)
///
/// v1 → v1.1 Fix Delta:
///   • SpawnFromPool(): transform.position/rotation artık SetActive(true) ÖNCE atanıyor.
///     Eski sıra: SetActive → OnEnable → _lastPos = YANLIŞ pozisyon → pos ata
///     Yeni sıra: pos ata → SetActive → OnEnable → _lastPos = DOĞRU pozisyon
///     Bu değişiklik Bullet'ın ilk frame OverlapCapsule sweep'ini düzeltiyor.
///
///   • Aktif (hâlâ uçan) bullet'lar artık yeniden kullanılmıyor.
///     Eski kod: her spawn'da sıranın başındaki objeyi alıyordu — aktif olup olmadığına
///     bakmıyordu. Hızlı ateşte uçmakta olan bullet'lar teleport ediyordu.
///     Yeni kod: foreach ile inaktif ilk obje bulunuyor. Hepsi aktifse havuz büyüyor.
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
                ConfigureSpawnedObject(pool.tag, obj);
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

        // FIX: Aktif (hâlâ uçmakta olan) objeleri atla; inaktif ilk objeyi seç.
        // Queue<T> foreach sırayı bozmadan iterate eder.
        foreach (GameObject obj in poolDictionary[tag])
        {
            if (obj.activeSelf) continue;

            // FIX: Pozisyon ve rotasyonu SetActive'den ÖNCE ata.
            // Böylece Bullet.OnEnable() → _lastPos = transform.position doğru pozisyonu yakalar.
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            ConfigureSpawnedObject(tag, obj);
            obj.SetActive(true);
            return obj;
        }

        // Havuzda inaktif obje kalmadı — havuzu büyüt.
        Pool poolDef = pools.Find(p => p.tag == tag);
        if (poolDef != null && poolDef.prefab != null)
        {
            GameObject newObj = Instantiate(poolDef.prefab);
            ConfigureSpawnedObject(tag, newObj);
            newObj.SetActive(false);
            newObj.transform.parent = transform;
            poolDictionary[tag].Enqueue(newObj);

            // FIX: Yeni obje için de aynı sıra: pozisyon → SetActive.
            newObj.transform.position = position;
            newObj.transform.rotation = rotation;
            ConfigureSpawnedObject(tag, newObj);
            newObj.SetActive(true);

            Debug.Log($"[ObjectPooler] Pool '{tag}' büyütüldü (aktif obje kalmamıştı).");
            return newObj;
        }

        Debug.LogWarning($"[ObjectPooler] '{tag}' havuzunda inaktif obje yok ve prefab bulunamadı.");
        return null;
    }

    void ConfigureSpawnedObject(string tag, GameObject obj)
    {
        if (tag != "Enemy" || obj == null) return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            SetLayerRecursive(obj, enemyLayer);

        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}

```

## Petcontroller.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War - Legacy compatibility wrapper.
///
/// Eski sahne referanslari icin tutulur; asil hareket/ates davranisi
/// Playercontroller taban sinifindan gelir.
/// </summary>
public class PlayerController : Playercontroller
{
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
/// Top End War — Oyuncu Hareketi v7.2 (Gameplay Fix Patch)
///
/// v7.1 → v7.2 Fix Delta:
///   • FindFrontTarget(): Physics.BoxCast → Physics.BoxCastAll
///     BoxCast sadece ilk çarpışmayı döner; terrain/prop önde varsa
///     enemy hiç hedeflenemiyordu. BoxCastAll tüm hitleri tarar,
///     geçerli ilk enemy/boss seçilir.
///   • IsCombatTarget(): activeInHierarchy kontrolü eklendi.
///     Ölü/deaktif enemy'lerin frame-edge durumlarına karşı savunmacı hat.
/// </summary>
public class Playercontroller : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    [Tooltip("Oyuncunun sabit Y yuksekligi — Inspector'dan degistirilebilir.")]
    public float playerY         = 1.2f;
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
    bool  _gameOver   = false;
    static Material _flashMaterial;

    void Start()
    {
        Vector3 p = transform.position;
        p.x = 0f;
        p.y = playerY;
        transform.position = p;

        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
        GameEvents.OnGameOver          += OnGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnAnchorModeChanged -= OnAnchorMode;
        GameEvents.OnGameOver          -= OnGameOver;
    }

    void OnGameOver()
    {
        _gameOver = true;
        _dragging = false;
        _nextFire = float.MaxValue;
        Debug.Log("[PlayerController] Game Over — hareket durduruldu.");
    }

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
        if (_gameOver) return;

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
            _dragging   = true;
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
        p.x  = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = playerY;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null)
            return;

        PlayerStats.RuntimeCombatSnapshot combat = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        float finalFireRate = combat.FireRate;
        int bCount = combat.ProjectileCount;
        int bulletDamage = combat.BulletDamage;

        Color tracerColor = GetWeaponTracerColor();

        Transform target = FindTarget();
        if (target == null) return;

        if (target.position.z <= transform.position.z + 1f)
            return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = aimPos - firePoint.position;
        if (baseDir.sqrMagnitude <= 0.0001f || baseDir.z <= 0.05f)
            return;

        baseDir.Normalize();

        int   armorPen        = combat.ArmorPen;
        int   pierceCount     = combat.PierceCount;
        float eliteDamageMult = GetCurrentEliteDamageMultiplier();
        float weaponRange     = combat.WeaponRange;
        float projectileSpeed = GetCurrentProjectileSpeed();
        WeaponFamily family   = GetCurrentWeaponFamily();

        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        bool firedAny = false;
        foreach (float angle in SPREAD[spreadIdx])
        {
            float finalAngle = GetWeaponSpreadAngle(angle);
            Vector3 dir = Quaternion.Euler(0f, finalAngle, 0f) * baseDir;
            firedAny |= FireOne(firePoint.position, dir.normalized, bulletDamage, armorPen, pierceCount, eliteDamageMult, tracerColor, weaponRange, projectileSpeed, family);
        }

        if (firedAny)
        {
            SpawnMuzzleFlash(firePoint.position, baseDir, tracerColor);
            _nextFire = Time.time + 1f / Mathf.Max(0.01f, finalFireRate);
        }
    }

    bool FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount, float eliteDamageMult, Color tracerColor, float weaponRange, float projectileSpeed, WeaponFamily family)
    {
        if (dir.sqrMagnitude <= 0.0001f || dir.z <= 0.05f)
            return false;

        dir.Normalize();
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        // FIX: Pool yoksa prefab'dan instantiate et — asla null kalmasın.
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b == null) return false;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = "Commander";
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult);
            bullet.SetMaxRange(weaponRange);
            bullet.SetTrailProfile(family);
            bullet.SetTracerColor(tracerColor);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * projectileSpeed;
        return true;
    }

    Color GetWeaponTracerColor()
    {
        switch (GetCurrentWeaponFamily())
        {
            case WeaponFamily.SMG:
                return new Color(0.30f, 0.95f, 1.00f, 1f);
            case WeaponFamily.Sniper:
                return new Color(1.00f, 0.35f, 0.85f, 1f);
            default:
                return new Color(1.00f, 0.88f, 0.25f, 1f);
        }
    }

    float GetWeaponSpreadAngle(float angle)
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        float bonus = w != null ? w.spreadBonus : 0f;
        float sign = angle < 0f ? -1f : (angle > 0f ? 1f : 0f);
        float signedAngle = angle + sign * bonus;

        if (GetCurrentWeaponFamily() == WeaponFamily.SMG)
            return Mathf.Clamp(signedAngle, -3f, 3f);

        return signedAngle;
    }

    float GetCurrentWeaponRange()
    {
        return PlayerStats.Instance != null ? PlayerStats.Instance.GetRuntimeWeaponRange() : 24f;
    }

    float GetCurrentProjectileSpeed()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        if (w != null && w.weaponArchetype != null)
            return Mathf.Max(1f, w.weaponArchetype.projectileSpeed);

        return 30f;
    }

    void SpawnMuzzleFlash(Vector3 pos, Vector3 dir, Color color)
    {
        var go = new GameObject("MuzzleFlash");
        go.transform.position = pos + dir.normalized * 0.08f;
        go.transform.rotation = Quaternion.LookRotation(dir.normalized);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.5f;
        light.intensity = 1.8f;
        light.color = color;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.18f;
        lr.endWidth = 0.02f;
        lr.material = GetFlashMaterial();
        lr.startColor = new Color(color.r, color.g, color.b, 0.95f);
        lr.endColor = new Color(color.r, color.g, color.b, 0f);
        lr.SetPosition(0, pos);
        lr.SetPosition(1, pos - dir.normalized * 0.7f);

        Destroy(go, 0.06f);
    }

    static Material GetFlashMaterial()
    {
        if (_flashMaterial == null)
            _flashMaterial = new Material(Shader.Find("Sprites/Default"));
        return _flashMaterial;
    }

    int GetCurrentArmorPen()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        return (w != null ? w.armorPen : 0)
             + (PlayerStats.Instance != null ? PlayerStats.Instance.RunArmorPenFlat : 0);
    }

    int GetCurrentPierceCount()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        return (w != null ? w.pierceCount : 0)
             + (PlayerStats.Instance != null ? PlayerStats.Instance.RunPierceCount : 0);
    }

    float GetCurrentEliteDamageMultiplier()
    {
        EquipmentData w      = PlayerStats.Instance?.equippedWeapon;
        float  equipMult     = w != null ? w.eliteDamageMultiplier : 1f;
        float  gateMult      = PlayerStats.Instance != null
                                ? (1f + PlayerStats.Instance.RunEliteDamagePercent / 100f) : 1f;
        return equipMult * gateMult;
    }

    WeaponFamily GetCurrentWeaponFamily()
    {
        return PlayerStats.Instance != null ? PlayerStats.Instance.GetRuntimeWeaponFamily() : WeaponFamily.Assault;
    }

    Transform FindTarget()
    {
        float weaponRange = GetCurrentWeaponRange();
        switch (GetCurrentWeaponFamily())
        {
            case WeaponFamily.SMG:
                return FindPackTarget(weaponRange);
            case WeaponFamily.Sniper:
                return FindPriorityTarget(weaponRange);
            default:
                return FindFrontTarget(weaponRange);
        }
    }

    // FIX: Eskiden Physics.BoxCast kullanılıyordu — sadece ilk physics hit'i döner.
    // Önde terrain/prop/başka collider varsa enemy hiç seçilemiyordu.
    // Physics.BoxCastAll ile tüm hitler taranır; geçerli en yakın enemy/boss seçilir.
    Transform FindFrontTarget(float weaponRange)
    {
        if (_anchorMode)
            return FindClosestTargetInSphere(weaponRange);

        RaycastHit[] hits = Physics.BoxCastAll(
            transform.position + Vector3.up,
            new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
            Vector3.forward,
            Quaternion.identity,
            weaponRange);

        Transform best    = null;
        float     bestDist = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // FIX: Deaktif/ölü objeler physics'ten çıkar ama savunmacı kontrol.
            if (!IsCombatTarget(hit.collider, out Transform target)) continue;

            bool isEnemy = hit.collider.GetComponent<Enemy>() != null
                        || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null
                        || hit.collider.GetComponentInParent<BossHitReceiver>() != null;

            if (!isEnemy && !isBoss) continue;

            // En yakın geçerli hedefi seç (mesafe bazlı)
            float d = (target.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best     = target;
            }
        }

        return best;
    }

    Transform FindPackTarget(float weaponRange)
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(weaponRange), scoreMode: TargetScoreMode.Cluster);
    }

    Transform FindPriorityTarget(float weaponRange)
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(weaponRange), scoreMode: TargetScoreMode.Priority);
    }

    Transform FindClosestTargetInSphere(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(GetTargetRangeOrigin(), radius);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Collider c in cols)
        {
            if (!IsCombatTarget(c, out Transform target)) continue;
            float d = (target.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = target;
            }
        }

        return best;
    }

    Collider[] DetectEnemyCandidates(float weaponRange)
    {
        return Physics.OverlapSphere(GetTargetRangeOrigin(), weaponRange);
    }

    enum TargetScoreMode
    {
        Cluster,
        Priority,
    }

    Transform FindBestEnemyByScore(Collider[] cols, TargetScoreMode scoreMode)
    {
        Transform best = null;
        float bestScore = float.MinValue;

        foreach (Collider c in cols)
        {
            if (!IsCombatTarget(c, out Transform target, out Enemy enemy, out bool isBoss)) continue;

            float dist = Vector3.Distance(transform.position, target.position);
            float score = 0f;

            if (scoreMode == TargetScoreMode.Cluster)
            {
                int neighborCount = CountNearbyEnemies(target.position, 4f);
                score = neighborCount * 100f - dist * 2f;
                if (enemy != null && enemy.Armor > 0) score += enemy.Armor * 1.5f;
            }
            else
            {
                score = isBoss ? 5000f : 0f;
                if (enemy != null)
                {
                    score += enemy.IsElite ? 1500f : 0f;
                    score += enemy.Armor * 40f;
                    score += Mathf.Clamp(10f - dist, -10f, 10f) * 15f;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        // Fallback: en yakın geçerli hedef
        return best ?? FindClosestTargetInSphere(GetCurrentWeaponRange());
    }

    int CountNearbyEnemies(Vector3 center, float radius)
    {
        int count = 0;
        Collider[] cols = Physics.OverlapSphere(center, radius);
        foreach (Collider c in cols)
            if (IsCombatTarget(c, out _))
                count++;
        return count;
    }

    bool IsCombatTarget(Collider col, out Transform target)
    {
        return IsCombatTarget(col, out target, out _, out _);
    }

    // FIX: activeInHierarchy kontrolü eklendi.
    // SetActive(false) physics'ten kaldırır ama aynı frame içinde edge case olabilir.
    bool IsCombatTarget(Collider col, out Transform target, out Enemy enemy, out bool isBoss)
    {
        target = null;
        isBoss = false;
        enemy  = null;

        // FIX: Deaktif obje hedeflenemez.
        if (col == null || !col.gameObject.activeInHierarchy) return false;

        enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
        BossHitReceiver boss = col.GetComponent<BossHitReceiver>() ?? col.GetComponentInParent<BossHitReceiver>();
        isBoss = boss != null;

        if (boss != null)
        {
            target = boss.transform;
            return IsTargetInWeaponWindow(target);
        }

        if (enemy != null)
        {
            if (!enemy.IsAlive) return false;
            target = enemy.transform;
            return IsTargetInWeaponWindow(target);
        }

        return false;
    }

    bool IsTargetInWeaponWindow(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = GetTargetRangeOrigin();
        Vector3 delta = target.position - origin;
        if (delta.z <= 0.5f) return false;

        float range = GetCurrentWeaponRange();
        return delta.sqrMagnitude <= range * range;
    }

    Vector3 GetTargetRangeOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    public void ResumeRun()
    {
        _gameOver    = false;
        _anchorMode  = false;
        _dragging    = false;
        _targetX     = 0f;
        _nextFire    = 0f;
        forwardSpeed = 10f;
    }

    public void ResetForStage(float startZ = 0f)
    {
        ResumeRun();

        Vector3 p = transform.position;
        p.x = 0f;
        p.y = playerY;
        p.z = startZ;
        transform.position = p;
    }
}

```

## PlayerStats.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Oyuncu Istatistikleri v9 (Runtime Stabilite Patch)
///
/// v8 → v9 Delta:
///   • _isDead flag eklendi: TakeContactDamage tekrar GameOver tetiklemez.
///   • ResetRunGateBonuses(): _isDead, _lastDmgTime ve HP sifirlanir —
///     StageManager bu metodu zaten cagirir, yeni run temiz baslar.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public struct RuntimeCombatSnapshot
    {
        public float TotalDPS;
        public float FireRate;
        public int ProjectileCount;
        public int BulletDamage;
        public int ArmorPen;
        public int PierceCount;
        public float WeaponRange;
        public float DisplayedDPS;
        public int CurrentHP;
        public int MaxHP;
        public int CombatPower;
    }

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

    // PATCH: Cift GameOver tetiklenmesini onler.
    private bool _isDead = false;

    // ── RUN-TIME GATE BONUSLARI ───────────────────────────────────────────
    float _runWeaponPowerPercent = 0f;
    float _runFireRatePercent    = 0f;
    float _runEliteDamagePercent = 0f;
    float _runBossDamagePercent  = 0f;
    int   _runArmorPenFlat       = 0;
    int   _runPierceCount        = 0;
    int   _runPelletCount        = 0;

    public float RunWeaponPowerPercent => _runWeaponPowerPercent;
    public float RunFireRatePercent    => _runFireRatePercent;
    public float RunEliteDamagePercent => _runEliteDamagePercent;
    public float RunBossDamagePercent  => _runBossDamagePercent;
    public int   RunArmorPenFlat       => _runArmorPenFlat;
    public int   RunPierceCount        => _runPierceCount;
    public int   RunPelletCount        => _runPelletCount;

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

    public RuntimeCombatSnapshot GetRuntimeCombatSnapshot()
    {
        float fireRate = Mathf.Max(0.01f, GetBaseFireRate() * (1f + _runFireRatePercent / 100f));
        float totalDps = Mathf.Max(0f, GetTotalDPS() * (1f + _runWeaponPowerPercent / 100f));
        int projectileCount = Mathf.Max(1, BulletCount);
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDps / (fireRate * projectileCount)));

        int armorPen = _runArmorPenFlat;
        int pierceCount = _runPierceCount;
        if (equippedWeapon != null)
        {
            armorPen += equippedWeapon.weaponArchetype != null ? equippedWeapon.weaponArchetype.armorPen : 0;
            armorPen += equippedWeapon.armorPen;
            pierceCount += equippedWeapon.weaponArchetype != null ? equippedWeapon.weaponArchetype.pierceCount : 0;
            pierceCount += equippedWeapon.pierceCount;
        }
        float displayedDps = bulletDamage * fireRate * projectileCount;
        float weaponRange = GetRuntimeWeaponRange();
        
        int combatPower = CalculateCombatPower(displayedDps, CommanderMaxHP, armorPen, pierceCount, weaponRange);

        return new RuntimeCombatSnapshot
        {
            TotalDPS = totalDps,
            FireRate = fireRate,
            ProjectileCount = projectileCount,
            BulletDamage = bulletDamage,
            ArmorPen = armorPen,
            PierceCount = pierceCount,
            WeaponRange = weaponRange,
            DisplayedDPS = displayedDps,
            CurrentHP = CommanderHP,
            MaxHP = CommanderMaxHP,
            CombatPower = combatPower,
        };
    }

    public float GetRuntimeWeaponRange()
    {
        WeaponFamily family = GetRuntimeWeaponFamily();
        float fallback = family switch
        {
            WeaponFamily.SMG => 18f,
            WeaponFamily.Sniper => 36f,
            _ => 24f
        };

        float range = equippedWeapon != null && equippedWeapon.weaponArchetype != null
            ? equippedWeapon.weaponArchetype.attackRange
            : fallback;

        return family == WeaponFamily.SMG
            ? Mathf.Clamp(range, 16f, 20f)
            : Mathf.Max(4f, range);
    }

    public WeaponFamily GetRuntimeWeaponFamily()
    {
        if (equippedWeapon == null)
            return WeaponFamily.Assault;

        if (equippedWeapon.weaponArchetype != null)
            return equippedWeapon.weaponArchetype.family;

        return equippedWeapon.weaponType switch
        {
            WeaponType.Automatic => WeaponFamily.SMG,
            WeaponType.Sniper => WeaponFamily.Sniper,
            WeaponType.Shotgun => WeaponFamily.Shotgun,
            WeaponType.Launcher => WeaponFamily.Launcher,
            WeaponType.Beam => WeaponFamily.Beam,
            _ => WeaponFamily.Assault,
        };
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
        => rarity switch { 1 => 1.0f, 2 => 1.3f, 3 => 1.7f, 4 => 2.2f, 5 => 3.0f, _ => 1.0f };

    /// <summary>
    /// Runtime Combat Power Formülü (DPS, HP, ArmorPen, Pierce, Range bileşimi).
    /// 
    /// Temel ilke:
    ///   - DPS, oyuncunun hasar hızı kapasitesi
    ///   - MaxHP, hayatta kalma gücü
    ///   - ArmorPen, düşman zırhına karşı verimlilik
    ///   - PierceCount, ek hasar etkinliği
    ///   - WeaponRange, strateji ve çok yönlülük
    /// 
    /// Formül:
    ///   power = round(displayedDps * 1.5 + maxHp * 0.2 + armorPen * 15 + pierceCount * 50 + range * 2)
    /// 
    /// Amaç: Stage targetDps (~70) ile karşılaştırılabilir power score oluşturmak.
    /// </summary>
    static int CalculateCombatPower(float displayedDps, int maxHp, int armorPen, int pierceCount, float weaponRange)
    {
        float power = 0f;
        power += displayedDps * 1.5f;       // DPS ağırlık
        power += maxHp * 0.2f;              // HP katkı
        power += armorPen * 8f;            // ArmorPen verimliliği
        power += pierceCount * 50f;         // Pierce utility bonus
        power += weaponRange * 2f;          // Range stratejik değer
        
        return Mathf.Max(1, Mathf.RoundToInt(power));
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
        RefreshWeaponDerivedStats();

        CommanderMaxHP = (activeCommander != null ? activeCommander.GetBaseHP(1) : 500)
                       + TotalEquipmentHPBonus();
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // DEĞİŞİKLİK: Enemy tarafı artık hasarın gerçekten işlenip işlenmediğini bilmek istiyor.
    // DEĞİŞİKLİK
public bool TryTakeContactDamage(int amount)
{
    if (_isDead)
    {
        Debug.Log("[PlayerStats] ContactDamage BLOCKED -> already dead");
        return false;
    }

    float dt = Time.time - _lastDmgTime;
    if (dt < invincibilityDuration)
    {
        Debug.Log($"[PlayerStats] ContactDamage BLOCKED -> iFrame aktif | dt={dt:F2} / inv={invincibilityDuration:F2}");
        return false;
    }

    _lastDmgTime = Time.time;

    float dr = TotalDamageReduction();
    int finalAmount = Mathf.RoundToInt(amount * (1f - dr));
    int oldHp = CommanderHP;

    CommanderHP = Mathf.Max(0, CommanderHP - finalAmount);

    GameEvents.OnCommanderDamaged?.Invoke(finalAmount, CommanderHP);
    GameEvents.OnPlayerDamaged?.Invoke(amount);
    GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

    Debug.Log($"[PlayerStats] ContactDamage APPLIED -> raw={amount} final={finalAmount} hp:{oldHp}->{CommanderHP}");

    if (CommanderHP <= 0)
    {
        _isDead = true;
        Debug.Log("[PlayerStats] ContactDamage APPLIED -> player dead, GameOver");
        GameEvents.OnGameOver?.Invoke();
    }

    return true;
}

    public void TakeContactDamage(int amount)
    {
        TryTakeContactDamage(amount);
    }

    // DEĞİŞİKLİK: Revive sonrası ölüm flagi ve hasar zamanlayıcısı temizlenir.
    public void ReviveFromGameOver()
    {
        _isDead = false;
        _lastDmgTime = -99f;
        CommanderHP = CommanderMaxHP;
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
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

    // ── Gate Config ───────────────────────────────────────────────────────

public void ResetRunGateBonuses()
{
    _runWeaponPowerPercent = 0f;
    _runFireRatePercent    = 0f;
    _runEliteDamagePercent = 0f;
    _runBossDamagePercent  = 0f;
    _runArmorPenFlat       = 0;
    _runPierceCount        = 0;
    _runPelletCount        = 0;

    // PATCH: yeni run baslarken olum flagini ve hasar zamanlayicisini sifirla.
    _isDead      = false;
    _lastDmgTime = -99f;
        CommanderHP  = CommanderMaxHP;

        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void ApplyGateConfig(GateConfig gate)
    {
        if (gate == null) return;
        ApplyModifierList(gate.modifiers);
        if (gate.IsRisk && gate.penaltyModifiers != null && gate.penaltyModifiers.Count > 0)
            ApplyModifierList(gate.penaltyModifiers);
        RefreshWeaponDerivedStats();
        Debug.Log($"[PlayerStats] Gate applied: {gate.title}");
    }

    public void ApplyGateConfig(GateRuntimeData gate)
    {
        if (gate == null) return;
        ApplyModifierList(gate.modifiers);
        if (gate.isRisk && gate.penaltyModifiers != null && gate.penaltyModifiers.Count > 0)
            ApplyModifierList(gate.penaltyModifiers);
        RefreshWeaponDerivedStats();
        Debug.Log($"[PlayerStats] Gate applied: {gate.title}");
    }

    void ApplyModifierList(List<GateModifier2> list)
    {
        if (list == null) return;
        foreach (var mod in list) ApplyModifier(mod);
    }

    void ApplyModifier(GateModifier2 mod)
    {
        if (mod == null) return;
        switch (mod.statType)
        {
            case GateStatType2.WeaponPowerPercent:        _runWeaponPowerPercent += mod.value; break;
            case GateStatType2.FireRatePercent:            _runFireRatePercent    += mod.value; break;
            case GateStatType2.EliteDamagePercent:         _runEliteDamagePercent += mod.value; break;
            case GateStatType2.BossDamagePercent:          _runBossDamagePercent  += mod.value; break;
            case GateStatType2.ArmorPenFlat:               _runArmorPenFlat += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.PierceCount:                _runPierceCount  += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.PelletCount:                _runPelletCount  += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.AddSoldierCount:
            {
                int count = Mathf.RoundToInt(mod.value);
                switch (mod.targetType)
                {
                    case GateTargetType2.PiyadeSoldiers:     ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade,    count: count); break;
                    case GateTargetType2.MekanikSoldiers:    ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik,   count: count); break;
                    case GateTargetType2.TeknolojiSoldiers:  ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: count); break;
                    default:                                  ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade,    count: count); break;
                }
                break;
            }
            case GateStatType2.HealCommanderPercent:
                HealCommander(Mathf.RoundToInt(CommanderMaxHP * (mod.value / 100f)));
                break;
            case GateStatType2.HealSoldiersPercent:
                ArmyManager.Instance?.HealAll(mod.value / 100f);
                break;
            default:
                Debug.Log($"[PlayerStats] Unsupported gate stat for slice now: {mod.statType}");
                break;
        }
    }

    // ── Tier ─────────────────────────────────────────────────────────────
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

    public void RefreshWeaponDerivedStats()
    {
        int baseCount = equippedWeapon != null && equippedWeapon.weaponArchetype != null
            ? equippedWeapon.weaponArchetype.projectileCount
            : 1;

        int nextCount = Mathf.Clamp(baseCount + _runPelletCount, 1, MAX_BULLETS);
        if (BulletCount == nextCount) return;

        BulletCount = nextCount;
        GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    public void SetBulletCount(int count)
    {
        BulletCount = Mathf.Clamp(count, 1, MAX_BULLETS);
        GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }
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
    public int   CurrentRunStagesCleared { get; private set; } = 0;
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
    public void RegisterStageComplete() => CurrentRunStagesCleared++;

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
    CurrentRunStagesCleared = 0;
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
/// Top End War — Bireysel Asker v4.1 (Gameplay Fix Patch)
///
/// v4 → v4.1 Fix Delta:
///   • FireBullet(): ObjectPooler null olunca fallback Instantiate eklendi.
///     Önceki kodda pool yoksa b == null → return ile askerler hiç ateş etmiyordu.
///     Fallback olarak PlayerController'ın bulletPrefab'ı kullanılır.
///   • _cachedPC: PlayerController referansı OnEnable'da bir kez önbelleğe alınır,
///     her shot'ta FindObjectOfType maliyeti önlenir.
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

    // FIX: PlayerController önbelleği — her shot'ta FindObjectOfType çağrılmaz.
    static Playercontroller _cachedPC;

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

        // FIX: PlayerController referansını bir kez önbelleğe al.
        if (_cachedPC == null)
            _cachedPC = FindFirstObjectByType<Playercontroller>();

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

            // FIX: Deaktif/ölü enemy'yi hedefleme.
            if (!enemy.gameObject.activeInHierarchy) continue;

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
        Vector3 dir = aimPoint - pos;
        if (dir.sqrMagnitude <= 0.0001f || dir.z <= 0.05f)
            return;

        dir.Normalize();

        GameObject b = null;

        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        // FIX: ObjectPooler null veya pool boşsa, PlayerController'ın bulletPrefab'ından instantiate et.
        // Önceden: pool yoksa b == null → return → askerler hiç ateş etmiyordu.
        if (b == null && _cachedPC != null && _cachedPC.bulletPrefab != null)
        {
            b = Instantiate(_cachedPC.bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 2f);
        }

        if (b == null)
        {
            Debug.LogWarning("[SoldierUnit] Bullet spawn edilemedi. ObjectPooler veya bulletPrefab eksik.");
            return;
        }

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = path.ToString();
            bullet.bulletColor = GetPathColor() * 0.85f;

            int armorPen = weaponConfig != null ? weaponConfig.armorPen : 0;
            int pierceCount = weaponConfig != null ? weaponConfig.pierceCount : 0;
            WeaponFamily family = weaponConfig != null ? weaponConfig.family : WeaponFamily.Assault;

            bullet.SetCombatStats(
                dmg,
                armorPen,
                pierceCount,
                1f,
                1f
            );
            bullet.SetMaxRange(GetAttackRange());
            bullet.SetTrailProfile(family);
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
/// Top End War — Spawn Yoneticisi v16 (Spawn Rhythm v1)
///
/// v15 → v16 Delta:
///   • [Spawn Rhythm] header + rhythmTable alani eklendi.
///   • _lastPacket: ayni packet ust uste gelmesin diye izlenir.
///   • ResetForStage(): _lastPacket sifirlanir.
///   • SpawnEnemyWave(): TrySpawnPacket() cagrilir —
///     TrySpawnConfiguredWave ve prosedural fallback'ten once.
///   • TrySpawnPacket(): rhythmTable'dan stage'e gore agirlikli secim.
///   • SpawnFromPacket(): packet icindeki WaveGroup'lari Z uzayina yayar,
///     jitterZ / jitterX / intraZStep / hasLeadGap uygulanir.
///
/// Secim onceligi (degismedi):
///   1. StageConfig.waveSequence dolu → TrySpawnConfiguredWave kullanilir.
///   2. rhythmTable atanmis          → TrySpawnPacket kullanilir.   ← YENİ
///   3. Ikisi de yoksa               → prosedural fallback.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;

    [Header("Gate Havuzlari")]
    public GatePoolConfig poolStage1To5;
    public GatePoolConfig poolStage6To10;

    [Header("Spawn")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    // ── Spawn Rhythm ─────────────────────────────────────────────────────
    [Header("Spawn Rhythm  (Stage 1-10 otomatik packet secimi)")]
    [Tooltip("Atanmazsa prosedural fallback devrede kalir. " +
             "StageConfig.waveSequence dolu stage'lerde bu tablo devreye GIRMEZ.")]
    public SpawnRhythmTable rhythmTable;

    // ── Runtime Durum ─────────────────────────────────────────────────────
    float _nextGateZ  = 40f;
    float _nextWaveZ  = 55f;
    bool  _bossSpawned = false;
    int   _waveCursor  = 0;
    int   _spawnedGateGroups = 0;
    int   _spawnedCombatPackets = 0;
    int   _nextGateChoiceGroupId = 1;
    float _lastCombatSpawnTime = -999f;
    float _lastCombatSpawnPlayerZ = -999f;
    bool  _lastCombatSpawnWasFollowup = false;
    float _lastGateSpawnZ = -999f;
    bool  _followupUsedForLastGate = true;
    float _lastSkipLogTime = -999f;

    const float FIRST_WAVE_Z = 36f;
    const float FIRST_GATE_Z = 52f;
    const float GATE_FOLLOWUP_MIN = 18f;
    const float GATE_FOLLOWUP_MAX = 30f;
    const float MAX_EMPTY_GAP = 65f;
    const float COMBAT_SPAWN_COOLDOWN = 1.2f;
    const float MIN_COMBAT_PLAYER_ADVANCE = 20f;
    const float MIN_COMBAT_SPAWN_AHEAD = 42f;
    const float SKIP_LOG_INTERVAL = 0.75f;

    // RHYTHM: son secilen packet — tekrar azaltmak icin izlenir
    SpawnPacketConfig _lastPacket = null;

    public void ResetForStage()
    {
        _nextGateZ   = FIRST_GATE_Z;
        _nextWaveZ   = FIRST_WAVE_Z;
        _bossSpawned = false;
        _waveCursor  = 0;
        _spawnedGateGroups = 0;
        _spawnedCombatPackets = 0;
        _nextGateChoiceGroupId = 1;
        _lastCombatSpawnTime = -999f;
        _lastCombatSpawnPlayerZ = -999f;
        _lastCombatSpawnWasFollowup = false;
        _lastGateSpawnZ = -999f;
        _followupUsedForLastGate = true;
        _lastSkipLogTime = -999f;
        _lastPacket  = null;   // RHYTHM: yeni stage'de cesitlilik yeniden baslar
        Gate.ResetChoiceState();
    }

    DifficultyManager.EnemyStats _stats;
    bool _statsReady = false;

    float _overrideNormalHP = 0f;
    float _overrideEliteHP  = 0f;
    float _densityMult      = 1f;
    bool  _hpOverrideActive = false;

    // ── Yasam Dongusu ─────────────────────────────────────────────────────

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

    // PATCH v15: Boss trigger'i yalnizca boss stage'lerde cal
    bool StageHasBoss()
{
    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    return stage != null && stage.IsBossStage;
}

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!_bossSpawned && StageHasBoss() && pz >= bossDistance)
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

        GuardSpawnCursors(pz);

        if (pz + spawnAhead >= _nextGateZ)
        {
            if (!(_spawnedCombatPackets == 0 && _nextGateZ > _nextWaveZ))
            {
                float gateZ = _nextGateZ;
                SpawnNextGateGroup(gateZ);
                ScheduleGateFollowupOnce(gateZ);
                _nextGateZ = gateZ + gateSpacing;
            }
        }

        if (pz + spawnAhead >= _nextWaveZ)
        {
            TrySpawnCombatAtCursor(pz);
        }
    }

    void GuardSpawnCursors(float playerZ)
    {
        float nearestUpcoming = Mathf.Min(_nextGateZ, _nextWaveZ);
        bool canFillGap = nearestUpcoming - playerZ > MAX_EMPTY_GAP
                       && !_lastCombatSpawnWasFollowup
                       && CanSpawnCombatNow(playerZ, out _)
                       && CountActiveEnemies() <= Mathf.Max(4, GetActiveEnemyCap() / 2);

        if (canFillGap)
        {
            _nextWaveZ = playerZ + 48f;
            Debug.Log($"[Spawn] gap fallback nextWaveZ={_nextWaveZ:F1} activeEnemies={CountActiveEnemies()}");
        }

        if (_spawnedGateGroups > _spawnedCombatPackets + 1)
            _nextWaveZ = Mathf.Min(_nextWaveZ, _nextGateZ - GATE_FOLLOWUP_MIN);
    }

    void ScheduleGateFollowupOnce(float gateZ)
    {
        if (_followupUsedForLastGate && Mathf.Approximately(_lastGateSpawnZ, gateZ))
            return;

        float latestFollowup = gateZ + GATE_FOLLOWUP_MAX;
        float earliestFollowup = gateZ + GATE_FOLLOWUP_MIN;
        if (_nextWaveZ > latestFollowup)
            _nextWaveZ = Random.Range(earliestFollowup, latestFollowup);
        else if (_nextWaveZ > gateZ && _nextWaveZ < earliestFollowup)
            _nextWaveZ = earliestFollowup;

        _followupUsedForLastGate = true;
        Debug.Log($"[Spawn] gate followup used gateZ={gateZ:F1} nextWaveZ={_nextWaveZ:F1}");
    }

    void TrySpawnCombatAtCursor(float playerZ)
    {
        if (!CanSpawnCombatNow(playerZ, out string reason))
        {
            LogSpawnSkip(reason);
            return;
        }

        float spawnZ = Mathf.Max(_nextWaveZ, playerZ + MIN_COMBAT_SPAWN_AHEAD);
        bool spawned = SpawnEnemyWave(spawnZ);
        float spacing = GetCombatTempoSpacing();
        _nextWaveZ = spawnZ + spacing;

        if (!spawned)
            return;

        int activeEnemies = CountActiveEnemies();
        _spawnedCombatPackets++;
        _lastCombatSpawnTime = Time.time;
        _lastCombatSpawnPlayerZ = playerZ;
        _lastCombatSpawnWasFollowup = _lastGateSpawnZ > 0f
                                   && spawnZ >= _lastGateSpawnZ + GATE_FOLLOWUP_MIN - 0.1f
                                   && spawnZ <= _lastGateSpawnZ + GATE_FOLLOWUP_MAX + 0.1f;

        Debug.Log($"[Spawn] packet={_lastPacket?.packetType.ToString() ?? "Fallback"} z={spawnZ:F1} activeEnemies={activeEnemies} nextWaveZ={_nextWaveZ:F1}");
    }

    bool CanSpawnCombatNow(float playerZ, out string reason)
    {
        int activeEnemies = CountActiveEnemies();
        if (activeEnemies >= GetActiveEnemyCap())
        {
            reason = "active cap";
            return false;
        }

        if (Time.time - _lastCombatSpawnTime < COMBAT_SPAWN_COOLDOWN)
        {
            reason = "cooldown";
            return false;
        }

        if (playerZ - _lastCombatSpawnPlayerZ < MIN_COMBAT_PLAYER_ADVANCE)
        {
            reason = "distance";
            return false;
        }

        reason = null;
        return true;
    }

    void LogSpawnSkip(string reason)
    {
        if (Time.time - _lastSkipLogTime < SKIP_LOG_INTERVAL)
            return;

        _lastSkipLogTime = Time.time;
        Debug.Log($"[Spawn] skipped {reason}");
    }

    float GetCombatTempoSpacing()
    {
        return Mathf.Max(24f, waveSpacing + Random.Range(-4f, 6f));
    }

    int GetActiveEnemyCap()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        if (stage <= 5) return 14;
        if (stage <= 10) return 18;
        if (stage <= 20) return 24;
        return 30;
    }

    int CountActiveEnemies()
    {
        int count = 0;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                count++;
        return count;
    }

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform;
    }

    // ── Wave Dispatch ─────────────────────────────────────────────────────

    bool SpawnEnemyWave(float zPos)
    {
        // 1) Manuel waveSequence (StageConfig'e elle baglanmis dalgalar)
        if (TrySpawnConfiguredWave(zPos)) return true;

        // 2) RHYTHM: rhythmTable atanmissa otomatik packet secimi  ← YENİ
        if (TrySpawnPacket(zPos)) return true;

        // 3) Prosedural fallback (eski davranis)
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int cnt    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }

        return true;
    }

    // ── RHYTHM: Packet Secim ve Spawn ─────────────────────────────────────

    /// <summary>
    /// rhythmTable'dan aktif stage'e gore bir packet secer ve spawn eder.
    /// StageConfig.waveSequence dolu stage'lerde bu metot hic cagrilmaz.
    /// </summary>
    bool TrySpawnPacket(float zPos)
    {
        if (rhythmTable == null) return false;

        int currentWorld = StageManager.Instance != null ? StageManager.Instance.CurrentWorldID : 1;
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        float stageProgress = StageManager.Instance != null ? StageManager.Instance.GetStageProgress01() : -1f;
        SpawnPacketConfig packet = rhythmTable.Pick(currentWorld, currentStage, _lastPacket, stageProgress);

        if (packet == null) return false;

        SpawnFromPacket(packet, zPos);
        _lastPacket = packet;

        Debug.Log($"[SpawnManager] Packet: {packet.packetId} ({packet.packetType}) @ z={zPos:F0}");
        return true;
    }

    /// <summary>
    /// Packet'teki WaveGroup'lari Z uzayina serer.
    /// hasLeadGap: ilk gruptan SONRA leadGapZ metre bosluk eklenir (DelayedCharger icin).
    /// jitterZ / jitterX: dogal gorunum icin per-enemy rastgele kayma.
    /// </summary>
    void SpawnFromPacket(SpawnPacketConfig packet, float baseZ)
    {
        if (packet == null || packet.groups == null || packet.groups.Count == 0) return;

        StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        float density = stage != null ? stage.spawnDensity : 1f;
        // Rhythm tarafinda density etkisini yumuşak tut: count bazli hafif olcekleme.
        float safeDensity = Mathf.Clamp(density, 0.75f, 1.35f);

        for (int g = 0; g < packet.groups.Count; g++)
        {
            WaveGroup group = packet.groups[g];
            if (group == null || group.archetype == null) continue;
            int spawnCount = Mathf.Clamp(Mathf.RoundToInt(group.count * safeDensity), 1, 15);
            float groupBaseZ = baseZ + (g * packet.groupZStep);
            // DelayedCharger: ilk grup normal gelsin, sonraki gruplardan once bosluk eklensin.
            if (packet.hasLeadGap && g > 0)
                groupBaseZ += packet.leadGapZ;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 shape = GetPacketShapeOffset(packet.packetType, g, i, spawnCount, packet.intraZStep);
                float z = groupBaseZ
                        + shape.y
                        + (i * packet.intraZStep * 0.35f)
                        + Random.Range(-packet.jitterZ * 0.5f, packet.jitterZ * 0.5f);

                Vector3 pos = GetLaneBiasedSpawnPos(group.laneBias, i, spawnCount, z);
                pos.x += shape.x;

                // X jitter (saf Spread dis diger lane'ler icin dogallik)
                if (packet.jitterX > 0f)
                    pos.x = Mathf.Clamp(
                        pos.x + Random.Range(-packet.jitterX * 0.5f, packet.jitterX * 0.5f),
                        -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);

                SpawnArchetypeEnemy(group.archetype, pos);
            }
        }
    }

    // Packet bazli hafif formation offset'leri.
    // Amaç: line-string hissini kırmak; mevcut lane ve rhythm akışını bozmamak.
    Vector2 GetPacketShapeOffset(PacketType type, int groupIndex, int memberIndex, int groupCount, float intraZStep)
    {
        float t = groupCount <= 1 ? 0f : (float)memberIndex / (groupCount - 1); // 0..1
        float centered = t - 0.5f;                                              // -0.5..0.5
        float zig = (memberIndex % 2 == 0) ? -1f : 1f;

        switch (type)
        {
            case PacketType.Baseline:
                // Hafif staggered line
                return new Vector2(centered * 0.55f + zig * 0.15f, zig * intraZStep * 0.35f);

            case PacketType.DenseSwarm:
            {
                // Cluster/blob: merkez etrafında küçük bulut
                float angle = memberIndex * 1.618f;
                float ring = 0.25f + (memberIndex % 3) * 0.22f;
                float x = Mathf.Cos(angle) * ring * 1.4f;
                float z = Mathf.Sin(angle) * ring * 1.0f + zig * intraZStep * 0.18f;
                return new Vector2(x, z);
            }

            case PacketType.DelayedCharger:
                // Support -> daha yaygın; Charger -> center biased ama üst üste değil
                if (groupIndex == 0)
                    return new Vector2(centered * 0.85f + zig * 0.12f, zig * intraZStep * 0.28f);
                return new Vector2(centered * 0.35f + zig * 0.18f, zig * intraZStep * 0.22f);

            case PacketType.ArmorCheck:
                // Front anchor (group0) + arka/yan destek (group1+)
                if (groupIndex == 0)
                    return new Vector2(centered * 0.28f, -0.45f * intraZStep);
                return new Vector2(centered * 0.75f + zig * 0.15f, 0.55f * intraZStep + zig * 0.12f);

            case PacketType.Relief:
                // Seyrek, rahat, ama tek çizgi değil
                return new Vector2(centered * 0.95f + zig * 0.20f, zig * intraZStep * 0.55f);

            default:
                return Vector2.zero;
        }
    }

    // ── Gate ──────────────────────────────────────────────────────────────

    void SpawnNextGateGroup(float zPos) => SpawnNormalPair(zPos, pity: false);

    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;

        GateConfig leftGate  = PickGateFromPool();
        GateConfig rightGate = PickGateFromPoolDistinct(leftGate);
        int choiceGroupId = _nextGateChoiceGroupId++;

        SpawnGate(leftGate,  new Vector3(-offset, 1.5f, zPos), scale: 1f, choiceGroupId: choiceGroupId);
        SpawnGate(rightGate, new Vector3( offset, 1.5f, zPos), scale: 1f, choiceGroupId: choiceGroupId);
        _spawnedGateGroups++;
        _lastGateSpawnZ = zPos;
        _followupUsedForLastGate = false;
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
        GatePoolConfig pool  = GetActiveGatePool();
        int            stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return pool != null ? pool.PickRandom(stage) : null;
    }

    void SpawnGate(GateConfig data, Vector3 pos, float scale = 1f, int choiceGroupId = 0)
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
            gate.SetChoiceGroup(choiceGroupId);
            gate.BindGateConfig(data);
        }

        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);

        Destroy(obj, 45f);
    }

    // ── Prosedural Fallback ───────────────────────────────────────────────

    int PickWaveType(float p)
    {
        if (p < 0.25f) return 0;
        float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2;
    }

    void NormalWave(float z, int n)
    {
        int   cols = Mathf.Min(n, 4);
        int   rows = Mathf.CeilToInt((float)n / cols);
        int   pl   = 0;
        float gap  = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx   = -(gap * (cols - 1)) * 0.5f;

        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            {
                PlaceEnemy(new Vector3(
                    Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                    1.2f, z + r * 3f));
                pl++;
            }
    }

    void HeavyWave(float z, int n)
    {
        for (int i = 0; i < n; i++)
            PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
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
        if (enemyPrefab != null)
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy"; obj.AddComponent<Enemy>();
        }

        ConfigureEnemyPhysics(obj);
        obj.GetComponent<Enemy>()?.Initialize(GetEnemyStatsForSpawn());
    }

    public void SetMobHP(int normalHP, int eliteHP, float density = 1f)
    {
        _overrideNormalHP = normalHP;
        _overrideEliteHP  = eliteHP;
        _densityMult      = density;
        _hpOverrideActive = true;
        Debug.Log($"[SpawnManager] Mob HP override: Normal={normalHP}, Elite={eliteHP}, Density={density}");
    }

    DifficultyManager.EnemyStats GetEnemyStatsForSpawn()
    {
        if (_hpOverrideActive)
        {
            return new DifficultyManager.EnemyStats(
                health:   Mathf.RoundToInt(_overrideNormalHP),
                damage:   _stats.Damage,
                speed:    _stats.Speed,
                cpReward: _stats.CPReward);
        }
        return _stats;
    }

    // ── Manuel WaveSequence (StageConfig'e el ile baglanmis dalgalar) ─────

    bool TrySpawnConfiguredWave(float zPos)
    {
        StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        if (stage == null || stage.waveSequence == null || stage.waveSequence.Count == 0)
            return false;

        int safeIndex = Mathf.Clamp(_waveCursor, 0, stage.waveSequence.Count - 1);
        WaveConfig wave = stage.waveSequence[safeIndex];
        if (wave == null) return false;

        if (wave.waveRole == WaveRole.Boss) return false;

        SpawnWaveFromConfig(wave, zPos);
        _waveCursor++;
        return true;
    }

    void SpawnWaveFromConfig(WaveConfig wave, float baseZ)
    {
        if (wave == null || wave.groups == null || wave.groups.Count == 0) return;

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

    // ── Archetype Spawn ───────────────────────────────────────────────────

    void SpawnArchetypeEnemy(EnemyArchetypeConfig archetype, Vector3 pos)
    {
        if (archetype == null) return;

        StageConfig stage    = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        float       targetDps = stage != null ? stage.targetDps : 100f;

        int hp       = archetype.GetHP(targetDps);
        int cpReward = archetype.GetCPReward(targetDps);

        var stats = new DifficultyManager.EnemyStats(
            health:   hp,
            damage:   archetype.contactDamage,
            speed:    archetype.moveSpeed,
            cpReward: cpReward);

        GameObject obj;
        if (enemyPrefab != null)
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy";
            obj.AddComponent<Enemy>();
        }

        ConfigureEnemyPhysics(obj);
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(stats);
            enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
            enemy.ConfigureArchetype(archetype);
        }
    }

    // ── Lane Yardimcisi ───────────────────────────────────────────────────

    void ConfigureEnemyPhysics(GameObject obj)
    {
        if (obj == null) return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            SetLayerRecursive(obj, enemyLayer);

        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    Vector3 GetLaneBiasedSpawnPos(LaneBias bias, int index, int total, float z)
    {
        float x     = 0f;
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
                x = (total <= 1) ? 0f : Mathf.Lerp(left, right, (float)index / (total - 1));
                break;
        }

        x = Mathf.Clamp(x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);
        return new Vector3(x, 1.2f, z);
    }
}

```

## Spawnpacketconfig.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Spawn Packet Konfigurasyonu v1
///
/// Bir "packet", WaveConfig'e benzer ama sahneye gömülü waveSequence'a
/// bagimli degil. SpawnRhythmTable uzerinden stage araligina gore
/// agirlikli olarak secilir ve SpawnManager'in prosedural fallback
/// katmani yerine gecer.
///
/// PacketType kodlari:
///   Baseline       — duz tek hat, Stage 1-3 zemini
///   DenseSwarm     — kalabalik, siki, merkez agirlikli
///   DelayedCharger — once destek, ardindan leadGapZ bosluk, sonra tehdit
///   ArmorCheck     — zirhli on hat + normal destek
///   Relief         — seyrek, yavas, nefes momenti
///
/// ASSETS: Create > TopEndWar > SpawnPacketConfig
/// </summary>
[CreateAssetMenu(fileName = "Packet_", menuName = "TopEndWar/SpawnPacketConfig")]
public class SpawnPacketConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string    packetId   = "baseline_01";
    public PacketType packetType = PacketType.Baseline;

    [Header("Gruplar")]
    [Tooltip("Her grup bir burst; gruplar arasi groupZStep kadar Z boslugu birakilir.\n" +
             "WaveGroup yapisi dogrudan kullanilir (archetype + count + laneBias).")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Header("Z Zamanlama")]
    [Tooltip("Gruplar arasi Z mesafesi (metre).")]
    public float groupZStep = 6f;

    [Tooltip("Grup icindeki her dusman icin rastgele Z kayması — ± bu degerin yarisi.")]
    public float jitterZ = 1.2f;

    [Tooltip("Grup icindeki her dusman icin rastgele X kayması — ± bu degerin yarisi.")]
    public float jitterX = 0.4f;

    [Tooltip("Grup icindeki dusman Z adimi (sira adimi). Arti deger grubu dagitir, 0 uust uste.")]
    public float intraZStep = 1.6f;

    [Header("Lead Gap  (DelayedCharger icin)")]
    [Tooltip("Ilk gruptan once bosluk birakilsin mi? Bir oncu destek grubundan sonra\n" +
             "tehdit grubunun geciktirmeli gelmesi icin kullanilir.")]
    public bool  hasLeadGap = false;

    [Tooltip("Bas boslugu Z miktari (metre). hasLeadGap = true ise uygulanir.")]
    public float leadGapZ   = 8f;

#if UNITY_EDITOR
    void OnValidate()
    {
        groupZStep  = Mathf.Max(2f, groupZStep);
        jitterZ     = Mathf.Max(0f, jitterZ);
        jitterX     = Mathf.Max(0f, jitterX);
        intraZStep  = Mathf.Max(0f, intraZStep);
        leadGapZ    = Mathf.Max(0f, leadGapZ);

        if (!string.IsNullOrEmpty(packetId))
            name = $"Packet_{packetType}_{packetId}";
    }
#endif
}

/// <summary>
/// Packet'in davranis sinifini tanimlar.
/// SpawnManager bu enum'u filtreleme veya debug icin kullanabilir;
/// gercek spawn davranisi WaveGroup + timing alanlari ile belirlenir.
/// </summary>
public enum PacketType
{
    Baseline,        // Sabit hat — tahmin edilebilir, Stage 1–3 zemini
    DenseSwarm,      // Kalabalik cluster — Swarm arketiplerini bekler
    DelayedCharger,  // Destek + bosluk + tehdit — hasLeadGap flag'i ile calisir
    ArmorCheck,      // Zirhli on hat + normal destek — ArmorPen degerini test eder
    Relief,          // Seyrek, yavas, nefes — skor araligi kapama icin
    GuardedCore,
    EliteSpike,
    BossPrep
}
```

## Spawnrhythmtable.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Spawn Rhythm Tablosu v1
///
/// SpawnManager, aktif stage ID'sine gore bu tablodan agirlikli random
/// bir SpawnPacketConfig secer. Tasarimci her stage bandi icin
/// packet agirliklarini Inspector'dan dugumleyebilir.
///
/// Secim onceligi:
///   StageConfig.waveSequence dolu → o kullanilir, bu tablo devreye GIRMEZ.
///   waveSequence bos              → bu tablodan packet secilir.
///   rhythmTable de atanmamissa    → prosedural fallback (SpawnManager eski kodu).
///
/// ASSETS: Create > TopEndWar > SpawnRhythmTable
/// </summary>
[CreateAssetMenu(fileName = "RhythmTable_", menuName = "TopEndWar/SpawnRhythmTable")]
public class SpawnRhythmTable : ScriptableObject
{
    [Header("Packet Havuzu")]
    [Tooltip("Stage araligina gore agirlikli packet secimi.\n" +
             "Ayni stage icin birden fazla entry eklenebilir; agirlikla orani belirlenir.")]
    public List<RhythmEntry> entries = new List<RhythmEntry>();

    /// <summary>
    /// Mevcut stage'e uygun kayitlar arasinda agirlikli random secim yapar.
    /// exclude: son secilen packet — ayni packet ust uste gelmemesi icin
    ///          agirliginin %25'ine dusurilur (tamamen elenmez, sonuz dongu olmaz).
    /// </summary>
    public SpawnPacketConfig Pick(int currentWorld,int currentStage, SpawnPacketConfig exclude = null, float stageProgress01 = -1f)
    {
        // Aktif stage araligina uyan kayitlari topla
        var pool  = new List<(SpawnPacketConfig packet, float weight)>();
        float total = 0f;

        foreach (RhythmEntry e in entries)
        {
            if (e.packet == null) continue;
            if (e.worldID != currentWorld) continue;
            if (currentStage < e.minStage || currentStage > e.maxStage) continue;

            // Son secilen packet'i tamamen eleme; agirligini yarisla (cesitlilik saglanir)
            float w = (e.packet == exclude) ? e.weight * 0.25f : e.weight;
            if (stageProgress01 >= 0f)
                w *= GetProgressMultiplier(e.packet.packetType, stageProgress01);

            pool.Add((e.packet, w));
            total += w;
        }

        if (pool.Count == 0) return null;

        float roll = Random.value * total;
        float acc  = 0f;
        foreach (var (p, w) in pool)
        {
            acc += w;
            if (roll <= acc) return p;
        }

        return pool[pool.Count - 1].packet;   // float tolerans kapama
    }

    float GetProgressMultiplier(PacketType packetType, float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);
        float mid    = Mathf.InverseLerp(0.26f, 0.66f, progress01);
        float early  = 1f - Mathf.InverseLerp(0.0f, 0.24f, progress01);
        float spike  = Mathf.InverseLerp(0.52f, 0.86f, progress01);
        float finish = Mathf.InverseLerp(0.78f, 1.0f, progress01);

        return packetType switch
        {
            PacketType.Relief       => Mathf.Lerp(2.8f, 0.4f, progress01),
            PacketType.Baseline     => 0.85f + early * 1.05f + Mathf.Max(0f, 0.3f - Mathf.Abs(progress01 - 0.18f)),
            PacketType.DenseSwarm   => 0.55f + mid * 1.25f + spike * 0.35f,
            PacketType.DelayedCharger => 0.35f + mid * 0.65f + spike * 1.25f + finish * 0.6f,
            PacketType.ArmorCheck   => 0.30f + mid * 0.35f + spike * 1.55f + finish * 1.15f,
            PacketType.GuardedCore  => 0.55f + spike * 0.55f + finish * 0.85f,
            PacketType.EliteSpike   => 0.25f + spike * 1.55f + finish * 1.3f,
            PacketType.BossPrep     => 0.20f + finish * 2.25f,
            _                       => 1f,
        };
    }
}

/// <summary>
/// Rhythm tablosundaki tek bir kayit: hangi packet, ne agirligi, hangi stage araliginda.
/// </summary>
[System.Serializable]
public class RhythmEntry
{
    [Tooltip("Secilecek packet konfigurasyonu.")]
    public SpawnPacketConfig packet;

    [Tooltip("Agirlik. Buyuk deger = daha sik secilir. Diger entry'lerle oranli calisir.")]
    [Range(0.1f, 10f)]
    public float weight = 1f;

    [Tooltip("Bu entry hangi stage'den itibaren aktif (dahil).")]
    public int minStage = 1;

    [Tooltip("Bu entry hangi stage'e kadar aktif (dahil).")]
    public int maxStage = 5;

    public int worldID = 1;
}

```

## StageClearUI.cs

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageClearUI : MonoBehaviour
{
    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    Canvas _canvas;
    GameObject _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _rewardText;
    Button _continueButton;
    Button _retryButton;
    Button _upgradeButton;
    Button _mainMenuButton;

    bool _visible;
    GameEvents.StageClearInfo _lastInfo;

    void OnEnable()
    {
        GameEvents.OnStageCleared += HandleStageCleared;
    }

    void OnDisable()
    {
        GameEvents.OnStageCleared -= HandleStageCleared;
    }

    void Start()
    {
        BuildUIIfNeeded();
        Hide();
    }

    void Update()
    {
        if (_visible)
            Time.timeScale = 0f;
    }

    void HandleStageCleared(GameEvents.StageClearInfo info)
    {
        _lastInfo = info;
        BuildUIIfNeeded();
        Show(info);
    }

    void Show(GameEvents.StageClearInfo info)
    {
        _visible = true;
        if (_panel != null) _panel.SetActive(true);

        if (_titleText != null)
            _titleText.text = info.worldCleared ? "WORLD CLEAR" : "STAGE CLEAR";

        if (_rewardText != null)
        {
            string nextText = info.hasNextStage ? "Continue ready" : "Run complete";
            _rewardText.text = $"{info.stageName}\nGold +{info.goldReward}\n{nextText}";
        }

        if (_continueButton != null)
            _continueButton.gameObject.SetActive(info.hasNextStage);

        Time.timeScale = 0f;
    }

    void Hide()
    {
        _visible = false;
        if (_panel != null) _panel.SetActive(false);
    }

    void BuildUIIfNeeded()
    {
        if (_panel != null) return;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            var canvasObj = new GameObject("StageClearCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 95;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        _panel = new GameObject("StageClearPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.10f, 0.95f);
        Stretch(_panel.GetComponent<RectTransform>());

        _titleText = MakeText(_panel, "STAGE CLEAR", new Vector2(0.5f, 0.78f), 64, new Color(1f, 0.85f, 0.15f), FontStyles.Bold);
        _rewardText = MakeText(_panel, "Reward", new Vector2(0.5f, 0.62f), 30, Color.white, FontStyles.Normal);

        _continueButton = MakeButton(_panel, "CONTINUE", new Vector2(0.5f, 0.38f), new Vector2(420, 96), new Color(0.15f, 0.75f, 0.25f), OnContinueClicked);
        _retryButton = MakeButton(_panel, "RETRY", new Vector2(0.5f, 0.28f), new Vector2(420, 88), new Color(0.55f, 0.20f, 0.20f), OnRetryClicked);
        _upgradeButton = MakeButton(_panel, "UPGRADE", new Vector2(0.5f, 0.18f), new Vector2(420, 88), new Color(0.20f, 0.35f, 0.65f), OnUpgradeClicked);
        _mainMenuButton = MakeButton(_panel, "MAIN MENU", new Vector2(0.5f, 0.08f), new Vector2(420, 80), new Color(0.25f, 0.25f, 0.35f), OnMainMenuClicked);
    }

    void OnContinueClicked()
    {
        Time.timeScale = 1f;
        Hide();
        StageManager.Instance?.ContinueAfterStageClear();
    }

    void OnRetryClicked()
    {
        Time.timeScale = 1f;
        Hide();
        StageManager.Instance?.RestartCurrentStage();
    }

    void OnUpgradeClicked()
    {
        EquipmentUI equipment = Object.FindAnyObjectByType<EquipmentUI>();
        if (equipment != null)
        {
            equipment.Toggle();
            Time.timeScale = 0f;
        }
    }

    void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        Hide();
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor, float size, Color color, FontStyles style)
    {
        var go = new GameObject("Text_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900, 120);
        return tmp;
    }

    Button MakeButton(GameObject parent, string label, Vector2 anchor, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = size.y * 0.35f;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;

        return btn;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

```

## Stageconfig.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v3.1
///
/// v3 → v3.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: stageNameKey, stageDescriptionKey, threatTagKeys, recommendedBuildKey
///   • DisplayStageName, DisplayDescription, DisplayRecommendedBuild property'leri eklendi
///   • Mevcut tüm denge, boss, spawn ve ödül alanları DOKUNULMADI
///
/// Eski alanlar:
///   locationName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > StageConfig
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID      = 1;
    public int    stageID      = 1;
    public string locationName = "Frontier Pass";

    // ── Localization Keys ──────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Display property'leri fallback olarak locationName vb. döner.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Stage adı lokalizasyon anahtarı  ör: stage_w1_01_name")]
    public string stageNameKey        = "";
    [Tooltip("Stage kısa açıklaması / briefing anahtarı  ör: stage_w1_01_desc")]
    public string stageDescriptionKey = "";
    [Tooltip("Önerilen build / strateji ipucu anahtarı  ör: stage_w1_01_build_tip")]
    public string recommendedBuildKey = "";
    [Tooltip("Stage tehdit özellikleri etiket anahtarları  ör: [ 'tag_heavy', 'tag_armored' ]")]
    public List<string> threatTagKeys = new List<string>();

    // ── Display Properties (Localization-ready fallback) ───────────────────
    public string DisplayStageName        => string.IsNullOrEmpty(stageNameKey)        ? locationName : stageNameKey;
    public string DisplayDescription      => string.IsNullOrEmpty(stageDescriptionKey) ? ""           : stageDescriptionKey;
    public string DisplayRecommendedBuild => string.IsNullOrEmpty(recommendedBuildKey) ? ""           : recommendedBuildKey;

    // ── Denge ─────────────────────────────────────────────────────────────
    [Header("Denge — Temel Deger")]
    [Tooltip(
        "Bu stage icin hedeflenen oyuncu DPS'i.\n" +
        "HP formulleri bu degere gore hesaplanir:\n" +
        "  Normal mob   = targetDps x 1.0\n" +
        "  Elite mob    = targetDps x 4.0\n" +
        "  Mini-boss HP = targetDps x 13\n" +
        "  Final boss   = targetDps x 36")]
    public float targetDps = 70f;

    [Tooltip(
        "Hedeflenen oyuncu Combat Power'i.\n" +
        "0 = otomatik hesapla (targetDps'ten türet).\n" +
        "Debug/UI'da player vs stage güç karşılaştırması yapılır.")]
    [Range(0f, 10000f)]
    public float targetPower = 0f;  // 0 = auto-calculate from targetDps

    [Header("Kapi Butcesi")]
    [Tooltip(
        "Bu stage'deki kapilarin verebilecegi max DPS buyume katsayisi.\n" +
        "entryDps = round(targetDps / gateBudgetMult)\n" +
        "Stage 1-5: 1.40 | 6-9: 1.50 | 10: 1.55 | 11-19: 1.65 | 20: 1.70\n" +
        "Stage 21-29: 1.80 | 30-34: 1.88 | 35: 1.95")]
    [Range(1f, 2.5f)]
    public float gateBudgetMult = 1.40f;

    // ── Boss Turu ─────────────────────────────────────────────────────────
    [Header("Boss")]
    public BossType   bossType   = BossType.None;
    public BossConfig bossConfig;

    [Header("Wave Sequence")]
    [Tooltip("Bu stage boyunca oynatilacak dalga sirasi. Bos birakılırsa eski procedural fallback kullanilir.")]
    public List<WaveConfig> waveSequence = new List<WaveConfig>();

    // ── Spawn Yogunlugu ───────────────────────────────────────────────────
    [Header("Spawn")]
    [Tooltip("1.0 = normal. DifficultyManager carpaniyla carpilir.")]
    [Range(0.5f, 3f)]
    public float spawnDensity = 1f;

    // ── Odüller ───────────────────────────────────────────────────────────
    [Header("Odüller")]
    [Tooltip("Bos birakılırsa EconomyConfig formulunden hesaplanir.")]
    public int    goldRewardOverride  = 0;  // 0 = formul kullan
    public bool   hasMidStageLoot     = true;
    [Range(0f, 1f)]
    public float  techCoreDropChance  = 0.15f;
    [Tooltip("Stage tamamlaninca saatlik altina eklenen miktar")]
    public int    offlineBoostPerHour = 5;

    // ── Tutorial ──────────────────────────────────────────────────────────
    [Header("Ozel")]
    public bool isTutorialStage = false;

    // ── HP Formul Metotlari (StageManager kullanir) ───────────────────────

    public int GetNormalMobHP()  => Mathf.RoundToInt(targetDps * 1.0f);
    public int GetEliteHP()      => Mathf.RoundToInt(targetDps * 4.0f);
    public int GetMiniBossHP()   => Mathf.RoundToInt(targetDps * 13f);
    public int GetFinalBossHP()  => Mathf.RoundToInt(targetDps * 36f);

    public int GetBossHP()
    {
        return bossType switch
        {
            BossType.MiniBoss  => GetMiniBossHP(),
            BossType.FinalBoss => GetFinalBossHP(),
            _                  => 0,
        };
    }

    public int GetEntryDps() => Mathf.RoundToInt(targetDps / gateBudgetMult);

    /// <summary>
    /// Etkili Stage Target Power (PlayerStats.CombatPower ile kıyaslanabilir).
    /// 
    /// Eğer targetPower > 0 ise manuel değer döner.
    /// Eğer targetPower == 0 ise targetDps'ten otomatik hesaplar.
    /// 
    /// Auto-calc formülü:
    ///   displayedDps = targetDps
    ///   maxHp = 500 (ortalama komutan)
    ///   armorPen = 5 (minimal)
    ///   pierceCount = 0 (standart)
    ///   range = 22 (Assault ortalama)
    /// </summary>
    public int GetEffectiveTargetPower()
    {
        if (targetPower > 0f)
            return Mathf.RoundToInt(targetPower);
        
        // Auto-calculate from targetDps
        float power = 0f;
        power += targetDps * 1.5f;              // DPS ağırlık
        power += 500f * 0.2f;                   // Ortalama maxHp katkısı (100)
        power += 5f * 8f;                      // Ortalama armorPen (75)
        power += 0f * 50f;                      // Typical pierce = 0
        power += 22f * 2f;                      // Típik range = 22 (44)
        
        return Mathf.Max(1, Mathf.RoundToInt(power));
    }

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
/// Top End War — Stage Yoneticisi v2.1 (Patched)
/// 
/// Yenilikler: 
/// - Her stage başında sahne temizliği (ClearRuntimeObjects) eklendi.
/// - Oyuncunun pozisyonu ve durumu her stage başında sıfırlanıyor.
/// - Stage mesafeleri yerel (0'dan başlar) hale getirildi.
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
    bool _stageClearLocked = false;

    /// <summary>Runtime sırasında aktif stage configuration'ı döner (null olabilir).</summary>
    public StageConfig GetActiveStageConfig() => _activeStage;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        GameEvents.OnBossDefeated += HandleBossDefeated;
    }

    void Start() => LoadStage(_currentWorldID, _currentStageID, true);

    void OnDestroy()
    {
        GameEvents.OnBossDefeated -= HandleBossDefeated;
    }

    void Update()
    {
        if (_activeStage == null) return;
        if (_stageClearLocked) return;
        if (_activeStage.IsBossStage) return;
        if (PlayerStats.Instance == null) return;

        if (PlayerStats.Instance.transform.position.z >= GetStageEndZ())
            OnStageComplete();
    }

    // ── Stage Yukle ───────────────────────────────────────────────────────
    public void LoadStage(int worldID, int stageID, bool resetRunState = true)
    {
        if (resetRunState)
        {
            RunState.Instance.Reset();
            SaveManager.Instance?.BeginRun();
            PlayerStats.Instance?.ResetRunGateBonuses();
        }

        Time.timeScale = 1f;
        _stageClearLocked = false;
        _currentWorldID = worldID;
        _currentStageID = stageID;

        _activeWorld = FindWorld(worldID);
        _activeStage = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

        // PATCH: Kirli runtime objelerini temizle
        ClearRuntimeObjects();

        // PATCH: Oyuncuyu yeni stage başlangıcına al
        Playercontroller pc = FindFirstObjectByType<Playercontroller>();
        if (pc != null)
            pc.ResetForStage(0f);

        // PATCH: HP ve ölüm state sıfırlama (Güvenlik için revive)
        PlayerStats.Instance?.ReviveFromGameOver();
        PlayerStats.Instance?.SetExpectedCP(_activeStage.targetDps);

        // Biyomu guncelle
        if (_activeWorld != null)
            BiomeManager.Instance?.SetBiome(_activeWorld.biome);

        // SpawnManager reset
        SpawnManager.Instance?.ResetForStage();

        // Mob HP ilet
        ApplyMobHP();

        // Boss stage ise BossManager'a HP ilet
        if (_activeStage.IsBossStage)
            ApplyBossHP();

        GameEvents.OnStageChanged?.Invoke(worldID, stageID);
        Debug.Log($"[StageManager] W{worldID}-{stageID} | targetDps={_activeStage.targetDps} " +
                  $"| mobHP={_activeStage.GetNormalMobHP()} | bossHP={_activeStage.GetBossHP()}");
    }

    // ── Runtime Temizliği (PATCH) ──────────────────────────────────────────
    void ClearRuntimeObjects()
    {
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (enemy != null) Destroy(enemy.gameObject);

        foreach (var gate in FindObjectsByType<Gate>(FindObjectsSortMode.None))
            if (gate != null) Destroy(gate.gameObject);

        foreach (var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            if (bullet != null && bullet.gameObject.activeSelf)
                bullet.gameObject.SetActive(false);

        foreach (var boss in FindObjectsByType<BossHitReceiver>(FindObjectsSortMode.None))
            if (boss != null)
                boss.gameObject.SetActive(false);
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
        if (_stageClearLocked) return;
        if (_activeStage.IsBossStage && BossManager.Instance != null && BossManager.Instance.IsActive())
            return;

        int gold = _activeStage.goldRewardOverride > 0
            ? _activeStage.goldRewardOverride
            : economyConfig != null
                ? economyConfig.GetGoldReward(_activeStage.stageID, _activeStage.targetDps)
                : 150;

        RunState.Instance.AddRunGold(gold);
        EconomyManager.Instance?.AddGold(gold);
        SaveManager.Instance?.RegisterStageComplete();
        
        Debug.Log($"[StageManager] Stage tamamlandi. Altin: +{gold}");
        _stageClearLocked = true;
        Time.timeScale = 0f;

        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        bool worldCleared = _activeStage.bossType == BossType.FinalBoss || nextStage == null;

        GameEvents.OnStageCleared?.Invoke(new GameEvents.StageClearInfo
        {
            worldID = _currentWorldID,
            stageID = _currentStageID,
            stageName = _activeStage.DisplayStageName,
            goldReward = gold,
            hasNextStage = nextStage != null,
            worldCleared = worldCleared
        });

        if (worldCleared)
            OnWorldComplete();
    }

    public void OnMidStageReached()
    {
        if (_activeStage == null || !_activeStage.hasMidStageLoot) return;

        int midGold = economyConfig != null
            ? economyConfig.GetMidLootGold(_activeStage.stageID, _activeStage.targetDps)
            : 50;

        EconomyManager.Instance?.AddGold(midGold);
        Debug.Log($"[StageManager] Micro-loot: +{midGold} Altin");
    }

    void OnWorldComplete()
    {
        if (_activeWorld != null)
        {
            EconomyManager.Instance?.AddOfflineRate(_activeWorld.offlineIncomeBoost);
            if (_activeWorld.unlockedCommander != null)
                Debug.Log($"[StageManager] Komutan acildi: {_activeWorld.unlockedCommander.commanderName}");
        }

        GameEvents.OnWorldChanged?.Invoke(_currentWorldID);
        GameEvents.OnVictory?.Invoke();
    }

    void HandleBossDefeated()
    {
        if (_activeStage != null && _activeStage.IsBossStage)
            OnStageComplete();
    }

    // ── Mesafe Hesaplamaları (PATCH) ───────────────────────────────────────
    public float GetStageLength()
    {
        float bossDistance = SpawnManager.Instance != null ? SpawnManager.Instance.bossDistance : 1200f;
        return Mathf.Max(300f, bossDistance);
    }

    public float GetStageStartZ() => 0f;
    public float GetStageEndZ() => GetStageLength();

    public float GetStageProgress01()
    {
        if (PlayerStats.Instance == null) return 0f;
        return Mathf.InverseLerp(GetStageStartZ(), GetStageEndZ(), PlayerStats.Instance.transform.position.z);
    }

    // ── Kontroller ────────────────────────────────────────────────────────
    public void ContinueAfterStageClear()
    {
        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        if (nextStage == null) return;
        LoadStage(_currentWorldID, _currentStageID + 1, false);
    }

    public void RestartCurrentStage() => LoadStage(_currentWorldID, _currentStageID, true);
    public bool HasNextStage() => FindStage(_currentWorldID, _currentStageID + 1) != null;

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
/// Top End War — Silah Arketip Konfigurasyonu v2.1
///
/// v2 → v2.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: weaponNameKey, descriptionKey, roleKey, tag1Key, tag2Key
///   • DisplayWeaponName, DisplayDescription, DisplayRole, DisplayTag1, DisplayTag2 property'leri eklendi
///   • Mevcut weaponName ve tüm combat alanları DOKUNULMADI
///
/// Eski alanlar:
///   weaponName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > WeaponArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "TopEndWar/WeaponArchetypeConfig")]
public class WeaponArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string weaponId = "assault";
    public string weaponName = "Assault Rifle";
    public WeaponFamily family = WeaponFamily.Assault;

    // ── Localization Keys ──────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Display property'leri fallback olarak weaponName vb. döner.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Silah adı lokalizasyon anahtarı  ör: weapon_assault_name")]
    public string weaponNameKey  = "";
    [Tooltip("Kısa açıklama / flavor text anahtarı  ör: weapon_assault_desc")]
    public string descriptionKey = "";
    [Tooltip("Silahın rolünü tanımlayan anahtar  ör: weapon_assault_role  →  'Orta Menzil Genel Amaç'")]
    public string roleKey        = "";
    [Tooltip("UI alt satır sol tag anahtarı  ör: weapon_assault_tag1  →  'HIZLI'")]
    public string tag1Key        = "";
    [Tooltip("UI alt satır sağ tag anahtarı  ör: weapon_assault_tag2  →  'DENGE'")]
    public string tag2Key        = "";

    // ── Display Properties (Localization-ready fallback) ───────────────────
    public string DisplayWeaponName  => string.IsNullOrEmpty(weaponNameKey)  ? weaponName : weaponNameKey;
    public string DisplayDescription => string.IsNullOrEmpty(descriptionKey) ? ""          : descriptionKey;
    public string DisplayRole        => string.IsNullOrEmpty(roleKey)        ? ""          : roleKey;
    public string DisplayTag1        => string.IsNullOrEmpty(tag1Key)        ? ""          : tag1Key;
    public string DisplayTag2        => string.IsNullOrEmpty(tag2Key)        ? ""          : tag2Key;

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

### Klasör: Assets\_TopEndWar\UI\Theme

## UIArtLibrary.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TopEndWar.UI.Theme
{
    [CreateAssetMenu(menuName = "Top End War/UI Art Library", fileName = "UIArtLibrary")]
    public class UIArtLibrary : ScriptableObject
    {
        const string AssetPath = "Assets/_TopEndWar/UI/Theme/UIArtLibrary.asset";

        static UIArtLibrary _instance;
        static readonly HashSet<string> MissingWarnings = new HashSet<string>();

        [Header("WorldMaps")]
        public Sprite World01MapViewport;
        public Sprite World01MapMaster;

        [Header("Buttons")]
        public Sprite PrimaryButton;
        public Sprite SecondaryButton;
        public Sprite TabButton;
        public Sprite BottomNavItem;
        public Sprite BottomNavItemActive;

        [Header("Panels")]
        public Sprite PanelDark;
        public Sprite PanelCream;
        public Sprite PanelHero;

        [Header("Icons")]
        public Sprite IconEnergy;
        public Sprite IconGold;
        public Sprite IconGems;
        public Sprite IconMail;
        public Sprite IconSettings;
        public Sprite IconHome;
        public Sprite IconMap;
        public Sprite IconCommander;
        public Sprite IconEvents;
        public Sprite IconShop;
        public Sprite IconMissions;
        public Sprite IconUpgrade;
        public Sprite IconFreeReward;
        public Sprite IconLocked;
        public Sprite IconBoss;
        public Sprite IconBack;

        [Header("Nodes")]
        public Sprite NodeNormal;
        public Sprite NodeCurrent;
        public Sprite NodeCompleted;
        public Sprite NodeLocked;
        public Sprite NodeBoss;

        [Header("Rewards")]
        public Sprite RewardGold;
        public Sprite RewardTechCore;
        public Sprite RewardGearBox;
        public Sprite RewardGems;
        public Sprite RewardParts;

        [Header("Commander")]
        public Sprite CommanderPortrait;
        public Sprite CommanderFull;

        public static UIArtLibrary Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

#if UNITY_EDITOR
                _instance = AssetDatabase.LoadAssetAtPath<UIArtLibrary>(AssetPath);
#endif
                return _instance;
            }
        }

        public static bool TryApply(Image image, Sprite sprite, Color fallbackColor, string assetName)
        {
            if (image == null)
            {
                return false;
            }

            if (sprite == null)
            {
                WarnMissing(assetName);
                image.sprite = null;
                image.color = fallbackColor;
                image.type = Image.Type.Simple;
                return false;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            return true;
        }

        public static void WarnMissing(string assetName)
        {
            if (string.IsNullOrEmpty(assetName) || MissingWarnings.Contains(assetName))
            {
                return;
            }

            MissingWarnings.Add(assetName);
            Debug.LogWarning($"[UI ART] Missing sprite: {assetName}");
        }

        public Sprite GetBottomNavSprite(bool active)
        {
            return active ? BottomNavItemActive : BottomNavItem;
        }

        public Sprite GetNavIcon(string screenId)
        {
            switch (screenId)
            {
                case Core.UIConstants.HomeScreenId:
                    return IconHome;
                case Core.UIConstants.WorldMapScreenId:
                    return IconMap;
                case Core.UIConstants.CommanderScreenId:
                    return IconCommander;
                case "events_placeholder":
                    return IconEvents;
                case "shop_placeholder":
                    return IconShop;
                default:
                    return null;
            }
        }

        public Sprite GetRewardSprite(string labelKey, string fallbackLabel, string accent)
        {
            string key = $"{labelKey} {fallbackLabel} {accent}".ToLowerInvariant();
            if (key.Contains("tech"))
            {
                return RewardTechCore;
            }

            if (key.Contains("gear"))
            {
                return RewardGearBox;
            }

            if (key.Contains("gem") || key.Contains("premium"))
            {
                return RewardGems;
            }

            if (key.Contains("part"))
            {
                return RewardParts;
            }

            return RewardGold;
        }

        public string GetRewardAssetName(string labelKey, string fallbackLabel, string accent)
        {
            string key = $"{labelKey} {fallbackLabel} {accent}".ToLowerInvariant();
            if (key.Contains("tech"))
            {
                return "Reward_TechCore";
            }

            if (key.Contains("gear"))
            {
                return "Reward_GearBox";
            }

            if (key.Contains("gem") || key.Contains("premium"))
            {
                return "Reward_Gems";
            }

            if (key.Contains("part"))
            {
                return "Reward_Parts";
            }

            return "Reward_Gold";
        }
    }
}

```

## UITheme.cs

```csharp
using UnityEngine;

namespace TopEndWar.UI.Theme
{
    public static class UITheme
    {
        public static readonly Color DeepNavy = Hex(0x07101A);
        public static readonly Color NavyPanel = Hex(0x102236);
        public static readonly Color Gunmetal = Hex(0x182D42);
        public static readonly Color WarmCream = Hex(0xEFE1C4);
        public static readonly Color SoftCream = Hex(0xFFF2D8);
        public static readonly Color Sand = Hex(0xD9BF8F);
        public static readonly Color MutedGold = Hex(0xD7B77A);
        public static readonly Color ButtonGoldTop = Hex(0xEFBD63);
        public static readonly Color ButtonGoldBottom = Hex(0xC77C35);
        public static readonly Color Teal = Hex(0x70D0CB);
        public static readonly Color TealDark = Hex(0x2D5A64);
        public static readonly Color Amber = Hex(0xE5A65D);
        public static readonly Color Danger = Hex(0xE8735D);
        public static readonly Color DangerDark = Hex(0x7A2D24);
        public static readonly Color EpicPurple = Hex(0xB28AE2);

        public static readonly Color TextPrimary = SoftCream;
        public static readonly Color TextSecondary = new Color(0.82f, 0.83f, 0.86f, 1f);
        public static readonly Color TextDark = DeepNavy;

        public static Color Hex(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}

```

### Klasör: Assets\_TopEndWar\UI\Screens

## CommanderScreenView.cs

```csharp
using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class CommanderScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _titleText;
        TMP_Text _summaryText;
        TMP_Text _tabContentText;
        TMP_Text _statusText;
        TMP_Text _commanderPlaceholderText;
        Image _commanderFullImage;
        Transform _slotContainer;
        Transform _leftSlotContainer;
        Transform _rightSlotContainer;
        Transform _reserveSlotContainer;
        readonly List<GameObject> _slotObjects = new List<GameObject>();
        int _upgradeBonus;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            CommanderScreenData data = _dataProvider.GetCommanderScreenData();
            _titleText.text = $"{UILocalization.Get("commander.header.title", "COMMANDER / EQUIPMENT")}  {data.commanderName}";
            _summaryText.text = $"POWER  {data.totalPower + _upgradeBonus:N0}\nHP  {data.hp:N0}   DPS  {data.dps:N0}   DEF  {data.defense:N0}\n{data.roleDescription}";
            _tabContentText.text = "Loadout focus: frontline pressure, armor sustain, and weapon uptime.";
            BindSlots(data.slots);
        }

        void Build()
        {
            if (_titleText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(16f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 110f, minHeight: 104f);
            _titleText = UIFactory.CreateText("Title", header.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_titleText.rectTransform, Vector2.zero, Vector2.zero);
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = 24f;
            _titleText.fontSizeMax = 38f;

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 780f);

            PanelBaseView commanderBoard = UIFactory.CreateUIObject("CommanderMainBoard", contentArea.transform).AddComponent<PanelBaseView>();
            commanderBoard.Build(24f, PanelVisualStyle.Hero);
            UIFactory.Stretch(commanderBoard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            PanelBaseView summaryPanel = CreateAnchoredPanel(commanderBoard.ContentRoot, "PowerSummary", new Vector2(0.18f, 1f), new Vector2(0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 118f), new Vector2(0f, -2f), PanelVisualStyle.Cream, 16f);
            _summaryText = UIFactory.CreateText("Summary", summaryPanel.ContentRoot, string.Empty, 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_summaryText.rectTransform, Vector2.zero, Vector2.zero);
            _summaryText.enableAutoSizing = true;
            _summaryText.fontSizeMin = 18f;
            _summaryText.fontSizeMax = 26f;

            PanelBaseView visual = CreateAnchoredPanel(commanderBoard.ContentRoot, "CommanderHeroArea", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 650f), new Vector2(0f, -130f), PanelVisualStyle.Cream, 18f);
            Image visualGlow = UIFactory.CreateImage("HeroGlow", visual.ContentRoot, new Color(0.96f, 0.78f, 0.42f, 0.12f));
            UIFactory.Stretch(visualGlow.rectTransform, new Vector2(210f, 28f), new Vector2(-210f, -28f));
            visualGlow.raycastTarget = false;

            GameObject leftSlots = UIFactory.CreateUIObject("LeftEquipmentSlots", visual.ContentRoot);
            UIFactory.SetAnchors(leftSlots.GetComponent<RectTransform>(), new Vector2(0f, 0.12f), new Vector2(0.26f, 0.92f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup leftLayout = UIFactory.AddVerticalLayout(leftSlots, 14f, TextAnchor.MiddleCenter, true, false);
            leftLayout.childForceExpandHeight = false;
            _leftSlotContainer = leftSlots.transform;

            GameObject rightSlots = UIFactory.CreateUIObject("RightEquipmentSlots", visual.ContentRoot);
            UIFactory.SetAnchors(rightSlots.GetComponent<RectTransform>(), new Vector2(0.74f, 0.12f), new Vector2(1f, 0.92f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rightLayout = UIFactory.AddVerticalLayout(rightSlots, 14f, TextAnchor.MiddleCenter, true, false);
            rightLayout.childForceExpandHeight = false;
            _rightSlotContainer = rightSlots.transform;

            _commanderFullImage = UIFactory.CreateUIObject("CommanderFullArt", visual.ContentRoot).AddComponent<Image>();
            _commanderFullImage.preserveAspect = true;
            _commanderFullImage.raycastTarget = false;
            RectTransform commanderRect = _commanderFullImage.rectTransform;
            commanderRect.anchorMin = new Vector2(0.5f, 0.5f);
            commanderRect.anchorMax = new Vector2(0.5f, 0.5f);
            commanderRect.pivot = new Vector2(0.5f, 0.5f);
            commanderRect.sizeDelta = new Vector2(430f, 550f);
            commanderRect.anchoredPosition = new Vector2(0f, -28f);
            UIArtLibrary art = UIArtLibrary.Instance;
            bool hasCommanderArt = UIConstants.UseCommanderSprites && UIArtLibrary.TryApply(_commanderFullImage, art != null ? art.CommanderFull : null, Color.clear, "Commander_Full_01");
            _commanderFullImage.enabled = hasCommanderArt;
            _commanderPlaceholderText = UIFactory.CreateText("VisualText", visual.ContentRoot, "COMMANDER DIORAMA\nUpgrade armor, rifle uptime, and squad sustain", 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(_commanderPlaceholderText.rectTransform, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _commanderPlaceholderText.enableAutoSizing = true;
            _commanderPlaceholderText.fontSizeMin = 18f;
            _commanderPlaceholderText.fontSizeMax = 26f;
            _commanderPlaceholderText.gameObject.SetActive(!hasCommanderArt);

            PanelBaseView tabs = CreateAnchoredPanel(commanderBoard.ContentRoot, "TabsPanel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 152f), new Vector2(0f, 306f), PanelVisualStyle.Dark, 14f);
            VerticalLayoutGroup tabsPanelLayout = UIFactory.AddVerticalLayout(tabs.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            tabsPanelLayout.childForceExpandHeight = false;
            GameObject tabsRow = UIFactory.CreateUIObject("TabsRow", tabs.ContentRoot);
            HorizontalLayoutGroup tabsLayout = UIFactory.AddHorizontalLayout(tabsRow, 12f, TextAnchor.MiddleCenter, true, false);
            tabsLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(tabsRow, preferredHeight: 62f, minHeight: 58f);
            CreateTabButton(tabsRow.transform, "commander.loadout_tab", "Loadout focus: frontline pressure, armor sustain, and weapon uptime.");
            CreateTabButton(tabsRow.transform, "commander.skills_tab", "Skill focus: suppression burst, emergency shield, and drone command.");
            CreateTabButton(tabsRow.transform, "commander.stats_tab", "Stat focus: HP, DPS, DEF, fire rate, and squad reinforcement tempo.");
            _tabContentText = UIFactory.CreateText("TabContent", tabs.ContentRoot, string.Empty, 19, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_tabContentText, 56f, true, 15f, 20f);

            PanelBaseView squad = CreateAnchoredPanel(commanderBoard.ContentRoot, "SquadPanel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(0f, 140f), PanelVisualStyle.Cream, 14f);
            VerticalLayoutGroup squadLayout = UIFactory.AddVerticalLayout(squad.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            squadLayout.childForceExpandHeight = false;
            TMP_Text squadTitle = UIFactory.CreateText("SquadTitle", squad.ContentRoot, "SQUAD", 22, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(squadTitle, 28f);
            GameObject squadRow = UIFactory.CreateUIObject("SquadRow", squad.ContentRoot);
            UIFactory.AddLayoutElement(squadRow, preferredHeight: 94f, minHeight: 88f);
            HorizontalLayoutGroup squadRowLayout = UIFactory.AddHorizontalLayout(squadRow, 10f, TextAnchor.MiddleCenter, true, false);
            squadRowLayout.childForceExpandHeight = false;
            CreateSquadCard(squadRow.transform, "Alpha", "20");
            CreateSquadCard(squadRow.transform, "Bulwark", "18");
            CreateSquadCard(squadRow.transform, "Medic", "18");
            CreateSquadCard(squadRow.transform, "Drone", "18");

            PanelBaseView reserveSlots = CreateAnchoredPanel(commanderBoard.ContentRoot, "ReserveSlots", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(0f, 12f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup reserveLayout = UIFactory.AddHorizontalLayout(reserveSlots.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, true, false);
            reserveLayout.childForceExpandHeight = false;
            _reserveSlotContainer = reserveSlots.ContentRoot;
            _slotContainer = _reserveSlotContainer;

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(14f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 142f, minHeight: 132f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 8f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;
            _statusText = UIFactory.CreateText("StatusText", footer.ContentRoot, "Upgrade console ready.", 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 26f, true, 15f, 18f);
            GameObject actions = UIFactory.CreateUIObject("ActionsRow", footer.ContentRoot);
            UIFactory.AddLayoutElement(actions, preferredHeight: 82f, minHeight: 76f);
            HorizontalLayoutGroup actionsLayout = UIFactory.AddHorizontalLayout(actions, 12f, TextAnchor.MiddleCenter, true, false);
            actionsLayout.childForceExpandHeight = false;
            TMP_Text costBox = UIFactory.CreateText("UpgradeCost", actions.transform, "COST\n12,000", 22, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AddLayoutElement(costBox.gameObject, preferredWidth: 160f, preferredHeight: 76f, minHeight: 70f);
            CreateActionButton(actions.transform, LocalizationKeys.CommanderUpgrade, ApplyUpgrade, ButtonVisualStyle.Primary);
            CreateActionButton(actions.transform, "commander.auto_equip", ApplyAutoEquip, ButtonVisualStyle.Secondary);
        }

        void ApplyUpgrade()
        {
            _upgradeBonus += 100;
            _statusText.text = $"Upgrade applied. Commander power +{_upgradeBonus:N0}.";
            _screenManager.ActionRouter.ApplyCommanderUpgrade();
            RefreshView();
        }

        void ApplyAutoEquip()
        {
            _tabContentText.text = "Auto Equip preview: strongest available gear slotted by mock rules.";
            _statusText.text = "Best gear equipped";
            _screenManager.ActionRouter.ApplyAutoEquip();
        }

        PanelBaseView CreateSection(Transform parent, string name, float preferredHeight, float minHeight, PanelVisualStyle style = PanelVisualStyle.Auto)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(18f, style);
            UIFactory.AddLayoutElement(panel.gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.UpperLeft, true, false);
            layout.childForceExpandHeight = false;
            return panel;
        }

        PanelBaseView CreateAnchoredPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, PanelVisualStyle style, float padding)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(padding, style);
            UIFactory.SetAnchors(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return panel;
        }

        void BindSlots(List<EquipmentSlotData> slots)
        {
            while (_slotObjects.Count < slots.Count)
            {
                GameObject slotGo = UIFactory.CreateUIObject("Slot", GetSlotParent(_slotObjects.Count));
                slotGo.AddComponent<EquipmentSlotView>();
                _slotObjects.Add(slotGo);
            }

            for (int i = 0; i < _slotObjects.Count; i++)
            {
                bool active = i < slots.Count;
                _slotObjects[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                Transform targetParent = GetSlotParent(i);
                if (_slotObjects[i].transform.parent != targetParent)
                {
                    _slotObjects[i].transform.SetParent(targetParent, false);
                }

                _slotObjects[i].GetComponent<EquipmentSlotView>().Bind(slots[i]);

                LayoutElement layoutElement = UIFactory.AddLayoutElement(_slotObjects[i], flexibleWidth: i < 6 ? -1f : 1f, preferredHeight: i < 6 ? 132f : 96f, minHeight: i < 6 ? 120f : 88f);
                if (i < 6)
                {
                    layoutElement.preferredWidth = -1f;
                    layoutElement.flexibleWidth = 1f;
                }
            }
        }

        Transform GetSlotParent(int index)
        {
            if (index < 3 && _leftSlotContainer != null)
            {
                return _leftSlotContainer;
            }

            if (index < 6 && _rightSlotContainer != null)
            {
                return _rightSlotContainer;
            }

            return _reserveSlotContainer != null ? _reserveSlotContainer : _slotContainer;
        }

        void CreateSquadCard(Transform parent, string label, string level)
        {
            PanelBaseView card = UIFactory.CreateUIObject($"{label}SquadCard", parent).AddComponent<PanelBaseView>();
            card.Build(10f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(card.gameObject, flexibleWidth: 1f, preferredHeight: 90f, minHeight: 84f);
            TMP_Text text = UIFactory.CreateText("SquadText", card.ContentRoot, $"{label}\nLv.{level}", 18, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 20f;
        }

        void CreateTabButton(Transform parent, string key, string content)
        {
            GameObject buttonGo = UIFactory.CreateUIObject($"{key}_Tab", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 60f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, key);
            button.SetOnClick(() => _tabContentText.text = content);
        }

        void CreateActionButton(Transform parent, string key, UnityEngine.Events.UnityAction action, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject($"{key}_Action", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 64f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            button.SetLabelKey(key, key);
            button.SetOnClick(action);
        }
    }
}

```

## HomeScreenView.cs

```csharp
using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class HomeScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerTitle;
        TMP_Text _headerSubtitle;
        TMP_Text _campaignInfo;
        TMP_Text _powerInfo;
        TMP_Text _claimNotice;
        TMP_Text _statusText;
        PrimaryButtonView _continueButton;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            HomeScreenData data = _dataProvider.GetHomeScreenData();
            _headerTitle.text = $"{UILocalization.Get("home.header.title", "HOME / HQ")}  {data.currentWorldName}";
            _headerSubtitle.text = UILocalization.Get("home.header.subtitle", "Frontline command, squad upkeep, and campaign control.");
            _campaignInfo.text = $"{data.currentStageName}\nSTAGE {data.currentStageId:00}  |  {data.completedStages}/{data.totalStages} CLEARED";
            _powerInfo.text = $"{UILocalization.Get("home.power.status", "POWER STATUS")}\nYOUR POWER  {data.playerPower:N0}\nTARGET POWER  {data.targetPower:N0}\nSTATE  {UILocalization.Get(data.powerState, data.powerState)}";
            _claimNotice.text = $"{UILocalization.Get("home.claim.notice", "Claim notice available at HQ dispatch.")}\nTOTAL RUNS  {data.totalRuns}";
            _statusText.text = UILocalization.Get("home.status.ready", "HQ console standing by.");
            _continueButton.SetLabelKey(LocalizationKeys.HomeContinueCta, "CONTINUE CAMPAIGN");
            _continueButton.SetOnClick(_screenManager.ActionRouter.ContinueCampaign);
        }

        void Build()
        {
            if (_headerTitle != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView headerPanel = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            headerPanel.Build(18f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(headerPanel.gameObject, preferredHeight: 122f, minHeight: 112f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(headerPanel.ContentRoot.gameObject, 4f, TextAnchor.MiddleCenter, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerTitle = UIFactory.CreateText("HeaderTitle", headerPanel.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            _headerSubtitle = UIFactory.CreateText("HeaderSubtitle", headerPanel.ContentRoot, string.Empty, 20, UITheme.TealDark, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_headerTitle, 40f, true, 24f, 34f);
            UIFactory.ConfigureTextBlock(_headerSubtitle, 42f, true, 16f, 20f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 680f);

            PanelBaseView homeBoard = UIFactory.CreateUIObject("HomeMainBoard", contentArea.transform).AddComponent<PanelBaseView>();
            homeBoard.Build(24f, PanelVisualStyle.Hero);
            UIFactory.Stretch(homeBoard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup boardLayout = UIFactory.AddVerticalLayout(homeBoard.ContentRoot.gameObject, 16f, TextAnchor.UpperCenter, true, false);
            boardLayout.padding = new RectOffset(8, 8, 8, 8);
            boardLayout.childForceExpandHeight = false;

            PanelBaseView campaignPanel = UIFactory.CreateUIObject("CampaignPanel", homeBoard.ContentRoot).AddComponent<PanelBaseView>();
            campaignPanel.Build(24f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(campaignPanel.gameObject, preferredHeight: 330f, minHeight: 300f);
            VerticalLayoutGroup campaignLayout = UIFactory.AddVerticalLayout(campaignPanel.ContentRoot.gameObject, 12f, TextAnchor.UpperCenter, true, false);
            campaignLayout.childForceExpandHeight = false;
            TMP_Text campaignLabel = UIFactory.CreateText("CampaignLabel", campaignPanel.ContentRoot, "CAMPAIGN OBJECTIVE", 20, UITheme.TealDark, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(campaignLabel, 30f);
            _campaignInfo = UIFactory.CreateText("CampaignInfo", campaignPanel.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_campaignInfo, 126f, true, 22f, 36f);
            GameObject continueButtonGo = UIFactory.CreateUIObject("ContinueButton", campaignPanel.ContentRoot);
            UIFactory.AddLayoutElement(continueButtonGo, preferredHeight: 94f, minHeight: 86f);
            _continueButton = continueButtonGo.AddComponent<PrimaryButtonView>();
            _continueButton.Build(ButtonVisualStyle.Primary);

            GameObject splitRow = UIFactory.CreateUIObject("StatusRow", homeBoard.ContentRoot);
            HorizontalLayoutGroup splitLayout = UIFactory.AddHorizontalLayout(splitRow, 16f, TextAnchor.UpperCenter, true, false);
            splitLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(splitRow, preferredHeight: 188f, minHeight: 176f);

            PanelBaseView powerPanel = UIFactory.CreateUIObject("PowerPanel", splitRow.transform).AddComponent<PanelBaseView>();
            powerPanel.Build(18f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(powerPanel.gameObject, flexibleWidth: 1f, preferredHeight: 188f, minHeight: 176f);
            _powerInfo = UIFactory.CreateText("PowerInfo", powerPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.Stretch(_powerInfo.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            _powerInfo.enableAutoSizing = true;
            _powerInfo.fontSizeMin = 16f;
            _powerInfo.fontSizeMax = 22f;

            PanelBaseView claimPanel = UIFactory.CreateUIObject("ClaimPanel", splitRow.transform).AddComponent<PanelBaseView>();
            claimPanel.Build(18f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(claimPanel.gameObject, flexibleWidth: 1f, preferredHeight: 188f, minHeight: 176f);
            _claimNotice = UIFactory.CreateText("ClaimNotice", claimPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.Stretch(_claimNotice.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            _claimNotice.enableAutoSizing = true;
            _claimNotice.fontSizeMin = 16f;
            _claimNotice.fontSizeMax = 22f;

            PanelBaseView quickActions = UIFactory.CreateUIObject("QuickActions", homeBoard.ContentRoot).AddComponent<PanelBaseView>();
            quickActions.Build(20f, PanelVisualStyle.Dark);
            UIFactory.AddLayoutElement(quickActions.gameObject, preferredHeight: 318f, minHeight: 286f);
            VerticalLayoutGroup quickLayout = UIFactory.AddVerticalLayout(quickActions.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            quickLayout.childForceExpandHeight = false;
            TMP_Text quickTitle = UIFactory.CreateText("QuickTitle", quickActions.ContentRoot, "QUICK ACTIONS", 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(quickTitle, 30f);
            _statusText = UIFactory.CreateText("StatusText", quickActions.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 30f, true, 15f, 18f);

            CreateQuickRow(quickActions.ContentRoot, new List<(string key, System.Action action)>
            {
                ("home.quick.free_reward", () => HandleLocalStatus("Reward claimed", _screenManager.ActionRouter.ClaimFreeReward)),
                ("home.quick.daily", () => HandleLocalStatus("Daily Missions coming soon", _screenManager.ActionRouter.OpenDailyMissions))
            });
            CreateQuickRow(quickActions.ContentRoot, new List<(string key, System.Action action)>
            {
                ("home.quick.upgrade", _screenManager.ActionRouter.ShowCommander),
                ("home.quick.event", () => HandleLocalStatus("Events coming soon", _screenManager.ActionRouter.OpenEvents))
            });
        }

        void HandleLocalStatus(string status, System.Action action)
        {
            _statusText.text = status;
            action?.Invoke();
        }

        void CreateQuickRow(Transform parent, List<(string key, System.Action action)> actions)
        {
            GameObject row = UIFactory.CreateUIObject("QuickRow", parent);
            HorizontalLayoutGroup rowLayout = UIFactory.AddHorizontalLayout(row, 12f, TextAnchor.MiddleCenter, true, false);
            rowLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(row, preferredHeight: 76f, minHeight: 72f);

            foreach ((string key, System.Action action) entry in actions)
            {
                GameObject buttonGo = UIFactory.CreateUIObject($"{entry.key}_Button", row.transform);
                UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 76f, minHeight: 72f);
                PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
                button.Build(ButtonVisualStyle.Secondary);
                button.SetLabelKey(entry.key, entry.key);
                button.SetOnClick(() => entry.action());
            }
        }
    }
}

```

## ResultScreenView.cs

```csharp
using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class ResultScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerText;
        TMP_Text _stageText;
        TMP_Text _starsText;
        TMP_Text _recommendationText;
        TMP_Text _failureText;
        Transform _performanceContainer;
        Transform _rewardContainer;
        GameObject _firstClearPanel;
        Transform _firstClearRewardContainer;
        readonly List<GameObject> _spawnedPerformance = new List<GameObject>();
        readonly List<GameObject> _spawnedRewards = new List<GameObject>();
        readonly List<GameObject> _spawnedFirstClear = new List<GameObject>();

        PrimaryButtonView _primaryButton;
        PrimaryButtonView _secondaryA;
        PrimaryButtonView _secondaryB;
        PrimaryButtonView _secondaryC;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            ResultScreenData data = _dataProvider.GetResultScreenData();
            _headerText.text = data.isVictory ? UILocalization.Get("result.victory", "STAGE CLEAR") : UILocalization.Get("result.defeat", "DEFEAT");
            _stageText.text = data.stageName;
            _starsText.text = data.isVictory ? new string('*', Mathf.Clamp(data.stars, 1, 3)) : "NO STARS";
            _failureText.gameObject.SetActive(!data.isVictory);
            _failureText.text = data.failureReason;
            _recommendationText.text = $"{UILocalization.Get("result.recommendation", "RECOMMENDATION")}\n{data.recommendation}";
            _performanceContainer.gameObject.SetActive(data.isVictory && data.performanceGoals.Count > 0);
            _firstClearPanel.SetActive(data.isVictory && data.hasFirstClearBonus);

            BindTagPills(_spawnedPerformance, _performanceContainer, data.performanceGoals);
            BindRewardCards(_spawnedRewards, _rewardContainer, data.rewards);
            BindRewardCards(_spawnedFirstClear, _firstClearRewardContainer, data.firstClearRewards);

            if (data.isVictory)
            {
                _primaryButton.SetLabelKey(LocalizationKeys.ResultNextStage, "NEXT STAGE");
                _primaryButton.SetOnClick(_screenManager.ActionRouter.NextStageFromResult);
                _secondaryA.SetLabelKey("result.secondary.upgrade", "UPGRADE");
                _secondaryA.SetOnClick(_screenManager.ActionRouter.ShowCommander);
                _secondaryB.SetLabelKey(LocalizationKeys.ResultWorldMap, "WORLD MAP");
                _secondaryB.SetOnClick(_screenManager.ActionRouter.ShowWorldMap);
                _secondaryC.SetLabelKey(LocalizationKeys.ResultRetryStage, "RETRY STAGE");
                _secondaryC.SetOnClick(_screenManager.ActionRouter.RetryCurrentStage);
                _secondaryC.gameObject.SetActive(true);
            }
            else
            {
                _primaryButton.SetLabelKey(LocalizationKeys.CommanderUpgrade, "UPGRADE");
                _primaryButton.SetOnClick(_screenManager.ActionRouter.ShowCommander);
                _secondaryA.SetLabelKey(LocalizationKeys.ResultRetryStage, "RETRY STAGE");
                _secondaryA.SetOnClick(_screenManager.ActionRouter.RetryCurrentStage);
                _secondaryB.SetLabelKey(LocalizationKeys.ResultWorldMap, "WORLD MAP");
                _secondaryB.SetOnClick(_screenManager.ActionRouter.ShowWorldMap);
                _secondaryC.gameObject.SetActive(false);
            }
        }

        void Build()
        {
            if (_headerText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(18f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 210f, minHeight: 190f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(header.ContentRoot.gameObject, 6f, TextAnchor.UpperCenter, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerText = UIFactory.CreateText("HeaderText", header.ContentRoot, string.Empty, 42, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            _stageText = UIFactory.CreateText("StageText", header.ContentRoot, string.Empty, 26, UITheme.TealDark, FontStyles.Bold, TextAlignmentOptions.Center);
            _starsText = UIFactory.CreateText("StarsText", header.ContentRoot, string.Empty, 28, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_headerText, 66f, true, 30f, 44f);
            UIFactory.ConfigureTextBlock(_stageText, 42f, true, 18f, 26f);
            UIFactory.ConfigureTextBlock(_starsText, 38f, true, 18f, 28f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 560f);

            PanelBaseView resultBoard = UIFactory.CreateUIObject("ResultMainBoard", contentArea.transform).AddComponent<PanelBaseView>();
            resultBoard.Build(24f, PanelVisualStyle.Hero);
            UIFactory.Stretch(resultBoard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup boardLayout = UIFactory.AddVerticalLayout(resultBoard.ContentRoot.gameObject, 16f, TextAnchor.UpperCenter, true, false);
            boardLayout.padding = new RectOffset(6, 6, 6, 6);
            boardLayout.childForceExpandHeight = false;

            PanelBaseView previewSwitch = CreateSection(resultBoard.ContentRoot, "PreviewSwitch", 86f, 84f, PanelVisualStyle.PlainDark);
            GameObject switchRow = UIFactory.CreateUIObject("SwitchRow", previewSwitch.ContentRoot);
            HorizontalLayoutGroup switchLayout = UIFactory.AddHorizontalLayout(switchRow, 12f, TextAnchor.MiddleCenter, true, false);
            switchLayout.childForceExpandHeight = false;
            CreatePreviewButton(switchRow.transform, true, "result.preview.victory", ButtonVisualStyle.Secondary);
            CreatePreviewButton(switchRow.transform, false, "result.preview.defeat", ButtonVisualStyle.Danger);
            previewSwitch.gameObject.SetActive(UIConstants.ShowDebugButtons);

            PanelBaseView performance = CreateSection(resultBoard.ContentRoot, "PerformancePanel", 132f, 116f, PanelVisualStyle.PlainDark);
            TMP_Text performanceTitle = UIFactory.CreateText("PerformanceTitle", performance.ContentRoot, "PERFORMANCE GOALS", 20, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(performanceTitle, 28f);
            GameObject performanceRow = UIFactory.CreateUIObject("PerformanceContainer", performance.ContentRoot);
            HorizontalLayoutGroup performanceLayout = UIFactory.AddHorizontalLayout(performanceRow, 10f, TextAnchor.MiddleCenter, false, false);
            performanceLayout.childForceExpandHeight = false;
            _performanceContainer = performanceRow.transform;

            PanelBaseView rewards = CreateSection(resultBoard.ContentRoot, "RewardsPanel", 230f, 210f, PanelVisualStyle.Dark);
            TMP_Text rewardsTitle = UIFactory.CreateText("RewardsTitle", rewards.ContentRoot, "REWARDS", 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(rewardsTitle, 30f);
            GameObject rewardGrid = UIFactory.CreateUIObject("RewardContainer", rewards.ContentRoot);
            UIFactory.AddLayoutElement(rewardGrid, preferredHeight: 140f, minHeight: 128f);
            GridLayoutGroup rewardLayout = rewardGrid.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(220f, 128f);
            rewardLayout.spacing = new Vector2(12f, 12f);
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;
            _rewardContainer = rewardGrid.transform;

            _firstClearPanel = CreateSection(resultBoard.ContentRoot, "FirstClearPanel", 204f, 190f, PanelVisualStyle.Dark).gameObject;
            PanelBaseView firstClear = _firstClearPanel.GetComponent<PanelBaseView>();
            TMP_Text firstClearTitle = UIFactory.CreateText("FirstClearTitle", firstClear.ContentRoot, UILocalization.Get("stage.first_clear", "FIRST CLEAR BONUS"), 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(firstClearTitle, 30f);
            GameObject firstClearGrid = UIFactory.CreateUIObject("Rewards", firstClear.ContentRoot);
            UIFactory.AddLayoutElement(firstClearGrid, preferredHeight: 142f, minHeight: 128f);
            GridLayoutGroup firstClearLayout = firstClearGrid.AddComponent<GridLayoutGroup>();
            firstClearLayout.cellSize = new Vector2(220f, 128f);
            firstClearLayout.spacing = new Vector2(12f, 12f);
            firstClearLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            firstClearLayout.constraintCount = 2;
            _firstClearRewardContainer = firstClearGrid.transform;

            PanelBaseView recommendation = CreateSection(resultBoard.ContentRoot, "RecommendationPanel", 168f, 148f, PanelVisualStyle.PlainDark);
            _failureText = UIFactory.CreateText("FailureText", recommendation.ContentRoot, string.Empty, 24, UITheme.Danger, FontStyles.Bold, TextAlignmentOptions.Center);
            _recommendationText = UIFactory.CreateText("RecommendationText", recommendation.ContentRoot, string.Empty, 21, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_failureText, 34f, true, 16f, 22f);
            UIFactory.ConfigureTextBlock(_recommendationText, 94f, true, 16f, 22f);

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(14f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 166f, minHeight: 154f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 8f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;

            GameObject primaryGo = UIFactory.CreateUIObject("PrimaryAction", footer.ContentRoot);
            UIFactory.AddLayoutElement(primaryGo, preferredHeight: 88f, minHeight: 82f);
            _primaryButton = primaryGo.AddComponent<PrimaryButtonView>();
            _primaryButton.Build(ButtonVisualStyle.Primary);

            GameObject secondaryRow = UIFactory.CreateUIObject("SecondaryRow", footer.ContentRoot);
            HorizontalLayoutGroup secondaryLayout = UIFactory.AddHorizontalLayout(secondaryRow, 12f, TextAnchor.MiddleCenter, true, false);
            secondaryLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(secondaryRow, preferredHeight: 64f, minHeight: 60f);
            _secondaryA = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
            _secondaryB = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
            _secondaryC = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
        }

        PanelBaseView CreateSection(Transform parent, string name, float preferredHeight, float minHeight, PanelVisualStyle style = PanelVisualStyle.Auto)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(18f, style);
            UIFactory.AddLayoutElement(panel.gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.UpperLeft, true, false);
            layout.childForceExpandHeight = false;
            return panel;
        }

        PrimaryButtonView CreateActionButton(Transform parent, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject("SecondaryAction", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            return button;
        }

        void CreatePreviewButton(Transform parent, bool victory, string key, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject(key, parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 60f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            button.SetLabelKey(key, key);
            button.SetOnClick(() =>
            {
                _dataProvider.SetResultPreview(victory);
                RefreshView();
            });
        }

        void BindTagPills(List<GameObject> cache, Transform parent, List<string> tags)
        {
            while (cache.Count < tags.Count)
            {
                GameObject go = UIFactory.CreateUIObject("Goal", parent);
                go.AddComponent<TagPillView>();
                cache.Add(go);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < tags.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<TagPillView>().SetLabel(tags[i]);
            }
        }

        void BindRewardCards(List<GameObject> cache, Transform parent, List<RewardItemData> rewards)
        {
            while (cache.Count < rewards.Count)
            {
                GameObject rewardGo = UIFactory.CreateUIObject("Reward", parent);
                rewardGo.AddComponent<RewardCardView>();
                cache.Add(rewardGo);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < rewards.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<RewardCardView>().Bind(rewards[i]);
            }
        }
    }
}

```

## StageDetailScreenView.cs

```csharp
using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class StageDetailScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _titleText;
        TMP_Text _briefingText;
        TMP_Text _powerCompareText;
        TagPillView _stateBadge;
        TagPillView _bossBadge;
        Transform _threatContainer;
        Transform _enemyContainer;
        Transform _rewardContainer;
        GameObject _firstClearPanel;
        Transform _firstClearRewardContainer;
        TMP_Text _loadoutText;
        TMP_Text _energyCostText;
        TMP_Text _statusText;
        PrimaryButtonView _startRunButton;
        PrimaryButtonView _changeLoadoutButton;

        readonly List<GameObject> _spawnedThreats = new List<GameObject>();
        readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        readonly List<GameObject> _spawnedRewards = new List<GameObject>();
        readonly List<GameObject> _spawnedFirstClearRewards = new List<GameObject>();

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            StageDetailData data = _dataProvider.GetStageDetailData();
            _titleText.text = $"{UILocalization.Get("stage.header.title", "STAGE DETAIL")}  W{data.worldId}-{data.stageId:00}";
            _briefingText.text = $"{data.stageName}\n{data.briefingText}";
            _powerCompareText.text = $"YOUR POWER  {data.playerPower:N0}\nTARGET POWER  {data.targetPower:N0}";
            _stateBadge.SetLabel(UILocalization.Get(data.powerStateKey, data.powerStateKey), data.powerStateKey == "stage.underpowered");
            _bossBadge.gameObject.SetActive(data.isBossStage);
            _loadoutText.text = $"{UILocalization.Get("stage.loadout", "ACTIVE LOADOUT")}\n{data.loadoutName}";
            _energyCostText.text = $"ENERGY\n{data.entryCost}";
            _firstClearPanel.SetActive(data.hasFirstClearBonus);
            _enemyContainer.gameObject.SetActive(data.enemyNames.Count > 0);
            _statusText.text = "Ready to deploy.";
            _startRunButton.SetLabelKey(LocalizationKeys.StageStartRun, "START RUN");

            BindTags(_spawnedThreats, _threatContainer, data.threatKeys);
            BindEnemyCards(data.enemyNames);
            BindRewardCards(_spawnedRewards, _rewardContainer, data.rewards);
            BindRewardCards(_spawnedFirstClearRewards, _firstClearRewardContainer, data.firstClearRewards);
        }

        void Build()
        {
            if (_titleText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);

            PanelBaseView board = UIFactory.CreateUIObject("StageMissionBoard", transform).AddComponent<PanelBaseView>();
            board.Build(28f, PanelVisualStyle.Hero);
            UIFactory.Stretch(board.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            PanelBaseView header = CreateAnchoredPanel(board.ContentRoot, "StageHeader", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 142f), new Vector2(0f, -4f), PanelVisualStyle.Cream, 18f);
            GameObject backButtonGo = UIFactory.CreateUIObject("BackButton", header.ContentRoot);
            UIFactory.SetAnchors(backButtonGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(138f, 64f), new Vector2(0f, 0f));
            PrimaryButtonView backButton = backButtonGo.AddComponent<PrimaryButtonView>();
            backButton.Build(ButtonVisualStyle.Tab);
            backButton.SetLabelKey("common.back", "BACK");
            backButton.SetOnClick(_screenManager.ActionRouter.GoBack);
            _titleText = UIFactory.CreateText("Title", header.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_titleText.rectTransform, new Vector2(150f, 18f), new Vector2(-150f, -18f));
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = 24f;
            _titleText.fontSizeMax = 40f;

            PanelBaseView preview = CreateAnchoredPanel(board.ContentRoot, "BattlePreviewHero", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 430f), new Vector2(0f, -158f), PanelVisualStyle.Cream, 26f);
            Image previewTint = UIFactory.CreateImage("PreviewDioramaTint", preview.ContentRoot, new Color(0.12f, 0.08f, 0.04f, 0.22f));
            UIFactory.Stretch(previewTint.rectTransform, Vector2.zero, Vector2.zero);
            previewTint.raycastTarget = false;
            TMP_Text previewLabel = UIFactory.CreateText("PreviewLabel", preview.ContentRoot, "MISSION DIORAMA", 20, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(previewLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(0f, -8f));
            _briefingText = UIFactory.CreateText("Briefing", preview.ContentRoot, string.Empty, 26, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_briefingText.rectTransform, new Vector2(54f, 62f), new Vector2(-54f, -54f));
            _briefingText.enableAutoSizing = true;
            _briefingText.fontSizeMin = 20f;
            _briefingText.fontSizeMax = 30f;

            GameObject progressRail = UIFactory.CreateUIObject("MissionProgressRail", preview.ContentRoot);
            UIFactory.SetAnchors(progressRail.GetComponent<RectTransform>(), new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(0f, 8f));
            HorizontalLayoutGroup railLayout = UIFactory.AddHorizontalLayout(progressRail, 18f, TextAnchor.MiddleCenter, true, false);
            railLayout.childForceExpandHeight = false;
            CreateRailStep(progressRail.transform, "START", UITheme.Teal);
            CreateRailStep(progressRail.transform, "GATE 1", UITheme.Sand);
            CreateRailStep(progressRail.transform, "GATE 2", UITheme.Sand);
            CreateRailStep(progressRail.transform, "BOSS", UITheme.Danger);

            GameObject stateRow = UIFactory.CreateUIObject("StateRow", board.ContentRoot);
            UIFactory.SetAnchors(stateRow.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 54f), new Vector2(0f, -602f));
            HorizontalLayoutGroup stateLayout = UIFactory.AddHorizontalLayout(stateRow, 12f, TextAnchor.MiddleCenter, false, false);
            stateLayout.childForceExpandHeight = false;
            _stateBadge = UIFactory.CreateUIObject("StateBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge = UIFactory.CreateUIObject("BossBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge.SetLabel(UILocalization.Get("stage.boss", "BOSS STAGE"), true);

            PanelBaseView power = CreateAnchoredPanel(board.ContentRoot, "PowerCompareArea", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 138f), new Vector2(0f, -666f), PanelVisualStyle.Cream, 18f);
            _powerCompareText = UIFactory.CreateText("PowerCompare", power.ContentRoot, string.Empty, 28, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_powerCompareText.rectTransform, new Vector2(24f, 18f), new Vector2(-24f, -18f));
            _powerCompareText.enableAutoSizing = true;
            _powerCompareText.fontSizeMin = 20f;
            _powerCompareText.fontSizeMax = 30f;

            PanelBaseView threatPanel = CreateAnchoredPanel(board.ContentRoot, "ThreatPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(0f, -818f), PanelVisualStyle.PlainDark, 12f);
            GameObject threatRow = UIFactory.CreateUIObject("ThreatContainer", threatPanel.ContentRoot);
            UIFactory.Stretch(threatRow.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup threatLayout = UIFactory.AddHorizontalLayout(threatRow, 10f, TextAnchor.MiddleLeft, false, false);
            threatLayout.childForceExpandHeight = false;
            _threatContainer = threatRow.transform;

            PanelBaseView enemyPanel = CreateAnchoredPanel(board.ContentRoot, "EnemyPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 190f), new Vector2(0f, -902f), PanelVisualStyle.Dark, 18f);
            VerticalLayoutGroup enemyPanelLayout = UIFactory.AddVerticalLayout(enemyPanel.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            enemyPanelLayout.childForceExpandHeight = false;
            TMP_Text enemyTitle = UIFactory.CreateText("EnemyTitle", enemyPanel.ContentRoot, UILocalization.Get("stage.section.enemies", "ENEMIES"), 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(enemyTitle, 30f);
            GameObject enemyList = UIFactory.CreateUIObject("EnemyContainer", enemyPanel.ContentRoot);
            UIFactory.AddLayoutElement(enemyList, preferredHeight: 118f, minHeight: 112f);
            HorizontalLayoutGroup enemyLayout = UIFactory.AddHorizontalLayout(enemyList, 12f, TextAnchor.MiddleCenter, true, false);
            enemyLayout.childForceExpandHeight = false;
            _enemyContainer = enemyList.transform;

            PanelBaseView rewardsPanel = CreateAnchoredPanel(board.ContentRoot, "RewardsPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 194f), new Vector2(0f, -1106f), PanelVisualStyle.Dark, 18f);
            VerticalLayoutGroup rewardsPanelLayout = UIFactory.AddVerticalLayout(rewardsPanel.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rewardsPanelLayout.childForceExpandHeight = false;
            TMP_Text rewardsTitle = UIFactory.CreateText("RewardsTitle", rewardsPanel.ContentRoot, UILocalization.Get("stage.section.rewards", "REWARDS"), 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(rewardsTitle, 30f);
            GameObject rewardGrid = UIFactory.CreateUIObject("RewardContainer", rewardsPanel.ContentRoot);
            UIFactory.AddLayoutElement(rewardGrid, preferredHeight: 126f, minHeight: 118f);
            GridLayoutGroup rewardLayout = rewardGrid.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(220f, 120f);
            rewardLayout.spacing = new Vector2(12f, 12f);
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;
            _rewardContainer = rewardGrid.transform;

            _firstClearPanel = CreateAnchoredPanel(board.ContentRoot, "FirstClearPanel", new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 0f), new Vector2(0f, 158f), new Vector2(0f, 118f), PanelVisualStyle.Dark, 14f).gameObject;
            PanelBaseView firstClearPanelBase = _firstClearPanel.GetComponent<PanelBaseView>();
            VerticalLayoutGroup firstClearPanelLayout = UIFactory.AddVerticalLayout(firstClearPanelBase.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            firstClearPanelLayout.childForceExpandHeight = false;
            TMP_Text firstClearTitle = UIFactory.CreateText("FirstClearTitle", firstClearPanelBase.ContentRoot, UILocalization.Get("stage.first_clear", "FIRST CLEAR BONUS"), 18, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(firstClearTitle, 24f);
            GameObject firstClearGrid = UIFactory.CreateUIObject("Rewards", firstClearPanelBase.ContentRoot);
            UIFactory.AddLayoutElement(firstClearGrid, preferredHeight: 112f, minHeight: 104f);
            HorizontalLayoutGroup firstClearLayout = UIFactory.AddHorizontalLayout(firstClearGrid, 10f, TextAnchor.MiddleCenter, true, false);
            firstClearLayout.childForceExpandHeight = false;
            _firstClearRewardContainer = firstClearGrid.transform;

            PanelBaseView loadoutPanel = CreateAnchoredPanel(board.ContentRoot, "LoadoutPanel", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 158f), new Vector2(0f, 118f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup loadoutLayout = UIFactory.AddHorizontalLayout(loadoutPanel.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, false);
            loadoutLayout.childForceExpandHeight = false;
            _loadoutText = UIFactory.CreateText("LoadoutText", loadoutPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.AddLayoutElement(_loadoutText.gameObject, flexibleWidth: 1f, preferredHeight: 80f, minHeight: 72f);
            _loadoutText.enableAutoSizing = true;
            _loadoutText.fontSizeMin = 16f;
            _loadoutText.fontSizeMax = 22f;
            GameObject loadoutButtonGo = UIFactory.CreateUIObject("ChangeLoadoutButton", loadoutPanel.ContentRoot);
            UIFactory.AddLayoutElement(loadoutButtonGo, preferredWidth: 210f, preferredHeight: 78f, minHeight: 72f);
            _changeLoadoutButton = loadoutButtonGo.AddComponent<PrimaryButtonView>();
            _changeLoadoutButton.Build(ButtonVisualStyle.Secondary);
            _changeLoadoutButton.SetLabelKey("stage.change_loadout", "CHANGE LOADOUT");
            _changeLoadoutButton.SetOnClick(_screenManager.ActionRouter.OpenChangeLoadout);

            _statusText = UIFactory.CreateText("StatusText", board.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(_statusText.rectTransform, new Vector2(0.24f, 0f), new Vector2(0.76f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(0f, 104f));
            _statusText.enableAutoSizing = true;
            _statusText.fontSizeMin = 15f;
            _statusText.fontSizeMax = 18f;

            PanelBaseView footer = CreateAnchoredPanel(board.ContentRoot, "BottomArea", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 98f), new Vector2(0f, 0f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup footerLayout = UIFactory.AddHorizontalLayout(footer.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;

            TMP_Text energyCost = UIFactory.CreateText("EnergyCost", footer.ContentRoot, "ENERGY\n10", 24, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            _energyCostText = energyCost;
            UIFactory.AddLayoutElement(energyCost.gameObject, preferredWidth: 170f, preferredHeight: 76f, minHeight: 72f);

            GameObject actionRow = UIFactory.CreateUIObject("ActionRow", footer.ContentRoot);
            HorizontalLayoutGroup actionLayout = UIFactory.AddHorizontalLayout(actionRow, 12f, TextAnchor.MiddleCenter, false, false);
            actionLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(actionRow, preferredHeight: 82f, minHeight: 78f, flexibleWidth: 1f);

            GameObject startGo = UIFactory.CreateUIObject("StartRunButton", actionRow.transform);
            UIFactory.AddLayoutElement(startGo, flexibleWidth: 1f, preferredHeight: 92f, minHeight: 84f);
            _startRunButton = startGo.AddComponent<PrimaryButtonView>();
            _startRunButton.Build(ButtonVisualStyle.Primary);
            _startRunButton.SetOnClick(StartRun);

            GameObject previewVictory = UIFactory.CreateUIObject("PreviewVictoryButton", actionRow.transform);
            UIFactory.AddLayoutElement(previewVictory, preferredWidth: 220f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView previewVictoryButton = previewVictory.AddComponent<PrimaryButtonView>();
            previewVictoryButton.Build(ButtonVisualStyle.Secondary);
            previewVictoryButton.SetLabelKey("result.preview.victory", "PREVIEW VICTORY");
            previewVictoryButton.SetOnClick(_screenManager.ActionRouter.ShowResultVictory);

            GameObject previewDefeat = UIFactory.CreateUIObject("PreviewDefeatButton", actionRow.transform);
            UIFactory.AddLayoutElement(previewDefeat, preferredWidth: 220f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView previewDefeatButton = previewDefeat.AddComponent<PrimaryButtonView>();
            previewDefeatButton.Build(ButtonVisualStyle.Danger);
            previewDefeatButton.SetLabelKey("result.preview.defeat", "PREVIEW DEFEAT");
            previewDefeatButton.SetOnClick(_screenManager.ActionRouter.ShowResultDefeat);
            previewVictory.SetActive(UIConstants.ShowDebugButtons);
            previewDefeat.SetActive(UIConstants.ShowDebugButtons);
        }

        void CreateRailStep(Transform parent, string label, Color accent)
        {
            GameObject step = UIFactory.CreateUIObject($"Rail_{label}", parent);
            UIFactory.AddLayoutElement(step, flexibleWidth: 1f, preferredHeight: 70f, minHeight: 64f);
            TMP_Text text = UIFactory.CreateText("Label", step.transform, label, 18, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        }

        PanelBaseView CreateAnchoredPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, PanelVisualStyle style, float padding)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(padding, style);
            UIFactory.SetAnchors(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return panel;
        }

        void BindTags(List<GameObject> cache, Transform parent, List<string> tags)
        {
            while (cache.Count < tags.Count)
            {
                GameObject go = UIFactory.CreateUIObject("Tag", parent);
                go.AddComponent<TagPillView>();
                cache.Add(go);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < tags.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<TagPillView>().SetLabel(tags[i], tags[i] == "Boss");
            }
        }

        void BindEnemyCards(List<string> enemies)
        {
            while (_spawnedEnemies.Count < enemies.Count)
            {
                GameObject panelGo = UIFactory.CreateUIObject("EnemyCard", _enemyContainer);
                PanelBaseView panel = panelGo.AddComponent<PanelBaseView>();
                panel.Build(12f, PanelVisualStyle.PlainDark);
                UIFactory.AddLayoutElement(panelGo, flexibleWidth: 1f, preferredHeight: 112f, minHeight: 104f);
                TMP_Text text = UIFactory.CreateText("EnemyText", panel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 22f;
                _spawnedEnemies.Add(panelGo);
            }

            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                bool active = i < enemies.Count;
                _spawnedEnemies[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                TMP_Text text = _spawnedEnemies[i].GetComponentInChildren<TMP_Text>();
                text.text = enemies[i];
            }
        }

        void BindRewardCards(List<GameObject> cache, Transform parent, List<RewardItemData> rewards)
        {
            while (cache.Count < rewards.Count)
            {
                GameObject rewardGo = UIFactory.CreateUIObject("Reward", parent);
                rewardGo.AddComponent<RewardCardView>();
                cache.Add(rewardGo);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < rewards.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<RewardCardView>().Bind(rewards[i]);
            }
        }

        void StartRun()
        {
            if (Application.CanStreamedLevelBeLoaded(UIConstants.SampleSceneName))
            {
                SceneManager.LoadScene(UIConstants.SampleSceneName);
            }
            else
            {
                _statusText.text = "SampleScene is not available in Build Settings.";
                Debug.LogWarning("[UI] SampleScene could not be loaded from MainMenu.");
            }
        }
    }
}

```

## WorldMapScreenView.cs

```csharp
using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class WorldMapScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerTitle;
        TMP_Text _worldSummaryText;
        TMP_Text _progressText;
        TMP_Text _statusText;
        Image _routeBackdrop;
        Image _mapImage;
        GameObject _mapPlaceholder;
        RectTransform _nodeContainer;
        readonly List<GameObject> _spawnedNodeObjects = new List<GameObject>();

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            _headerTitle.text = $"WORLD {world.worldId}";
            _worldSummaryText.text = $"{world.worldName}\nCURRENT {world.currentStageId:00} / TOTAL {world.stageCount:00}";
            _progressText.text = $"{UILocalization.Get("world.progress", "WORLD PROGRESS")}  {world.completedStages}/{world.stageCount}  |  BOSS  {world.bossStageId:00}";
            _statusText.text = $"Current route: stage {world.currentStageId:00} to boss {world.bossStageId:00}.";
            BindMapBackground(world);
            Canvas.ForceUpdateCanvases();

            List<StageNodeData> visibleNodes = GetVisibleNodes(BuildNodes(world), world);
            while (_spawnedNodeObjects.Count < visibleNodes.Count)
            {
                GameObject nodeGo = UIFactory.CreateUIObject("Node", _nodeContainer);
                _spawnedNodeObjects.Add(nodeGo);
            }

            for (int i = 0; i < _spawnedNodeObjects.Count; i++)
            {
                bool shouldBeActive = i < visibleNodes.Count;
                GameObject nodeGo = _spawnedNodeObjects[i];
                nodeGo.SetActive(shouldBeActive);
                if (!shouldBeActive)
                {
                    continue;
                }

                StageNodeData node = visibleNodes[i];
                nodeGo.name = $"Node_{node.stageId:00}";
                RectTransform rect = nodeGo.GetComponent<RectTransform>();
                float size = node.isCurrent ? 104f : node.isBoss ? 94f : node.isLocked ? 50f : 58f;
                UIFactory.SetAnchors(rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(size, size), node.anchoredPosition);
                WorldNodeView view = UIFactory.GetOrAdd<WorldNodeView>(nodeGo);
                view.Bind(node, () => HandleNodeClick(node));
                rect.SetAsLastSibling();
            }
        }

        public void DebugSelectWorld(int worldId)
        {
            _dataProvider.SelectWorldById(worldId);
            RefreshView();
        }

        void HandleNodeClick(StageNodeData node)
        {
            _screenManager.ActionRouter.HandleWorldNode(node);
            if (node.isLocked)
            {
                _statusText.text = UILocalization.Get("world.locked", "STAGE LOCKED");
            }
        }

        List<StageNodeData> BuildNodes(TopEndWar.UI.Data.WorldConfig world)
        {
            List<StageNodeData> nodes = new List<StageNodeData>();
            Rect rect = _nodeContainer != null ? _nodeContainer.rect : new Rect(0f, 0f, 960f, 1280f);
            float width = Mathf.Max(720f, rect.width);
            float height = Mathf.Max(860f, rect.height);
            int denominator = Mathf.Max(1, world.stageCount - 1);

            for (int i = 1; i <= world.stageCount; i++)
            {
                float progressT = (i - 1) / (float)denominator;
                Vector2 position = EvaluateLayout(world.layoutTemplateId, progressT, width, height);
                bool isCompleted = i <= world.completedStages;
                bool isCurrent = i == world.currentStageId;
                bool isBoss = i == world.bossStageId;
                bool isUnlocked = i <= world.currentStageId;

                nodes.Add(new StageNodeData
                {
                    stageId = i,
                    anchoredPosition = position,
                    isBoss = isBoss,
                    isCurrent = isCurrent,
                    isCompleted = isCompleted,
                    isLocked = !isUnlocked,
                    isUnlocked = isUnlocked
                });
            }

            return nodes;
        }

        List<StageNodeData> GetVisibleNodes(List<StageNodeData> allNodes, TopEndWar.UI.Data.WorldConfig world)
        {
            HashSet<int> keepIds = new HashSet<int>();
            keepIds.Add(1);
            keepIds.Add(world.bossStageId);

            int min = Mathf.Max(1, world.currentStageId - 4);
            int max = Mathf.Min(world.stageCount, world.currentStageId + 4);
            for (int i = min; i <= max; i++)
            {
                keepIds.Add(i);
            }

            List<StageNodeData> filtered = new List<StageNodeData>();
            foreach (StageNodeData node in allNodes)
            {
                if (keepIds.Contains(node.stageId))
                {
                    filtered.Add(node);
                }
            }

            return filtered;
        }

        Vector2 EvaluateLayout(string layoutTemplateId, float t, float width, float height)
        {
            if (layoutTemplateId == "arc")
            {
                return EvaluateWorldOnePath(t, width, height);
            }

            float x = Mathf.Lerp(width * 0.12f, width * 0.88f, t);
            float centerY = height * 0.48f;
            float amplitude = height * 0.18f;
            float y = centerY;

            if (layoutTemplateId == "zigzag")
            {
                y = centerY + Mathf.Sin(t * Mathf.PI * 4f) * amplitude;
            }
            else if (layoutTemplateId == "switchback")
            {
                y = Mathf.Lerp(height * 0.16f, height * 0.84f, Mathf.PingPong(t * 3f, 1f));
            }
            else
            {
                y = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
            }

            return new Vector2(x, y);
        }

        Vector2 EvaluateWorldOnePath(float t, float width, float height)
        {
            Vector2[] points =
            {
                new Vector2(0.50f, 0.92f),
                new Vector2(0.48f, 0.78f),
                new Vector2(0.55f, 0.63f),
                new Vector2(0.43f, 0.50f),
                new Vector2(0.55f, 0.38f),
                new Vector2(0.70f, 0.25f),
                new Vector2(0.73f, 0.12f)
            };

            float scaled = Mathf.Clamp01(t) * (points.Length - 1);
            int index = Mathf.Min(points.Length - 2, Mathf.FloorToInt(scaled));
            float localT = scaled - index;
            Vector2 normalized = Vector2.Lerp(points[index], points[index + 1], SmoothStep(localT));

            float horizontalInset = 56f;
            float topInset = 76f;
            float bottomInset = 92f;
            float usableWidth = Mathf.Max(1f, width - horizontalInset * 2f);
            float usableHeight = Mathf.Max(1f, height - topInset - bottomInset);
            float x = horizontalInset + normalized.x * usableWidth;
            float y = bottomInset + (1f - normalized.y) * usableHeight;
            return new Vector2(x, y);
        }

        float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        void Build()
        {
            if (_headerTitle != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);

            GameObject viewport = UIFactory.CreateUIObject("MapViewport", transform);
            _routeBackdrop = viewport.AddComponent<Image>();
            _routeBackdrop.color = UITheme.DeepNavy;
            UIFactory.Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject mapImageGo = UIFactory.CreateUIObject("MapImage", viewport.transform);
            _mapImage = mapImageGo.AddComponent<Image>();
            _mapImage.preserveAspect = true;
            _mapImage.color = Color.white;
            _mapImage.raycastTarget = false;
            RectTransform mapImageRect = mapImageGo.GetComponent<RectTransform>();
            mapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapImageRect.pivot = new Vector2(0.5f, 0.5f);
            mapImageRect.anchoredPosition = Vector2.zero;

            _mapPlaceholder = UIFactory.CreateUIObject("MapPlaceholder", viewport.transform);
            RectTransform placeholderRect = _mapPlaceholder.GetComponent<RectTransform>();
            UIFactory.Stretch(placeholderRect, Vector2.zero, Vector2.zero);
            Image placeholderImage = _mapPlaceholder.AddComponent<Image>();
            placeholderImage.color = new Color(UITheme.TealDark.r, UITheme.TealDark.g, UITheme.TealDark.b, 0.45f);
            TMP_Text placeholderText = UIFactory.CreateText("PlaceholderText", _mapPlaceholder.transform, "WORLD MAP ART\nPLACEHOLDER", 28, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(placeholderText.rectTransform, new Vector2(32f, 32f), new Vector2(-32f, -32f));
            placeholderText.enableAutoSizing = true;
            placeholderText.fontSizeMin = 18f;
            placeholderText.fontSizeMax = 28f;

            _nodeContainer = UIFactory.CreateUIObject("NodeContainer", viewport.transform).GetComponent<RectTransform>();
            UIFactory.Stretch(_nodeContainer, Vector2.zero, Vector2.zero);

            PanelBaseView header = UIFactory.CreateUIObject("WorldTitleOverlay", transform).AddComponent<PanelBaseView>();
            header.Build(14f, PanelVisualStyle.Dark);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(430f, 116f);
            headerRect.anchoredPosition = new Vector2(18f, -18f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(header.ContentRoot.gameObject, 2f, TextAnchor.UpperLeft, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerTitle = UIFactory.CreateText("HeaderTitle", header.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
            _worldSummaryText = UIFactory.CreateText("WorldSummary", header.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_headerTitle, 30f, true, 18f, 24f);
            UIFactory.ConfigureTextBlock(_worldSummaryText, 56f, true, 14f, 18f);

            GameObject utility = UIFactory.CreateUIObject("UtilityOverlay", transform);
            RectTransform utilityRect = utility.GetComponent<RectTransform>();
            utilityRect.anchorMin = new Vector2(1f, 1f);
            utilityRect.anchorMax = new Vector2(1f, 1f);
            utilityRect.pivot = new Vector2(1f, 1f);
            utilityRect.sizeDelta = new Vector2(360f, 68f);
            utilityRect.anchoredPosition = new Vector2(-18f, -18f);
            HorizontalLayoutGroup utilityLayout = UIFactory.AddHorizontalLayout(utility, 10f, TextAnchor.MiddleRight, true, false);
            utilityLayout.childForceExpandHeight = false;
            CreateUtilityButton(utility.transform, "world.utility.mail");
            CreateUtilityButton(utility.transform, "world.utility.missions");

            PanelBaseView progress = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            progress.Build(12f, PanelVisualStyle.Dark);
            RectTransform progressRect = progress.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0f, 0f);
            progressRect.anchorMax = new Vector2(1f, 0f);
            progressRect.pivot = new Vector2(0.5f, 0f);
            progressRect.offsetMin = new Vector2(18f, 18f);
            progressRect.offsetMax = new Vector2(-18f, 100f);
            VerticalLayoutGroup progressLayout = UIFactory.AddVerticalLayout(progress.ContentRoot.gameObject, 2f, TextAnchor.MiddleCenter, true, false);
            progressLayout.childForceExpandHeight = false;
            _progressText = UIFactory.CreateText("ProgressText", progress.ContentRoot, string.Empty, 20, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_progressText, 30f, true, 15f, 20f);
            _statusText = UIFactory.CreateText("StatusText", progress.ContentRoot, string.Empty, 16, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 26f, true, 13f, 16f);
        }

        void CreateUtilityButton(Transform parent, string key)
        {
            GameObject go = UIFactory.CreateUIObject(key, parent);
            UIFactory.AddLayoutElement(go, flexibleWidth: 1f, preferredHeight: 58f, minHeight: 54f);
            PrimaryButtonView button = go.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, key);
            button.SetOnClick(() =>
            {
                if (key == "world.utility.missions")
                {
                    _screenManager.ActionRouter.OpenDailyMissions();
                    _statusText.text = "Daily Missions coming soon";
                    return;
                }

                _statusText.text = $"{UILocalization.Get(key, key)} ready.";
            });
        }

        void BindMapBackground(TopEndWar.UI.Data.WorldConfig world)
        {
            Sprite sprite = LoadWorldMapSprite(world.worldId);
            bool hasSprite = sprite != null;
            _mapImage.sprite = sprite;
            _mapImage.enabled = hasSprite;
            if (hasSprite)
            {
                FitMapImageToViewport(sprite);
            }

            if (_mapPlaceholder != null)
            {
                _mapPlaceholder.SetActive(!hasSprite);
            }

            if (!hasSprite)
            {
                UIArtLibrary.WarnMissing($"World_{world.worldId:00}_Map_Viewport");
            }
        }

        Sprite LoadWorldMapSprite(int worldId)
        {
            if (!UIConstants.UseWorldMapSprite)
            {
                return null;
            }

            UIArtLibrary art = UIArtLibrary.Instance;
            if (art == null)
            {
                return null;
            }

            if (worldId == 1 && art.World01MapMaster != null)
            {
                return art.World01MapMaster;
            }

            return worldId == 1 ? art.World01MapViewport : null;
        }

        void FitMapImageToViewport(Sprite sprite)
        {
            if (_mapImage == null || sprite == null)
            {
                return;
            }

            RectTransform imageRect = _mapImage.rectTransform;
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null || sprite.rect.height <= 0f)
            {
                return;
            }

            float parentWidth = Mathf.Max(1f, parentRect.rect.width);
            float parentHeight = Mathf.Max(1f, parentRect.rect.height);
            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float parentAspect = parentWidth / parentHeight;
            float width = parentWidth;
            float height = parentHeight;

            if (spriteAspect > parentAspect)
            {
                height = parentHeight;
                width = height * spriteAspect;
            }
            else
            {
                width = parentWidth;
                height = width / spriteAspect;
            }

            imageRect.sizeDelta = new Vector2(width, height);
            imageRect.anchoredPosition = Vector2.zero;
        }
    }
}

```

### Klasör: Assets\_TopEndWar\UI\Localization

## LocalizationKeys.cs

```csharp
namespace TopEndWar.UI.Localization
{
    public static class LocalizationKeys
    {
        public const string HomeContinueCta = "home.continue.cta";
        public const string StageStartRun = "stage.start_run";
        public const string CommanderUpgrade = "commander.upgrade";
        public const string ResultNextStage = "result.next_stage";
        public const string ResultRetryStage = "result.retry_stage";
        public const string ResultWorldMap = "result.world_map";
    }
}

```

## UILocalization.cs

```csharp
using System.Collections.Generic;

namespace TopEndWar.UI.Localization
{
    public static class UILocalization
    {
        // LOCALIZATION: Minimal dictionary wrapper with fallback support.
        static readonly Dictionary<string, string> Entries = new Dictionary<string, string>
        {
            { "nav.home", "HOME" },
            { "nav.map", "MAP" },
            { "nav.commander", "COMMANDER" },
            { "nav.events", "EVENTS" },
            { "nav.shop", "SHOP" },
            { "topbar.energy", "ENERGY" },
            { "topbar.gold", "GOLD" },
            { "topbar.gems", "GEMS" },
            { "topbar.mail", "MAIL" },
            { "topbar.settings", "SETTINGS" },
            { "home.header.title", "HOME / HQ" },
            { "home.header.subtitle", "Frontline command, squad upkeep, and campaign control." },
            { "home.continue.cta", "CONTINUE CAMPAIGN" },
            { "home.quick.free_reward", "FREE REWARD" },
            { "home.quick.daily", "DAILY MISSIONS" },
            { "home.quick.upgrade", "UPGRADE" },
            { "home.quick.event", "EVENT" },
            { "home.claim.notice", "Claim notice available at HQ dispatch." },
            { "home.power.status", "POWER STATUS" },
            { "world.header.title", "WORLD MAP" },
            { "world.progress", "WORLD PROGRESS" },
            { "world.utility.mail", "MAIL" },
            { "world.utility.missions", "MISSIONS" },
            { "world.locked", "STAGE LOCKED" },
            { "world.preview.w1", "WORLD 1" },
            { "world.preview.w2", "WORLD 2" },
            { "world.preview.w5", "WORLD 5" },
            { "stage.header.title", "STAGE DETAIL" },
            { "stage.start_run", "START RUN" },
            { "stage.first_clear", "FIRST CLEAR BONUS" },
            { "stage.loadout", "ACTIVE LOADOUT" },
            { "stage.ready", "READY" },
            { "stage.risky", "RISKY" },
            { "stage.underpowered", "UNDERPOWERED" },
            { "stage.boss", "BOSS STAGE" },
            { "stage.section.enemies", "ENEMY PREVIEW" },
            { "stage.section.rewards", "REWARDS" },
            { "stage.section.threats", "THREAT TAGS" },
            { "commander.header.title", "COMMANDER / EQUIPMENT" },
            { "commander.loadout_tab", "LOADOUT" },
            { "commander.skills_tab", "SKILLS" },
            { "commander.stats_tab", "STATS" },
            { "commander.auto_equip", "AUTO EQUIP" },
            { "commander.upgrade", "UPGRADE" },
            { "result.victory", "STAGE CLEAR" },
            { "result.defeat", "DEFEAT" },
            { "result.next_stage", "NEXT STAGE" },
            { "result.retry_stage", "RETRY STAGE" },
            { "result.world_map", "WORLD MAP" },
            { "result.preview.victory", "PREVIEW VICTORY" },
            { "result.preview.defeat", "PREVIEW DEFEAT" },
            { "result.secondary.upgrade", "UPGRADE" },
            { "result.secondary.retry", "RETRY STAGE" },
            { "result.recommendation", "RECOMMENDATION" },
            { "equipment.weapon", "WEAPON" },
            { "equipment.armor", "ARMOR" },
            { "equipment.helmet", "HELMET" },
            { "equipment.boots", "BOOTS" },
            { "equipment.tech_core", "TECH CORE" },
            { "equipment.gear_box", "GEAR BOX" },
            { "equipment.drone", "DRONE" },
            { "equipment.support_gear", "SUPPORT GEAR" },
            { "equipment.emblem", "EMBLEM / CHIP" }
        };

        public static string Get(string key, string fallback = null)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                return value;
            }

            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }

        public static bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && Entries.ContainsKey(key);
        }
    }
}

```

### Klasör: Assets\_TopEndWar\UI\Data

## CommanderScreenData.cs

```csharp
using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class CommanderScreenData
    {
        public string commanderName;
        public int totalPower;
        public int hp;
        public int dps;
        public int defense;
        public string roleDescription;
        public List<EquipmentSlotData> slots = new List<EquipmentSlotData>();
        public List<string> squadMembers = new List<string>();
    }
}

```

## EquipmentSlotData.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class EquipmentSlotData
    {
        public string slotKey;
        public string itemName;
        public string state;
    }
}

```

## HomeScreenData.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class HomeScreenData
    {
        public string currentWorldName;
        public int currentStageId;
        public string currentStageName;
        public int completedStages;
        public int totalStages;
        public int playerPower;
        public int targetPower;
        public string powerState;
        public bool upgradeRecommended;
        public int playerLevel;
        public int energy;
        public int gold;
        public int premiumCurrency;
        public int mailCount;
        public int totalRuns;
    }
}

```

## MockUIDataProvider.cs

```csharp
using System.Collections.Generic;
using TopEndWar.UI.Localization;
using UnityEngine;

namespace TopEndWar.UI.Data
{
    public class MockUIDataProvider : MonoBehaviour
    {
        // DATA-BINDING: UI reads current state from this provider so screens stay decoupled from gameplay scene code.
        readonly List<WorldConfig> _worlds = new List<WorldConfig>();
        readonly PlayerProgress _playerProgress = new PlayerProgress();

        int _selectedWorldIndex;
        int _selectedStageId;

        public bool ResultPreviewVictory { get; private set; } = true;
        public int SelectedStageId => _selectedStageId;
        public PlayerProgress CurrentProgress => _playerProgress;

        void Awake()
        {
            EnsureSeeded();
        }

        public HomeScreenData GetHomeScreenData()
        {
            EnsureSeeded();
            SaveManager save = SaveManager.Instance;
            WorldConfig world = GetCurrentWorld();

            return new HomeScreenData
            {
                currentWorldName = world.worldName,
                currentStageId = _playerProgress.currentStageId,
                currentStageName = GetStageName(_playerProgress.currentStageId),
                completedStages = world.completedStages,
                totalStages = world.stageCount,
                playerPower = 120,
                targetPower = 100,
                powerState = "stage.ready",
                upgradeRecommended = false,
                playerLevel = 1,
                energy = 50,
                gold = save != null ? Mathf.Max(0, save.HighScoreCP) : 0,
                premiumCurrency = 0,
                mailCount = 0,
                totalRuns = save != null ? save.TotalRuns : 0
            };
        }

        public TopBarData GetTopBarData()
        {
            EnsureSeeded();
            HomeScreenData home = GetHomeScreenData();
            return new TopBarData
            {
                commanderName = "Cmdr. Voss",
                playerLevel = home.playerLevel,
                energy = home.energy,
                maxEnergy = 50,
                gold = home.gold,
                premiumCurrency = home.premiumCurrency,
                mailCount = home.mailCount,
                showPremiumCurrency = true
            };
        }

        public IReadOnlyList<WorldConfig> GetWorlds()
        {
            EnsureSeeded();
            return _worlds;
        }

        public WorldConfig GetCurrentWorld()
        {
            EnsureSeeded();
            return _worlds[Mathf.Clamp(_selectedWorldIndex, 0, _worlds.Count - 1)];
        }

        public void SelectWorldById(int worldId)
        {
            EnsureSeeded();
            for (int i = 0; i < _worlds.Count; i++)
            {
                if (_worlds[i].worldId == worldId)
                {
                    _selectedWorldIndex = i;
                    _playerProgress.currentWorldId = _worlds[i].worldId;
                    _playerProgress.currentStageId = _worlds[i].currentStageId;
                    _playerProgress.completedStages = _worlds[i].completedStages;
                    _selectedStageId = _playerProgress.currentStageId;
                    return;
                }
            }
        }

        public void SelectStage(int stageId)
        {
            EnsureSeeded();
            _selectedStageId = Mathf.Clamp(stageId, 1, GetCurrentWorld().stageCount);
        }

        public StageDetailData GetStageDetailData()
        {
            EnsureSeeded();
            WorldConfig world = GetCurrentWorld();
            int stageId = Mathf.Clamp(_selectedStageId, 1, world.stageCount);
            bool isBossStage = stageId == world.bossStageId;
            int targetPower = stageId == 1 ? 100 : 27000 + (stageId * 450);
            int playerPower = 120;

            return new StageDetailData
            {
                worldId = world.worldId,
                stageId = stageId,
                stageName = GetStageName(stageId),
                playerPower = playerPower,
                targetPower = targetPower,
                powerStateKey = playerPower >= targetPower ? "stage.ready" : stageId < world.currentStageId ? "stage.risky" : "stage.underpowered",
                isBossStage = isBossStage,
                hasFirstClearBonus = stageId >= world.currentStageId,
                entryCost = stageId == 1 ? 5 : 10,
                loadoutName = "Assault Grid / Mk-II",
                briefingText = isBossStage ? "Fortified enemy command node. Expect armor and artillery pressure." : "Advance through the canyon approach and secure the outpost lane.",
                threatKeys = new List<string> { "Armored", "Swarm", isBossStage ? "Boss" : "Mid Range" },
                enemyNames = isBossStage ? new List<string> { "Shield Trooper", "Rocket Crew", "Boss Walker" } : new List<string> { "Rifle Squad", "Turret Nest", "Scout Bike" },
                rewards = new List<RewardItemData>
                {
                    new RewardItemData { labelKey = "topbar.gold", fallbackLabel = "Gold", amount = stageId == 1 ? 100 : 1250 + (stageId * 20), accent = "gold" },
                    new RewardItemData { labelKey = "topbar.xp", fallbackLabel = "XP", amount = stageId == 1 ? 25 : 100 + (stageId * 5), accent = "yellow" }
                },
                firstClearRewards = new List<RewardItemData>
                {
                    new RewardItemData { labelKey = "topbar.gold", fallbackLabel = "Gold", amount = stageId == 1 ? 50 : 200, accent = "gold" }
                }
            };
        }

        public CommanderScreenData GetCommanderScreenData()
        {
            EnsureSeeded();
            return new CommanderScreenData
            {
                commanderName = "Cmdr. Voss",
                totalPower = 120,
                hp = 500,
                dps = 35,
                defense = 20,
                roleDescription = "Assault leader tuned for mid-range pressure and squad sustain.",
                slots = new List<EquipmentSlotData>
                {
                    new EquipmentSlotData { slotKey = "equipment.weapon", itemName = "VX Assault Rifle", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.armor", itemName = "Frontier Plate", state = "upgradeable" },
                    new EquipmentSlotData { slotKey = "equipment.helmet", itemName = "Recon Helm", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.boots", itemName = "Rapid Boots", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.tech_core", itemName = "Mk-IV Core", state = "upgradeable" },
                    new EquipmentSlotData { slotKey = "equipment.gear_box", itemName = "Field Relay", state = "empty" },
                    new EquipmentSlotData { slotKey = "equipment.drone", itemName = "Unlock at HQ Lv.20", state = "locked" },
                    new EquipmentSlotData { slotKey = "equipment.support_gear", itemName = "Unlock at HQ Lv.24", state = "locked" },
                    new EquipmentSlotData { slotKey = "equipment.emblem", itemName = "Unlock at HQ Lv.28", state = "locked" }
                },
                squadMembers = new List<string> { "Alpha Squad", "Bulwark Team", "Medic Pair", "Drone Crew" }
            };
        }

        public ResultScreenData GetResultScreenData()
        {
            EnsureSeeded();
            StageDetailData stage = GetStageDetailData();
            return ResultPreviewVictory
                ? new ResultScreenData
                {
                    isVictory = true,
                    stageName = stage.stageName,
                    stars = 3,
                    recommendation = "Push the next stage or upgrade armor for a safer clear.",
                    hasFirstClearBonus = stage.hasFirstClearBonus,
                    performanceGoals = new List<string> { "No retreat used", "Commander HP above 50%", "Elite wave cleared fast" },
                    rewards = new List<RewardItemData>(stage.rewards),
                    firstClearRewards = new List<RewardItemData>(stage.firstClearRewards)
                }
                : new ResultScreenData
                {
                    isVictory = false,
                    stageName = stage.stageName,
                    stars = 0,
                    failureReason = "Armor line collapsed during the rocket volley.",
                    recommendation = "Upgrade armor and tech core before retrying this lane.",
                    hasFirstClearBonus = false,
                    rewards = new List<RewardItemData>
                    {
                        new RewardItemData { labelKey = "topbar.gold", fallbackLabel = "Gold", amount = 420, accent = "gold" }
                    }
                };
        }

        public void SetResultPreview(bool victory)
        {
            ResultPreviewVictory = victory;
        }

        public string GetStageName(int stageId)
        {
            EnsureSeeded();
            if (stageId == 1)
            {
                return "Coastal Landing";
            }

            if (stageId == GetCurrentWorld().bossStageId)
            {
                return "Citadel Breach";
            }

            return $"Stage {stageId:00}";
        }

        void SeedData()
        {
            _worlds.Clear();
            _worlds.Add(new WorldConfig
            {
                worldId = 1,
                worldName = "Frontier Conflict",
                stageCount = 35,
                completedStages = 0,
                currentStageId = 1,
                bossStageId = 35,
                layoutTemplateId = "arc"
            });
            _worlds.Add(new WorldConfig
            {
                worldId = 2,
                worldName = "Iron Tundra",
                stageCount = 24,
                completedStages = 9,
                currentStageId = 10,
                bossStageId = 24,
                layoutTemplateId = "zigzag"
            });
            _worlds.Add(new WorldConfig
            {
                worldId = 5,
                worldName = "Ashen Ring",
                stageCount = 45,
                completedStages = 18,
                currentStageId = 19,
                bossStageId = 45,
                layoutTemplateId = "switchback"
            });

            SelectWorldById(1);
        }

        void EnsureSeeded()
        {
            if (_worlds.Count == 0)
            {
                SeedData();
            }
        }
    }
}

```

## PlayerProgress.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public int currentWorldId;
        public int currentStageId;
        public int completedStages;
    }
}

```

## ResultScreenData.cs

```csharp
using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class ResultScreenData
    {
        public bool isVictory;
        public string stageName;
        public int stars;
        public string failureReason;
        public string recommendation;
        public bool hasFirstClearBonus;
        public List<string> performanceGoals = new List<string>();
        public List<RewardItemData> rewards = new List<RewardItemData>();
        public List<RewardItemData> firstClearRewards = new List<RewardItemData>();
    }
}

```

## RewardItemData.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class RewardItemData
    {
        public string labelKey;
        public string fallbackLabel;
        public int amount;
        public string accent;
    }
}

```

## StageDetailData.cs

```csharp
using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class StageDetailData
    {
        public int worldId;
        public int stageId;
        public string stageName;
        public int playerPower;
        public int targetPower;
        public string powerStateKey;
        public bool isBossStage;
        public bool hasFirstClearBonus;
        public int entryCost = 10;
        public string loadoutName;
        public string briefingText;
        public List<string> threatKeys = new List<string>();
        public List<string> enemyNames = new List<string>();
        public List<RewardItemData> rewards = new List<RewardItemData>();
        public List<RewardItemData> firstClearRewards = new List<RewardItemData>();
    }
}

```

## StageNodeData.cs

```csharp
using System;
using UnityEngine;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class StageNodeData
    {
        public int stageId;
        public Vector2 anchoredPosition;
        public bool isBoss;
        public bool isCurrent;
        public bool isCompleted;
        public bool isLocked;
        public bool isUnlocked;
    }
}

```

## TopBarData.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class TopBarData
    {
        public string commanderName;
        public int playerLevel;
        public int energy;
        public int maxEnergy;
        public int gold;
        public int premiumCurrency;
        public int mailCount;
        public bool showPremiumCurrency;
    }
}

```

## WorldConfig.cs

```csharp
using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class WorldConfig
    {
        public int worldId;
        public string worldName;
        public int stageCount;
        public int completedStages;
        public int currentStageId;
        public int bossStageId;
        public string layoutTemplateId;
    }
}

```

### Klasör: Assets\_TopEndWar\UI\Core

## UIActionRouter.cs

```csharp
using TopEndWar.UI.Data;
using UnityEngine;

namespace TopEndWar.UI.Core
{
    public class UIActionRouter
    {
        readonly UIScreenManager _screenManager;
        readonly MockUIDataProvider _dataProvider;

        public UIActionRouter(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
        }

        public void ShowHome()
        {
            _screenManager.ShowHome();
        }

        public void ShowWorldMap()
        {
            _screenManager.ShowWorldMap();
        }

        public void ShowStageDetail(int stageId)
        {
            _screenManager.ShowStageDetail(stageId);
        }

        public void ShowCommander()
        {
            _screenManager.ShowCommander();
        }

        public void ShowResultVictory()
        {
            _screenManager.ShowResultVictory();
        }

        public void ShowResultDefeat()
        {
            _screenManager.ShowResultDefeat();
        }

        public void ShowComingSoon(string featureName)
        {
            _screenManager.ShowComingSoon(featureName);
        }

        public void GoBack()
        {
            _screenManager.GoBack();
        }

        public void ContinueCampaign()
        {
            // DATA-BINDING: Continue always resolves through current player progress instead of a hardcoded stage.
            _screenManager.ShowStageDetail(_dataProvider.CurrentProgress.currentStageId);
        }

        public void ClaimFreeReward()
        {
            Debug.Log("[UI] Free Reward clicked.");
            _screenManager.ShowToast("Reward claimed");
        }

        public void OpenDailyMissions()
        {
            ShowComingSoon("Daily Missions");
        }

        public void OpenEvents()
        {
            ShowComingSoon("Events");
        }

        public void OpenShop()
        {
            ShowComingSoon("Shop");
        }

        public void HandleBottomNav(string screenId)
        {
            switch (screenId)
            {
                case UIConstants.HomeScreenId:
                    ShowHome();
                    break;
                case UIConstants.WorldMapScreenId:
                    ShowWorldMap();
                    break;
                case UIConstants.CommanderScreenId:
                    ShowCommander();
                    break;
                case "events_placeholder":
                    OpenEvents();
                    break;
                case "shop_placeholder":
                    OpenShop();
                    break;
                default:
                    _screenManager.ShowToast($"{screenId} unavailable");
                    break;
            }
        }

        public void HandleWorldNode(StageNodeData node)
        {
            if (node.isLocked)
            {
                _screenManager.ShowToast("Stage locked");
                return;
            }

            _dataProvider.SelectStage(node.stageId);
            _screenManager.ShowStageDetail(node.stageId);
        }

        public void OpenChangeLoadout()
        {
            ShowCommander();
        }

        public void ApplyCommanderUpgrade()
        {
            Debug.Log("[UI] Commander upgrade requested.");
        }

        public void ApplyAutoEquip()
        {
            Debug.Log("[UI] Auto Equip requested.");
        }

        public void NextStageFromResult()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            int nextStageId = _dataProvider.SelectedStageId + 1;
            if (nextStageId > world.stageCount)
            {
                ShowWorldMap();
                return;
            }

            _dataProvider.SelectStage(nextStageId);
            _screenManager.ShowStageDetail(nextStageId);
        }

        public void RetryCurrentStage()
        {
            _screenManager.ShowStageDetail(_dataProvider.SelectedStageId);
        }
    }
}

```

## UIConstants.cs

```csharp
namespace TopEndWar.UI.Core
{
    public static class UIConstants
    {
        public const string HomeScreenId = "home";
        public const string WorldMapScreenId = "world_map";
        public const string StageDetailScreenId = "stage_detail";
        public const string CommanderScreenId = "commander";
        public const string ResultScreenId = "result";

        public const string MainMenuSceneName = "MainMenu";
        public const string SampleSceneName = "SampleScene";

        public const float DefaultPadding = 24f;
        public const float SectionSpacing = 18f;
        public const float CardCornerSpritePixelsPerUnit = 100f;
        public const bool ShowDebugButtons = false;

        public static readonly bool UseWorldMapSprite = true;
        public static readonly bool UseIconSprites = true;
        public static readonly bool UseNodeSprites = true;
        public static readonly bool UseCommanderSprites = true;

        public static readonly bool UsePanelSprites = true;
        public static readonly bool UseButtonSprites = true;
        public static readonly bool UseBottomNavSprites = true;
        public static readonly bool UseRewardFrameSprites = false;
    }
}

```

## UIFactory.cs

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Core
{
    public static class UIFactory
    {
        public static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(RectTransform rectTransform, Vector2 paddingMin, Vector2 paddingMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = paddingMin;
            rectTransform.offsetMax = paddingMax;
            return rectTransform;
        }

        public static RectTransform SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            return rectTransform;
        }

        public static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            GameObject go = CreateUIObject(name, parent);
            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            // READABILITY: Use truncate because the current TMP font asset lacks the ellipsis glyph.
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing, TextAnchor anchor = TextAnchor.MiddleLeft, bool expandWidth = true, bool expandHeight = false)
        {
            HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(go);
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return layout;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing, TextAnchor anchor = TextAnchor.UpperLeft, bool expandWidth = true, bool expandHeight = false)
        {
            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return layout;
        }

        public static ContentSizeFitter AddContentSizeFitter(GameObject go, ContentSizeFitter.FitMode horizontal, ContentSizeFitter.FitMode vertical)
        {
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
            fitter.horizontalFit = horizontal;
            fitter.verticalFit = vertical;
            return fitter;
        }

        public static LayoutElement AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f, float minWidth = -1f, float minHeight = -1f)
        {
            LayoutElement element = GetOrAdd<LayoutElement>(go);
            if (preferredWidth >= 0f)
            {
                element.preferredWidth = preferredWidth;
            }

            if (preferredHeight >= 0f)
            {
                element.preferredHeight = preferredHeight;
            }

            if (flexibleWidth >= 0f)
            {
                element.flexibleWidth = flexibleWidth;
            }

            if (flexibleHeight >= 0f)
            {
                element.flexibleHeight = flexibleHeight;
            }

            if (minWidth >= 0f)
            {
                element.minWidth = minWidth;
            }

            if (minHeight >= 0f)
            {
                element.minHeight = minHeight;
            }

            return element;
        }

        public static void ConfigureTextBlock(TMP_Text text, float preferredHeight, bool autoSize = false, float minFontSize = 16f, float maxFontSize = 32f)
        {
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            // READABILITY: Use truncate because the current TMP font asset lacks the ellipsis glyph.
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = autoSize;
            if (autoSize)
            {
                text.fontSizeMin = minFontSize;
                text.fontSizeMax = maxFontSize;
            }

            AddLayoutElement(text.gameObject, preferredHeight: preferredHeight, minHeight: preferredHeight * 0.75f);
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}

```

## UIScreenManager.cs

```csharp
using TopEndWar.UI.Components;
using TopEndWar.UI.Data;
using TopEndWar.UI.Screens;
using TopEndWar.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Core
{
    public class UIScreenManager : MonoBehaviour
    {
        MockUIDataProvider _dataProvider;
        TopBarView _topBarView;
        BottomNavView _bottomNavView;
        HomeScreenView _homeScreen;
        WorldMapScreenView _worldMapScreen;
        StageDetailScreenView _stageDetailScreen;
        CommanderScreenView _commanderScreen;
        ResultScreenView _resultScreen;
        UIActionRouter _actionRouter;
        GameObject _toastRoot;
        TMP_Text _toastText;
        Coroutine _toastCoroutine;

        bool _isBootstrapped;

        public UIActionRouter ActionRouter => _actionRouter;

        public void Bootstrap()
        {
            if (_isBootstrapped)
            {
                RefreshSharedChrome();
                ShowHome();
                return;
            }

            // DEGISIKLIK: MainMenu now boots into a reusable screen manager instead of a one-off menu builder.
            _dataProvider = UIFactory.GetOrAdd<MockUIDataProvider>(gameObject);
            _actionRouter = new UIActionRouter(this, _dataProvider);
            RectTransform root = GetComponent<RectTransform>();
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            Vector2 safePadding = new Vector2(24f, 24f);

            GameObject background = UIFactory.CreateUIObject("Backdrop", transform);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = UITheme.DeepNavy;
            UIFactory.Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject topBarGo = UIFactory.CreateUIObject("TopBar", transform);
            RectTransform topBarRect = topBarGo.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(0f, 120f);
            topBarRect.anchoredPosition = new Vector2(0f, -safePadding.y);
            topBarRect.offsetMin = new Vector2(safePadding.x, -144f);
            topBarRect.offsetMax = new Vector2(-safePadding.x, -safePadding.y);
            _topBarView = topBarGo.AddComponent<TopBarView>();
            _topBarView.Build();

            GameObject bottomNavGo = UIFactory.CreateUIObject("BottomNav", transform);
            RectTransform bottomNavRect = bottomNavGo.GetComponent<RectTransform>();
            bottomNavRect.anchorMin = new Vector2(0f, 0f);
            bottomNavRect.anchorMax = new Vector2(1f, 0f);
            bottomNavRect.pivot = new Vector2(0.5f, 0f);
            bottomNavRect.sizeDelta = new Vector2(0f, 140f);
            bottomNavRect.anchoredPosition = new Vector2(0f, safePadding.y);
            bottomNavRect.offsetMin = new Vector2(safePadding.x, safePadding.y);
            bottomNavRect.offsetMax = new Vector2(-safePadding.x, 164f);
            _bottomNavView = bottomNavGo.AddComponent<BottomNavView>();
            _bottomNavView.Build(_actionRouter.HandleBottomNav);

            GameObject contentRoot = UIFactory.CreateUIObject("ContentRoot", transform);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            UIFactory.Stretch(contentRect, new Vector2(safePadding.x, 188f), new Vector2(-safePadding.x, -164f));

            _homeScreen = CreateScreen<HomeScreenView>("HomeScreen", contentRoot.transform);
            _worldMapScreen = CreateScreen<WorldMapScreenView>("WorldMapScreen", contentRoot.transform);
            _stageDetailScreen = CreateScreen<StageDetailScreenView>("StageDetailScreen", contentRoot.transform);
            _commanderScreen = CreateScreen<CommanderScreenView>("CommanderScreen", contentRoot.transform);
            _resultScreen = CreateScreen<ResultScreenView>("ResultScreen", contentRoot.transform);

            _homeScreen.Initialize(this, _dataProvider);
            _worldMapScreen.Initialize(this, _dataProvider);
            _stageDetailScreen.Initialize(this, _dataProvider);
            _commanderScreen.Initialize(this, _dataProvider);
            _resultScreen.Initialize(this, _dataProvider);
            BuildToastOverlay();

            _isBootstrapped = true;
            ShowHome();
        }

        public void ShowHome()
        {
            RefreshSharedChrome();
            _homeScreen.RefreshView();
            SetVisibleScreen(UIConstants.HomeScreenId, true, true);
        }

        public void ShowWorldMap()
        {
            RefreshSharedChrome();
            _worldMapScreen.RefreshView();
            SetVisibleScreen(UIConstants.WorldMapScreenId, true, true);
        }

        public void ShowStageDetail(int stageId)
        {
            _dataProvider.SelectStage(stageId);
            _stageDetailScreen.RefreshView();
            SetVisibleScreen(UIConstants.StageDetailScreenId, false, false);
        }

        public void ShowCommander()
        {
            RefreshSharedChrome();
            _commanderScreen.RefreshView();
            SetVisibleScreen(UIConstants.CommanderScreenId, true, true);
        }

        public void ShowResult(bool victoryPreview)
        {
            _dataProvider.SetResultPreview(victoryPreview);
            _resultScreen.RefreshView();
            SetVisibleScreen(UIConstants.ResultScreenId, false, false);
        }

        public void ShowResultVictory()
        {
            ShowResult(true);
        }

        public void ShowResultDefeat()
        {
            ShowResult(false);
        }

        public void AdvanceToNextStage()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            int nextStage = _dataProvider.SelectedStageId + 1;
            if (nextStage > world.stageCount)
            {
                ShowWorldMap();
                return;
            }

            _dataProvider.SelectStage(nextStage);
            ShowStageDetail(nextStage);
        }

        public void ShowComingSoon(string featureName)
        {
            ShowToast($"{featureName} coming soon");
        }

        public void GoBack()
        {
            // DEGISIKLIK: First-pass navigation contract keeps Stage Detail back behavior predictable.
            ShowWorldMap();
        }

        public void ShowToast(string message)
        {
            if (_toastRoot == null || _toastText == null)
            {
                Debug.Log($"[UI] {message}");
                return;
            }

            _toastText.text = message;
            _toastRoot.SetActive(true);
            _toastRoot.transform.SetAsLastSibling();

            if (_toastCoroutine != null)
            {
                StopCoroutine(_toastCoroutine);
            }

            _toastCoroutine = StartCoroutine(HideToastAfterDelay(1.8f));
        }

        void RefreshSharedChrome()
        {
            if (_topBarView != null)
            {
                _topBarView.Bind(_dataProvider.GetTopBarData());
            }
        }

        void SetVisibleScreen(string screenId, bool showTopBar, bool showBottomNav)
        {
            Component activeScreen = null;
            SetPrimaryScreenState(_homeScreen, screenId == UIConstants.HomeScreenId, ref activeScreen);
            SetPrimaryScreenState(_worldMapScreen, screenId == UIConstants.WorldMapScreenId, ref activeScreen);
            SetPrimaryScreenState(_stageDetailScreen, screenId == UIConstants.StageDetailScreenId, ref activeScreen);
            SetPrimaryScreenState(_commanderScreen, screenId == UIConstants.CommanderScreenId, ref activeScreen);
            SetPrimaryScreenState(_resultScreen, screenId == UIConstants.ResultScreenId, ref activeScreen);

            if (activeScreen != null)
            {
                activeScreen.transform.SetAsLastSibling();
            }

            SetChromeState(_topBarView, showTopBar);
            SetChromeState(_bottomNavView, showBottomNav);
            _bottomNavView.SetActiveScreen(screenId);
        }

        T CreateScreen<T>(string name, Transform parent) where T : Component
        {
            GameObject go = UIFactory.CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            UIFactory.Stretch(rect, Vector2.zero, Vector2.zero);
            CanvasGroup canvasGroup = UIFactory.GetOrAdd<CanvasGroup>(go);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return go.AddComponent<T>();
        }

        void SetPrimaryScreenState(Component screen, bool active, ref Component activeScreen)
        {
            if (screen == null)
            {
                return;
            }

            CanvasGroup canvasGroup = UIFactory.GetOrAdd<CanvasGroup>(screen.gameObject);
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
            screen.gameObject.SetActive(active);

            if (active)
            {
                activeScreen = screen;
            }
        }

        void SetChromeState(Component chrome, bool active)
        {
            if (chrome == null)
            {
                return;
            }

            CanvasGroup canvasGroup = UIFactory.GetOrAdd<CanvasGroup>(chrome.gameObject);
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
            chrome.gameObject.SetActive(active);
        }

        void BuildToastOverlay()
        {
            if (_toastRoot != null)
            {
                return;
            }

            PanelBaseView toastPanel = UIFactory.CreateUIObject("ToastOverlay", transform).AddComponent<PanelBaseView>();
            toastPanel.Build(18f);
            _toastRoot = toastPanel.gameObject;
            RectTransform toastRect = _toastRoot.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = new Vector2(520f, 84f);
            toastRect.anchoredPosition = new Vector2(0f, -150f);

            _toastText = UIFactory.CreateText("ToastText", toastPanel.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_toastText.rectTransform, Vector2.zero, Vector2.zero);
            _toastText.enableAutoSizing = true;
            _toastText.fontSizeMin = 18f;
            _toastText.fontSizeMax = 24f;
            _toastRoot.SetActive(false);
        }

        System.Collections.IEnumerator HideToastAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_toastRoot != null)
            {
                _toastRoot.SetActive(false);
            }
            _toastCoroutine = null;
        }
    }
}

```

### Klasör: Assets\_TopEndWar\UI\Components

## BottomNavView.cs

```csharp
using System;
using System.Collections.Generic;
using TopEndWar.UI.Core;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class BottomNavView : MonoBehaviour
    {
        readonly Dictionary<string, PrimaryButtonView> _buttons = new Dictionary<string, PrimaryButtonView>();
        bool _isBuilt;

        public void Build(Action<string> onNavigate)
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(16f);
            HorizontalLayoutGroup row = UIFactory.AddHorizontalLayout(panel.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, true);
            row.padding = new RectOffset(0, 0, 0, 0);

            CreateNavButton(panel.ContentRoot, UIConstants.HomeScreenId, "nav.home", onNavigate);
            CreateNavButton(panel.ContentRoot, UIConstants.WorldMapScreenId, "nav.map", onNavigate);
            CreateNavButton(panel.ContentRoot, UIConstants.CommanderScreenId, "nav.commander", onNavigate);
            CreateNavButton(panel.ContentRoot, "events_placeholder", "nav.events", onNavigate);
            CreateNavButton(panel.ContentRoot, "shop_placeholder", "nav.shop", onNavigate);
            _isBuilt = true;
        }

        public void SetActiveScreen(string screenId)
        {
            foreach (KeyValuePair<string, PrimaryButtonView> entry in _buttons)
            {
                entry.Value.SetSelected(entry.Key == screenId);
            }
        }

        void CreateNavButton(Transform parent, string screenId, string key, Action<string> onNavigate)
        {
            if (_buttons.ContainsKey(screenId))
            {
                return;
            }

            GameObject go = UIFactory.CreateUIObject($"{screenId}_Button", parent);
            UIFactory.AddLayoutElement(go, preferredHeight: 72f, flexibleWidth: 1f, minHeight: 64f);
            PrimaryButtonView button = go.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, UILocalization.Get(key, key));
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseIconSprites && art != null)
            {
                button.SetIcon(art.GetNavIcon(screenId), ResolveIconAssetName(screenId));
            }

            button.SetOnClick(() => onNavigate?.Invoke(screenId));
            _buttons.Add(screenId, button);
        }

        string ResolveIconAssetName(string screenId)
        {
            switch (screenId)
            {
                case UIConstants.HomeScreenId:
                    return "Icon_Home";
                case UIConstants.WorldMapScreenId:
                    return "Icon_Map";
                case UIConstants.CommanderScreenId:
                    return "Icon_Commander";
                case "events_placeholder":
                    return "Icon_Events";
                case "shop_placeholder":
                    return "Icon_Shop";
                default:
                    return "BottomNavIcon";
            }
        }
    }
}

```

## EquipmentSlotView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;

namespace TopEndWar.UI.Components
{
    public class EquipmentSlotView : MonoBehaviour
    {
        TMP_Text _slotName;
        TMP_Text _itemName;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(14f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(gameObject, preferredHeight: 100f, minHeight: 92f);
            UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 4f, TextAnchor.MiddleLeft, true, false);

            if (_slotName == null)
            {
                _slotName = UIFactory.CreateText("SlotName", panel.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Bold);
                _itemName = UIFactory.CreateText("ItemName", panel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
                UIFactory.ConfigureTextBlock(_slotName, 28f, true, 14f, 18f);
                UIFactory.ConfigureTextBlock(_itemName, 42f, true, 16f, 22f);
            }

            _isBuilt = true;
        }

        public void Bind(EquipmentSlotData data)
        {
            Build();
            _slotName.text = UILocalization.Get(data.slotKey, data.slotKey);
            _itemName.text = data.itemName;

            switch (data.state)
            {
                case "upgradeable":
                    _itemName.color = UITheme.Teal;
                    break;
                case "locked":
                    _itemName.color = UITheme.Danger;
                    break;
                case "empty":
                    _itemName.color = UITheme.Amber;
                    break;
                default:
                    _itemName.color = UITheme.SoftCream;
                    break;
            }
        }
    }
}

```

## NotificationBadgeView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class NotificationBadgeView : MonoBehaviour
    {
        TMP_Text _countText;

        public void Build()
        {
            Image image = UIFactory.GetOrAdd<Image>(gameObject);
            image.color = UITheme.Danger;
            LayoutElement layout = UIFactory.GetOrAdd<LayoutElement>(gameObject);
            layout.preferredWidth = 28f;
            layout.preferredHeight = 28f;

            if (_countText == null)
            {
                _countText = UIFactory.CreateText("Count", transform, "0", 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                RectTransform rect = _countText.rectTransform;
                UIFactory.Stretch(rect, Vector2.zero, Vector2.zero);
            }
        }

        public void SetCount(int count)
        {
            Build();
            gameObject.SetActive(count > 0);
            _countText.text = count.ToString();
        }
    }
}

```

## PanelBaseView.cs

```csharp
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public enum PanelVisualStyle
    {
        Auto,
        Dark,
        Cream,
        Hero,
        PlainDark
    }

    public class PanelBaseView : MonoBehaviour
    {
        public RectTransform ContentRoot { get; private set; }

        bool _isBuilt;

        public void Build(float padding = 18f, PanelVisualStyle style = PanelVisualStyle.Auto)
        {
            if (_isBuilt)
            {
                return;
            }

            // UI: Shared panel shell for the warm heroic diorama look.
            Image background = UIFactory.GetOrAdd<Image>(gameObject);
            ApplyPanelArt(background, style);

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = UITheme.MutedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow shadow = UIFactory.GetOrAdd<Shadow>(gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -4f);

            GameObject content = UIFactory.CreateUIObject("Content", transform);
            ContentRoot = content.GetComponent<RectTransform>();
            UIFactory.Stretch(ContentRoot, new Vector2(padding, padding), new Vector2(-padding, -padding));

            _isBuilt = true;
        }

        void ApplyPanelArt(Image background, PanelVisualStyle style)
        {
            if (!UIConstants.UsePanelSprites)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = ResolveFallbackColor(style);
                return;
            }

            PanelVisualStyle resolved = style == PanelVisualStyle.Auto ? ResolveStyleFromName() : style;
            if (resolved == PanelVisualStyle.PlainDark)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = ResolveFallbackColor(resolved);
                return;
            }

            Sprite sprite = null;
            string assetName = "UI_Panel_Dark_01";
            Color fallback = UITheme.NavyPanel;
            UIArtLibrary art = UIArtLibrary.Instance;

            if (art != null)
            {
                switch (resolved)
                {
                    case PanelVisualStyle.Cream:
                        sprite = art.PanelCream;
                        assetName = "UI_Panel_Cream_01";
                        fallback = UITheme.WarmCream;
                        break;
                    case PanelVisualStyle.Hero:
                        sprite = art.PanelHero;
                        assetName = "UI_Panel_Hero_01";
                        fallback = UITheme.NavyPanel;
                        break;
                    default:
                        sprite = art.PanelDark;
                        break;
                }
            }

            UIArtLibrary.TryApply(background, sprite, fallback, assetName);
        }

        Color ResolveFallbackColor(PanelVisualStyle style)
        {
            PanelVisualStyle resolved = style == PanelVisualStyle.Auto ? ResolveStyleFromName() : style;
            switch (resolved)
            {
                case PanelVisualStyle.Hero:
                    return UITheme.NavyPanel;
                case PanelVisualStyle.Cream:
                case PanelVisualStyle.PlainDark:
                    return UITheme.Gunmetal;
                default:
                    return UITheme.NavyPanel;
            }
        }

        PanelVisualStyle ResolveStyleFromName()
        {
            string objectName = gameObject.name.ToLowerInvariant();
            if (objectName.Contains("toparea") || objectName.Contains("campaign") || objectName.Contains("preview") || objectName.Contains("visual"))
            {
                return PanelVisualStyle.Hero;
            }

            if (objectName.Contains("slot") || objectName.Contains("enemycard") || objectName.Contains("toast"))
            {
                return PanelVisualStyle.PlainDark;
            }

            return PanelVisualStyle.Dark;
        }
    }
}

```

## PrimaryButtonView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public enum ButtonVisualStyle
    {
        Primary,
        Secondary,
        Danger,
        Tab
    }

    public class PrimaryButtonView : MonoBehaviour
    {
        Button _button;
        Image _background;
        Image _icon;
        TMP_Text _label;
        ButtonVisualStyle _currentStyle;

        public void Build(ButtonVisualStyle style = ButtonVisualStyle.Primary)
        {
            _currentStyle = style;
            _background = UIFactory.GetOrAdd<Image>(gameObject);
            _button = UIFactory.GetOrAdd<Button>(gameObject);
            ApplyStyle(style);

            if (_label == null)
            {
                int fontSize = style == ButtonVisualStyle.Tab ? 18 : 24;
                _label = UIFactory.CreateText("Label", transform, string.Empty, fontSize, style == ButtonVisualStyle.Primary ? UITheme.DeepNavy : UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, new Vector2(12f, 10f), new Vector2(-12f, -10f));
            }

            _label.enableAutoSizing = true;
            _label.fontSizeMin = style == ButtonVisualStyle.Tab ? 16f : 22f;
            _label.fontSizeMax = style == ButtonVisualStyle.Tab ? 18f : 26f;
            _label.color = style == ButtonVisualStyle.Primary ? UITheme.DeepNavy : UITheme.SoftCream;
            float preferredHeight = style == ButtonVisualStyle.Primary ? 98f : style == ButtonVisualStyle.Tab ? 72f : 76f;
            float minHeight = style == ButtonVisualStyle.Primary ? 90f : style == ButtonVisualStyle.Tab ? 68f : 64f;
            UIFactory.AddLayoutElement(gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
        }

        void ApplyStyle(ButtonVisualStyle style)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = ResolveColor(style, false);
            colors.highlightedColor = ResolveColor(style, true);
            colors.pressedColor = ResolveColor(style, false) * 0.82f;
            colors.selectedColor = ResolveColor(style, true);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.75f);
            _button.colors = colors;
            _button.targetGraphic = _background;
            ApplyBackground(style, colors.normalColor);

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = style == ButtonVisualStyle.Danger ? UITheme.DangerDark : UITheme.MutedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public void SetLabelKey(string key, string fallback = null)
        {
            Build(_currentStyle);
            // LOCALIZATION: Buttons resolve through keys first, then fall back to readable text.
            _label.text = UILocalization.Get(key, fallback);
        }

        public void SetLabelText(string value)
        {
            Build(_currentStyle);
            _label.text = value;
        }

        public void SetOnClick(UnityAction action)
        {
            Build(_currentStyle);
            _button.onClick.RemoveAllListeners();
            if (action != null)
            {
                _button.onClick.AddListener(action);
            }
        }

        public void SetSelected(bool selected)
        {
            Build(_currentStyle);
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseBottomNavSprites && art != null && _currentStyle == ButtonVisualStyle.Tab)
            {
                UIArtLibrary.TryApply(_background, art.GetBottomNavSprite(selected), selected ? ResolveColor(_currentStyle, true) : ResolveColor(_currentStyle, false), selected ? "UI_BottomNav_Item_Active" : "UI_BottomNav_Item");
                return;
            }

            _background.sprite = null;
            _background.type = Image.Type.Simple;
            _background.color = selected ? ResolveColor(_currentStyle, true) : ResolveColor(_currentStyle, false);
        }

        public void SetIcon(Sprite sprite, string assetName)
        {
            Build(_currentStyle);
            if (_icon == null)
            {
                _icon = UIFactory.CreateUIObject("Icon", transform).AddComponent<Image>();
                _icon.raycastTarget = false;
                RectTransform iconRect = _icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(30f, 30f);
                iconRect.anchoredPosition = new Vector2(0f, -9f);
            }

            bool hasIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(_icon, sprite, Color.clear, assetName);
            _icon.enabled = hasIcon;
            if (_label != null)
            {
                UIFactory.Stretch(_label.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, hasIcon ? -34f : -8f));
            }
        }

        Color ResolveColor(ButtonVisualStyle style, bool active)
        {
            switch (style)
            {
                case ButtonVisualStyle.Secondary:
                    return active ? UITheme.Gunmetal : UITheme.NavyPanel;
                case ButtonVisualStyle.Danger:
                    return active ? UITheme.Danger : UITheme.DangerDark;
                case ButtonVisualStyle.Tab:
                    return active ? UITheme.TealDark : UITheme.Gunmetal;
                default:
                    return active ? UITheme.ButtonGoldTop : UITheme.ButtonGoldBottom;
            }
        }

        void ApplyBackground(ButtonVisualStyle style, Color fallbackColor)
        {
            if (!UIConstants.UseButtonSprites)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            if (style == ButtonVisualStyle.Tab)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            if (style == ButtonVisualStyle.Secondary && ShouldUseCompactFallback())
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            UIArtLibrary.TryApply(_background, ResolveSprite(style), fallbackColor, ResolveAssetName(style));
        }

        bool ShouldUseCompactFallback()
        {
            string objectName = gameObject.name.ToLowerInvariant();
            return objectName.Contains("quick")
                || objectName.Contains("back")
                || objectName.Contains("preview")
                || objectName.Contains("utility")
                || objectName.Contains("_tab");
        }

        Sprite ResolveSprite(ButtonVisualStyle style)
        {
            UIArtLibrary art = UIArtLibrary.Instance;
            if (art == null)
            {
                return null;
            }

            switch (style)
            {
                case ButtonVisualStyle.Primary:
                    return art.PrimaryButton;
                case ButtonVisualStyle.Tab:
                    return art.TabButton != null ? art.TabButton : art.BottomNavItem;
                default:
                    return art.SecondaryButton;
            }
        }

        string ResolveAssetName(ButtonVisualStyle style)
        {
            switch (style)
            {
                case ButtonVisualStyle.Primary:
                    return "UI_Button_Primary";
                case ButtonVisualStyle.Tab:
                    return "UI_Button_Tab";
                default:
                    return "UI_Button_Secondary";
            }
        }
    }
}

```

## RewardCardView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class RewardCardView : MonoBehaviour
    {
        TMP_Text _label;
        TMP_Text _amount;
        Image _icon;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(14f, UIConstants.UseRewardFrameSprites ? PanelVisualStyle.Cream : PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(gameObject, preferredWidth: 220f, preferredHeight: 128f, minWidth: 180f, minHeight: 120f);
            UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 6f, TextAnchor.MiddleCenter, false, false);

            if (_label == null)
            {
                _label = UIFactory.CreateText("Label", panel.ContentRoot, string.Empty, 20, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
                _amount = UIFactory.CreateText("Amount", panel.ContentRoot, string.Empty, 32, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                // READABILITY: Reward card labels should not drop into tiny prototype-sized text.
                UIFactory.ConfigureTextBlock(_label, 40f, true, 18f, 20f);
                UIFactory.ConfigureTextBlock(_amount, 52f, true, 20f, 32f);
            }

            if (_icon == null)
            {
                _icon = UIFactory.CreateUIObject("RewardIcon", transform).AddComponent<Image>();
                _icon.raycastTarget = false;
                RectTransform iconRect = _icon.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(52f, 52f);
                iconRect.anchoredPosition = new Vector2(18f, 0f);
            }

            _isBuilt = true;
        }

        public void Bind(RewardItemData data)
        {
            Build();
            _label.text = UILocalization.Get(data.labelKey, data.fallbackLabel);
            _amount.text = $"+{data.amount:N0}";
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseIconSprites && art != null)
            {
                UIArtLibrary.TryApply(_icon, art.GetRewardSprite(data.labelKey, data.fallbackLabel, data.accent), Color.clear, art.GetRewardAssetName(data.labelKey, data.fallbackLabel, data.accent));
            }
            else if (_icon != null)
            {
                _icon.enabled = false;
            }

            Color accent = UITheme.MutedGold;
            switch (data.accent)
            {
                case "teal":
                    accent = UITheme.Teal;
                    break;
                case "purple":
                    accent = UITheme.EpicPurple;
                    break;
                case "danger":
                    accent = UITheme.Danger;
                    break;
            }

            _amount.color = accent;
        }
    }
}

```

## TagPillView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class TagPillView : MonoBehaviour
    {
        TMP_Text _label;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            Image image = UIFactory.GetOrAdd<Image>(gameObject);
            image.color = UITheme.TealDark;

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = UITheme.Teal;
            outline.effectDistance = new Vector2(1f, -1f);

            if (_label == null)
            {
                _label = UIFactory.CreateText("Label", transform, string.Empty, 18, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
                _label.enableAutoSizing = true;
                _label.fontSizeMin = 12f;
                _label.fontSizeMax = 18f;
            }

            UIFactory.AddLayoutElement(gameObject, preferredHeight: 42f, minHeight: 38f, minWidth: 72f);
            UIFactory.AddContentSizeFitter(gameObject, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize);
            _isBuilt = true;
        }

        public void SetLabel(string value, bool danger = false)
        {
            Build();
            _label.text = value;
            GetComponent<Image>().color = danger ? UITheme.DangerDark : UITheme.TealDark;
        }
    }
}

```

## TopBarView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class TopBarView : MonoBehaviour
    {
        TMP_Text _identityText;
        TMP_Text _resourceA;
        TMP_Text _resourceB;
        TMP_Text _resourceC;
        NotificationBadgeView _mailBadge;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(18f);
            HorizontalLayoutGroup row = UIFactory.AddHorizontalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, false, false);
            row.padding = new RectOffset(0, 0, 0, 0);

            GameObject identity = UIFactory.CreateUIObject("Identity", panel.ContentRoot);
            UIFactory.AddLayoutElement(identity, flexibleWidth: 1f, preferredHeight: 80f);
            UIFactory.AddHorizontalLayout(identity, 10f, TextAnchor.MiddleLeft, false, false);

            Image avatar = UIFactory.CreateUIObject("CommanderAvatar", identity.transform).AddComponent<Image>();
            UIFactory.AddLayoutElement(avatar.gameObject, preferredWidth: 72f, preferredHeight: 72f, minWidth: 72f, minHeight: 72f);
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseCommanderSprites)
            {
                UIArtLibrary.TryApply(avatar, art != null ? art.CommanderPortrait : null, UITheme.Gunmetal, "Commander_Portrait_01");
            }
            else
            {
                avatar.sprite = null;
                avatar.color = UITheme.Gunmetal;
            }

            GameObject identityTextStack = UIFactory.CreateUIObject("IdentityTextStack", identity.transform);
            UIFactory.AddLayoutElement(identityTextStack, flexibleWidth: 1f, preferredHeight: 80f);
            UIFactory.AddVerticalLayout(identityTextStack, 2f, TextAnchor.MiddleLeft, true, false);

            if (_identityText == null)
            {
                _identityText = UIFactory.CreateText("IdentityText", identityTextStack.transform, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
                UIFactory.ConfigureTextBlock(_identityText, 34f, true, 18f, 24f);
                TMP_Text identitySub = UIFactory.CreateText("IdentitySub", identityTextStack.transform, "FIELD COMMAND", 14, UITheme.TextSecondary, FontStyles.Normal);
                UIFactory.ConfigureTextBlock(identitySub, 24f);
            }

            _resourceA = CreateResourceLabel(panel.ContentRoot, "ResourceA", art != null ? art.IconEnergy : null, "Icon_Energy");
            _resourceB = CreateResourceLabel(panel.ContentRoot, "ResourceB", art != null ? art.IconGold : null, "Icon_Gold");
            _resourceC = CreateResourceLabel(panel.ContentRoot, "ResourceC", art != null ? art.IconGems : null, "Icon_Gems");

            GameObject mail = UIFactory.CreateUIObject("Mail", panel.ContentRoot);
            UIFactory.AddLayoutElement(mail, preferredWidth: 88f, preferredHeight: 76f, minWidth: 88f);
            Image mailImage = mail.AddComponent<Image>();
            mailImage.color = UITheme.Gunmetal;
            Image mailIcon = UIFactory.CreateUIObject("MailIcon", mail.transform).AddComponent<Image>();
            mailIcon.raycastTarget = false;
            bool hasMailIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(mailIcon, art != null ? art.IconMail : null, Color.clear, "Icon_Mail");
            mailIcon.enabled = hasMailIcon;
            UIFactory.Stretch(mailIcon.rectTransform, new Vector2(26f, 28f), new Vector2(-26f, -18f));
            TMP_Text mailText = UIFactory.CreateText("MailLabel", mail.transform, "MAIL", 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(mailText.rectTransform, new Vector2(4f, 48f), new Vector2(-4f, 2f));
            _mailBadge = UIFactory.CreateUIObject("Badge", mail.transform).AddComponent<NotificationBadgeView>();
            RectTransform badgeRect = _mailBadge.GetComponent<RectTransform>();
            UIFactory.SetAnchors(badgeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(28f, 28f), new Vector2(-6f, -6f));

            GameObject settings = UIFactory.CreateUIObject("Settings", panel.ContentRoot);
            UIFactory.AddLayoutElement(settings, preferredWidth: 96f, preferredHeight: 76f, minWidth: 96f);
            Image settingsImage = settings.AddComponent<Image>();
            settingsImage.color = UITheme.Gunmetal;
            Image settingsIcon = UIFactory.CreateUIObject("SettingsIcon", settings.transform).AddComponent<Image>();
            settingsIcon.raycastTarget = false;
            bool hasSettingsIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(settingsIcon, art != null ? art.IconSettings : null, Color.clear, "Icon_Settings");
            settingsIcon.enabled = hasSettingsIcon;
            UIFactory.Stretch(settingsIcon.rectTransform, new Vector2(30f, 26f), new Vector2(-30f, -18f));
            TMP_Text settingsText = UIFactory.CreateText("SettingsText", settings.transform, UILocalization.Get("topbar.settings", "SETTINGS"), 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(settingsText.rectTransform, new Vector2(4f, 48f), new Vector2(-4f, 2f));

            _isBuilt = true;
        }

        public void Bind(TopBarData data)
        {
            Build();
            _identityText.text = $"{data.commanderName}  LV.{data.playerLevel}";
            _resourceA.text = $"{UILocalization.Get("topbar.energy", "ENERGY")}  {data.energy}/{data.maxEnergy}";
            _resourceB.text = $"{UILocalization.Get("topbar.gold", "GOLD")}  {data.gold:N0}";
            _resourceC.text = data.showPremiumCurrency
                ? $"{UILocalization.Get("topbar.gems", "GEMS")}  {data.premiumCurrency:N0}"
                : $"{UILocalization.Get("topbar.mail", "MAIL")}  {data.mailCount}";

            _mailBadge.SetCount(data.mailCount);
        }

        TMP_Text CreateResourceLabel(Transform parent, string name, Sprite iconSprite, string assetName)
        {
            GameObject go = UIFactory.CreateUIObject(name, parent);
            UIFactory.AddLayoutElement(go, preferredWidth: 170f, preferredHeight: 68f, minWidth: 150f);
            Image background = go.AddComponent<Image>();
            background.color = UITheme.Gunmetal;
            Image icon = UIFactory.CreateUIObject("Icon", go.transform).AddComponent<Image>();
            icon.raycastTarget = false;
            bool hasIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(icon, iconSprite, Color.clear, assetName);
            icon.enabled = hasIcon;
            UIFactory.Stretch(icon.rectTransform, new Vector2(10f, 16f), new Vector2(-128f, -16f));
            TMP_Text label = UIFactory.CreateText($"{name}Text", go.transform, string.Empty, 17, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 13f;
            label.fontSizeMax = 17f;
            UIFactory.Stretch(label.rectTransform, new Vector2(44f, 8f), new Vector2(-8f, -8f));
            return label;
        }
    }
}

```

## WorldNodeView.cs

```csharp
using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class WorldNodeView : MonoBehaviour
    {
        Button _button;
        Image _background;
        TMP_Text _label;
        LayoutElement _layoutElement;

        public void Build()
        {
            _background = UIFactory.GetOrAdd<Image>(gameObject);
            _button = UIFactory.GetOrAdd<Button>(gameObject);
            _button.targetGraphic = _background;
            _layoutElement = UIFactory.GetOrAdd<LayoutElement>(gameObject);
            _layoutElement.preferredWidth = 64f;
            _layoutElement.preferredHeight = 64f;

            if (_label == null)
            {
                _label = UIFactory.CreateText("StageLabel", transform, string.Empty, 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, Vector2.zero, Vector2.zero);
                _label.enableAutoSizing = true;
                _label.fontSizeMin = 20f;
                _label.fontSizeMax = 28f;
            }
        }

        public void Bind(StageNodeData data, UnityAction onClick)
        {
            Build();
            _label.text = data.stageId.ToString();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
            float size = data.isCurrent ? 104f : data.isBoss ? 92f : 64f;
            _layoutElement.preferredWidth = size;
            _layoutElement.preferredHeight = size;
            ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            _label.fontSizeMin = 20f;
            _label.fontSizeMax = data.isCurrent ? 28f : 24f;

            if (data.isBoss)
            {
                ApplyNodeArt(art => art.NodeBoss, UITheme.Danger, "UI_Node_Boss");
                _label.color = UITheme.SoftCream;
            }
            else if (data.isCurrent)
            {
                ApplyNodeArt(art => art.NodeCurrent, UITheme.Teal, "UI_Node_Current");
                _label.color = UITheme.DeepNavy;
            }
            else if (data.isCompleted)
            {
                ApplyNodeArt(art => art.NodeCompleted, UITheme.ButtonGoldTop, "UI_Node_Completed");
                _label.color = UITheme.DeepNavy;
            }
            else if (data.isLocked)
            {
                ApplyNodeArt(art => art.NodeLocked, UITheme.Gunmetal, "UI_Node_Locked");
                _label.color = UITheme.TextSecondary;
            }
            else
            {
                ApplyNodeArt(art => art.NodeNormal, UITheme.WarmCream, "UI_Node_Normal");
                _label.color = UITheme.DeepNavy;
            }
        }

        void ApplyNodeArt(System.Func<UIArtLibrary, Sprite> selector, Color fallback, string assetName)
        {
            if (!UIConstants.UseNodeSprites)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallback;
                return;
            }

            UIArtLibrary art = UIArtLibrary.Instance;
            Sprite sprite = art != null ? selector(art) : null;
            UIArtLibrary.TryApply(_background, sprite, fallback, assetName);
        }
    }
}

```

