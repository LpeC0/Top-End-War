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

Bosshitreceiver.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Boss Isabet Alici (Claude)
/// Boss prefab'ine eklenir. Bullet.cs bu componenti bulur.
///
/// KURULUM:
///   Boss GameObject'ine ekle.
///   Inspector'dan bossManager alanina BossManager objesini sur.
///   (bos birakılırsa Instance'tan alir — fallback)
/// </summary>
public class BossHitReceiver : MonoBehaviour
{
    [Tooltip("BossManager objesi. Bos birakılırsa BossManager.Instance kullanilir.")]
    public BossManager bossManager;   // ← Bullet.cs bu field'i ariyordu

    void Awake()
    {
        if (bossManager == null)
            bossManager = BossManager.Instance;
    }

    /// <summary>Bullet.cs bu metodu cagirir.</summary>
    public void TakeDamage(int dmg)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(dmg);
    }
}
```

BossManager.cs

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Boss Yoneticisi v6 (Claude)
///
/// v6 degisiklikleri:
///   + Phase Shield: HP %60 ve %30'da 2sn dokunulmazlik
///   + Faz gecisleri coroutine ile yonetilir
///   + BossHitReceiver ayri component olarak ayrildi (Bullet.cs uyumu)
///   - Enemy.SetHP() bagimliligı kaldirildi (minyon spawn sadece ObjectPooler kullanir)
///
/// KURULUM:
///   1. Hierarchy'de bir Boss GameObject olustur.
///   2. BossHitReceiver.cs'i bu objeye ekle (Bullet.cs bunu arar).
///   3. BossManager.cs ayri bir sahne objesine (BossManager) ekle.
///   4. Inspector'dan bossMaxHP ayarla veya StageManager.SetupBoss() kullan.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Ayarlari")]
    public int   bossMaxHP           = 41000;
    public float phaseShieldDuration = 2f;
    public float enrageSpeedMult     = 2.2f;

    [Header("Minyon Spawn (Faz 2)")]
    [Tooltip("ObjectPooler 'Enemy' havuzundan cekilir. Pool bos ise spawn edilmez.")]
    public int   minionsPerWave  = 4;
    public float minionInterval  = 8f;
    [Tooltip("Minyon spawn pozisyonu icin bos referans noktalari (opsiyonel)")]
    public Transform[] minionSpawnPoints;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] private int  _currentHP;
    [SerializeField] private int  _currentPhase;   // 1=normal, 2=minyon, 3=enrage
    [SerializeField] private bool _invulnerable;
    [SerializeField] private bool _phase2Triggered;
    [SerializeField] private bool _phase3Triggered;
    [SerializeField] private bool _active;

    Coroutine _minionCoroutine;
    Coroutine _shieldCoroutine;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Boss Baslatma ─────────────────────────────────────────────────────
    /// <summary>
    /// SpawnManager veya StageManager bu metodu cagirir.
    /// hp = -1 ise Inspector'daki bossMaxHP kullanilir.
    /// </summary>
    public void StartBoss(int hp = -1)
    {
        if (hp > 0) bossMaxHP = hp;

        _currentHP       = bossMaxHP;
        _currentPhase    = 1;
        _invulnerable    = false;
        _phase2Triggered = false;
        _phase3Triggered = false;
        _active          = true;

        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);
        GameEvents.OnBossEncountered?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(true);

        Debug.Log($"[BossManager] Basliyor. HP: {bossMaxHP}");
    }

    // ── Hasar Al ─────────────────────────────────────────────────────────
    /// <summary>BossHitReceiver bu metodu cagirir.</summary>
    public void TakeDamage(int dmg)
    {
        if (!_active || _invulnerable || _currentHP <= 0) return;

        _currentHP = Mathf.Max(0, _currentHP - dmg);
        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);

        CheckPhaseTransitions();

        if (_currentHP <= 0) OnBossDefeated();
    }

    // ── Faz Kontrolu ─────────────────────────────────────────────────────
    void CheckPhaseTransitions()
    {
        float ratio = (float)_currentHP / bossMaxHP;

        if (!_phase2Triggered && ratio <= 0.60f)
        {
            _phase2Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(PhaseShieldRoutine(toPhase: 2));
        }

        if (!_phase3Triggered && ratio <= 0.30f)
        {
            _phase3Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(PhaseShieldRoutine(toPhase: 3));
        }
    }

    // ── Phase Shield ─────────────────────────────────────────────────────
    IEnumerator PhaseShieldRoutine(int toPhase)
    {
        _invulnerable = true;
        Debug.Log($"[BossManager] Phase Shield aktif — Faz {toPhase} geliyor...");
        GameEvents.OnBossPhaseShield?.Invoke(toPhase);

        yield return new WaitForSeconds(phaseShieldDuration);

        _invulnerable = false;
        Debug.Log($"[BossManager] Phase Shield bitti — Faz {toPhase} aktif.");

        if      (toPhase == 2) EnterPhase2();
        else if (toPhase == 3) EnterPhase3();
    }

    // ── Faz 2: Minyon Dalgasi ────────────────────────────────────────────
    void EnterPhase2()
    {
        _currentPhase = 2;
        GameEvents.OnBossPhaseChanged?.Invoke(2);

        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        _minionCoroutine = StartCoroutine(MinionWaveRoutine());
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

        Debug.Log($"[BossManager] {minionsPerWave} minyon spawn edildi.");
    }

    // ── Faz 3: Enrage ────────────────────────────────────────────────────
    void EnterPhase3()
    {
        _currentPhase = 3;
        if (_minionCoroutine != null) { StopCoroutine(_minionCoroutine); _minionCoroutine = null; }

        GameEvents.OnBossPhaseChanged?.Invoke(3);
        GameEvents.OnBossEnraged?.Invoke(enrageSpeedMult);
        Debug.Log($"[BossManager] Faz 3: Enrage! Hiz x{enrageSpeedMult}");
    }

    // ── Boss Yenildi ─────────────────────────────────────────────────────
    void OnBossDefeated()
    {
        _active = false;
        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);

        GameEvents.OnBossDefeated?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        Debug.Log("[BossManager] Boss yenildi!");
    }

    // ── Yardimcilar ───────────────────────────────────────────────────────
    public float GetHPRatio() => bossMaxHP > 0 ? (float)_currentHP / bossMaxHP : 0f;
    public bool  IsActive()   => _active;
    public bool  IsInvulnerable() => _invulnerable;
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

Commanderdata.cs

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
/// Top End War — Zorluk Yoneticisi v3 (Claude)
///
/// v3: exponent 1.3→1.1, cpScalingFactor 0.9→0.5
/// EnemyStats field isimleri Enemy.cs ve SpawnManager ile eslestirild:
///   Health, Damage, Speed, CPReward
/// IsInPityZone() ve PlayerPowerRatio eklendi (SpawnManager kullanir).
/// OnDifficultyChanged(float mult, float powerRatio) — 2 parametre.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    // ── EnemyStats — Enemy.cs ve SpawnManager tarafindan kullanilir ───────
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

    // ── Konfigurasyon ─────────────────────────────────────────────────────
    [Header("Konfigurasyon (ProgressionConfig SO)")]
    public ProgressionConfig config;

    [Header("Temel Dusman Degerleri (Config yoksa yedek)")]
    public float baseEnemyHP     = 1100f;
    public float baseEnemySpeed  = 4.5f;
    public float baseEnemyDamage = 30f;
    public int   baseEnemyReward = 15;

    [Header("Pity Zone (boss oncesi kotu kapi engeli)")]
    [Tooltip("Boss mesafesinin yuzde kaci kala kotu kapilari engelle (0.15 = %15)")]
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

    // ── Zorluk Carpani ────────────────────────────────────────────────────
    public float CalculateMultiplier(float z, int playerCP, float expectedCP)
    {
        float exp   = config != null ? config.difficultyExponent    : 1.1f;
        float scale = config != null ? config.distanceScale         : 1000f;
        float cpSF  = config != null ? config.playerCPScalingFactor : 0.5f;
        float minPA = config != null ? config.minPowerAdjust        : 0.7f;
        float maxPA = config != null ? config.maxPowerAdjust        : 1.4f;

        float distanceFactor = 1f + Mathf.Pow(z / scale, exp);

        float rawRatio       = expectedCP > 0f ? (float)playerCP / expectedCP : 1f;
        _smoothedPowerRatio  = Mathf.Lerp(_smoothedPowerRatio, rawRatio, 0.08f);

        float powerAdjust    = 1f + (_smoothedPowerRatio - 1f) * cpSF;
        powerAdjust          = Mathf.Clamp(powerAdjust, minPA, maxPA);

        _currentMultiplier   = distanceFactor * powerAdjust;

        // 2 parametre: SpawnManager (m, r) olarak abone
        GameEvents.OnDifficultyChanged?.Invoke(_currentMultiplier, _smoothedPowerRatio);
        return _currentMultiplier;
    }

    // ── Dusman Istatistikleri ─────────────────────────────────────────────
    public EnemyStats GetScaledEnemyStats()
    {
        float m = _currentMultiplier;
        return new EnemyStats(
            health:   Mathf.RoundToInt(baseEnemyHP     * m),
            damage:   Mathf.RoundToInt(baseEnemyDamage * m),
            speed:    Mathf.Min(baseEnemySpeed + (m - 1f) * 1.4f, 7.5f),
            cpReward: Mathf.RoundToInt(baseEnemyReward * m)
        );
    }

    // ── SpawnManager'in Kullandigi Yardimcilar ────────────────────────────

    /// <summary>
    /// Boss mesafesine yakin midir?
    /// SpawnManager pity=true olunca kotu kapilari listeden cikarir.
    /// </summary>
    public bool IsInPityZone(float bossDistance)
    {
        if (PlayerStats.Instance == null) return false;
        float z          = PlayerStats.Instance.transform.position.z;
        float pityStart  = bossDistance * (1f - pityZoneFraction);
        return z >= pityStart;
    }

    /// <summary>
    /// Oyuncunun guc orani (SmoothedPowerRatio).
    /// SpawnManager dalga sertlestirmede kullanir.
    /// </summary>
    public float PlayerPowerRatio => _smoothedPowerRatio;

    // ── Diger Yardimcilar ─────────────────────────────────────────────────
    public float GetCurrentMultiplier()  => _currentMultiplier;
    public float GetSmoothedPowerRatio() => _smoothedPowerRatio;

    public void SetExpectedCP(float expected)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.SetExpectedCP(expected);
    }
}
```

Economyconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ekonomi Konfigurasyonu v1 (Claude)
///
/// Tum ekonomi formullerini tek bir SO'da toplar.
/// ChatGPT canonical JSON'undan turetildi.
///
/// FORMÜLLER:
///   SlotGoldCost(level)     = round(180 * 1.22^(level-1))
///   GoldReward(stage,dps)   = round(120 + 10*stage + 0.20*targetDps)
///   MidLootGold             = round(goldReward * 0.35)
///
/// TECH CORE BANTLARI:
///   Level 1-5   → 1 TC
///   Level 6-10  → 2 TC
///   Level 11-15 → 3 TC
///   Level 16-20 → 4 TC
///   Level 21-30 → 5 TC
///   Level 31-50 → 7 TC
///
/// ASSETS: Create > TopEndWar > EconomyConfig
/// EconomyManager bu SO'yu okur.
/// </summary>
[CreateAssetMenu(fileName = "EconomyConfig", menuName = "TopEndWar/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    // ── Slot Gold Maliyeti ────────────────────────────────────────────────
    [Header("Slot Yukseltme — Altin Maliyeti")]
    [Tooltip("Temel maliyet (level 1). Her seviye 1.22x artar.")]
    public float slotGoldCostBase    = 180f;

    [Tooltip("Buyume katsayisi. 1.22 = her seviye %22 pahali.")]
    public float slotGoldCostGrowth  = 1.22f;

    // ── Slot Tech Core Maliyeti (Bantli) ─────────────────────────────────
    [Header("Slot Yukseltme — Tech Core Maliyeti (Bantli)")]
    [Tooltip("Level aralik baslangici")]
    public int[] tcBandFromLevel     = { 1,  6, 11, 16, 21, 31 };
    [Tooltip("Level aralik bitis (kapsamli)")]
    public int[] tcBandToLevel       = { 5, 10, 15, 20, 30, 50 };
    [Tooltip("Her banttaki Tech Core maliyeti")]
    public int[] tcBandCost          = { 1,  2,  3,  4,  5,  7 };

    // ── Stage Altin Odulu ─────────────────────────────────────────────────
    [Header("Stage Odulu Formulü")]
    [Tooltip("Baz altin: round(goldBase + goldPerStage*stage + goldDpsFactor*targetDps)")]
    public float goldBase            = 120f;
    public float goldPerStage        = 10f;
    public float goldDpsFactor       = 0.20f;

    [Tooltip("Stage ortasi micro-loot orani (0.35 = odulun %35'i)")]
    [Range(0f, 1f)]
    public float midLootFraction     = 0.35f;

    // ── Offline Gelir ─────────────────────────────────────────────────────
    [Header("Offline Gelir")]
    public int   baseOfflineRate     = 50;     // Altin / saat (baslangic)
    [Range(8f, 24f)]
    public float offlineCapHours     = 15f;

    // ── Reklam Sinirlamalari ──────────────────────────────────────────────
    [Header("Reklam Politikasi")]
    public int   reviveAdsPerRun     = 1;
    public int   doubleGoldAdsDaily  = 3;
    public int   bonusChestAdsDaily  = 4;
    // techCoreAds ve hardCurrencyAds kapalı — kod seviyesinde bypass yok

    // ── Pity Timer ────────────────────────────────────────────────────────
    [Header("Pity Timer (Acima Sayaci)")]
    [Tooltip("Kac bos stage sonra Basic Scroll garantilenir")]
    public int   pityStagThreshold   = 20;

    // ── API ───────────────────────────────────────────────────────────────

    /// <summary>Belirtilen seviyenin slot yükseltme altin maliyetini dondurur.</summary>
    public int GetSlotGoldCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        return Mathf.RoundToInt(slotGoldCostBase * Mathf.Pow(slotGoldCostGrowth, level - 1));
    }

    /// <summary>Belirtilen seviyenin Tech Core maliyetini dondurur.</summary>
    public int GetSlotTechCoreCost(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        for (int i = 0; i < tcBandFromLevel.Length; i++)
            if (level >= tcBandFromLevel[i] && level <= tcBandToLevel[i])
                return tcBandCost[i];
        return tcBandCost[tcBandCost.Length - 1];
    }

    /// <summary>Stage altin odulunu hesaplar.</summary>
    public int GetGoldReward(int stageNumber, float targetDps)
        => Mathf.RoundToInt(goldBase + goldPerStage * stageNumber + goldDpsFactor * targetDps);

    /// <summary>Stage ortasi micro-loot altinini hesaplar.</summary>
    public int GetMidLootGold(int stageNumber, float targetDps)
        => Mathf.RoundToInt(GetGoldReward(stageNumber, targetDps) * midLootFraction);
}
```

EconomyManager.cs

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

        int goldCost = config.GetSlotGoldCost(currentLevel);      // mevcut level maliyeti
        int tcCost   = config.GetSlotTechCoreCost(currentLevel);

        if (Gold < goldCost)
        {
            failReason = $"Yetersiz altin. Gerekli: {goldCost}, Mevcut: {Gold}";
            return false;
        }
        if (TechCore < tcCost)
        {
            failReason = $"Yetersiz TechCore. Gerekli: {tcCost}, Mevcut: {TechCore}";
            return false;
        }

        Gold     -= goldCost;
        TechCore -= tcCost;
        Save();
        OnEconomyChanged?.Invoke();
        Debug.Log($"[Economy] Slot Lv{currentLevel}→{nextLevel} | -{goldCost}G -{tcCost}TC");
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
/// Top End War — Ekipman Verisi v3 (Claude)
///
/// v3 degisiklikleri:
///   + globalDmgMultiplier eklendi (Yuzuk/Kolye icin DPS carpani)
///   - cpMultiplier artik SADECE CP Gear Score icin kullanilir, DPS hesabinda YOK
///   ArmorType enum yorumlari duzeltildi (enum sadece kategori, gercek deger fieldlarda)
///
/// KURULUM:
///   Assets > Create > TopEndWar > Equipment
///   Slot sec, degerleri doldur, PlayerStats'e surukle.
/// </summary>

public enum EquipmentSlot
{
    Weapon,      // Silah    — atisHizi + hasar
    Armor,       // Zirh     — HP + hasarAzaltma
    Shoulder,    // Omuzluk  — CP bonus + kucuk DR
    Knee,        // Dizlik   — hafif HP bonus
    Boots,       // Ayakkabi — hareket bonusu (gelecek)
    Necklace,    // Kolye    — CP carpani + globalDmg
    Ring,        // Yuzuk    — globalDmgMultiplier
}

public enum WeaponType
{
    None,
    Pistol,      // Tabanca:    atisHizi x1.5, hasar x0.7
    Rifle,       // Tufek:      atisHizi x1.0, hasar x1.0  (standart)
    Automatic,   // Otomatik:   atisHizi x2.2, hasar x0.6
    Sniper,      // Nishanci:   atisHizi x0.35, hasar x3.5
    Shotgun,     // Pompa:      atisHizi x0.5,  hasar x2.0
}

public enum ArmorType
{
    None,
    Light,       // Genelde dusuk DR, dusuk HP bonusu
    Medium,      // Dengeli
    Heavy,       // Yuksek HP bonusu, orta DR
    Shield,      // En yuksek DR odakli
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TopEndWar/Equipment")]
public class EquipmentData : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string        equipmentName = "Yeni Ekipman";
    public EquipmentSlot slot          = EquipmentSlot.Weapon;
    public Sprite        icon;
    [TextArea(2, 4)]
    public string        description   = "";

    // ── Tur ───────────────────────────────────────────────────────────────
    [Header("Silah Turu (sadece Weapon slot)")]
    public WeaponType weaponType = WeaponType.None;

    [Header("Zirh Turu (sadece Armor/Shoulder/Knee)")]
    public ArmorType armorType = ArmorType.None;

    // ── CP Gear Score (Meta-Hub gostergesi) ───────────────────────────────
    [Header("CP Gear Score Bonusu")]
    [Tooltip("Kusanilinca CP Gear Score'una duz eklenir. DPS ile ilgisi yoktur.")]
    public int baseCPBonus = 0;

    /// <summary>
    /// CP carpani — SADECE Gear Score icin. DPS hesabinda KULLANILMAZ.
    /// DPS icin globalDmgMultiplier kullan.
    /// </summary>
    [Header("CP Carpani (kolye/yuzuk — Gear Score icin)")]
    [Tooltip("1.0 = etki yok. DPS etkilemez, sadece CP puanini carpar.")]
    [Range(1f, 2f)]
    public float cpMultiplier = 1f;

    // ── Silah Statistikleri ────────────────────────────────────────────────
    [Header("Atis Hizi Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 2.2 = %120 daha hizli")]
    [Range(0.2f, 3.0f)]
    public float fireRateMultiplier = 1f;

    [Header("Hasar Carpani (sadece silahlar)")]
    [Tooltip("1.0 = base, 3.5 = keskin nisanci")]
    [Range(0.2f, 5.0f)]
    public float damageMultiplier = 1f;

    // ── Global DPS Carpani (Yuzuk / Kolye) ───────────────────────────────
    [Header("Global Hasar Carpani (yuzuk/kolye — DPS'e etki eder)")]
    [Tooltip(
        "1.0 = etki yok. Bu alan DPS formulundeki GlobalMult'tur.\n" +
        "cpMultiplier'dan FARKLIDIR — o sadece Gear Score icindir.\n" +
        "Ornekler: Yuzuk 1.1 = DPS %10 artar. Necklace 1.05 = DPS %5 artar.")]
    [Range(1f, 2f)]
    public float globalDmgMultiplier = 1f;

    // ── Zirh / Savunma ─────────────────────────────────────────────────────
    [Header("Hasar Azaltma (zirh/aksesuar)")]
    [Tooltip("0.0-0.5. Toplam max %60 (PlayerStats.TotalDamageReduction() ile sinirli)")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("Komutan HP Bonusu (zirh/aksesuar)")]
    [Tooltip("Maks HP'ye duz eklenir")]
    public int commanderHPBonus = 0;

    // ── Diger ────────────────────────────────────────────────────────────
    [Header("Mermi Spread Bonusu (sadece silahlar)")]
    [Range(0f, 25f)]
    public float spreadBonus = 0f;

    [Header("Nadir (rarity) 1=Gri 2=Yesil 3=Mavi 4=Mor 5=Altin")]
    [Range(1, 5)]
    public int rarity = 1;

    // ── Yardimci ─────────────────────────────────────────────────────────
    /// <summary>Silah/zirh turune gore kisa aciklama dondurur (Inspector icin).</summary>
    public string GetTypeDescription()
    {
        return weaponType switch
        {
            WeaponType.Pistol    => "Tabanca: Hizli, kisa menzilli",
            WeaponType.Rifle     => "Tufek: Dengeli, cok yonlu",
            WeaponType.Automatic => "Otomatik: Yuksek DPS, genis spread",
            WeaponType.Sniper    => "Keskin Nisanci: Dev hasar, yavash",
            WeaponType.Shotgun   => "Pompa: Yakin mesafe katili",
            _ => armorType switch
            {
                ArmorType.Light  => "Hafif Zirh: Dusuk DR, dusuk HP",
                ArmorType.Medium => "Orta Zirh: Dengeli savunma",
                ArmorType.Heavy  => "Agir Zirh: Yuksek HP, orta DR",
                ArmorType.Shield => "Kalkan: En yuksek DR",
                _                => "Aksesuar",
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
        int techBack = Mathf.RoundToInt(_runTechEarned * 0.20f);
        if (retreatRewardText != null)
            retreatRewardText.text = $"Altin +{goldBack}  TechCore +{techBack}";
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
        int techBack = Mathf.RoundToInt(_runTechEarned * 0.20f);
        EconomyManager.Instance?.AddGold(goldBack);
        EconomyManager.Instance?.AddTechCore(techBack);
        Debug.Log($"[GameOverUI] Retreat: +{goldBack} Altin, +{techBack} TechCore.");
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

Gamestartup.cs

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

Inventorymanager.cs

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

# Mainmenuui.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Ana Menü (Claude)
///
/// UNITY KURULUM:
///   1. File → New Scene → "MainMenu" olarak kaydet
///   2. Hierarchy → Create Empty → "MainMenuManager" → bu scripti ekle
///   3. Build Settings'e MainMenu sahnesini 0. sıraya, SampleScene'i 1. sıraya ekle
///   4. gameSceneName = "SampleScene"
///
/// NE GÖSTERIR:
///   - Oyun adı + slogan
///   - En iyi skor + mesafe
///   - Toplam run sayısı
///   - OYNA butonu
///   - Ekipman butonu (EquipmentUI'yi açar — aynı sahnede)
///   - Sıfırla butonu (debug)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Sahne")]
    public string gameSceneName = "SampleScene";

    [Header("Arkaplan Rengi")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.12f);

    Canvas          _canvas;
    TextMeshProUGUI _bestCPText;
    TextMeshProUGUI _bestDistText;
    TextMeshProUGUI _totalRunsText;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        Camera.main.backgroundColor = bgColor;
        Camera.main.clearFlags      = CameraClearFlags.SolidColor;
        BuildUI();
        RefreshStats();
    }

    void BuildUI()
    {
        // Canvas
        var cObj = new GameObject("MainMenuCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)cObj.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);
        cObj.AddComponent<GraphicRaycaster>();

        // Arkaplan
        var bg = new GameObject("BG");
        bg.transform.SetParent(_canvas.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        Stretch(bg.GetComponent<RectTransform>());

        // Oyun adı
        MakeText(_canvas.gameObject, "TOP END WAR",
            new Vector2(0.5f, 1f), new Vector2(0, -160),
            72, new Color(1f, 0.85f, 0.1f), FontStyles.Bold);

        MakeText(_canvas.gameObject, "Sivas'ı Ele Geçir",
            new Vector2(0.5f, 1f), new Vector2(0, -250),
            30, new Color(0.7f, 0.7f, 0.9f), FontStyles.Italic);

        // İstatistikler
        _bestCPText    = MakeText(_canvas.gameObject, "En iyi CP: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 200),
            32, Color.white, FontStyles.Normal);

        _bestDistText  = MakeText(_canvas.gameObject, "En iyi Mesafe: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 155),
            28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        _totalRunsText = MakeText(_canvas.gameObject, "Toplam Sefer: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0, 110),
            24, new Color(0.6f, 0.6f, 0.6f), FontStyles.Normal);

        // OYNA butonu
        MakeButton(_canvas.gameObject, "OYNA",
            new Vector2(0.5f, 0.5f), new Vector2(0, -30),
            new Vector2(400, 110),
            new Color(0.15f, 0.75f, 0.25f),
            () => SceneManager.LoadScene(gameSceneName));

        // SIFIRLA butonu (debug — küçük, köşede)
        MakeButton(_canvas.gameObject, "Skoru Sifirla",
            new Vector2(0f, 0f), new Vector2(130, 60),
            new Vector2(220, 55),
            new Color(0.4f, 0.1f, 0.1f, 0.7f),
            () =>
            {
                SaveManager.Instance?.ResetAll();
                RefreshStats();
            });

        // Versiyon
        MakeText(_canvas.gameObject, "v0.4 — Top End War",
            new Vector2(1f, 0f), new Vector2(-80, 35),
            18, new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);
    }

    void RefreshStats()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

        _bestCPText.text    = $"En iyi CP: {save.HighScoreCP:N0}";
        _bestDistText.text  = $"En iyi Mesafe: {save.HighScoreDistance:N0}m";
        _totalRunsText.text = $"Toplam Sefer: {save.TotalRuns}";
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("T_" + text.Substring(0, Mathf.Min(8, text.Length)));
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(900, 80);
        return t;
    }

    void MakeButton(GameObject parent, string label, Vector2 anchor,
        Vector2 pos, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent.transform, false);
        var img = obj.AddComponent<Image>(); img.color = bg;
        var btn = obj.AddComponent<Button>(); btn.targetGraphic = img;

        // Hover rengi
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f;
        cols.pressedColor     = bg * 0.7f;
        btn.colors = cols;
        btn.onClick.AddListener(onClick);

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(obj.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = size.y * 0.35f;
        t.color = Color.white; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
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
/// Top End War — Oyuncu Hareketi v5 (Claude)
///
/// v5 degisiklikleri:
///   + AutoShoot: bulletDamage = GetTotalDPS() / (GetBaseFireRate() * BulletCount)
///   + DAMAGE[] ve BASE_FIRE_RATES[] dizileri KALDIRILDI — PlayerStats'ten gelir
///   + staticFire degiskeni kaldirildi
///   Onceki mantik (v4) aynen korundu: FindTarget, drag, spread, anchor.
///
/// HASAR FORMULU (Degismez Kural):
///   TotalDPS = BaseDMG[tier] * WeaponMult * SlotMult * RarityMult * GlobalMult
///   BulletDamage = TotalDPS / (FireRate * BulletCount)
///
///   NEDEN BulletCount boluyor:
///   5 mermi ayni hasar verirse toplam hasar 5x DPS olur.
///   Spread = daha genis alan, toplam hasar degil.
///
/// Spread formation (V sekli):
///   1 mermi: duz, 2: +-8, 3: -12 0 +12, 4: -18 -6 +6 +18, 5: -22 -11 0 +11 +22
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

    // ── Spread Tablosu ────────────────────────────────────────────────────
    static readonly float[][] SPREAD =
    {
        new[] {  0f },
        new[] { -8f,  8f },
        new[] { -12f, 0f, 12f },
        new[] { -18f, -6f, 6f, 18f },
        new[] { -22f, -11f, 0f, 11f, 22f },
    };

    // ── Dahili Durum ──────────────────────────────────────────────────────
    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
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
        c.height = 2f; c.radius = 0.4f; c.isTrigger = false;
    }

    void Update()
    {
        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    // ── Surukle / Hareket ─────────────────────────────────────────────────
    void HandleDrag()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            _targetX = Mathf.Clamp(_targetX - 10f * Time.deltaTime, -xLimit, xLimit);
        if (Input.GetKey(KeyCode.RightArrow))
            _targetX = Mathf.Clamp(_targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0)) { _dragging = true; _lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))    _dragging = false;

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

    // ── Otomatik Ates ─────────────────────────────────────────────────────
    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null) return;

        // ── Atis Hizi ────────────────────────────────────────────────────
        float finalFireRate = PlayerStats.Instance.GetBaseFireRate();

        // ── Hasar Hesabi (v5 formulu) ────────────────────────────────────
        // TotalDPS PlayerStats tarafindan hesaplandi:
        //   BaseDMG[tier] * WeaponMult * SlotMult * RarityMult * GlobalMult
        // BulletDamage = DPS / (FireRate * BulletCount)
        // BulletCount icin boluyoruz: 5 mermi = genis alan, toplam hasar x5 olmaz.
        int bCount      = PlayerStats.Instance.BulletCount;
        float totalDPS  = PlayerStats.Instance.GetTotalDPS();
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * bCount)));

        // ── Hedef Bul ────────────────────────────────────────────────────
        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        // ── Spread ile Ates ──────────────────────────────────────────────
        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, bulletDamage);
        }

        _nextFire = Time.time + 1f / finalFireRate;
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

    // ── Hedef Bulma ───────────────────────────────────────────────────────
    /// <summary>
    /// Normal modda BoxCast (serit tarama).
    /// Anchor modda OverlapSphere 70 birim (boss kesin yakalanir).
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

/// <summary>
/// Top End War — Oyuncu Istatistikleri v7 (Claude)
///
/// v7 degisiklikleri:
///   + activeCommander (CommanderData SO) — tum tier tablolari buradan gelir
///   + GetTotalDPS(): BaseDMG[tier] x WeaponMult x SlotMult x RarityMult x GlobalMult
///   + GetRarityMult(): rarity 1-5 carpan tablosu
///   + GetBaseFireRate(): activeCommander'dan okur
///   - startCP kaldirildi — starter equipment zorunlu
///   - DAMAGE[] dizisi kaldirildi — PlayerController artik GetTotalDPS() kullanir
///   - BASE_FIRE_RATES dizisi kaldirildi — GetBaseFireRate() kullanilir
///
/// DPS FORMULU (Magic Number Yok):
///   CommanderDPS = BaseDMG[tier] * WeaponDmgMult * SlotLevelMult * RarityMult * GlobalMult
///   BulletDamage = CommanderDPS / (FinalFireRate * BulletCount)
///   [PlayerController.AutoShoot() hesaplar]
///
/// CP KURALI:
///   CP = Gear Score (meta-hub UI icin). DPS hesabinda KULLANILMAZ.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ── Komutan ───────────────────────────────────────────────────────────
    [Header("Aktif Komutan (CommanderData SO)")]
    [Tooltip("Assets > Create > TopEndWar > CommanderData. Inspector'a sur.")]
    public CommanderData activeCommander;

    // ── Ekipman ───────────────────────────────────────────────────────────
    [Header("Ekipman Seti (EquipmentLoadout SO)")]
    public EquipmentLoadout equippedLoadout;

    [Header("Tekil Ekipmanlar (Loadout yoksa veya override icin)")]
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
    private int   _baseCP        = 0;   // startCP kaldirildi — ekipmandan gelir
    private int   _riskBonusLeft = 0;
    private float _expectedCP    = 200f;
    private float _lastDmgTime   = -99f;

    // ── CP Property ───────────────────────────────────────────────────────
    /// <summary>
    /// Gear Score (meta-hub UI). Ekipman bonuslari + kolye/yuzuk carpanlari dahil.
    /// DPS hesabinda KULLANILMAZ.
    /// </summary>
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

    // ── DPS Formulu ───────────────────────────────────────────────────────
    /// <summary>
    /// Komutanin saniye basi hasari.
    /// Formul: BaseDMG[tier] * WeaponDmgMult * SlotLevelMult * RarityMult * GlobalMult
    ///
    /// PlayerController bu degeri fireRate ve BulletCount ile boler:
    ///   BulletDamage = GetTotalDPS() / (GetBaseFireRate() * BulletCount)
    /// </summary>
    public float GetTotalDPS()
    {
        if (activeCommander == null)
        {
            Debug.LogWarning("[PlayerStats] activeCommander atanmamis! Varsayilan degerler kullaniliyor.");
            return 60f;
        }

        float baseDMG    = activeCommander.GetBaseDMG(CurrentTier);
        float weaponMult = equippedWeapon != null ? equippedWeapon.damageMultiplier    : 1f;
        float slotMult   = GetSlotLevelMult(weaponSlotLevel);
        float rarityMult = GetRarityMult(equippedWeapon != null ? equippedWeapon.rarity : 1);
        float globalMult = 1f;

        // Global DPS carpani: once kolye, sonra yuzuk
        if (equippedNecklace != null) globalMult *= equippedNecklace.globalDmgMultiplier;
        if (equippedRing     != null) globalMult *= equippedRing.globalDmgMultiplier;

        return baseDMG * weaponMult * slotMult * rarityMult * globalMult;
    }

    /// <summary>
    /// Tier ve silah carpanina gore nihai atis hizi.
    /// PlayerController bu degeri kullanir.
    /// </summary>
    public float GetBaseFireRate()
    {
        if (activeCommander == null) return 1.5f;
        float baseRate  = activeCommander.GetBaseFireRate(CurrentTier);
        float equipMult = equippedWeapon != null ? equippedWeapon.fireRateMultiplier : 1f;
        return baseRate * equipMult;
    }

    // ── Slot Seviye Carpani ────────────────────────────────────────────────
    /// <summary>
    /// Azalan verimler:
    ///   Level 1-10:  +%5/seviye  (1-10 = +%50)
    ///   Level 11-30: +%3/seviye  (11-30 = +%60)
    ///   Level 31-50: +%1.5/seviye (31-50 = +%30)
    ///   Max (50):    +%140 → carpan 2.40
    /// Rarity carpani her zaman dominant — yeni silah bulmak her zaman daha degerli.
    /// </summary>
    public static float GetSlotLevelMult(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        float bonus = 0f;

        int   tier1 = Mathf.Min(level, 10);
        bonus += tier1 * 0.05f;

        if (level > 10)
        {
            int tier2 = Mathf.Min(level - 10, 20);
            bonus += tier2 * 0.03f;
        }
        if (level > 30)
        {
            int tier3 = level - 30;
            bonus += tier3 * 0.015f;
        }
        return 1f + bonus;
    }

    // ── Rarity Carpani ────────────────────────────────────────────────────
    /// <summary>
    /// Rarity carpani her zaman SlotLevelMult'tan buyuktur.
    /// Mor silah, max slotlu Gri silahi her zaman yener.
    /// Altin (rarity 5) World 5+'ta acilir.
    /// </summary>
    public static float GetRarityMult(int rarity)
    {
        return rarity switch
        {
            1 => 1.0f,  // Gri
            2 => 1.3f,  // Yesil
            3 => 1.7f,  // Mavi
            4 => 2.2f,  // Mor
            5 => 3.0f,  // Altin (World 5+)
            _ => 1.0f,
        };
    }

    // ── Hasar Azaltma ─────────────────────────────────────────────────────
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

    // ── Ekipman HP Bonusu ─────────────────────────────────────────────────
    public int TotalEquipmentHPBonus()
    {
        int bonus = 0;
        bonus += equippedArmor    != null ? equippedArmor.commanderHPBonus    : 0;
        bonus += equippedShoulder != null ? equippedShoulder.commanderHPBonus : 0;
        bonus += equippedKnee     != null ? equippedKnee.commanderHPBonus     : 0;
        return bonus;
    }

    // ── Tier ve Diger Durum ───────────────────────────────────────────────
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

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        equippedLoadout?.ApplyTo(this);

        // startCP yok — baslangic gucu tamamen ekipmandan gelir
        _baseCP = 0;

        // Komutan HP'sini hesapla
        if (activeCommander == null)
            Debug.LogError("[PlayerStats] activeCommander atanmamis! Inspector'a CommanderData SO suru.");

        CommanderMaxHP = (activeCommander != null ? activeCommander.GetBaseHP(1) : 500)
                       + TotalEquipmentHPBonus();
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Hasar / Iyilesme ─────────────────────────────────────────────────
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

    // ── CP Guncellemeleri ─────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        _baseCP = Mathf.Min(_baseCP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── Kapi Etkileri ─────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

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
                PiyadePath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;
            case GateEffectType.PathBoost_Mekanize:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath += 1f;
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
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Piyade!");
                break;
            }
            case GateEffectType.AddSoldier_Mekanik:
            {
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Mekanik!");
                break;
            }
            case GateEffectType.AddSoldier_Teknoloji:
            {
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Teknoloji!");
                break;
            }
            case GateEffectType.HealCommander:
            {
                HealCommander(Mathf.RoundToInt(data.effectValue));
                if (_riskBonusLeft > 0)
                {
                    CommanderMaxHP += 100;
                    CommanderHP = Mathf.Min(CommanderHP + 50, CommanderMaxHP);
                    GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
                    ShowPopupMessage("RISK: +100 MaxHP!");
                }
                break;
            }
            case GateEffectType.HealSoldiers:
            {
                float pct = _riskBonusLeft > 0 ? 1.0f : Mathf.Clamp(data.effectValue, 0f, 1f);
                ArmyManager.Instance?.HealAll(pct);
                ShowPopupMessage(_riskBonusLeft > 0 ? "RISK: Asker FULL HP!" :
                    $"Asker +%{Mathf.RoundToInt(pct * 100)}");
                break;
            }
        }

        // Risk sayacini dusuR
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        _baseCP = Mathf.Clamp(_baseCP, 0, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    // ── Merge ────────────────────────────────────────────────────────────
    void HandleMerge(bool riskActive = false)
    {
        ArmyManager.Instance?.TryMerge();

        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float riskBonus = riskActive ? 0.2f : 0f;
        float multiplier;
        string role = "none";

        if (riskActive) ShowPopupMessage("RISK: Merge Guclendi!");

        if (total < 1f)
        {
            multiplier = 1.1f + riskBonus;
        }
        else
        {
            float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;
            float minPath = Mathf.Min(p, Mathf.Min(m, t));
            if (minPath > 0.28f)
            {
                multiplier = 1.7f + riskBonus; role = "PERFECT";
                GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS!");
            }
            else if (t >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Teknoloji"; }
            else if (p >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Piyade"; }
            else if (m >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Mekanik"; }
            else                { multiplier = 1.2f + riskBonus; }
        }

        _baseCP = Mathf.RoundToInt(_baseCP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();
    }

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

    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 2f) return;
        float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
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

ProgressionConfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Konfigurasyonu v2 (Claude)
///
/// v2 degisiklikleri:
///   difficultyExponent varsayilan:    1.3 → 1.1
///   playerCPScalingFactor varsayilan: 0.9 → 0.5
///   minPowerAdjust / maxPowerAdjust eklendi (carpan siniri)
///
/// Assets > Create > TopEndWar > ProgressionConfig ile olustur.
/// DifficultyManager bu SO'yu okur.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/ProgressionConfig")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Zorluk Egrisi")]
    [Tooltip("Mesafeye gore zorluk artis ussu. 1.1 = yavash artar, 1.5 = agresif")]
    [Range(0.8f, 2.0f)]
    public float difficultyExponent = 1.1f;

    [Tooltip("Mesafe olcegi. Kucuk = daha erken zorlasmaya baslar")]
    public float distanceScale = 1000f;

    [Header("Oyuncu Gucu Uyumu")]
    [Tooltip(
        "Oyuncunun gucune gore zorluk ne kadar uyum saglar?\n" +
        "0.0 = hic uyum yok (saf Fixed Difficulty)\n" +
        "0.5 = orta uyum (oyuncu cok gucluyse hafif zorlar)\n" +
        "1.0 = tam uyum (kostu bandi — onerilmez)")]
    [Range(0f, 1f)]
    public float playerCPScalingFactor = 0.5f;

    [Tooltip("Guc ayari alt siniri (oyuncuyu asiri kolaylastirmaz)")]
    [Range(0.5f, 1f)]
    public float minPowerAdjust = 0.7f;

    [Tooltip("Guc ayari ust siniri (oyuncuyu asiri cezalandirmaz)")]
    [Range(1f, 2f)]
    public float maxPowerAdjust = 1.4f;

    [Header("Beklenen CP (SpawnManager kullanir)")]
    [Tooltip("Her 1000 unitede beklenen CP artisi")]
    public float expectedCPGrowthPerKm = 150f;
}
```

Savemanager.cs

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
        var stats = GetEnemyStatsForSpawn();
        obj.GetComponent<Enemy>()?.Initialize(stats);
    }
    // ═══════════════════════════════════════════════════════════════════════════
// MEVCUT SpawnManager.cs'e EKLENECEK KISIM
// Dosyanin EN ALTINA, son kapanan } oncesine yapistir.
// ═══════════════════════════════════════════════════════════════════════════

// SpawnManager sinifi icine ekle:

    // ── StageManager tarafindan cagrilir ─────────────────────────────────

    // Sahne basi varsayilan degerler
    float _overrideNormalHP = 0f;
    float _overrideEliteHP  = 0f;
    float _densityMult      = 1f;
    bool  _hpOverrideActive = false;

    /// <summary>
    /// StageManager, StageConfig'deki targetDps formulunden hesaplanan HP'yi
    /// buraya iletir. Bu degerler PlaceEnemy() icerisinde Enemy.Initialize()'a aktarilir.
    /// </summary>
    public void SetMobHP(int normalHP, int eliteHP, float density = 1f)
    {
        _overrideNormalHP  = normalHP;
        _overrideEliteHP   = eliteHP;
        _densityMult       = density;
        _hpOverrideActive  = true;
        Debug.Log($"[SpawnManager] Mob HP override: Normal={normalHP}, Elite={eliteHP}, Density={density}");
    }

// Ayrica PlaceEnemy() icindeki su satiri degistir:
//
//   ESKISI:
//   obj.GetComponent<Enemy>()?.Initialize(_stats);
//
//   YENISI:
//   var stats = GetEnemyStatsForSpawn();
//   obj.GetComponent<Enemy>()?.Initialize(stats);
//
// Ve su metodu SpawnManager sinifi icine ekle:

    DifficultyManager.EnemyStats GetEnemyStatsForSpawn()
    {
        if (_hpOverrideActive)
        {
            // Fixed Difficulty: HP StageConfig'den gelir, DifficultyManager'dan degil
            float speed  = _stats.Speed;   // Hiz yine DifficultyManager'dan
            int   reward = _stats.CPReward;
            return new DifficultyManager.EnemyStats(
                health:   Mathf.RoundToInt(_overrideNormalHP),
                damage:   _stats.Damage,
                speed:    speed,
                cpReward: reward);
        }
        return _stats; // Fallback: eski sistem (StageManager yoksa)
    }

// ═══════════════════════════════════════════════════════════════════════════
// KURULUM:
//   1. SpawnManager.cs'i ac.
//   2. Sinifin son kapanan } oncesine yukardaki alanlari ve metotlari yapistir.
//   3. PlaceEnemy() icindeki   obj.GetComponent<Enemy>()?.Initialize(_stats);
//      satirini soyle degistir:
//      var stats = GetEnemyStatsForSpawn();
//      obj.GetComponent<Enemy>()?.Initialize(stats);
// ═══════════════════════════════════════════════════════════════════════════
}
```

Stageconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v2 (Claude)
///
/// v2: targetDps eklendi. HP degerleri artik formule gore hesaplanir:
///   normalMobHP = targetDps * 1.0
///   eliteHP     = targetDps * 4.0
///   miniBossHP  = targetDps * 18
///   finalBossHP = targetDps * 36
///
/// gateBudgetMult: Bu stage'de kapilarin verebilecegi max DPS artis cokeni.
/// BandliBütçe (ChatGPT canonical JSON):
///   Stage 1-5:  1.40 | 6-9:   1.50 | 10: 1.55
///   Stage 11-19: 1.65 | 20:   1.70
///   Stage 21-29: 1.80 | 30-34: 1.88 | 35: 1.95
///
/// HP degerleri Inspector'dan ELLE YAZILMAZ — GetXxxHP() metotlari kullanilir.
/// StageManager bu metotlari cagirip SpawnManager ve BossManager'a iletir.
///
/// ASSETS: Create > TopEndWar > StageConfig
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID        = 1;
    public int    stageID        = 1;
    public string locationName   = "Sivas - Sinir Boyu";

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

    /// <summary>Normal mob HP = targetDps x 1.0</summary>
    public int GetNormalMobHP()   => Mathf.RoundToInt(targetDps * 1.0f);

    /// <summary>Elite mob HP = targetDps x 4.0</summary>
    public int GetEliteHP()       => Mathf.RoundToInt(targetDps * 4.0f);

    /// <summary>Mini-boss HP = targetDps x 18. BossType.MiniBoss icin kullanilir.</summary>
    public int GetMiniBossHP()    => Mathf.RoundToInt(targetDps * 18f);

    /// <summary>Final boss HP = targetDps x 36. BossType.FinalBoss icin kullanilir.</summary>
    public int GetFinalBossHP()   => Mathf.RoundToInt(targetDps * 36f);

    /// <summary>BossType'a gore dogru HP degerini dondurur.</summary>
    public int GetBossHP()
    {
        return bossType switch
        {
            BossType.MiniBoss   => GetMiniBossHP(),
            BossType.FinalBoss  => GetFinalBossHP(),
            _                   => 0,
        };
    }

    /// <summary>entryDps = round(targetDps / gateBudgetMult)</summary>
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

StageManager.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Stage Yoneticisi v2 (Claude)
///
/// v2: StageConfig.targetDps formullerine gore HP degerlerini
///     SpawnManager ve BossManager'a iletir.
///     EconomyManager'a altin ve offline boost ekler.
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
    public WorldConfig[]  worlds;

    [Header("Stage Verileri")]
    [Tooltip("Bos birakılırsa Resources/Stages/Stage_W{w}_{s:D2} yolundan yuklenir")]
    public StageConfig[]  stageConfigs;

    [Header("Ekonomi Formulü")]
    public EconomyConfig  economyConfig;

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
        _currentWorldID = worldID;
        _currentStageID = stageID;

        _activeWorld = FindWorld(worldID);
        _activeStage = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

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

        EconomyManager.Instance?.AddGold(gold);
        EconomyManager.Instance?.AddOfflineRate(_activeStage.offlineBoostPerHour);

        Debug.Log($"[StageManager] Stage tamamlandi. Altin: +{gold}");

        if (_activeStage.IsBossStage)
            OnWorldComplete();
        else
            LoadStage(_currentWorldID, _currentStageID + 1);
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

        // Tech Core sans kontrolu
        if (Random.value < _activeStage.techCoreDropChance)
        {
            EconomyManager.Instance?.AddTechCore(1);
            Debug.Log("[StageManager] Micro-loot: +1 TechCore!");
        }
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
    public StageConfig ActiveStage     => _activeStage;
    public WorldConfig ActiveWorld     => _activeWorld;
    public int         CurrentWorldID  => _currentWorldID;
    public int         CurrentStageID  => _currentStageID;
}
```

Tiervisualizer.cs

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

Worldconfig.cs

```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dunya Konfigurasyonu v1 (Claude)
///
/// Her dunya icin ayri bir SO olustur:
///   Assets > Create > TopEndWar > WorldConfig
///
/// WorldConfig nedir?
///   Bir dunya (ornegin Sivas, Tokat) kac stage'den olusuyor,
///   hangi biyom, hangi rarity esigi ve hangi komutan aciliyor.
///
/// StageManager bu SO'yu okur, BiomeManager biyomu buradan alir.
/// </summary>
[CreateAssetMenu(fileName = "World_", menuName = "TopEndWar/WorldConfig")]
public class WorldConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID   = 1;
    public string worldName = "Sivas";

    [Header("Biyom")]
    [Tooltip("BiomeManager'in taniydigi biyom adi: Tas, Orman, Col, Karli, Tarim")]
    public string biome     = "Tas";

    [Header("Stage Yapisi")]
    [Tooltip("Bu dunyada kac stage var (ornegin 15)")]
    public int stageCount   = 15;

    [Header("Rarity Esigi")]
    [Tooltip(
        "Bu dunyada drop edilebilecek max rarity.\n" +
        "Dunya 1 = 2 (Yesil), Dunya 3 = 4 (Mor), Dunya 5+ = 5 (Altin)")]
    [Range(1, 5)]
    public int maxRarity    = 2;

    [Header("Komutan Kilidi")]
    [Tooltip("Bu dunya bittikten sonra acilan CommanderData SO. Bos = komutan acilmaz.")]
    public CommanderData unlockedCommander;

    [Header("Offline Kazanc")]
    [Tooltip("Bu dunya temizlenince saatlik altina eklenen miktar")]
    public int offlineIncomeBoost = 30;

    [Header("Gorunumler")]
    [Tooltip("Haritada bu dunya icin gosterilecek ikon veya renk (gelecek)")]
    public Color mapColor   = Color.green;
}
```
