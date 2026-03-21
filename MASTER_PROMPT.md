# Top End War — MASTER PROMPT v10
**Repo:** https://github.com/LpeC0/Top-End-War

Projem Unity 6.3 LTS URP 3D mobil runner/auto-shooter: Top End War

---

## OYUN TANIMI
Runner/auto-shooter. Player otomatik kosar, surukleme ile serbest hareket.
Yolda matematiksel kapılar (sol/sag — oyuncu birinden gecer).
Dusmanlar dalga halinde (3 tip), auto-shoot ile vurulur.
CP = savas gucu = can. Tier atlarken model morph.
1200m boss, sonra Turkiye haritasinda yeni sehir.

---

## DEGISTIRILEMEZ KURALLAR
```
xLimit = 8              PlayerController + Enemy + SpawnManager.ROAD_HALF_WIDTH AYNI
Player Rigidbody YOK    transform.position hareketi
Cinemachine YOK         SimpleCameraFollow (X sabit)
Input: Old/Legacy
Namespace YOK
GameEvents: Action<>    Raise...() metod YOK — abonelik += ile
PlayerStats.CP          Property (public field degil, partial class degil)
Gate shader: Sprites/Default
Gate Panel: QUAD (Cube degil)
SetActive(false)        pool icin (Destroy degil)
Unicode sembol KULLANMA
Player'a Enemy.cs/Bullet.cs EKLEME
DOTween kurulu
```

---

## HIERARCHY
```
SampleScene
  PoolManager         ObjectPooler  (Bullet:20, Enemy:20)
  DifficultyManager   DifficultyManager + ProgressionConfig (opsiyonel)
  GameOverManager     GameOverUI
  Player              PlayerController + PlayerStats + MorphController + GateFeedback [Tag:Player]
      FirePoint
  Main Camera         SimpleCameraFollow
  SpawnManager        SpawnManager (GatePrefab, EnemyPrefab, GateDataList — hepsi opsiyonel)
  ChunkManager        ChunkManager (RoadChunk X scale=1.6)
  Canvas
      CPText, TierText (TEXT BOSALT), PopupText, SynergyText
      DamageFlash (Image, full stretch, alpha=0, RaycastTarget=false)
      PiyadeBar, MekanizeBar, TeknolojiBar (Slider)
      HUDPanel        GameHUD
```

---

## TUM SCRIPTLER
#ArmyManager.cs
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
#BiomeManager.cs
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
#BossManager.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — Boss v4 (Claude)
///
/// Boss HP 60000 → 150000
/// (T4+4mermi=5040DPS → 30s; fazlarla ~60s; T3+2m=1160DPS → 129s ✓)
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss")]
    public int   bossMaxHP         = 150000;
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

    [Header("Prefab (boş = fallback küp)")]
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

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnBossEncountered += StartFight;
        BuildHPBar();
        _bossCanvas?.gameObject.SetActive(false);
    }

    void OnDestroy() => GameEvents.OnBossEncountered -= StartFight;

    void StartFight()
    {
        if (_active) return;
        Debug.Log("[Boss] Başlıyor!");
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
        SetLabel("GOKMEDRESE MUHAFIZI  |  FAZ 1: TAS ZIRH");
        Debug.Log("[Boss] Aktif! HP=" + _hp);
    }

    void SpawnBoss(Vector3 pos)
    {
        _bossObj = bossPrefab != null
            ? Instantiate(bossPrefab, pos, Quaternion.identity)
            : MakeFallbackCube(pos);

        _bossRend = _bossObj.GetComponent<Renderer>();
        _bossObj.tag = "Enemy";

        Collider col = _bossObj.GetComponent<Collider>() ?? _bossObj.AddComponent<BoxCollider>();
        // isTrigger=false → Bullet.OverlapSphere tag="Enemy" ile bulur

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

    void Update()
    {
        if (!_active || _dead || _bossObj == null || PlayerStats.Instance == null) return;

        float tZ  = PlayerStats.Instance.transform.position.z + bossStopDist;
        Vector3 p = _bossObj.transform.position;

        if (p.z > tZ)
            p.z = Mathf.MoveTowards(p.z, tZ, bossApproachSpeed * Time.deltaTime);
        else
        {
            PlayerStats.Instance.TakeContactDamage(bossContactDmg);
            p.z += 8f;
        }
        _bossObj.transform.position = p;
    }

    public void TakeDamage(int dmg)
    {
        if (!_active || _dead) return;
        _hp = Mathf.Max(0, _hp - dmg);
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
        SetLabel("FAZ 2: MINYON");
        if (_bossRend) _bossRend.material.color = new Color(0.7f, 0.3f, 0.1f);
        InvokeRepeating(nameof(SpawnMinions), 1f, minionInterval);
    }

    void Phase3()
    {
        SetLabel("FAZ 3: CEKIRDEK");
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

        Txt(panel, "SIVAS ELE GECIRILDI!", new Vector2(0,100), 46, new Color(1f,0.85f,0f), FontStyles.Bold);
        Txt(panel, "CP: " + (PlayerStats.Instance?.CP.ToString("N0") ?? "0"), new Vector2(0,25), 28, Color.white, FontStyles.Normal);
        Txt(panel, "Mermi: " + (PlayerStats.Instance?.BulletCount ?? 1), new Vector2(0,-20), 22, new Color(0.7f,0.7f,1f), FontStyles.Normal);
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

        _phaseLabel = Txt(strip, "GOKMEDRESE MUHAFIZI", Vector2.zero, 14, Color.white, FontStyles.Bold);
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

    // ── UI Helpers ─────────────────────────────────────────────────────────
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
### Bullet.cs
```csharp
using UnityEngine;
public class Bullet : MonoBehaviour
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
    public int   damage      = 60;
    public Color bulletColor = new Color(0.6f, 0.1f, 1.0f);

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

            BossHitReceiver bossRecv = col.GetComponent<BossHitReceiver>();
            if (bossRecv != null)
                bossRecv.bossManager?.TakeDamage(damage);
            else
                col.GetComponent<Enemy>()?.TakeDamage(damage);

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

### ChunkManager.cs
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

### DifficultyManager.cs
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
    const float BASE_DMG    = 25f;
    const float BASE_SPEED  = 4.0f;
    const float MAX_SPEED   = 7.5f;
    const float BASE_REWARD = 15f;

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
        CurrentDifficultyMultiplier = 1f + Mathf.Pow(norm, 1.3f);

        int   expected = config != null
            ? config.CalculateExpectedCP(_currentZ)
            : Mathf.RoundToInt(200f * Mathf.Pow(1.15f, _currentZ / 100f));

        int   actual   = PlayerStats.Instance?.CP ?? 200;
        float raw      = (float)actual / Mathf.Max(1, expected);
        PlayerPowerRatio = Mathf.Lerp(PlayerPowerRatio, raw, 0.08f);

        PlayerStats.Instance?.SetExpectedCP(expected);
        GameEvents.OnDifficultyChanged?.Invoke(CurrentDifficultyMultiplier, PlayerPowerRatio);
    }

    public EnemyStats GetScaledEnemyStats()
    {
        float diff  = CurrentDifficultyMultiplier;
        float pScale= config != null
            ? Mathf.Lerp(1f, PlayerPowerRatio, config.playerCPScalingFactor)
            : Mathf.Lerp(1f, PlayerPowerRatio, 0.7f);
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

    void OnDestroy() { if (Instance == this) Instance = null; }
}
```

### Enemy.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dushman (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic=true) + CapsuleCollider(IsTrigger=true)
///
/// Initialize() DifficultyManager statlari uygular.
/// Config yoksa mesafe bazli AutoInit() devreye girer.
/// Separation: her 0.15s bir guncellenen cached vektör.
/// EnemyHealthBar otomatik eklenir.
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

    // Separation cache
    float   _lastSepTime  = 0f;
    Vector3 _separationVec= Vector3.zero;
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
        _maxHealth     = Mathf.RoundToInt(100f * mult);
        _currentHealth = _maxHealth;
        _contactDamage = Mathf.RoundToInt(25f  * mult);
        _moveSpeed     = Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f);
        _cpReward      = Mathf.RoundToInt(15f  * mult);
    }

    void UseDefaults()
    {
        _maxHealth = _currentHealth = 120;
        _contactDamage = 50; _moveSpeed = 4.5f; _cpReward = 15;
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

        // Separation cache
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

    public void TakeDamage(int dmg)
    {
        if (_isDead) return;
        _currentHealth -= dmg;
        _hpBar?.UpdateBar(_currentHealth);
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);
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
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _hasDamagedPlayer || _isDead) return;
        _hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

    void OnDisable() { CancelInvoke(); _initialized = false; }
}
```

### EnemyHealthBar.cs
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

### GameEvents.cs
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

### GameHUD.cs
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

        if (cpText   == null) cpText   = MakeText(canvas.gameObject, "CP", new Vector2(0.5f,1f), new Vector2(0,-35),  36, Color.white);
        if (tierText == null) tierText = MakeText(canvas.gameObject, "TIER 1", new Vector2(0.5f,1f), new Vector2(0,-75), 26, Color.yellow);
        if (popupText== null) popupText= MakeText(canvas.gameObject, "", new Vector2(0.5f,0.5f), new Vector2(0,60), 40, Color.cyan);

        // ── Komutan HP Bar ────────────────────────────────────────────────
        // Unity Slider standart yapısı: Slider → Background + Fill Area → Fill
        if (commanderHPSlider == null)
            commanderHPSlider = BuildHPBar(canvas,
                new Vector2(0.05f, 0.94f), new Vector2(0.75f, 0.99f),
                new Color(0.2f, 0.8f, 0.2f), "KomutanHP");

        // HP text (slider'ın yanında)
        if (commanderHPText == null)
            commanderHPText = MakeText(canvas.gameObject, "HP",
                new Vector2(0.77f, 0.965f), Vector2.zero, 18, Color.white);

        // ── Asker Sayısı ──────────────────────────────────────────────────
        if (soldierCountText == null)
            soldierCountText = MakeText(canvas.gameObject, "Asker: 0/20",
                new Vector2(0.0f, 0.93f), new Vector2(80, 0), 20, new Color(0.9f,0.9f,0.9f));

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

### GameOverUI.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Ekrani (Claude)
///
/// KURULUM (super basit):
///   Hierarchy'de bos bir obje olustur → adi "GameOverManager"
///   Bu scripti ekle → bitti.
///   Baska HIC BIR SEY yapma, kod kendi Canvas'ini olusturur.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Sahne Adi")]
    public string gameSceneName = "SampleScene";

    // Olusturulan UI referanslari
    Canvas         _canvas;
    GameObject     _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _cpText;
    TextMeshProUGUI _distText;
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

    // ── UI'yi programatik olustur ─────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("GameOverCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99; // Her seyin ustunde
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Koyu arka plan paneli
        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Baslik
        _titleText = CreateText(_panel, "SAVAS BITTI",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 80f),
            52, Color.red, FontStyles.Bold);

        // CP
        _cpText = CreateText(_panel, "",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            32, Color.white, FontStyles.Normal);

        // Mesafe
        _distText = CreateText(_panel, "",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -35f),
            28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        // Tekrar Dene butonu
        CreateButton(_panel, "TEKRAR DENE",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -100f),
            new Vector2(260f, 60f),
            new Color(0.2f, 0.8f, 0.2f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene(gameSceneName); });

        // Panel baslangicta gizli
        _panel.SetActive(false);
    }

    // ── Game Over tetiklenince ─────────────────────────────────────────────────
    void ShowGameOver()
    {
        if (_shown) return;
        _shown = true;

        Time.timeScale = 0f;
        _panel.SetActive(true);

        if (_cpText != null && PlayerStats.Instance != null)
            _cpText.text = "Son CP: " + PlayerStats.Instance.CP.ToString("N0");

        if (_distText != null && PlayerStats.Instance != null)
            _distText.text = "Mesafe: " + Mathf.RoundToInt(PlayerStats.Instance.transform.position.z) + "m";
    }

    // ── Yardimci: Text olustur ────────────────────────────────────────────────
    TextMeshProUGUI CreateText(GameObject parent, string text,
        Vector2 anchor, Vector2 anchoredPos,
        float fontSize, Color color, FontStyles style)
    {
        GameObject obj = new GameObject("Text_" + text.Substring(0, Mathf.Min(6, text.Length)));
        obj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(500f, 60f);

        return tmp;
    }

    // ── Yardimci: Button olustur ──────────────────────────────────────────────
    void CreateButton(GameObject parent, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        // Arka plan
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        // Yazi
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 24f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin      = Vector2.zero;
        tr.anchorMax      = Vector2.one;
        tr.offsetMin      = Vector2.zero;
        tr.offsetMax      = Vector2.zero;
    }
}
```

### Gate.cs
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

### GateData.cs
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

### GateFeedback.cs
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

### MorphController.cs
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

    [Header("Visual Components")]
    [SerializeField] private Renderer characterRenderer; // Komutanın veya Askerin Mesh Renderer'ı
    
    // Performans için Property Block tanımlıyoruz
    private MaterialPropertyBlock _propBlock;

    // Shader referans ID'lerini (String yerine ID kullanmak çok daha hızlıdır) önbelleğe alıyoruz
    private static readonly int TierColorID = Shader.PropertyToID("_TierColor");
    private static readonly int BiomeTintID = Shader.PropertyToID("_BiomeTint");

    // Tum tier modelleri onceden olusturulur, sadece aktif/pasif yapilir
    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged += HandleTierChange;
        ActivateTier(0);
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= HandleTierChange;
    }

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

    void HandleTierChange(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, _spawnedModels.Length - 1);
        if (index == _currentTierIndex || _isMorphing) return;
        StartCoroutine(MorphCoroutine(index));
    }

    IEnumerator MorphCoroutine(int targetIndex)
    {
        _isMorphing = true;

        // Mevcut modeli kucult
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
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f).SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
private void Awake()
    {
        // Bellek tahsisini oyun başında sadece bir kere yapıyoruz (Garbage Collector'ı yormamak için)
        _propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Karakterin görsel renklerini günceller. Tier atladığında veya Biyom değiştiğinde çağrılır.
    /// </summary>
    public void UpdateVisuals(Color tierColor, Color biomeTint)
    {
        if (characterRenderer == null) return;

        // 1. O anki render ayarlarını bloğa al
        characterRenderer.GetPropertyBlock(_propBlock);
        
        // 2. Yeni renkleri bloğa işle
        _propBlock.SetColor(TierColorID, tierColor);
        _propBlock.SetColor(BiomeTintID, biomeTint);
        
        // 3. Bloğu tekrar renderer'a geri ver (Materyal kopyalanmadan renk değişir!)
        characterRenderer.SetPropertyBlock(_propBlock);
    }

}
```

### ObjectPooler.cs
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

### PlayerController.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v3 (Claude)
///
/// Anchor Modu: OnAnchorModeChanged(true) gelince forwardSpeed=0,
///   oyuncu sadece X ekseninde hareket eder, boss ile savaşır.
///   OnAnchorModeChanged(false) gelince normal koşuya döner.
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

    static readonly float[] FIRE_RATES = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    static readonly int[]   DAMAGE     = { 60,   95,   145,  210,  300  };

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
    bool  _anchorMode = false;  // Boss sahnesi

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
        if (active)
        {
            forwardSpeed = 0f;
            Debug.Log("[Player] Anchor modu actif — kosu durduruldu.");
        }
        else
        {
            forwardSpeed = 10f;
        }
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

        // Anchor modda daha geniş BoxCast (boss büyük)
        float halfW   = _anchorMode ? xLimit : xLimit * 0.6f;
        float range   = _anchorMode ? 60f : detectRange;

        RaycastHit hit;
        bool found = Physics.BoxCast(
            transform.position + Vector3.up,
            new Vector3(halfW, 1.2f, 0.5f),
            Vector3.forward, out hit,
            Quaternion.identity, range);

        if (!found || !hit.collider.CompareTag("Enemy")) return;

        // Lead hedefleme
        float   dist   = Vector3.Distance(firePoint.position, hit.transform.position);
        Vector3 aimPos = hit.transform.position + Vector3.back * (dist / 30f * 4f);
        Vector3 baseDir= (aimPos - firePoint.position).normalized;

        // Spread
        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, DAMAGE[idx]);
        }

        _nextFire = Time.time + 1f / FIRE_RATES[idx];
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

    // Boss modu için: bosluk bittikten sonra normal hıza dön
    public void ResumeRun() => OnAnchorMode(false);
}
```

### PlayerStats.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v5 (Claude)
///
/// v5 DEGISIKLIKLER:
///   - CommanderHP sistemi eklendi (CP'den BAGIMSIZ)
///   - TakeContactDamage artik CommanderHP'yi dusuruyor, CP'yi degil
///   - Yeni gate tipleri: AddSoldier_*, HealCommander, HealSoldiers
///   - PathBoost_* hala calisir (geriye donuk uyum)
///   - AddBullet legacy korundu (BulletCount arttirır, AddSoldier_Piyade gibi davranır)
///
/// UNITY NOTU:
///   - [DefaultExecutionOrder(-10)] — Start'ta PlayerStats.Instance hazir olmali
///   - Player GameObject'e ekle, tag "Player" olmali
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ── Inspector Ayarlari ───────────────────────────────────────────────
    [Header("Baslangic CP")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    // ── CP + Tier ─────────────────────────────────────────────────────────
    public int   CP           { get; private set; }
    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;   // Komutan spread

    // ── Path skorlari (PathBoost kapilari icin) ───────────────────────────
    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    // ── Komutan HP (v5 — CP'den bagimsiz) ────────────────────────────────
    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;

    // ── DDA (DifficultyManager icin) ─────────────────────────────────────
    public float SmoothedPowerRatio { get; private set; } = 1f;

    // ── Dahili ────────────────────────────────────────────────────────────
    float _lastDmgTime    = -99f;
    int   _riskBonusLeft  = 0;
    float _expectedCP     = 200f;

    static readonly int[]    TIER_CP    = { 0, 500, 1500, 4000, 9000 };
    static readonly string[] TIER_NAMES =
        { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
    // Komutan max HP tier'e gore (RefreshTier'da guncellenir)
    static readonly int[]    COMMANDER_HP_BY_TIER = { 500, 700, 950, 1200, 1500 };
    const  int MAX_BULLETS = 5;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP           = startCP;
        CommanderMaxHP = COMMANDER_HP_BY_TIER[0];
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Komutan Hasar Alma (v5) ───────────────────────────────────────────
    /// <summary>
    /// Dusman temasinda veya boss saldirısında cagrilir.
    /// CP ARTIK DUSMEZ — sadece CommanderHP azalir.
    /// </summary>
    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        CommanderHP = Mathf.Max(0, CommanderHP - amount);
        GameEvents.OnCommanderDamaged?.Invoke(amount, CommanderHP);
        GameEvents.OnPlayerDamaged?.Invoke(amount);   // hasar flash icin
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

        if (CommanderHP <= 0) GameEvents.OnGameOver?.Invoke();
    }

    /// <summary>Komutan HP'yi yeniler (HealCommander kapisi).</summary>
    public void HealCommander(int amount)
    {
        CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + amount);
        GameEvents.OnCommanderHealed?.Invoke(CommanderHP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
        Debug.Log($"[Commander] Heal +{amount} → {CommanderHP}/{CommanderMaxHP}");
    }

    // ── Kill Odulu ───────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP = Mathf.Min(CP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── Kapi Etkisi ──────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

        switch (data.effectType)
        {
            // ── Var olan kapı tipleri ────────────────────────────────────
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * 1.2f);
                break;

            // AddBullet: eski sahnelerle uyumluluk — artık piyade asker de ekler
            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                }
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade);
                break;

            case GateEffectType.Merge:
                HandleMerge();
                break;

            case GateEffectType.PathBoost_Piyade:
                CP        += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;

            case GateEffectType.PathBoost_Mekanize:
                CP          += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Mekanik");
                break;

            case GateEffectType.PathBoost_Teknoloji:
                CP            += Mathf.RoundToInt(data.effectValue * scale * bonus);
                TeknolojiPath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;

            case GateEffectType.NegativeCP:
                CP = Mathf.Max(50, CP - Mathf.RoundToInt(data.effectValue * scale));
                break;

            case GateEffectType.RiskReward:
                int pen = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(100, CP - pen);
                _riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
                break;

            // ── v3 Yeni Kapi Tipleri ─────────────────────────────────────
            case GateEffectType.AddSoldier_Piyade:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.AddSoldier_Mekanik:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.AddSoldier_Teknoloji:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.HealCommander:
                HealCommander(Mathf.RoundToInt(data.effectValue));
                break;

            case GateEffectType.HealSoldiers:
                float healPct = Mathf.Clamp(data.effectValue, 0f, 1f);
                ArmyManager.Instance?.HealAll(healPct);
                ShowPopupMessage($"ASKER +%{Mathf.RoundToInt(healPct * 100)}");
                break;
        }

        // Risk bonus sayaci
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        CP = Mathf.Clamp(CP, 50, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    // ── Merge: path-bazli carpan ─────────────────────────────────────────
    void HandleMerge()
    {
        // Asker merge (yeni sistem)
        bool mergeOccurred = ArmyManager.Instance != null &&
                             ArmyManager.Instance.TryMerge();

        // CP carpani (eski+yeni birlikte calisir)
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float multiplier;
        string role = "none";

        if (total < 1f)
        {
            multiplier = 1.1f;
        }
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

        CP = Mathf.RoundToInt(CP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();

        Debug.Log($"[Merge] CP x{multiplier} | Asker merge: {mergeOccurred}");
    }

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged()
    {
        // Komutan max HP'yi guncelle (HP eksik dusmesin — fark kadar ekle)
        int oldMax = CommanderMaxHP;
        CommanderMaxHP = COMMANDER_HP_BY_TIER[Mathf.Clamp(CurrentTier - 1, 0, 4)];
        if (CommanderMaxHP > oldMax)
        {
            int bonus = CommanderMaxHP - oldMax;
            CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + bonus);
        }
        GameEvents.OnTierChanged?.Invoke(CurrentTier);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── DDA Yardimcilari ─────────────────────────────────────────────────
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

    // ── Sinerji ──────────────────────────────────────────────────────────
    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 2f) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    // ── Popup yardimci (HUD yok olabilir) ────────────────────────────────
    void ShowPopupMessage(string msg)
        => GameEvents.OnSynergyFound?.Invoke(msg); // HUD popup bunu yakaliyor

    // ── Getterlar ─────────────────────────────────────────────────────────
    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}
```

### ProgressionConfig.cs
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
    [Range(0.5f, 1.5f)] public float playerCPScalingFactor = 0.9f;

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

### SimpleCameraFollow.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Runner Kamera (Claude)
/// X sabit — serit degistirince kamera sallanmaz.
/// Cinemachine GEREKMIYOR. Main Camera'ya attach et.
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;
    public float heightOffset = 9f;
    public float backOffset   = 11f;
    public float followSpeed  = 12f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(0f, target.position.y + heightOffset, target.position.z - backOffset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
```

### SpawnManager.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v11 (Claude)
///
/// v11: Kapı isimleri ve renkleri netleştirildi.
///   Her kapı tipi için belirgin emoji prefix + açık isim.
///   Renk = tip kategorisi (yeşil=artı, kırmızı=eksi, mor=özel, sarı=risk)
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

    float _nextGateZ   = 40f;
    float _nextWaveZ   = 55f;
    bool  _bossSpawned = false;

    DifficultyManager.EnemyStats _stats;
    bool       _statsReady  = false;
    GateData[] _runtimeGates;
    float      _totalWeight = 0f;

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

    // ── KAPILARIN GÖRSEL ŞEMASI ───────────────────────────────────────────
    //
    //  YEŞİL  = CP artışı veya asker ekleme (pozitif kaynak)
    //  GRİ    = Mekanik asker (zırhlı, yavaş ateş)
    //  MAVİ   = Teknoloji asker (hızlı ateş, kırılgan)
    //  MOR    = Özel (Merge, buff)
    //  PEMBE  = Heal (komutan veya asker)
    //  SARI   = Risk/ödül
    //  TURUNCU= Çarpan bonus
    //  KIRMIZI= Negatif
    //
    //  İSİM FORMATI: "[etki] [değer]"  →  oyuncu bir bakışta anlasın
    //
    void BuildRuntimeGates()
    {
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            // ── CP Artışı (yeşil) ──────────────────────────────────────────
            MakeGate("CP  +80",   GateEffectType.AddCP,  80f, new Color(0.15f,0.80f,0.15f,0.80f), 0.22f),
            MakeGate("CP  +45",   GateEffectType.AddCP,  45f, new Color(0.15f,0.80f,0.15f,0.80f), 0.18f),

            // ── Asker Ekleme ───────────────────────────────────────────────
            // Piyade: parlak yeşil, tüfek savaşçısı
            MakeGate("PIY x2",    GateEffectType.AddSoldier_Piyade,    30f, new Color(0.1f,0.90f,0.3f,0.85f),  0.08f),
            // Mekanik: koyu gri, minigun taşıyıcı
            MakeGate("MEK x2",    GateEffectType.AddSoldier_Mekanik,   30f, new Color(0.55f,0.55f,0.55f,0.85f),0.08f),
            // Teknoloji: parlak mavi, plazma savaşçısı
            MakeGate("TEK x2",    GateEffectType.AddSoldier_Teknoloji, 30f, new Color(0.1f,0.45f,1.0f,0.85f),  0.07f),

            // ── Merge (mor) ───────────────────────────────────────────────
            // Mor + "MERGE" yazısı → oyuncu zamanla öğrenir (3 aynı asker birleşir)
            MakeGate("MERGE",     GateEffectType.Merge, 0f, new Color(0.65f,0.05f,0.95f,0.85f), 0.08f),

            // ── Heal (pembe/kırmızı) ──────────────────────────────────────
            MakeGate("KMT +HP",   GateEffectType.HealCommander, 300f, new Color(1.0f,0.2f,0.55f,0.80f), 0.05f),
            MakeGate("ASK HP+",   GateEffectType.HealSoldiers,  0.5f, new Color(0.5f,1.0f,0.55f,0.80f), 0.04f),

            // ── PathBoost (turuncu — küçük ağırlık) ──────────────────────
            MakeGate("PIY +25%",  GateEffectType.PathBoost_Piyade,   50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.04f),
            MakeGate("MEK +25%",  GateEffectType.PathBoost_Mekanize, 50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.04f),
            MakeGate("TEK +25%",  GateEffectType.PathBoost_Teknoloji,50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.03f),

            // ── Özel ──────────────────────────────────────────────────────
            MakeGate("CP x1.2",   GateEffectType.MultiplyCP,   1.2f, new Color(1.0f,0.75f,0.0f,0.80f), 0.04f),
            MakeGate("RISK !",    GateEffectType.RiskReward,    0f,  new Color(1.0f,0.85f,0.0f,0.80f), 0.04f),
            MakeGate("+MERMI",    GateEffectType.AddBullet,    40f,  new Color(0.5f,0.0f,0.85f,0.80f), 0.04f),

            // ── Negatif (kırmızı) ─────────────────────────────────────────
            MakeGate("CP -60",    GateEffectType.NegativeCP, 60f, new Color(0.9f,0.1f,0.1f,0.80f), 0.05f),
        };
        // Toplam ağırlık: 0.22+0.18+0.08+0.08+0.07+0.08+0.05+0.04+0.04+0.04+0.03+0.04+0.04+0.04+0.05 = 1.08 → normalize edilecek
        // CacheWeights() bölme yapmıyor, WeightedRandom toplam üzerinden hesaplıyor — sorun yok.
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

        while (pz + spawnAhead >= _nextGateZ) { SpawnGatePair(_nextGateZ); _nextGateZ += gateSpacing; }
        while (pz + spawnAhead >= _nextWaveZ) { SpawnEnemyWave(_nextWaveZ); _nextWaveZ += waveSpacing; }
    }

    void TryFindPlayer()
    { if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform; }

    void SpawnGatePair(float zPos)
    {
        bool  pity   = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;
        float offset = ROAD_HALF_WIDTH * 0.40f;
        SpawnGate(PickGate(pity), new Vector3(-offset, 1.5f, zPos));
        SpawnGate(PickGate(pity), new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        if (data == null) return;
        GameObject obj;
        if (gatePrefab != null)
        {
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(2.2f, 3.2f, 1f);
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.95f,1f,1.5f);
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) { gate.gateData = data; gate.Refresh(); }
        Destroy(obj, 45f);
    }

    // ── Düşman Dalgası ────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   cnt  = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f) cnt = Mathf.Min(cnt+1, 9);

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
        int   cols=Mathf.Min(n,4), rows=Mathf.CeilToInt((float)n/cols), pl=0;
        float gap =Mathf.Max((ROAD_HALF_WIDTH*1.6f)/cols,2.2f), sx=-(gap*(cols-1))*0.5f;
        for(int r=0;r<rows&&pl<n;r++)
            for(int c=0;c<cols&&pl<n;c++)
            { PlaceEnemy(new Vector3(Mathf.Clamp(sx+c*gap,-ROAD_HALF_WIDTH+1f,ROAD_HALF_WIDTH-1f),1.2f,z+r*3f)); pl++; }
    }

    void HeavyWave(float z, int n)
    { for(int i=0;i<n;i++) PlaceEnemy(new Vector3(Random.Range(-3f,3f),1.2f,z+i*2.5f)); }

    void FlankWave(float z, int n)
    {
        int h=n/2;
        for(int i=0;i<h;i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
        }
        if(n%2==1) PlaceEnemy(new Vector3(0f,1.2f,z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        foreach(Collider c in Physics.OverlapSphere(pos,1.2f))
            if(c.CompareTag("Enemy")){ pos.x+=2.4f; break; }
        pos.x=Mathf.Clamp(pos.x,-ROAD_HALF_WIDTH+0.8f,ROAD_HALF_WIDTH-0.8f);

        GameObject obj;
        if(enemyPrefab!=null) obj=Instantiate(enemyPrefab,pos,Quaternion.identity);
        else
        {
            obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position=pos;
            Destroy(obj.GetComponent<Collider>());
            var cc=obj.AddComponent<CapsuleCollider>(); cc.isTrigger=true;
            var rb=obj.AddComponent<Rigidbody>(); rb.isKinematic=true;
            obj.tag="Enemy"; obj.AddComponent<Enemy>();
        }
        obj.GetComponent<Enemy>()?.Initialize(_stats);
    }
}
```
#Soliderunit.cs
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
---

## KULLANIM SABLONU
```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
GitHub: https://github.com/LpeC0/Top-End-War
MASTER PROMPT: [bu dosyanin tamami]

Scriptler: PlayerController, PlayerStats, SimpleCameraFollow,
GameEvents, GateData, Gate, SpawnManager, GameHUD, ObjectPooler,
ChunkManager, MorphController, Enemy, Bullet, EnemyHealthBar,
ProgressionConfig, DifficultyManager, GameOverUI, GateFeedback

[X] yazmak istiyorum.
Unity 6.3 LTS URP, Rigidbody YOK, Input Legacy, DOTween kurulu.
xLimit=8, Sprites/Default shader, unicode yok, Namespace yok.
GameEvents: Action<> pattern, Raise...() metod yok.
PlayerStats.CP property (field degil).
```

---

## DEGISIKLIK GECMISI
```
v1-v3   Grok+Gemini+Claude: Temel sistem
v4-v6   Claude: Drag, pool, morph, spawn
v7      Claude: DDA, RiskReward, pity timer, 3 dalga
v8      Claude: HP bar, GameOver, GateFeedback
v9      Claude: MorphController crash fix (PrewarmModels)
v10     Claude: GPT tahribati temizlendi — GameEvents Action<> geri alindi,
               PlayerStats.CP property korundu, namespace kaldirildi,
               HandleMerge PlayerStats icine alindi, DESIGN_BIBLE v3
```
