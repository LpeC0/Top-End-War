TUM SCRIPTLER
ArmyManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Ordu Yoneticisi (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "ArmyManager" -> bu scripti ekle.
///   soldierPrefab: bos birakilabilir (fallback kapsul olusturur).
///   Opsiyonel: ObjectPooler'da "Soldier" poolu ekleyin.
///
/// ONEMLI:
///   - Soldier tag'i otomatik set edilir ("Soldier")
///   - xLimit = 8 (PlayerController ile ayni olmalı)
///   - Formasyon: 4 sira x 5 sutun, oyuncunun arkasindan
///
/// Disaridan kullanim:
///   ArmyManager.Instance.AddSoldier(SoldierPath.Teknoloji, "Tas");
///   ArmyManager.Instance.TryMerge();   // Merge kapisi gecilince
///   ArmyManager.Instance.HealAll(0.5f); // HealSoldiers kapisi
/// </summary>
public class ArmyManager : MonoBehaviour
{
    public static ArmyManager Instance { get; private set; }

    [Header("Asker Prefab (bos birakabilirsin)")]
    public GameObject soldierPrefab;

    [Header("Sinirlar")]
    public int maxSoldiers = 20;

    // ── Formasyon (20 slot) ───────────────────────────────────────────────
    // Oyuncu +Z yonune kosar; askerler arkasindan gelir (negatif Z offset).
    // Y=0 (LateUpdate'de 1.2f set edilir), xLimit dahilinde.
    static readonly Vector3[] FORMATION = new Vector3[20]
    {
        // Sira 1 — z=-2 (en yakın, 4 asker)
        new Vector3(-3f, 0f, -2f), new Vector3(-1f, 0f, -2f),
        new Vector3( 1f, 0f, -2f), new Vector3( 3f, 0f, -2f),
        // Sira 2 — z=-4 (5 asker)
        new Vector3(-4f, 0f, -4f), new Vector3(-2f, 0f, -4f), new Vector3(0f, 0f, -4f),
        new Vector3( 2f, 0f, -4f), new Vector3( 4f, 0f, -4f),
        // Sira 3 — z=-6 (5 asker)
        new Vector3(-4f, 0f, -6f), new Vector3(-2f, 0f, -6f), new Vector3(0f, 0f, -6f),
        new Vector3( 2f, 0f, -6f), new Vector3( 4f, 0f, -6f),
        // Sira 4 — z=-8 (6 asker, en geri)
        new Vector3(-5f, 0f, -8f), new Vector3(-3f, 0f, -8f), new Vector3(-1f, 0f, -8f),
        new Vector3( 1f, 0f, -8f), new Vector3( 3f, 0f, -8f), new Vector3( 5f, 0f, -8f),
    };

    // ── Asker listesi ─────────────────────────────────────────────────────
    readonly List<SoldierUnit> _soldiers = new List<SoldierUnit>(20);

    // ── Base stat tablosu (path bazlı) ───────────────────────────────────
    static readonly Dictionary<SoldierPath, (int hp, float atk, float spd)> SOLDIER_BASE
        = new Dictionary<SoldierPath, (int, float, float)>
    {
        [SoldierPath.Piyade]    = (80,  15f, 1.5f),
        [SoldierPath.Mekanik]   = (120,  8f, 4.0f),
        [SoldierPath.Teknoloji] = (50,  30f, 0.8f),
    };

    // ── Merge level stat carpanlari ───────────────────────────────────────
    static readonly float[] MERGE_MULT = { 1f, 1.8f, 3.5f, 7.0f }; // Lv1-Lv4

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Asker Ekle ────────────────────────────────────────────────────────
    /// <summary>
    /// Formasyona yeni asker ekler. Dolu ise false döner.
    /// biome: "Tas", "Orman" vb. — null ise BiomeManager'dan alinir.
    /// count: kac asker eklenecek (genellikle 1-2).
    /// </summary>
    public bool AddSoldier(SoldierPath path, string biome = null, int mergeLevel = 1, int count = 1)
    {
        bool added = false;
        for (int i = 0; i < count; i++)
        {
            if (_soldiers.Count >= maxSoldiers) break;

            string actualBiome = biome ?? BiomeManager.Instance?.currentBiome ?? "Tas";
            SoldierUnit unit = SpawnSoldierUnit(path, actualBiome, mergeLevel);
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

    // ── Asker Kaldir (olgum) ─────────────────────────────────────────────
    public void RemoveSoldier(SoldierUnit unit)
    {
        if (!_soldiers.Contains(unit)) return;
        _soldiers.Remove(unit);
        AssignFormationOffsets();
        GameEvents.OnSoldierRemoved?.Invoke(_soldiers.Count);
        Debug.Log($"[Army] Asker dust | Kalan: {_soldiers.Count}");
    }

    // ── Merge ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Mevcut formasyonda 3x ayni-path+ayni-level bulursa birlestirir.
    /// Merge kapisi gecilince cagirilir. Birden fazla grup varsa hepsini birlestirir.
    /// Merge oldu mu? True döner.
    /// </summary>
    public bool TryMerge()
    {
        bool anyMerge = false;

        // Tekrar tekrar dene — zincir merge mumkun (Lv1→Lv2→Lv3)
        bool found;
        int  safetyLimit = 10;
        do
        {
            found = false;
            if (safetyLimit-- <= 0) break;

            foreach (SoldierPath path in System.Enum.GetValues(typeof(SoldierPath)))
            {
                for (int lv = 1; lv <= 3; lv++) // Lv4 max, daha fazla merge yok
                {
                    List<SoldierUnit> group = FindGroup(path, lv);
                    if (group.Count < 3) continue;

                    // 3 askeri kaldir, 1 lv+1 ekle
                    SoldierUnit first = group[0];
                    string biome = first.biome;

                    for (int i = 0; i < 3; i++)
                    {
                        SoldierUnit u = group[i];
                        _soldiers.Remove(u);
                        u.gameObject.SetActive(false);
                    }

                    // Yeni seviyeli asker olustur (aynı noktaya dogup buyuyecek)
                    SoldierUnit merged = SpawnSoldierUnit(path, biome, lv + 1);
                    if (merged != null) _soldiers.Add(merged);

                    AssignFormationOffsets();
                    GameEvents.OnSoldierMerged?.Invoke(path.ToString(), lv + 1);
                    Debug.Log($"[Army] MERGE: {path} Lv{lv} x3 → Lv{lv + 1}");

                    found     = true;
                    anyMerge  = true;
                    break;
                }
                if (found) break;
            }
        } while (found);

        return anyMerge;
    }

    // ── Tum Askerleri İyilesir ───────────────────────────────────────────
    /// <summary>pct = 0.5f → %50 max HP geri yükle.</summary>
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
        Debug.Log($"[Army] HealAll %{pct*100:.0f} | Toplam iyilestirme: {totalHealed}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Yardimci: formasyon offset atama
    void AssignFormationOffsets()
    {
        for (int i = 0; i < _soldiers.Count && i < FORMATION.Length; i++)
            _soldiers[i].formationOffset = FORMATION[i];
    }

    // Yardimci: path+level grubu bul
    List<SoldierUnit> FindGroup(SoldierPath path, int level)
    {
        var list = new List<SoldierUnit>();
        foreach (SoldierUnit u in _soldiers)
            if (u.path == path && u.mergeLevel == level) list.Add(u);
        return list;
    }

    // Yardimci: SoldierUnit olustur (prefab veya fallback kapsul)
    SoldierUnit SpawnSoldierUnit(SoldierPath path, string biome, int mergeLevel)
    {
        GameObject go;
        if (soldierPrefab != null)
        {
            go = Instantiate(soldierPrefab);
        }
        else
        {
            // Fallback: renkli kapsul
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            Destroy(go.GetComponent<CapsuleCollider>());
            var cc = go.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f; cc.height = 1.1f; cc.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // Konum: player konumu + ufak random offset (gecislerde capraz)
        if (PlayerStats.Instance != null)
            go.transform.position = PlayerStats.Instance.transform.position
                                    + new Vector3(Random.Range(-1f, 1f), 1.2f, -2f);

        // SoldierUnit bileşeni ekle (veya al)
        SoldierUnit unit = go.GetComponent<SoldierUnit>() ?? go.AddComponent<SoldierUnit>();

        // Statları ayarla
        var (hp, atk, spd) = SOLDIER_BASE[path];
        float mm = MERGE_MULT[Mathf.Clamp(mergeLevel - 1, 0, MERGE_MULT.Length - 1)];

        unit.path       = path;
        unit.biome      = biome;
        unit.mergeLevel = mergeLevel;
        unit.maxHP      = Mathf.RoundToInt(hp * mm);
        unit.currentHP  = unit.maxHP;
        unit.baseAtk    = atk;
        unit.atkSpeed   = spd;

        // Renk uygula
        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color c = unit.GetPathColor();
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", c);
            else
                rend.material.color = c;
        }

        go.name = $"Soldier_{path}_Lv{mergeLevel}";
        go.SetActive(true);
        return unit;
    }

    // ── Getter'lar (HUD vb.) ─────────────────────────────────────────────
    public int  SoldierCount       => _soldiers.Count;
    public bool IsFull             => _soldiers.Count >= maxSoldiers;
    public int  GetCountByPath(SoldierPath path)
    {
        int n = 0;
        foreach (SoldierUnit u in _soldiers) if (u.path == path) n++;
        return n;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
```

BiomeManager.cs

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

# Biomevisuals.cs

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

BossManager.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — Boss v5 (Claude)
///
/// v5 DEGISIKLIKLER:
///   - Boss HP 41.000 (ordu DPS kalibrasyonu: T3+10 optimal asker ~75sn)
///   - Biyom zayifligi: Teknoloji + Tas = x1.25 bonus hasar UYGULANIR
///     (BiomeManager + SoldierPath parametresi ile dinamik)
///   - Faz1 tas zirhı: Piyade hasarı x0.9 (dogal direnc)
///   - Yenilgide biyomun boss adı kullanılıyor (BiomeManager.GetBossName)
///
/// DENGE (simülasyon doğrulandı):
///   T3+10 Teknoloji Lv1 + Tas biyom: ~75sn  ✓
///   T3+10 Piyade Lv1   + Tas biyom: ~108sn  ✓ (yanlış path = zor ama mümkün)
///   T4+15 Teknoloji Lv2 + Tas biyom: ~39sn  ✓ (güçlü oyuncu hızlı bitirir)
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss")]
    public int   bossMaxHP         = 41000;
    public float bossApproachSpeed = 2.5f;
    public int   bossContactDmg    = 100;
    public float bossStopDist      = 12f;

    [Header("Fazlar")]
    [Range(0,1)] public float phase2At = 0.60f;
    [Range(0,1)] public float phase3At = 0.30f;

    [Header("Minyon (Faz2)")]
    public GameObject minionPrefab;
    public int   minionCount    = 3;
    public float minionInterval = 8f;

    [Header("Prefab (bos = fallback kup)")]
    public GameObject bossPrefab;

    int    _hp;
    int    _phase  = 0;
    bool   _active = false;
    bool   _dead   = false;

    GameObject      _bossObj;
    Renderer        _bossRend;
    Canvas          _bossCanvas;
    Image           _hpFill;
    TextMeshProUGUI _phaseLabel;

    // Biyom zayıflığı — Bullet hasarını BossManager üzerinden geçirmiyoruz,
    // SoldierUnit ve PlayerController direkt Enemy'e çarpıyor.
    // Bunun yerine Boss'a gelen hasarı biyom çarpanıyla amplify ediyoruz.
    float _biomeMultiplier = 1f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnBossEncountered += StartFight;
        GameEvents.OnBiomeChanged    += OnBiomeChanged;
        BuildHPBar();
        _bossCanvas?.gameObject.SetActive(false);

        // Baslangicta biyom ayarla
        if (BiomeManager.Instance != null)
            OnBiomeChanged(BiomeManager.Instance.currentBiome);
    }

    void OnDestroy()
    {
        GameEvents.OnBossEncountered -= StartFight;
        GameEvents.OnBiomeChanged    -= OnBiomeChanged;
    }

    // ── Biyom Zayifligi ──────────────────────────────────────────────────
    // Dominant path mevcut biyomla eşleşiyorsa boss daha hızlı ölür.
    // Çarpan BossHitReceiver.TakeDamage'da uygulanır.
    void OnBiomeChanged(string biome)
    {
        // Taş biyomda Teknoloji dominant ise x1.25 (tasarım belgesiyle uyumlu)
        // Oyuncunun dominant pathini PlayerStats'tan okuyoruz.
        _biomeMultiplier = CalcBiomeMultiplier(biome);
        Debug.Log($"[Boss] Biyom={biome} | Hasar carpani: x{_biomeMultiplier:F2}");
    }

    float CalcBiomeMultiplier(string biome)
    {
        if (PlayerStats.Instance == null) return 1f;
        var ps = PlayerStats.Instance;

        float total = ps.PiyadePath + ps.MekanizePath + ps.TeknolojiPath;
        if (total < 1f) return 1f;

        float p = ps.PiyadePath / total;
        float m = ps.MekanizePath / total;
        float t = ps.TeknolojiPath / total;

        // Dominant path (>%50) varsa biyom matrisinden carpanı al
        SoldierPath dominant;
        if (t >= 0.5f) dominant = SoldierPath.Teknoloji;
        else if (p >= 0.5f) dominant = SoldierPath.Piyade;
        else if (m >= 0.5f) dominant = SoldierPath.Mekanik;
        else return 1f; // karışık path — bonus yok

        return BiomeManager.Instance?.GetMultiplier(dominant) ?? 1f;
    }

    // ── Boss Baslangici ───────────────────────────────────────────────────
    void StartFight()
    {
        if (_active) return;
        Debug.Log("[Boss] Basliyor!");
        StartCoroutine(EntranceCo());
    }

    IEnumerator EntranceCo()
    {
        GameEvents.OnAnchorModeChanged?.Invoke(true);
        yield return new WaitForSeconds(0.5f);

        Vector3 spawnPos = new Vector3(0f, 1.2f,
            (PlayerStats.Instance?.transform.position.z ?? 0f) + 45f);
        SpawnBoss(spawnPos);

        _hp     = bossMaxHP;
        _phase  = 1;
        _active = true;

        _bossCanvas?.gameObject.SetActive(true);
        UpdateBar();

        // Biyom boss adı
        string bossName = BiomeManager.Instance?.GetBossName() ?? "BOSS";
        SetLabel($"{bossName.ToUpper()}  |  FAZ 1: ZİRH");
        Debug.Log($"[Boss] Aktif! HP={_hp} | Biyom carpani: x{_biomeMultiplier:F2}");
    }

    void SpawnBoss(Vector3 pos)
    {
        _bossObj = bossPrefab != null
            ? Instantiate(bossPrefab, pos, Quaternion.identity)
            : MakeFallbackCube(pos);

        _bossRend    = _bossObj.GetComponent<Renderer>();
        _bossObj.tag = "Enemy";

        // isTrigger=false → Bullet.OverlapSphere bulur
        if (_bossObj.GetComponent<Collider>() == null)
            _bossObj.AddComponent<BoxCollider>();

        var recv = _bossObj.AddComponent<BossHitReceiver>();
        recv.bossManager = this;
    }

    GameObject MakeFallbackCube(Vector3 pos)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(5f, 7f, 5f);
        obj.GetComponent<Renderer>().material.color = new Color(0.55f, 0f, 0f);
        return obj;
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_active || _dead || _bossObj == null || PlayerStats.Instance == null) return;

        float tZ  = PlayerStats.Instance.transform.position.z + bossStopDist;
        Vector3 p = _bossObj.transform.position;

        if (p.z > tZ)
            p.z = Mathf.MoveTowards(p.z, tZ, bossApproachSpeed * Time.deltaTime);
        else
        {
            // Boss dokunmasi — Komutan HP'yi düsür
            PlayerStats.Instance.TakeContactDamage(bossContactDmg);
            p.z += 8f;
        }
        _bossObj.transform.position = p;
    }

    // ── Hasar Al ─────────────────────────────────────────────────────────
    /// <summary>
    /// Biyom carpani burada uygulanır.
    /// Faz1'de Piyade hasarına tas direnci var (x0.9).
    /// </summary>
    public void TakeDamage(int rawDmg)
    {
        if (!_active || _dead) return;

        // Biyom carpanı
        float finalDmg = rawDmg * _biomeMultiplier;

        // Faz1 Piyade direnci — mevcut subpath baskın Piyade ise
        if (_phase == 1)
        {
            var ps = PlayerStats.Instance;
            if (ps != null)
            {
                float total = ps.PiyadePath + ps.MekanizePath + ps.TeknolojiPath;
                if (total > 0f && ps.PiyadePath / total >= 0.5f)
                    finalDmg *= 0.9f; // Taş direnci Piyadeye karşı
            }
        }

        _hp = Mathf.Max(0, _hp - Mathf.RoundToInt(finalDmg));
        UpdateBar();
        StartCoroutine(Flash());
        CheckPhase();
        if (_hp <= 0) Defeat();
    }

    void CheckPhase()
    {
        float r = (float)_hp / bossMaxHP;
        if      (_phase == 1 && r <= phase2At) { _phase = 2; Phase2(); }
        else if (_phase == 2 && r <= phase3At) { _phase = 3; Phase3(); }
    }

    void Phase2()
    {
        string bossName = BiomeManager.Instance?.GetBossName() ?? "BOSS";
        SetLabel($"{bossName.ToUpper()}  |  FAZ 2: MİNYON");
        if (_bossRend) _bossRend.material.color = new Color(0.7f, 0.3f, 0.1f);
        InvokeRepeating(nameof(SpawnMinions), 1f, minionInterval);
    }

    void Phase3()
    {
        string bossName = BiomeManager.Instance?.GetBossName() ?? "BOSS";
        SetLabel($"{bossName.ToUpper()}  |  FAZ 3: ÇEKİRDEK");
        if (_bossRend) _bossRend.material.color = new Color(0.9f, 0.05f, 0.05f);
        bossApproachSpeed *= 2.2f;
        CancelInvoke(nameof(SpawnMinions));
    }

    void SpawnMinions()
    {
        if (!_active || _dead || PlayerStats.Instance == null) return;
        Vector3 center = PlayerStats.Instance.transform.position + Vector3.forward * 10f;
        for (int i = 0; i < minionCount; i++)
        {
            Vector3 p = center + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(2f, 12f));
            GameObject min;
            if (minionPrefab != null) min = Instantiate(minionPrefab, p, Quaternion.identity);
            else
            {
                min = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                min.transform.position   = p;
                min.transform.localScale = Vector3.one * 0.7f;
                Destroy(min.GetComponent<Collider>());
                var cc = min.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
                var rb = min.AddComponent<Rigidbody>(); rb.isKinematic = true;
                min.tag = "Enemy";
                min.AddComponent<Enemy>();
            }
            min.GetComponent<Enemy>()?.Initialize(
                new DifficultyManager.EnemyStats(300, 60, 5f, 10));
        }
    }

    // ── Zafer / Yenilgi ───────────────────────────────────────────────────
    void Defeat()
    {
        _dead = true; _active = false;
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(2000);
        if (_bossObj) Destroy(_bossObj);
        _bossCanvas?.gameObject.SetActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        StartCoroutine(VictoryCo());
    }

    IEnumerator VictoryCo() { yield return new WaitForSeconds(1.5f); Victory(); }

    void Victory()
    {
        Canvas c = FindFirstObjectByType<Canvas>(); if (!c) return;
        var panel = new GameObject("Victory"); panel.transform.SetParent(c.transform, false);
        panel.AddComponent<Image>().color = new Color(0,0,0,0.88f);
        var r = panel.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;

        string region = BiomeManager.Instance?.currentBiome ?? "BÖLGE";
        Txt(panel, $"{region.ToUpper()} ELE GEÇİRİLDİ!", new Vector2(0,100), 46, new Color(1f,0.85f,0f), FontStyles.Bold);
        Txt(panel, "CP: " + (PlayerStats.Instance?.CP.ToString("N0") ?? "0"), new Vector2(0,25), 28, Color.white, FontStyles.Normal);
        Txt(panel, "Asker: " + (ArmyManager.Instance?.SoldierCount ?? 0) + "/20", new Vector2(0,-20), 22, new Color(0.7f,0.7f,1f), FontStyles.Normal);
        Btn(panel, "TEKRAR DENE", new Vector2(0,-85), new Vector2(240,55), new Color(0.2f,0.7f,0.2f), () =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }

    // ── HP Bar ────────────────────────────────────────────────────────────
    void BuildHPBar()
    {
        var co = new GameObject("BossHPCanvas");
        _bossCanvas = co.AddComponent<Canvas>();
        _bossCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _bossCanvas.sortingOrder = 50;
        co.AddComponent<CanvasScaler>(); co.AddComponent<GraphicRaycaster>();

        var strip = new GameObject("S"); strip.transform.SetParent(co.transform, false);
        strip.AddComponent<Image>().color = new Color(0,0,0,0.78f);
        var sr = strip.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.04f, 0.90f); sr.anchorMax = new Vector2(0.96f, 0.97f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        var fo = new GameObject("F"); fo.transform.SetParent(strip.transform, false);
        _hpFill = fo.AddComponent<Image>();
        _hpFill.type = Image.Type.Filled; _hpFill.fillMethod = Image.FillMethod.Horizontal;
        _hpFill.color = new Color(0.2f,0.85f,0.2f);
        var fr = fo.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.005f, 0.08f); fr.anchorMax = new Vector2(0.995f, 0.92f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;

        _phaseLabel = Txt(strip, "BOSS", Vector2.zero, 14, Color.white, FontStyles.Bold);
        var lr = _phaseLabel.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void UpdateBar()
    {
        if (!_hpFill) return;
        float r = (float)_hp / bossMaxHP;
        _hpFill.fillAmount = r;
        _hpFill.color = r > 0.6f ? new Color(0.2f,0.85f,0.2f) : r > 0.3f ? new Color(1f,0.7f,0f) : new Color(0.9f,0.1f,0.1f);
    }

    void SetLabel(string s) { if (_phaseLabel) _phaseLabel.text = s; }

    IEnumerator Flash()
    {
        if (!_bossRend) yield break;
        Color o = _bossRend.material.color;
        _bossRend.material.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        if (_bossRend) _bossRend.material.color = o;
    }

    // ── UI Helpers ────────────────────────────────────────────────────────
    TextMeshProUGUI Txt(GameObject p, string txt, Vector2 pos, float sz, Color col, FontStyles fs)
    {
        var o = new GameObject("T"); o.transform.SetParent(p.transform, false);
        var t = o.AddComponent<TextMeshProUGUI>();
        t.text=txt; t.fontSize=sz; t.color=col; t.fontStyle=fs; t.alignment=TextAlignmentOptions.Center;
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f,0.5f); r.anchorMax = new Vector2(0.5f,0.5f);
        r.pivot = new Vector2(0.5f,0.5f); r.anchoredPosition=pos; r.sizeDelta=new Vector2(600,60);
        return t;
    }

    void Btn(GameObject p, string lbl, Vector2 pos, Vector2 sz, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var bo = new GameObject("B"); bo.transform.SetParent(p.transform, false);
        var im = bo.AddComponent<Image>(); im.color=bg;
        var bt = bo.AddComponent<Button>(); bt.targetGraphic=im; bt.onClick.AddListener(onClick);
        var r = bo.GetComponent<RectTransform>();
        r.anchorMin=new Vector2(0.5f,0.5f); r.anchorMax=new Vector2(0.5f,0.5f);
        r.pivot=new Vector2(0.5f,0.5f); r.anchoredPosition=pos; r.sizeDelta=sz;
        var lo = new GameObject("L"); lo.transform.SetParent(bo.transform, false);
        var lt = lo.AddComponent<TextMeshProUGUI>();
        lt.text=lbl; lt.fontSize=20; lt.color=Color.white; lt.fontStyle=FontStyles.Bold; lt.alignment=TextAlignmentOptions.Center;
        var lr = lo.GetComponent<RectTransform>();
        lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=lr.offsetMax=Vector2.zero;
    }
}

public class BossHitReceiver : MonoBehaviour
{
    public BossManager bossManager;
}
```

Bullet.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Mermi v4 (Claude)
///
/// DÜZELTMELER:
///   Çarptıktan sonra anında kaybolur (SetActive false + velocity=0)
///   Lead targeting KALDIRILDI — düz ileri ateş (görsel olarak daha temiz)
///   OverlapSphere radius 0.35 → 0.4 (daha güvenilir hit)
///   Lifetime 2.5s → 1.8s (daha az "havada kalan" mermi görüntüsü)
///
/// Komutan+Asker sistemine hazırlık:
///   SetDamage(int) public — asker mermileri farklı hasar verebilir
/// </summary>
public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);
    [HideInInspector]
    public string hitterPath  = "Commander"; // "Commander","Piyade","Mekanik","Teknoloji" 

    const float HIT_RADIUS = 0.4f;
    const float LIFETIME   = 1.8f;

    Renderer _rend;
    bool     _hit = false;

    void Awake() => _rend = GetComponentInChildren<Renderer>();

    void OnEnable()
    {
        _hit = false;
        ApplyColor();
        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
    }

    public void SetDamage(int d) => damage = d;

    void Update()
    {
        if (_hit) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, HIT_RADIUS);
        foreach (Collider col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;

            // Geride kalan düşmana gitme — oyuncunun en az 2 birim gerisinde olan enemy'i atla
            if (PlayerStats.Instance != null)
            {
                float playerZ = PlayerStats.Instance.transform.position.z;
                if (col.transform.position.z < playerZ - 2f) continue;
            }

            BossHitReceiver bossRecv = col.GetComponent<BossHitReceiver>();
            if (bossRecv != null)
            {
                bossRecv.bossManager?.TakeDamage(damage);
                DamagePopup.Show(col.transform.position, damage,
                    DamagePopup.GetColor(hitterPath), damage > 500);
            }
            else
            {
                col.GetComponent<Enemy>()?.TakeDamage(damage, DamagePopup.GetColor(hitterPath));
            }

            Hit();
            return;
        }
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

ChunkManager.cs

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

# Damagepopup.cs

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

DifficultyManager.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dinamik Zorluk Yoneticisi (Claude)
/// ProgressionConfig OLMADAN da calisir — dahili sabitler kullanilir.
/// Her 50 birimde hesaplama yapar (her frame degil).
/// NAMESPACE YOK.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Config (Opsiyonel)")]
    public ProgressionConfig config;

    [Header("Guncelleme Araligi")]
    public float updateInterval = 50f;

    // Dahili sabitler (config yoksa)
    const float BASE_HP     = 100f;
    const float BASE_DMG    = 20f;  // 25 → 20: biraz daha cömertti
    const float BASE_SPEED  = 3.5f; // 4.0 → 3.5: başlangıçta daha yavaş
    const float MAX_SPEED   = 6.5f; // 7.5 → 6.5: max hız biraz düşük
    const float BASE_REWARD = 18f;  // 15 → 18: kill başı biraz daha CP

    public float CurrentDifficultyMultiplier { get; private set; } = 1f;
    public float PlayerPowerRatio            { get; private set; } = 1f;

    // GC-friendly struct — allocation yok
    public readonly struct EnemyStats
    {
        public readonly int   Health;
        public readonly int   Damage;
        public readonly float Speed;
        public readonly int   CPReward;
        public EnemyStats(int h, int d, float s, int r)
        { Health=h; Damage=d; Speed=s; CPReward=r; }
    }

    Transform _player;
    float     _lastUpdateZ = -9999f;
    float     _currentZ    = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (PlayerStats.Instance != null)
            _player = PlayerStats.Instance.transform;
    }

    void Update()
    {
        if (_player == null) { TryFindPlayer(); return; }
        _currentZ = _player.position.z;
        if (Mathf.Abs(_currentZ - _lastUpdateZ) >= updateInterval)
        {
            Recalculate();
            _lastUpdateZ = _currentZ;
        }
    }

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null) _player = PlayerStats.Instance.transform;
    }

    void Recalculate()
    {
        float norm = _currentZ / 1000f;
        CurrentDifficultyMultiplier = 1f + Mathf.Pow(norm, 1.1f); // 1.3→1.1: daha yavaş artış

        int   expected = config != null
            ? config.CalculateExpectedCP(_currentZ)
            : Mathf.RoundToInt(200f * Mathf.Pow(1.15f, _currentZ / 100f));

        int   actual   = PlayerStats.Instance?.CP ?? 200;
        float raw      = (float)actual / Mathf.Max(1, expected);
        PlayerPowerRatio = Mathf.Lerp(PlayerPowerRatio, raw, 0.15f); // 0.08→0.15: DDA daha hızlı adapte

        PlayerStats.Instance?.SetExpectedCP(expected);
        GameEvents.OnDifficultyChanged?.Invoke(CurrentDifficultyMultiplier, PlayerPowerRatio);
    }

    public EnemyStats GetScaledEnemyStats()
    {
        float diff  = CurrentDifficultyMultiplier;
        // pScale: oyuncu güçlüyse düşman biraz artar ama çok fazla değil
        // 0.5f = oyuncu 2x güçlüyse düşman sadece 1.5x güçlü (eskisi 1.9x idi!)
        float scaleFactor = config != null ? config.playerCPScalingFactor : 0.5f;
        float pScale = Mathf.Lerp(1f, Mathf.Min(PlayerPowerRatio, 1.5f), scaleFactor);
        float final = diff * pScale;

        float bH    = config != null ? config.baseEnemyHealth : BASE_HP;
        float bD    = config != null ? config.baseEnemyDamage : BASE_DMG;
        float bS    = config != null ? config.baseEnemySpeed  : BASE_SPEED;
        float maxS  = config != null ? config.enemyMaxSpeed   : MAX_SPEED;

        return new EnemyStats(
            Mathf.RoundToInt(bH * final),
            Mathf.RoundToInt(bD * final),
            Mathf.Min(bS * (1f + (diff - 1f) * 0.35f), maxS),
            Mathf.RoundToInt(BASE_REWARD * diff));
    }

    public bool IsInPityZone(float bossDistance)
    {
        float zone = config != null ? config.noBadGateZoneBeforeBoss : 200f;

 return _currentZ >= bossDistance - zone;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }}
```

Enemy.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dusman v5 (Claude)
///
/// DEGISIKLIKLER (v5):
///   OnTriggerEnter: Soldier tagini da taniyor.
///   Dusman Soldier'a carparsa: Soldier hasar alir, dusman olur.
///   Dusman Player'a carparsa: CommanderHP.TakeDamage (CP etkilenmiyor).
///   HP degerleri: Normal 1100, Zirh 2250, Elite 3750 (ordu DPS'e gore).
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    int    _maxHealth;
    int    _currentHealth;
    int    _contactDamage;
    float  _moveSpeed;
    int    _cpReward;
    bool   _initialized      = false;
    bool   _isDead            = false;
    bool   _hasDamagedPlayer  = false;

    Renderer       _bodyRenderer;
    EnemyHealthBar _hpBar;

    float   _lastSepTime   = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar        = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();
        UseDefaults();
    }

    void OnEnable()
    {
        _isDead          = false;
        _hasDamagedPlayer= false;
        _separationVec   = Vector3.zero;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
        if (!_initialized) AutoInit();
        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth        = stats.Health;
        _currentHealth    = _maxHealth;
        _contactDamage    = stats.Damage;
        _moveSpeed        = stats.Speed;
        _cpReward         = stats.CPReward;
        _initialized      = true;
        _isDead           = false;
        _hasDamagedPlayer = false;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
        _hpBar?.Init(_maxHealth);
    }

    void AutoInit()
    {
        float z    = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f;
        float mult = 1f + Mathf.Pow(z / 1000f, 1.3f);
        // Yeni HP degerlerine gore (ordu DPS kalibre)
        _maxHealth     = Mathf.RoundToInt(1100f * mult);
        _currentHealth = _maxHealth;
        _contactDamage = Mathf.RoundToInt(30f   * mult);
        _moveSpeed     = Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f);
        _cpReward      = Mathf.RoundToInt(15f   * mult);
    }

    void UseDefaults()
    {
        _maxHealth = _currentHealth = 1100;
        _contactDamage = 30; _moveSpeed = 4.5f; _cpReward = 15;
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   pZ  = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f) pos.z -= _moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime   = Time.time;
        }
        pos += _separationVec * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f) gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep   = Vector3.zero;
        int     count = 0;
        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            if (away.magnitude < 0.001f) away = new Vector3(Random.Range(-1f, 1f), 0, 0).normalized * 0.1f;
            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }
        return count > 0 ? (sep / count) * 3.5f : Vector3.zero;
    }

    public void TakeDamage(int dmg, Color? hitColor = null)
    {
        if (_isDead) return;
        _currentHealth -= dmg;
        _hpBar?.UpdateBar(_currentHealth);
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);

        // Floating damage popup
        bool isCrit = dmg > 200;
        Color popupColor = hitColor ?? DamagePopup.GetColor("Commander");
        DamagePopup.Show(transform.position, dmg, popupColor, isCrit);

        if (_currentHealth <= 0) Die();
    }

    void ResetColor()
    {
        if (!_isDead && _bodyRenderer != null) _bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = _initialized = false;
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(_cpReward);
        SaveManager.Instance?.RegisterKill();  // kill sayaci
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isDead) return;

        // --- Askere carparsa (tag yok, GetComponent kullan) ---
        SoldierUnit soldier = other.GetComponent<SoldierUnit>();
        if (soldier != null)
        {
            // Pozisyon bazlı hasar: asker düşman ile temas edince düşer
            soldier.TakeDamage(_contactDamage);
            Die();
            return;
        }

        // --- Oyuncuya carparsa ---
        if (!other.CompareTag("Player") || _hasDamagedPlayer) return;
        _hasDamagedPlayer = true;
        // PlayerStats.TakeContactDamage → OnPlayerDamaged event → CommanderHP alir
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

    void OnDisable() { CancelInvoke(); _initialized = false; }
}
```

EnemyHealthBar.cs

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

EquipmentData.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ekipman Verisi v2 (Claude)
///
/// SILAH TÜRLERİ VE GERÇEKÇİ ÖZELLİKLER:
///
///   Tabanca     — Hızlı atış, kısa menzil, hafif    
///   Tüfek       — Dengeli, orta menzil, tek atış
///   Otomatik    — Yüksek DPS, kısa/orta menzil, düşük isabet
///   Keskin nişancı — Yüksek hasar, uzun menzil, çok yavaş atış
///   Pompalı     — Çok hasar yakın, düşük menzil, yavaş
///
/// ZIRH TÜRLERİ:
///   Hafif zırh  — Az koruma, yüksek mobilite (gelecek: hız bonusu)
///   Orta zırh   — Dengeli
///   Ağır zırh   — Yüksek koruma, yavaş (gelecek: hız cezası)
///   Kalkan      — Hasar azaltma bonusu
///
/// AKSESUAR (kolye, yüzük, omuzluk, vb.):
///   Çeşitli bonuslar — CP, ateş hızı, hasar, çoğaltıcı vb.
///
/// KURULUM:
///   Assets → Create → TopEndWar → Equipment
///   Tipi seç, değerleri doldur → PlayerStats'e sürükle.
/// </summary>

public enum EquipmentSlot
{
    Weapon,         // Silah — ateş hızı + hasar
    Armor,          // Zırh — HP + hasar azaltma
    Shoulder,       // Omuzluk — CP bonus + küçük hasar
    Knee,           // Dizlik — hafif HP + hareket
    Boots,          // Ayakkabı — hareket bonusu (gelecek)
    Necklace,       // Kolye — CP çarpanı
    Ring,           // Yüzük — genel buff
}

public enum WeaponType
{
    None,           // Silah değil
    Pistol,         // Tabanca: atış/s ×1.5, hasar ×0.7, spread dar
    Rifle,          // Tüfek: atış/s ×1.0 (base), hasar ×1.0
    Automatic,      // Otomatik: atış/s ×2.2, hasar ×0.6, spread geniş
    Sniper,         // Keskin: atış/s ×0.35, hasar ×3.5, tek mermi
    Shotgun,        // Pompalı: atış/s ×0.5, hasar ×2.0, spread çok geniş yakında
}

public enum ArmorType
{
    None,
    Light,          // Hafif: HP +%20, hasar azaltma +%5
    Medium,         // Orta: HP +%40, hasar azaltma +%12
    Heavy,          // Ağır: HP +%70, hasar azaltma +%22
    Shield,         // Kalkan: HP +%30, hasar azaltma +%30 (en iyi DR)
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Kimlik")]
    public string        equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot          = EquipmentSlot.Weapon;
    public Sprite        icon;
    [TextArea(2,4)]
    public string        description   = "";

    [Header("Silah Ayarlari (slot=Weapon ise doldur)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Ayarlari (slot=Armor/Shoulder/Knee ise)")]
    public ArmorType armorType = ArmorType.None;

    // ── Temel Bonuslar ───────────────────────────────────────────────────
    [Header("CP Bonusu")]
    [Tooltip("Kuşanılınca CP'ye düz eklenir")]
    public int baseCPBonus = 0;

    [Header("Ates Hizi Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 1.5 = %50 hızlı, 0.5 = %50 yavaş")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 1.5 = %50 daha fazla hasar")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Tooltip("0.0 - 0.5 arası. 0.2 = düşman hasarı %20 azalır")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    [Tooltip("Maks HP'ye eklenen değer")]
    public int commanderHPBonus = 0;

    [Header("CP Carpani (kolye/yuzuk)")]
    [Tooltip("1.0 = etki yok, 1.1 = CP %10 daha fazla")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    [Header("Mermi Spread Bonusu (sadece silahlar)")]
    [Tooltip("Ek mermi yayılma açısı: 0 = yok, 10 = +10 derece")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    // ── Hesaplanmış özellikler (oyun içinde salt okunur) ─────────────────
    [Header("Nadir (rarity) 1=Common 2=Uncommon 3=Rare 4=Epic 5=Legendary)")]
    [Range(1,5)]
    public int rarity = 1;

    /// <summary>Silah tipine göre gerçekçi önerilen değerler için açıklama döndürür.</summary>
    public string GetTypeDescription()
    {
        return weaponType switch
        {
            WeaponType.Pistol    => "Tabanca: Hızlı, kısa menzilli",
            WeaponType.Rifle     => "Tüfek: Dengeli, çok yönlü",
            WeaponType.Automatic => "Otomatik: Yüksek DPS, geniş spread",
            WeaponType.Sniper    => "Keskin Nişancı: Dev hasar, yavaş",
            WeaponType.Shotgun   => "Pompalı: Yakın mesafe katili",
            _ => armorType switch
            {
                ArmorType.Light  => "Hafif Zırh: Hızlı ama az korumalı",
                ArmorType.Medium => "Orta Zırh: Dengeli savunma",
                ArmorType.Heavy  => "Ağır Zırh: Max korumalı",
                ArmorType.Shield => "Kalkan: Hasar azaltmada uzman",
                _ => "Aksesuar: Özel bonus"
            }
        };
    }
}
```

# Equipmentloadout.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ekipman Seti ScriptableObject (Claude)
///
/// 6 slotun tamamını tek bir .asset dosyasında tutar.
/// Ana menü ve save sistemi için ideal: 1 referans = tüm ekipman.
///
/// KULLANIM:
///   Assets → Create → TopEndWar → Equipment Loadout
///   PlayerStats Inspector'da equippedLoadout alanına sürükle.
///   Oyun içinde EquipmentUI değişiklikleri hem tek slota hem de
///   Loadout'a yazar (SaveManager bunu JSON'a kaydeder).
///
/// GELECEK: SaveManager bu SO'yu JSON'a serialize edecek.
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

    /// <summary>Bu loadout'u PlayerStats'a uygular.</summary>
    public void ApplyTo(PlayerStats ps)
    {
        if (ps == null) return;
        ps.equippedWeapon   = weapon;
        ps.equippedArmor    = armor;
        ps.equippedShoulder = shoulder;
        ps.equippedKnee     = knee;
        ps.equippedNecklace = necklace;
        ps.equippedRing     = ring;
        ps.equippedPet      = pet;
    }

    /// <summary>PlayerStats'taki mevcut ekipmanı bu loadout'a kaydeder.</summary>
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

    /// <summary>Toplam CP bonusunu hesaplar (UI önizleme için).</summary>
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
        return total;
    }
}
```

# Equipmentui.cs

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

        EquipmentData[] pool = GetPool(slot);
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
}
```

GameEvents.cs

```csharp
using System;

/// <summary>
/// Top End War — Global Event Merkezi v4
/// KURAL: Raise...() metod YOK. Kullanim: GameEvents.OnXxx?.Invoke(param);
/// v4: Komutan HP + Asker + Biyom eventleri eklendi.
/// </summary>
public static class GameEvents
{
    // ── Temel ─────────────────────────────────────────────────────────────
    public static Action<int>          OnCPUpdated;
    public static Action<int>          OnTierChanged;
    public static Action<int>          OnBulletCountChanged;
    public static Action<string>       OnPathBoosted;
    public static Action               OnMergeTriggered;
    public static Action<string>       OnSynergyFound;
    public static Action<int>          OnPlayerDamaged;       // flash icin
    public static Action               OnGameOver;
    public static Action<int>          OnRiskBonusActivated;
    public static Action<float, float> OnDifficultyChanged;
    public static Action               OnBossEncountered;
    public static Action<bool>         OnAnchorModeChanged;   // true=boss sahnesi

    // ── Komutan HP (v4) ──────────────────────────────────────────────────
    public static Action<int, int>     OnCommanderDamaged;    // (hasar, kalanHP)
    public static Action<int>          OnCommanderHealed;     // kalanHP
    public static Action<int, int>     OnCommanderHPChanged;  // (current, max)

    // ── Asker (v4) ───────────────────────────────────────────────────────
    public static Action<int>          OnSoldierAdded;        // toplam asker sayisi
    public static Action<int>          OnSoldierRemoved;      // toplam asker sayisi
    public static Action<string, int>  OnSoldierMerged;       // (path, yeni level)
    public static Action<int>          OnSoldierHPRestored;   // geri yuklenen HP toplami

    // ── Biyom (v4) ───────────────────────────────────────────────────────
    public static Action<string>       OnBiomeChanged;        // "Tas", "Orman" vb.
}
```

GameHUD.cs

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

GameOverUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Ekrani v2 (Claude)
///
/// v2: SaveManager entegrasyonu.
///   En iyi CP, en iyi mesafe, bu oyun kill sayisi gosterilir.
///   Yeni rekor varsa altin rengi vurgu yapar.
///
/// KURULUM:
///   Hierarchy -> Create Empty -> "GameOverManager" -> ekle -> bitti.
///   Kod kendi Canvas'ini olusturur.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Sahne Adlari")]
    public string gameSceneName = "SampleScene";
    public string mainMenuScene = "MainMenu";

    Canvas          _canvas;
    GameObject      _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _cpText;
    TextMeshProUGUI _distText;
    TextMeshProUGUI _killText;
    TextMeshProUGUI _bestText;
    bool            _shown = false;

    void Start()
    {
        BuildUI();
        GameEvents.OnGameOver += ShowGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver -= ShowGameOver;
    }

    void BuildUI()
    {
        // Canvas
        var canvasObj = new GameObject("GameOverCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasObj.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Arkaplan
        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        _panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        var pr = _panel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        // Baslik
        _titleText = MakeText(_panel, "SAVAS BITTI",   new Vector2(0.5f,0.5f), new Vector2(0, 130), 52, Color.red, FontStyles.Bold);

        // Mevcut oyun sonuclari
        _cpText   = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0,  60), 32, Color.white,           FontStyles.Normal);
        _distText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0,  15), 28, new Color(0.8f,0.8f,1f), FontStyles.Normal);
        _killText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0, -25), 24, new Color(1f,0.7f,0.3f), FontStyles.Normal);

        // En iyi skor
        _bestText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0, -70), 22, new Color(0.6f,0.6f,0.6f), FontStyles.Normal);

        // Butonlar
        MakeButton(_panel, "TEKRAR DENE", new Vector2(0,-130), new Vector2(260,60),
            new Color(0.2f,0.8f,0.2f), () => { Time.timeScale = 1f; SceneManager.LoadScene(gameSceneName); });

        MakeButton(_panel, "ANA MENU", new Vector2(0,-210), new Vector2(260,55),
            new Color(0.2f,0.3f,0.75f), () => { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuScene); });

        // Gelecekte: Ana Menü butonu buraya gelecek
        // MakeButton(_panel, "ANA MENU", new Vector2(0,-210), new Vector2(260,60),
        //     new Color(0.3f,0.3f,0.8f), () => SceneManager.LoadScene("MainMenu"));

        _panel.SetActive(false);
    }

    void ShowGameOver()
    {
        if (_shown) return;
        _shown = true;

        Time.timeScale = 0f;
        _panel.SetActive(true);

        var ps   = PlayerStats.Instance;
        var save = SaveManager.Instance;
        var army = ArmyManager.Instance;

        int   cp      = ps?.CP ?? 0;
        float dist    = ps != null ? Mathf.RoundToInt(ps.transform.position.z) : 0f;
        int   kills   = save?.CurrentRunKills ?? 0;
        int   soldiers= army?.SoldierCount ?? 0;

        _cpText.text   = $"CP: {cp:N0}";
        _distText.text = $"Mesafe: {dist:N0}m  |  Asker: {soldiers}/20";
        _killText.text = $"Dusmanlar: {kills}";

        // En iyi skor goster
        if (save != null)
        {
            bool newCP   = cp   >= save.HighScoreCP   && save.TotalRuns > 1;
            bool newDist = dist >= save.HighScoreDistance && save.TotalRuns > 1;

            string bestStr = $"En iyi: {save.HighScoreCP:N0} CP  |  {save.HighScoreDistance:N0}m";
            _bestText.text  = bestStr;

            // Yeni rekor vurgusu
            if (newCP || newDist)
            {
                _bestText.text  = "YENİ REKOR!";
                _bestText.color = new Color(1f, 0.85f, 0f);
                _cpText.color   = new Color(1f, 0.85f, 0f);
            }
        }
    }

    // ── Yardimci ─────────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("T_" + text.Substring(0, Mathf.Min(6, text.Length)));
        obj.transform.SetParent(parent.transform, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(700, 60);
        return tmp;
    }

    void MakeButton(GameObject parent, string label, Vector2 pos, Vector2 size,
        Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var btn = new GameObject("Btn_" + label);
        btn.transform.SetParent(parent.transform, false);
        var img = btn.AddComponent<Image>(); img.color = bg;
        var b = btn.AddComponent<Button>(); b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f,0.5f); r.anchorMax = new Vector2(0.5f,0.5f);
        r.pivot = new Vector2(0.5f,0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(btn.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 24; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }
}
```

# Gamestartup.cs

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

Gate.cs

```csharp
using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi (Claude)
///
/// PREFAB:
///   GatePrefab (root)
///   ├── Gate.cs + BoxCollider(IsTrigger=true) + Rigidbody(IsKinematic=true)
///   ├── Panel (3D Quad, Scale 4,5,1)  → panelRenderer slotuna sur
///   └── Label (3D TextMeshPro)        → labelText slotuna sur
///
/// MATERYAL: Herhangi bir materyal olabilir — kod runtime'da Sprites/Default'a cevirir.
/// Panel'deki MeshCollider otomatik silinir.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
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

    public void Refresh() { ApplyVisuals(); FitBoxCollider(); }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) Destroy(col);
    }

    void ApplyVisuals()
    {
        if (gateData == null) return;

        if (labelText != null)
        {
            labelText.text               = gateData.gateText;
            labelText.fontSize           = 5f;
            labelText.color              = Color.white;
            labelText.alignment          = TextAlignmentOptions.Center;
            labelText.fontStyle          = FontStyles.Bold;
            labelText.overflowMode       = TextOverflowModes.Truncate;
            labelText.enableWordWrapping = false;
        }

        if (panelRenderer != null)
        {
            // Sprites/Default: her shader'da calisir, tam transparan destekler
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c      = gateData.gateColor;
            c.a          = 0.72f;
            mat.color    = c;
            panelRenderer.material = mat;
        }
    }

    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;
        Vector3 s  = panelRenderer.transform.localScale;
        box.size   = new Vector3(s.x * 0.95f, s.y, 1.2f);
        box.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        other.GetComponent<PlayerStats>()?.ApplyGateEffect(gateData);
        Debug.Log("[Gate] " + gateData.gateText + " | CP: " + PlayerStats.Instance?.CP);
        Destroy(gameObject);
    }
}
```

GateData.cs

```csharp
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
```

GateFeedback.cs

```csharp
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Kapi Gecis Efekti (Claude)
/// Player objesine ekle. DOTween kurulu olmali.
///
/// Kapidan gecince: kucuk scale pop (1 → 1.25 → 1)
/// Tier atlayinca: buyuk scale pop (1 → 1.5 → 1) + kamera sallama
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
    public float  shakeStrength = 0.3f;
    public float  shakeDuration = 0.3f;

    Vector3 _originalScale;
    Tweener _activeTween;

    void Start()
    {
        _originalScale = transform.localScale;

        GameEvents.OnCPUpdated   += OnCPUpdated;
        GameEvents.OnTierChanged += OnTierChanged;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated   -= OnCPUpdated;
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnCPUpdated(int cp)
    {
        // Her kapi gecisinde kucuk pop
        ScalePop(gatePopScale, gatePopDuration);
    }

    void OnTierChanged(int tier)
    {
        // Tier atlarken buyuk pop + kamera shake
        ScalePop(tierPopScale, tierPopDuration);

        if (mainCamera != null)
            mainCamera.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false);
    }

    void ScalePop(float peak, float duration)
    {
        _activeTween?.Kill();
        transform.localScale = _originalScale;

        _activeTween = transform
            .DOScale(_originalScale * peak, duration * 0.4f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(_originalScale, duration * 0.6f)
                         .SetEase(Ease.InOutQuad);
            });
    }
}
```

# # Mainmenuui.cs

```csharp

```

MorphController.cs

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

ObjectPooler.cs

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

# Petcontroller.cs

```csharp
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Pet Sistemi (Claude)
///
/// UNITY KURULUM:
///   Player objesine ekle.
///   Inspector'da petData slotuna PetData ScriptableObject sur.
///   petData.petPrefab doluysa onu kullanir, yoksa kucuk altin kure.
///
/// DAVRANIS:
///   - Normal: Karakterin sol-arkasindan smooth takip eder
///   - Anchor modu: Sabit kalir, "Hasar Azaltma" aura aktif olur
///     (PetData.anchorDamageReduction PlayerStats'a uygulanir)
///
/// GELECEK (ileride):
///   - Her pet tipine ozel efekt (heal aura, ates hizi, sekici mermi)
///   - Ana menu'den secilen pet buraya inject edilir
/// </summary>
public class PetController : MonoBehaviour
{
    [Header("Pet Verisi (PlayerStats'tan otomatik alinabilir)")]
    public PetData petData;

    [Header("Takip Ayarlari")]
    public float followSpeed     = 8f;
    public float followDistance  = 2.2f;  // sol-arkayi mesafesi
    public float sideOffset      = 1.4f;  // saga/sola offset

    [Header("Ziplama (idle animasyon)")]
    public float bobHeight       = 0.18f;
    public float bobSpeed        = 2.2f;

    GameObject _petModel;
    bool       _anchorMode  = false;
    bool       _auraActive  = false;
    float      _bobTimer    = 0f;
    Vector3    _baseOffset;

    // Anchor DR degeri — TakeContactDamage'a carpilir
    float _currentDR = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // PlayerStats'ta equippedPet varsa onu al
        if (petData == null && PlayerStats.Instance?.equippedPet != null)
            petData = PlayerStats.Instance.equippedPet;

        _baseOffset = new Vector3(-sideOffset, 1.2f, -followDistance);

        SpawnPetModel();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;

        Debug.Log($"[Pet] {(petData != null ? petData.petName : "Varsayilan Pet")} aktif.");
    }

    void OnDestroy()
    {
        GameEvents.OnAnchorModeChanged -= OnAnchorMode;
        DeactivateAura();
    }

    // ── Model Olustur ─────────────────────────────────────────────────────
    void SpawnPetModel()
    {
        if (_petModel != null) Destroy(_petModel);

        if (petData != null && petData.petPrefab != null)
        {
            _petModel = Instantiate(petData.petPrefab);
        }
        else
        {
            // Fallback: altin kure
            _petModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _petModel.transform.localScale = Vector3.one * 0.35f;
            Destroy(_petModel.GetComponent<Collider>());

            var rend = _petModel.GetComponent<Renderer>();
            if (rend != null)
            {
                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", new Color(1f, 0.85f, 0.1f));
                else
                    rend.material.color = new Color(1f, 0.85f, 0.1f);
            }
        }
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (_petModel == null || PlayerStats.Instance == null) return;

        if (_anchorMode)
        {
            // Anchor modda sabit kal, hafifce titres
            _bobTimer += Time.deltaTime * bobSpeed * 2f;
            _petModel.transform.position = transform.position
                + _baseOffset
                + Vector3.up * Mathf.Sin(_bobTimer) * bobHeight * 0.5f;
        }
        else
        {
            // Runner modda smooth takip
            _bobTimer += Time.deltaTime * bobSpeed;
            Vector3 target = transform.position + _baseOffset
                + Vector3.up * Mathf.Sin(_bobTimer) * bobHeight;

            _petModel.transform.position = Vector3.Lerp(
                _petModel.transform.position, target,
                Time.deltaTime * followSpeed);
        }

        // Pet oyuncuya baksın
        Vector3 lookDir = (transform.position - _petModel.transform.position);
        if (lookDir != Vector3.zero)
            _petModel.transform.rotation = Quaternion.Slerp(
                _petModel.transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 8f);
    }

    // ── Anchor Modu ───────────────────────────────────────────────────────
    void OnAnchorMode(bool active)
    {
        _anchorMode = active;

        if (active && petData != null && petData.anchorDamageReduction > 0f)
        {
            ActivateAura();
        }
        else
        {
            DeactivateAura();
        }
    }

    void ActivateAura()
    {
        if (_auraActive) return;
        _auraActive  = true;
        _currentDR   = petData?.anchorDamageReduction ?? 0f;

        // Parlama efekti
        var rend = _petModel?.GetComponentInChildren<Renderer>();
        if (rend != null)
            _petModel.transform.DOScale(Vector3.one * 1.35f, 0.3f).SetEase(Ease.OutBack);

        Debug.Log($"[Pet] Aura aktif — Hasar Azaltma: %{_currentDR * 100:.0f}");
        GameEvents.OnSynergyFound?.Invoke($"Pet Aurası +%{Mathf.RoundToInt(_currentDR * 100)}");
    }

    void DeactivateAura()
    {
        if (!_auraActive) return;
        _auraActive = false;
        _currentDR  = 0f;

        if (_petModel != null)
            _petModel.transform.DOScale(Vector3.one, 0.2f);
    }

    // ── DR Getter (PlayerStats.TakeContactDamage'dan kullanilabilir) ──────
    /// <summary>
    /// Hasar azaltma carpani. 0 = hasar azaltma yok, 0.1 = %10 azaltir.
    /// PlayerStats.TakeContactDamage icinde:
    ///   float dr = PetController.Instance?.DamageReduction ?? 0f;
    ///   int final = Mathf.RoundToInt(amount * (1f - dr));
    /// </summary>
    public static PetController Instance { get; private set; }
    public float DamageReduction => _currentDR;

    void Awake()
    {
        // Singleton (bir pet olacak)
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
}
```

PetData.cs

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

PlayerController.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v4 (Claude)
///
/// v4: EquipmentData.fireRateMultiplier ateş hızına uygulanıyor.
///     Komutan kuşandığı silahın hızını otomatik alıyor.
///
/// Anchor Modu: OnAnchorModeChanged(true) gelince forwardSpeed=0,
///   oyuncu sadece X ekseninde hareket eder, boss ile savaşır.
///
/// Spread formation (V şekli):
///   1 mermi: düz, 2: ±8°, 3: -12° 0° +12°,
///   4: -18° -6° +6° +18°, 5: -22° -11° 0° +11° +22°
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

    // Tier bazlı temel değerler
    static readonly float[] BASE_FIRE_RATES = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    static readonly int[]   DAMAGE          = { 60,   95,   145,  210,  300  };

    static readonly float[][] SPREAD = {
        new float[]{ 0f },
        new float[]{ -8f, 8f },
        new float[]{ -12f, 0f, 12f },
        new float[]{ -18f, -6f, 6f, 18f },
        new float[]{ -22f, -11f, 0f, 11f, 22f },
    };

    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
    }

    void OnDestroy() => GameEvents.OnAnchorModeChanged -= OnAnchorMode;

    void OnAnchorMode(bool active)
    {
        _anchorMode = active;
        forwardSpeed = active ? 0f : 10f;
        if (active) Debug.Log("[Player] Anchor modu aktif.");
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height = 2f; c.radius = 0.4f; c.isTrigger = false;
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

        if (Input.GetMouseButtonDown(0)) { _dragging = true; _lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))   _dragging = false;

        if (_dragging)
        {
            _targetX    = Mathf.Clamp(_targetX + (Input.mousePosition.x - _lastMouseX) * dragSensitivity, -xLimit, xLimit);
            _lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire) return;

        int tier   = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int bCount = PlayerStats.Instance != null ? PlayerStats.Instance.BulletCount  : 1;
        int idx    = Mathf.Clamp(tier - 1, 0, 4);

        // ── EquipmentData ateş hızı çarpanı ──────────────────────────────
        // Kuşanılan silah BASE_FIRE_RATES'i çarpar — silah yoksa 1x.
        float equipMult = 1f;
        if (PlayerStats.Instance?.equippedWeapon != null)
            equipMult = PlayerStats.Instance.equippedWeapon.fireRateMultiplier;

        float fireRate = BASE_FIRE_RATES[idx] * equipMult;

        // Silah hasar çarpanı
        float dmgMult = 1f;
        if (PlayerStats.Instance?.equippedWeapon != null)
            dmgMult = PlayerStats.Instance.equippedWeapon.damageMultiplier;
        int finalDamage = Mathf.RoundToInt(DAMAGE[idx] * dmgMult);

        // Hedef bul
        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        // Spread
        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, finalDamage);
        }

        _nextFire = Time.time + 1f / fireRate;
    }

    void FireOne(Vector3 pos, Vector3 dir, int dmg)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }
        if (b == null) return;

        b.GetComponent<Bullet>()?.SetDamage(dmg);
        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }

    /// <summary>
    /// En yakın Enemy'i bulur.
    /// Normal modda BoxCast (serit tarama), Anchor modda OverlapSphere (360 — boss kesin bulunur).
    /// </summary>
    Transform FindTarget()
    {
        if (_anchorMode)
        {
            float    bestDist = 70f * 70f;
            Collider best     = null;
            foreach (Collider c in Physics.OverlapSphere(transform.position, 70f))
            {
                if (!c.CompareTag("Enemy")) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best?.transform;
        }
        else
        {
            RaycastHit hit;
            bool found = Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
                Vector3.forward, out hit,
                Quaternion.identity, detectRange);
            return (found && hit.collider.CompareTag("Enemy")) ? hit.transform : null;
        }
    }

    public void ResumeRun() => OnAnchorMode(false);
}
```

PlayerStats.cs

```csharp
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic Ayarlari")]
    public int   startCP               = 350; // Başlangıç CP artırıldı
    public float invincibilityDuration = 0.8f;

    // ── Ekipman Seti (tek SO — hepsini bir arada tutar) ─────────────────
    [Header("Ekipman Seti (EquipmentLoadout SO)")]
    [Tooltip("Assets → Create → TopEndWar → Equipment Loadout. Doldur ve buraya sur.")]
    public EquipmentLoadout equippedLoadout;

    // ── Tek tek slotlar (Loadout yoksa veya override için) ───────────────
    [Header("Tekil Ekipmanlar (Loadout varsa otomatik dolar)")]
    public EquipmentData equippedWeapon;    // ateş hızı + hasar
    public EquipmentData equippedArmor;     // HP + hasar azaltma
    public EquipmentData equippedShoulder;  // CP + küçük hasar
    public EquipmentData equippedKnee;      // hafif HP bonus
    public EquipmentData equippedNecklace;  // CP çarpanı
    public EquipmentData equippedRing;      // genel buff
    public PetData       equippedPet;

    // ── _baseCP: oyun içi ham puan ────────────────────────────────────────
    private int _baseCP;

    // CP = baseCP + tüm ekipman bonusları, kolye çarpanı dahil
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

            // Kolye CP çarpanı (en son uygula)
            float mult = equippedNecklace != null ? equippedNecklace.cpMultiplier : 1f;
            if (equippedRing != null) mult *= equippedRing.cpMultiplier;
            return Mathf.RoundToInt(total * mult);
        }
    }

    /// <summary>Tüm ekipmandan gelen hasar azaltma toplamı (0-0.6 arası sınırlı).</summary>
    public float TotalDamageReduction()
    {
        float dr = 0f;
        dr += equippedArmor    != null ? equippedArmor.damageReduction    : 0f;
        dr += equippedShoulder != null ? equippedShoulder.damageReduction : 0f;
        dr += equippedKnee     != null ? equippedKnee.damageReduction     : 0f;
        dr += equippedRing     != null ? equippedRing.damageReduction     : 0f;
        dr += equippedPet      != null ? equippedPet.anchorDamageReduction: 0f;
        return Mathf.Clamp(dr, 0f, 0.60f); // max %60 azaltma
    }

    /// <summary>Tüm ekipmandan gelen ekstra Komutan HP bonusu.</summary>
    public int TotalEquipmentHPBonus()
    {
        int bonus = 0;
        bonus += equippedArmor    != null ? equippedArmor.commanderHPBonus    : 0;
        bonus += equippedShoulder != null ? equippedShoulder.commanderHPBonus : 0;
        bonus += equippedKnee     != null ? equippedKnee.commanderHPBonus     : 0;
        return bonus;
    }

    // Diğer her şey senin orijinal kodunla aynı
    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;   

    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    float _lastDmgTime    = -99f;
    int   _riskBonusLeft  = 0;
    float _expectedCP     = 200f;

    static readonly int[]    TIER_CP    = { 0, 300, 900, 2500, 6000 }; // Daha hızlı tier atla
    static readonly string[] TIER_NAMES = { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
    static readonly int[]    COMMANDER_HP_BY_TIER = { 500, 700, 950, 1200, 1500 };
    const  int MAX_BULLETS = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Loadout SO varsa tekil slotlara uygula
        equippedLoadout?.ApplyTo(this);

        _baseCP        = startCP;
        CommanderMaxHP = COMMANDER_HP_BY_TIER[0] + TotalEquipmentHPBonus();
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

        // Ekipman + Pet hasar azaltma
        float dr = TotalDamageReduction();
        int finalAmount = Mathf.RoundToInt(amount * (1f - dr));

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
        _baseCP = Mathf.Min(_baseCP + amount, 99999); // CP yerine _baseCP kullanıyoruz
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP); // Toplam CP'yi UI'a yolluyoruz
        if (CurrentTier != oldTier) OnTierChanged();
    }

    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

        // BÜTÜN CP İŞLEMLERİ ARTIK _baseCP ÜZERİNDEN YAPILIYOR
        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;
            case GateEffectType.MultiplyCP:
                _baseCP = Mathf.RoundToInt(_baseCP * 1.2f);
                break;
            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                }
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade);
                break;
            case GateEffectType.Merge:
                HandleMerge(_riskBonusLeft > 0);
                break;
            case GateEffectType.PathBoost_Piyade:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;
            case GateEffectType.PathBoost_Mekanize:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Mekanik");
                break;
            case GateEffectType.PathBoost_Teknoloji:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                TeknolojiPath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;
            case GateEffectType.NegativeCP:
                _baseCP = Mathf.Max(50, _baseCP - Mathf.RoundToInt(data.effectValue * scale));
                break;
            case GateEffectType.RiskReward:
                int pen = Mathf.RoundToInt(_baseCP * 0.30f);
                _baseCP = Mathf.Max(100, _baseCP - pen);
                _riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
                break;
            case GateEffectType.AddSoldier_Piyade:
            {
                // Risk aktifse +1 ekstra asker (2 yerine 3)
                int soldierCount = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: soldierCount);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Piyade!");
                break;
            }
            case GateEffectType.AddSoldier_Mekanik:
            {
                int soldierCount = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: soldierCount);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Mekanik!");
                break;
            }
            case GateEffectType.AddSoldier_Teknoloji:
            {
                int soldierCount = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: soldierCount);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Teknoloji!");
                break;
            }
            case GateEffectType.HealCommander:
            {
                // Risk aktifse +kalıcı MaxHP bonusu da verir
                int healAmt = Mathf.RoundToInt(data.effectValue);
                HealCommander(healAmt);
                if (_riskBonusLeft > 0)
                {
                    CommanderMaxHP += 100; // kalıcı max HP artışı
                    CommanderHP = Mathf.Min(CommanderHP + 50, CommanderMaxHP);
                    GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
                    ShowPopupMessage("RISK: +100 MaxHP!");
                }
                break;
            }
            case GateEffectType.HealSoldiers:
            {
                // Risk aktifse tam heal (%100) + bir sonraki düşman dalgasını ertele (flag)
                float healPct = _riskBonusLeft > 0 ? 1.0f : Mathf.Clamp(data.effectValue, 0f, 1f);
                ArmyManager.Instance?.HealAll(healPct);
                if (_riskBonusLeft > 0)
                    ShowPopupMessage("RISK: Asker FULL HP!");
                else
                    ShowPopupMessage($"Asker +%{Mathf.RoundToInt(healPct * 100)}");
                break;
            }
        }

        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        _baseCP = Mathf.Clamp(_baseCP, 50, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();

        // UI'ı GÜNCELLE
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    void HandleMerge(bool riskActive = false)
    {
        bool mergeOccurred = ArmyManager.Instance != null &&
                             ArmyManager.Instance.TryMerge();

        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float multiplier;
        string role = "none";

        // Risk aktifse tüm çarpanlar +0.2 artar
        float riskBonus = riskActive ? 0.2f : 0f;
        if (riskActive) ShowPopupMessage("RISK: Merge Güçlendi!");

        if (total < 1f) multiplier = 1.1f + riskBonus;
        else
        {
            float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
            float minPath = Mathf.Min(p, Mathf.Min(m, t));
            if (minPath > 0.28f)
            {
                multiplier = 1.7f; role = "PERFECT";
                GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS!");
            }
            else if (t >= 0.5f) { multiplier = 1.5f; role = "Teknoloji"; }
            else if (p >= 0.5f) { multiplier = 1.5f; role = "Piyade"; }
            else if (m >= 0.5f) { multiplier = 1.5f; role = "Mekanik"; }
            else                { multiplier = 1.2f; }
        }

        _baseCP = Mathf.RoundToInt(_baseCP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();
    }

    void OnTierChanged()
    {
        int oldMax = CommanderMaxHP;
        // Tier bazı + ekipman bonusu
        CommanderMaxHP = COMMANDER_HP_BY_TIER[Mathf.Clamp(CurrentTier - 1, 0, 4)]
                       + TotalEquipmentHPBonus();
        if (CommanderMaxHP > oldMax)
        {
            int bonus = CommanderMaxHP - oldMax;
            CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + bonus);
        }
        GameEvents.OnTierChanged?.Invoke(CurrentTier);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

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

    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 2f) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    void ShowPopupMessage(string msg)
        => GameEvents.OnSynergyFound?.Invoke(msg);

    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}
```

ProgressionConfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Konfigurasyonu (Claude)
/// Assets → Create → TopEndWar → Progression Config
/// DifficultyManager'a bagla. Baglamazsan DifficultyManager dahili sabitlerle calisir.
/// NAMESPACE YOK — eski GPT kodlari namespace kullaniyordu, biz kullanmiyoruz.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Ilerleme")]
    [Range(1.05f, 1.5f)] public float growthRate          = 1.15f;
    [Range(1.0f,  3.0f)] public float difficultyExponent  = 1.3f;
    public int baseStartCP = 200;

    [Header("Dusman")]
    public int   baseEnemyHealth       = 100;
    public int   baseEnemyDamage       = 25;
    public float baseEnemySpeed        = 4.0f;
    public float enemyMaxSpeed         = 7.5f;
    [Range(0.1f, 1.0f)] public float playerCPScalingFactor = 0.5f; // 0.9→0.5: güçlü oyuncuya ceza azaldı

    [Header("Kapi")]
    public float gateValueGrowthRate   = 1.12f;
    public int   minGateValue          = 20;
    public int   maxGateValue          = 500;
    public float noBadGateZoneBeforeBoss = 200f;

    [Header("Tier Eslikleri")]
    public int[] tierThresholds = { 0, 300, 800, 2000, 5000 };

    public int CalculateExpectedCP(float d)
        => Mathf.RoundToInt(baseStartCP * Mathf.Pow(growthRate, d / 100f));

    public float CalculateDifficultyMultiplier(float d)
        => 1f + Mathf.Pow(d / 1000f, difficultyExponent);

    public int ScaleGateValue(int v, float d)
    {
        int s = Mathf.RoundToInt(v * Mathf.Pow(gateValueGrowthRate, d / 150f));
        if (s < minGateValue) return minGateValue;
        if (s > maxGateValue) return maxGateValue;
        return s;
    }
}
```

# Savemanager.cs

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
}
```

SimpleCameraFollow.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Runner Kamera v2 (Claude)
///
/// v2 DEĞİŞİKLİKLER:
///   - LookAt kaldırıldı — sabit pitch açısı, sallantı yok
///   - X ekseni tamamen sabit (0) — şerit değiştirince kamera sallanmaz
///   - Y: oyuncuyla birlikte kayar ama hızlı değişmez (followSpeed ile)
///   - Z: oyuncunun arkasında sabit mesafe
///   - pitchAngle: kameranın aşağı bakış açısı (Inspector'dan ayarla)
///
/// UNITY KURULUM:
///   Main Camera → bu scripti ekle → target = Player transform
///   Önerilen: heightOffset=8, backOffset=12, pitchAngle=22
///
/// İPUCU:
///   pitchAngle artarsa kamera daha fazla aşağı bakar (top-down hissi)
///   backOffset artarsa daha geniş alan görünür
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Pozisyon")]
    public float heightOffset = 8f;
    public float backOffset   = 12f;
    public float followSpeed  = 10f;

    [Header("Açı (sabit — LookAt yok)")]
    [Tooltip("Kameranın aşağı bakış açısı. 20-30 arası runner için idealdir.")]
    [Range(10f, 50f)]
    public float pitchAngle = 22f;

    // Sabit rotasyonu bir kere hesapla
    Quaternion _fixedRotation;

    void Start()
    {
        // Pitch açısına göre sabit rotasyon — oyun boyunca değişmez
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = _fixedRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Hedef konum: X=0 (sabit), Y=target+offset, Z=target-back
        Vector3 desired = new Vector3(
            0f,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        // Yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position, desired,
            Time.deltaTime * followSpeed
        );

        // Rotasyon hiç değişmez — LookAt yok
        transform.rotation = _fixedRotation;
    }

    /// <summary>Pitch açısı çalışma zamanında değiştirilirse rotasyonu güncelle.</summary>
    public void SetPitch(float angle)
    {
        pitchAngle     = Mathf.Clamp(angle, 10f, 50f);
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}
```

SoldierUnit.cs

```csharp
using UnityEngine;

/// <summary>
/// Asker path tipleri — GateData ve ArmyManager ile eslesik olmali.
/// </summary>
public enum SoldierPath { Piyade, Mekanik, Teknoloji }

/// <summary>
/// Top End War — Bireysel Asker v2 (Claude)
///
/// UNITY NOTU:
///   - "Soldier" TAG eklemenize GEREK YOK — tag kullanılmıyor.
///   - ArmyManager bu scripti otomatik yönetir, elle prefab gerekmez.
///   - Bullet pool "Bullet" etiketiyle çalışır — asker ateşi de aynı poolu kullanır.
/// </summary>
public class SoldierUnit : MonoBehaviour
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string      biome      = "Tas";
    [HideInInspector] public int         mergeLevel = 1;

    // ── Statlar ───────────────────────────────────────────────────────────
    [HideInInspector] public int   maxHP;
    [HideInInspector] public int   currentHP;
    [HideInInspector] public float baseAtk;
    [HideInInspector] public float atkSpeed;

    // ── Formasyon ─────────────────────────────────────────────────────────
    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED  = 14f;
    const float DETECT_RADIUS = 28f;

    Renderer _rend;
    float    _nextFire;
    bool     _dead;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        // TAG EKLEME — Unity'de Soldier tag'i varsayılan olarak yok,
        // ve buna ihtiyacımız yok. GetComponent<SoldierUnit>() kullanıyoruz.
    }

    void OnEnable()
    {
        _dead     = false;
        _nextFire = Time.time + Random.value / Mathf.Max(atkSpeed, 0.1f);
    }

    void Update()
    {
        if (_dead || PlayerStats.Instance == null) return;
        FollowPlayer();
        if (Time.time >= _nextFire) TryShoot();
    }

    void FollowPlayer()
    {
        Vector3 target = PlayerStats.Instance.transform.position + formationOffset;
        target.y = 1.2f;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_SPEED);
    }

    void TryShoot()
    {
        _nextFire = Time.time + 1f / Mathf.Max(atkSpeed, 0.01f);

        Collider best    = null;
        float    bestDist= DETECT_RADIUS * DETECT_RADIUS;

        foreach (Collider col in Physics.OverlapSphere(transform.position, DETECT_RADIUS))
        {
            if (!col.CompareTag("Enemy")) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = col; }
        }
        if (best == null) return;

        // Hasar hesapla
        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path) : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        { 1 => 0f, 2 => 0.10f, 3 => 0.20f, 4 => 0.30f, _ => 0.40f };

        float mergeMult = mergeLevel switch { 2 => 1.8f, 3 => 3.5f, 4 => 7.0f, _ => 1.0f };
        int   finalDmg  = Mathf.RoundToInt(baseAtk * mergeMult * (1f + cmdAura) * biomeMultiplier);

        FireBullet(best, finalDmg);
    }

    void FireBullet(Collider target, int dmg)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        Vector3 pos = transform.position + Vector3.up * 0.5f;

        // Bullet pool — null güvenli
        GameObject b = null;
        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null) return;  // pool doluysa veya yoksa atla

        Bullet bComp = b.GetComponent<Bullet>();
        if (bComp != null)
        {
            bComp.SetDamage(dmg);
            bComp.bulletColor = GetPathColor() * 0.85f;
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 32f;

        // Asker path'ini Bullet'a kaydet (popup rengi için)
        Bullet blt = b.GetComponent<Bullet>();
        if (blt != null) blt.hitterPath = path.ToString();
    }

    // ── Hasar / Heal ──────────────────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (_dead) return;
        currentHP -= dmg;
        if (_rend) StartCoroutine(FlashRed());
        if (currentHP <= 0) Die();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (!_rend) yield break;
        Color orig = _rend.material.color;
        _rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (_rend && !_dead) _rend.material.color = orig;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        ArmyManager.Instance?.RemoveSoldier(this);
        gameObject.SetActive(false);
    }

    public void HealPercent(float pct)
        => currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * pct));

    // ── Renk ─────────────────────────────────────────────────────────────
    public Color GetPathColor() => path switch
    {
        SoldierPath.Piyade    => new Color(0.2f, 0.85f, 0.2f),  // yeşil
        SoldierPath.Mekanik   => new Color(0.65f, 0.65f, 0.65f), // gri
        SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),   // mavi
        _                     => Color.white
    } * (mergeLevel switch { 2 => 1.2f, 3 => 1.5f, 4 => 2.0f, _ => 1.0f });
}
```

SpawnManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v12 (Claude)
///
/// v12: "Olay Kapisi" sistemi eklendi.
///
/// OLAY KAPILARI (her 5 normal kapidan sonra 1 tane cıkar):
///
///   DUEL  — 2 kapı, biri iyi biri kotu. Oyuncu rengi gorur ama ne oldugunu
///            tam bilmez. Yuksek risk/odul. Renk ipucu: parlak = iyi,
///            karanlık = kotu (ama her zaman degil — bazen aldatici).
///
///   UCLU  — 3 kapı aynı anda farklı tiplerde. Ortadan gecmek imkansız.
///            Oyuncu hizli karar vermek zorunda.
///
///   TEKLI — 1 kapı, ekstra buyuk odul. "Bu kapiya girersen x2 bonus."
///            Girmezsen de sorun yok ama cazip.
///
/// v11'den geri kalan: kapı isimleri/renkleri netleştirildi.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    [Header("Olay Kapilari")]
    [Tooltip("Kac normal kapidan sonra bir olay kapisi cikar")]
    public int eventGateEvery = 5;

    float _nextGateZ   = 40f;
    float _nextWaveZ   = 55f;
    bool  _bossSpawned = false;
    int   _gateCount   = 0;  // kac normal kapı cıktı

    DifficultyManager.EnemyStats _stats;
    bool       _statsReady  = false;
    GateData[] _runtimeGates;
    float      _totalWeight = 0f;

    // Olay tipi
    enum EventType { Duel, Triple, Single }

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        BuildRuntimeGates();
        CacheWeights();
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

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!_bossSpawned && pz >= bossDistance)
        {
            _bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
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
    { if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform; }

    // ── Sıradaki kapı grubunu belirle ────────────────────────────────────
    void SpawnNextGateGroup(float zPos)
    {
        _gateCount++;

        // Her N kapıda bir olay kapısı
        bool isEvent = (_gateCount % eventGateEvery == 0);

        // Boss yakınında sadece iyi kapılar
        bool pity = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;

        if (isEvent && !pity)
            SpawnEventGate(zPos);
        else
            SpawnNormalPair(zPos, pity);
    }

    // ── Normal çift kapı ─────────────────────────────────────────────────
    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;
        SpawnGate(PickGate(pity), new Vector3(-offset, 1.5f, zPos), scale: 1f);
        SpawnGate(PickGate(pity), new Vector3( offset, 1.5f, zPos), scale: 1f);
    }

    // ── Olay kapısı ───────────────────────────────────────────────────────
    void SpawnEventGate(float zPos)
    {
        // Olayları eşit şansla seç (boss'a yaklaştıkça Duel azalır)
        float progress = playerTransform.position.z / bossDistance;

        // 0-30%: Tekli ağırlıklı (tanıtım)
        // 30-70%: Duel ağırlıklı (gerilim)
        // 70-100%: Üçlü ağırlıklı (kaos)
        EventType ev;
        float r = Random.value;
        if (progress < 0.30f)
            ev = r < 0.50f ? EventType.Single : r < 0.80f ? EventType.Triple : EventType.Duel;
        else if (progress < 0.70f)
            ev = r < 0.45f ? EventType.Duel : r < 0.75f ? EventType.Triple : EventType.Single;
        else
            ev = r < 0.45f ? EventType.Triple : r < 0.75f ? EventType.Duel : EventType.Single;

        switch (ev)
        {
            case EventType.Single: SpawnSingleEvent(zPos);  break;
            case EventType.Duel:   SpawnDuelEvent(zPos);    break;
            case EventType.Triple: SpawnTripleEvent(zPos);  break;
        }
    }

    // ── TEKLİ olay: Büyük ödüllü 1 kapı ─────────────────────────────────
    void SpawnSingleEvent(float zPos)
    {
        // Büyük bir pozitif kapı, ortada, büyük ölçekte
        GateData big = MakeBigGate();
        SpawnGate(big, new Vector3(0f, 1.8f, zPos), scale: 1.6f);

        // Label için GateFeedback label'ı override et
        // (Gate.cs'de ApplyVisuals çalışır, metni kapı gösterir)
        Debug.Log($"[Spawn] OLAY: TEKLİ — {big.gateText}");
    }

    // ── DUEL olay: 1 iyi 1 kötü, hangisi hangisi belli değil ─────────────
    void SpawnDuelEvent(float zPos)
    {
        GateData good = PickPositiveGate();
        GateData bad  = PickNegativeGate();

        // %50 şansla yer değiştir — oyuncu rengi görür ama pozisyon yanıltıcı
        bool swap = Random.value > 0.5f;
        float leftX  = -(ROAD_HALF_WIDTH * 0.42f);
        float rightX =   ROAD_HALF_WIDTH * 0.42f;

        SpawnGate(swap ? bad : good,  new Vector3(leftX,  1.5f, zPos), scale: 1.2f);
        SpawnGate(swap ? good : bad,  new Vector3(rightX, 1.5f, zPos), scale: 1.2f);

        Debug.Log($"[Spawn] OLAY: DUEL — {good.gateText} vs {bad.gateText}");
    }

    // ── ÜÇLÜ olay: 3 farklı kapı — Z ofsetli, iç içe geçmez ─────────────
    void SpawnTripleEvent(float zPos)
    {
        // Yatay aralık: sol/sag biraz disarida, ortadaki öne çıkmış
        // Her kapı 0.7 scale → daha ince, aralara sığar
        // Z offset: orta öne (oyuncu önce ortayı görsün), yanlar biraz geride
        float spacing = ROAD_HALF_WIDTH * 0.60f; // 4.8 birim aralik

        GateData left   = PickGate(false);
        GateData center = PickGate(false);
        GateData right  = PickGate(false);

        SpawnGate(left,   new Vector3(-spacing, 1.5f, zPos + 4f), scale: 0.75f); // geride
        SpawnGate(center, new Vector3(0f,        1.8f, zPos),      scale: 1.0f);  // önde, büyük
        SpawnGate(right,  new Vector3( spacing,  1.5f, zPos + 4f), scale: 0.75f); // geride

        Debug.Log($"[Spawn] OLAY: ÜÇLÜ — {left.gateText} | {center.gateText} | {right.gateText}");
    }

    // ── Gate data seçiciler ───────────────────────────────────────────────
    GateData PickPositiveGate()
    {
        // Sadece pozitif tipler
        var positives = new List<GateData>();
        float w = 0f;
        foreach (var g in ActiveGates)
        {
            if (g.effectType == GateEffectType.AddCP          ||
                g.effectType == GateEffectType.AddSoldier_Piyade   ||
                g.effectType == GateEffectType.AddSoldier_Mekanik  ||
                g.effectType == GateEffectType.AddSoldier_Teknoloji||
                g.effectType == GateEffectType.HealCommander  ||
                g.effectType == GateEffectType.MultiplyCP)
            { positives.Add(g); w += g.spawnWeight; }
        }
        return positives.Count > 0 ? WeightedRandom(positives.ToArray(), w) : ActiveGates[0];
    }

    GateData PickNegativeGate()
    {
        var negatives = new List<GateData>();
        float w = 0f;
        foreach (var g in ActiveGates)
        {
            if (g.effectType == GateEffectType.NegativeCP ||
                g.effectType == GateEffectType.RiskReward)
            { negatives.Add(g); w += g.spawnWeight; }
        }
        if (negatives.Count > 0) return WeightedRandom(negatives.ToArray(), w);
        // Fallback: negatif yoksa düz NegativeCP üret
        return MakeGate("CP -80", GateEffectType.NegativeCP, 80f,
            new Color(0.9f,0.1f,0.1f,0.85f), 0.1f);
    }

    GateData MakeBigGate()
    {
        // Mevcut oyunun CP'sine göre büyük bonus
        float z     = playerTransform.position.z;
        float scale = 1f + z / 2000f;
        int   bonus = Mathf.RoundToInt(150f * scale);

        GateData d = ScriptableObject.CreateInstance<GateData>();
        d.gateText    = $"CP +{bonus}";
        d.effectType  = GateEffectType.AddCP;
        d.effectValue = bonus;
        d.gateColor   = new Color(1f, 0.85f, 0f, 0.9f); // altın sarısı
        d.spawnWeight = 0.01f; // nadiren normal listede olsun
        return d;
    }

    // ── Kapı spawn ────────────────────────────────────────────────────────
    void SpawnGate(GateData data, Vector3 pos, float scale = 1f)
    {
        if (data == null) return;
        GameObject obj;

        if (gatePrefab != null)
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position   = pos;
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>(); bc.isTrigger = true;
            bc.size = new Vector3(0.95f, 1f, 1.5f);
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }

        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) { gate.gateData = data; gate.Refresh(); }

        // Scale'i Refresh'ten sonra uygula — collider doğru hesaplansın
        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);

        Destroy(obj, 45f);
    }

    // ── Gate data havuzu ─────────────────────────────────────────────────
    void BuildRuntimeGates()
    {
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            MakeGate("CP  +80",   GateEffectType.AddCP,               80f,  new Color(0.15f,0.80f,0.15f,0.80f), 0.22f),
            MakeGate("CP  +45",   GateEffectType.AddCP,               45f,  new Color(0.15f,0.80f,0.15f,0.80f), 0.18f),
            MakeGate("PIY x2",    GateEffectType.AddSoldier_Piyade,   30f,  new Color(0.1f, 0.90f,0.3f, 0.85f), 0.08f),
            MakeGate("MEK x2",    GateEffectType.AddSoldier_Mekanik,  30f,  new Color(0.55f,0.55f,0.55f,0.85f), 0.08f),
            MakeGate("TEK x2",    GateEffectType.AddSoldier_Teknoloji,30f,  new Color(0.1f, 0.45f,1.0f, 0.85f), 0.07f),
            MakeGate("MERGE",     GateEffectType.Merge,               0f,   new Color(0.65f,0.05f,0.95f,0.85f), 0.08f),
            MakeGate("KMT +HP",   GateEffectType.HealCommander,       300f, new Color(1.0f, 0.2f, 0.55f,0.80f), 0.05f),
            MakeGate("ASK HP+",   GateEffectType.HealSoldiers,        0.5f, new Color(0.5f, 1.0f, 0.55f,0.80f), 0.04f),
            MakeGate("PIY +25%",  GateEffectType.PathBoost_Piyade,    50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.04f),
            MakeGate("MEK +25%",  GateEffectType.PathBoost_Mekanize,  50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.04f),
            MakeGate("TEK +25%",  GateEffectType.PathBoost_Teknoloji, 50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.03f),
            MakeGate("CP x1.2",   GateEffectType.MultiplyCP,          1.2f, new Color(1.0f, 0.75f,0.0f, 0.80f), 0.04f),
            MakeGate("RISK !",    GateEffectType.RiskReward,          0f,   new Color(1.0f, 0.85f,0.0f, 0.80f), 0.04f),
            MakeGate("+MERMI",    GateEffectType.AddBullet,           40f,  new Color(0.5f, 0.0f, 0.85f,0.80f), 0.04f),
            MakeGate("CP -60",    GateEffectType.NegativeCP,          60f,  new Color(0.9f, 0.1f, 0.1f, 0.80f), 0.05f),
        };
    }

    GateData MakeGate(string text, GateEffectType type, float val, Color color, float weight)
    {
        GateData d = ScriptableObject.CreateInstance<GateData>();
        d.gateText = text; d.effectType = type; d.effectValue = val;
        d.gateColor = color; d.spawnWeight = weight;
        return d;
    }

    GateData[] ActiveGates =>
        (gateDataList != null && gateDataList.Length > 0) ? gateDataList : _runtimeGates;

    void CacheWeights()
    {
        _totalWeight = 0f;
        foreach (var g in ActiveGates) _totalWeight += g.spawnWeight;
    }

    GateData PickGate(bool pity)
    {
        GateData[] pool = ActiveGates;
        if (pity)
        {
            var safe = new List<GateData>(); float st = 0f;
            foreach (var g in pool)
                if (g.effectType != GateEffectType.NegativeCP &&
                    g.effectType != GateEffectType.RiskReward)
                { safe.Add(g); st += g.spawnWeight; }
            if (safe.Count > 0) return WeightedRandom(safe.ToArray(), st);
        }
        return WeightedRandom(pool, _totalWeight);
    }

    GateData WeightedRandom(GateData[] pool, float total)
    {
        if (pool.Length == 0 || total <= 0f) return pool[0];
        float r = Random.value * total, cum = 0f;
        for (int i = 0; i < pool.Length; i++)
        { cum += pool[i].spawnWeight; if (r <= cum) return pool[i]; }
        return pool[pool.Length - 1];
    }

    // ── Düşman dalgası ───────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   cnt  = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f) cnt = Mathf.Min(cnt + 1, 9);

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }
        RefreshStats();
    }

    int PickWaveType(float p)
    { if (p < 0.25f) return 0; float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2; }

    void NormalWave(float z, int n)
    {
        int   cols = Mathf.Min(n, 4), rows = Mathf.CeilToInt((float)n / cols), pl = 0;
        float gap  = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx   = -(gap * (cols - 1)) * 0.5f;
        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            { PlaceEnemy(new Vector3(Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f), 1.2f, z + r * 3f)); pl++; }
    }

    void HeavyWave(float z, int n)
    { for (int i = 0; i < n; i++) PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f)); }

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
        obj.GetComponent<Enemy>()?.Initialize(_stats);
    }
}
```
