# Top End War — README & Master Prompt v6

Projem Unity 6.3 LTS URP 3D mobil runner/auto-shooter oyunu: Top End War
GitHub repo: https://github.com/LpeC0/Top-End-War

---

## MASTER PROMPT

```
Projem Unity 6.3 LTS URP 3D mobil runner/auto-shooter oyunu: Top End War
GitHub repo: https://github.com/LpeC0/Top-End-War

OYUN TANIMI:
Runner/auto-shooter. Player otomatik koşar, sürükleyerek serbest hareket eder.
Yolda matematiksel kapılar (her biri ikiye ayrılmış, oyuncu birinden geçer).
Düşmanlar dalga halinde gelir, auto-shoot ile vurulur.
CP = savaş gücü = can. Tier atladıkça model morph eder, mermi sayısı artar.
1200 birimde boss, boss sonrası Türkiye haritasında yeni şehir.

TEKNİK KISITLAR (değiştirilemez):
- Player Rigidbody YOK — transform.position ile hareket
- Cinemachine YOK — SimpleCameraFollow (X sabit)
- Input Manager: Old/Legacy
- xLimit = 8 (PlayerController, Enemy, SpawnManager hepsinde AYNI)
- Gate: Rigidbody(IsKinematic) + BoxCollider(IsTrigger)
- Mermi: ObjectPooler (tag:"Bullet", size:20)
- Spawn: SpawnManager (kapı + düşman bağımsız Z slotları)
- Unicode sembol KULLANMA — LiberationSans desteklemiyor
- Player'a Bullet.cs veya Enemy.cs EKLEME

TIER / CP:
Tier1=0CP "Gonullu Er" 1 mermi
Tier2=300CP "Elit Komando" 2 mermi
Tier3=800CP "Gatling Timi" 3 mermi
Tier4=2000CP "Hava Indirme" 4 mermi
Tier5=5000CP "Suru Drone" 5 mermi

BOLUM (Sivas):
0-300: kolay, 2-3 dushman/dalga
300-800: orta, 3-5 dushman/dalga
800-1200: zor, 5-8 dushman/dalga
1200+: Boss (Gokmedrese Muhafizi, HP:3000, 3 faz)

HIERARCHY:
SampleScene
  PoolManager    → ObjectPooler (tag:Bullet, size:20)
  Player         → PlayerController + PlayerStats + MorphController [Tag:Player]
      FirePoint
  Main Camera    → SimpleCameraFollow
  SpawnManager   → SpawnManager
  ChunkManager   → ChunkManager
  Canvas
      CPText, TierText(bos birak), PopupText, SynergyText
      DamageFlash (Image, full stretch, alpha=0, RaycastTarget=false)
      PiyadeBar, MekanizeBar, TeknolojiBar (Slider)

Simdi bu projeyi tamamen anla ve [X] konusunda yardim et.
```

---

## TUM SCRIPTLER (TAM HALI)

### PlayerController.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v4 (Claude)
/// Serbest surukleme. xLimit=8. Tier bazli 1-5 mermi.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Ileri Hareket")]
    public float forwardSpeed = 10f;

    [Header("Yatay Hareket")]
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      fireRate    = 2.5f;
    public float      detectRange = 30f;

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
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
            targetX = Mathf.Clamp(targetX - 10f * Time.deltaTime, -xLimit, xLimit);
        if (Input.GetKey(KeyCode.RightArrow))
            targetX = Mathf.Clamp(targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0)) { isDragging = true;  lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))   { isDragging = false; }

        if (isDragging)
        {
            float delta = (Input.mousePosition.x - lastMouseX) * dragSensitivity;
            targetX    = Mathf.Clamp(targetX + delta, -xLimit, xLimit);
            lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (Time.time < nextFireTime || !firePoint) return;

        int tier        = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int bulletCount = tier;
        float spread    = 1.2f;

        RaycastHit hit;
        if (!Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.5f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, detectRange)
            || !hit.collider.CompareTag("Enemy")) return;

        for (int i = 0; i < bulletCount; i++)
        {
            float   offsetX  = (i - (bulletCount - 1) * 0.5f) * spread;
            Vector3 spawnPos = firePoint.position + new Vector3(offsetX, 0f, 0f);
            Vector3 dir      = (hit.transform.position - spawnPos).normalized;
            FireBullet(spawnPos, dir);
        }

        nextFireTime = Time.time + 1f / fireRate;
    }

    void FireBullet(Vector3 pos, Vector3 dir)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }
        if (b != null)
        {
            Rigidbody rb = b.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = dir * 28f;
        }
    }
}
```

### PlayerStats.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v4 (Claude)
/// CP = can. DefaultExecOrder(-10) → TierText bos baslamaz.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int startCP = 200;

    [Header("Hasar Korumasi")]
    public float invincibilityDuration = 1.2f;

    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;

    float lastDamageTime = -99f;

    static readonly int[]    tierCP    = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames =
    {
        "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone"
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    public void TakeContactDamage(int amount)
    {
        if (Time.time - lastDamageTime < invincibilityDuration) return;
        lastDamageTime = Time.time;

        int oldTier = CurrentTier;
        CP = Mathf.Max(10, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 10) GameEvents.OnGameOver?.Invoke();
    }

    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    public void ApplyGateEffect(GateData data)
    {
        int oldTier = CurrentTier;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue); break;
            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * data.effectValue); break;
            case GateEffectType.Merge:
                CP = Mathf.RoundToInt(CP * 1.8f);
                GameEvents.OnMergeTriggered?.Invoke(); break;
            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue);
                PiyadePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Piyade"); break;
            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue);
                MekanizePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize"); break;
            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue);
                TeknolojiPath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji"); break;
            case GateEffectType.NegativeCP:
                CP = Mathf.Max(20, CP - Mathf.RoundToInt(data.effectValue)); break;
        }

        CP = Mathf.Max(10, CP);
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    void RefreshTier()
    {
        for (int i = tierCP.Length - 1; i >= 0; i--)
            if (CP >= tierCP[i]) { CurrentTier = i + 1; return; }
        CurrentTier = 1;
    }

    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total == 0) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if (Mathf.Min(p, Mathf.Min(m, t)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p > 0.5f && m > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");  return; }
        if (p > 0.5f && t > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");    return; }
        if (m > 0.4f && t > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");   return; }
    }

    public string GetTierName() => tierNames[Mathf.Clamp(CurrentTier - 1, 0, 4)];
}
```

### SimpleCameraFollow.cs
```csharp
using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Kamera")]
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

### GameEvents.cs
```csharp
using System;

public static class GameEvents
{
    public static Action<int>    OnCPUpdated;
    public static Action<int>    OnTierChanged;
    public static Action<string> OnPathBoosted;
    public static Action         OnMergeTriggered;
    public static Action<string> OnSynergyFound;
    public static Action<int>    OnPlayerDamaged;
    public static Action         OnGameOver;
}
```

### GateData.cs
```csharp
using UnityEngine;

public enum GateEffectType
{
    AddCP, MultiplyCP, Merge,
    PathBoost_Piyade, PathBoost_Mekanize, PathBoost_Teknoloji,
    NegativeCP
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")]
    public string gateText  = "+60";
    public Color  gateColor = new Color(0f, 0.7f, 1f, 0.6f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 60f;
}
```

### Gate.cs
```csharp
using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi (Claude)
/// PREFAB: GatePrefab → Gate.cs + BoxCollider(IsTrigger) + Rigidbody(IsKinematic)
///   └── Panel (Cube 3x4x0.3) → panelRenderer buraya
///   └── Label (3D TextMeshPro) → labelText buraya
/// Panel materyali: herhangi bir URP/Lit mat — kod transparanligi halleder.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool triggered = false;

    void Start()    { if (gateData != null) ApplyVisuals(); }
    void OnEnable() { if (gateData != null) ApplyVisuals(); }

    void ApplyVisuals()
    {
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.fontSize  = 9f;
            labelText.color     = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
        }

        if (panelRenderer != null)
        {
            Material mat = new Material(panelRenderer.sharedMaterial);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",   0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            Color c = gateData.gateColor;
            c.a = 0.65f;
            mat.color = c;
            panelRenderer.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        other.GetComponent<PlayerStats>()?.ApplyGateEffect(gateData);
        Debug.Log("[Gate] " + gateData.gateText);
        Destroy(gameObject);
    }
}
```

### SpawnManager.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v3 (Claude)
/// xLimit=8. Kapi+dushman ayri Z slotlari. Boss: 1200 birim.
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
    public float waveSpacing = 32f;

    [Header("Zorluk")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    float nextGateZ   = 40f;
    float nextWaveZ   = 60f;
    bool  bossSpawned = false;

    void Update()
    {
        if (playerTransform == null) return;
        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            Debug.Log("BOSS! Z: " + pz);
            GameEvents.OnGameOver?.Invoke();
            return;
        }

        while (pz + spawnAhead >= nextGateZ) { SpawnGatePair(nextGateZ); nextGateZ += gateSpacing; }
        while (pz + spawnAhead >= nextWaveZ) { SpawnEnemyWave(nextWaveZ); nextWaveZ += waveSpacing; }
    }

    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;
        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];
        float offset = ROAD_HALF_WIDTH * 0.45f;
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 40f);
    }

    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;
        float progress = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   count    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));
        int   cols     = Mathf.Min(count, 4);
        int   rows     = Mathf.CeilToInt((float)count / cols);
        float colGap   = (ROAD_HALF_WIDTH * 1.4f) / Mathf.Max(cols, 1);
        float startX   = -(colGap * (cols - 1)) / 2f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = Mathf.Clamp(startX + c * colGap, -ROAD_HALF_WIDTH + 0.5f, ROAD_HALF_WIDTH - 0.5f);
                Instantiate(enemyPrefab, new Vector3(x, 1.2f, zPos + r * 2.8f), Quaternion.identity);
                spawned++;
            }
    }
}
```

### GameHUD.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameHUD : MonoBehaviour
{
    [Header("CP / Tier")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barlari")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash")]
    public Image damageFlashImage;

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogWarning("HUD: PlayerStats yok!"); return; }

        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;

        lastCP = stats.CP;
        if (cpText)   cpText.text   = stats.CP.ToString("N0");
        if (tierText) tierText.text = "TIER 1 | " + stats.GetTierName();
        if (damageFlashImage != null)
            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated     -= OnCPUpdated;
        GameEvents.OnTierChanged   -= OnTierChanged;
        GameEvents.OnSynergyFound  -= OnSynergy;
        GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
    }

    void OnCPUpdated(int cp)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;
        if (cpText) cpText.text = cp.ToString("N0");

        float total = stats.PiyadePath + stats.MekanizePath + stats.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = stats.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = stats.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = stats.TeknolojiPath / total;
        }

        int delta = cp - lastCP;
        if (delta != 0)
            ShowPopup(delta > 0 ? "+" + delta : "" + delta, delta > 0 ? Color.cyan : Color.red);
        lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (tierText && stats != null)
            tierText.text = "TIER " + tier + " | " + stats.GetTierName();
        ShowPopup("TIER " + tier + "!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) return;
        StopCoroutine("HideSynergy");
        synergyText.text  = name;
        synergyText.color = new Color(1f, 0.84f, 0f);
        StartCoroutine("HideSynergy");
    }

    void OnPlayerDamaged(int amount)
    {
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
        if (damageFlashImage == null) yield break;
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.5f);
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            damageFlashImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.5f, 0f, t / 0.45f));
            yield return null;
        }
        damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
    }

    void ShowPopup(string msg, Color color)
    {
        if (popupText == null) return;
        StopCoroutine("HidePopup");
        popupText.text  = msg;
        popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text   = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }
}
```

### ObjectPooler.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Nesne Havuzu (Gemini)
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool { public string tag; public GameObject prefab; public int size; }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

### ChunkManager.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Sonsuz Yol (Gemini)
/// RoadChunk prefabini Inspector'dan bagla. chunkLength=50.
/// RoadChunk scale: X=1.6 (genislik=16, xLimit=8 ile uyumlu)
/// </summary>
public class ChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Transform  playerTransform;
    public int        initialChunks = 5;
    public float      chunkLength   = 50f;

    float spawnZ = 0f;
    Queue<GameObject> activeChunks = new Queue<GameObject>();

    void Start() { for (int i = 0; i < initialChunks; i++) SpawnChunk(); }

    void Update()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.z - (chunkLength * 1.5f) > (spawnZ - (initialChunks * chunkLength)))
        { SpawnChunk(); DeleteOldChunk(); }
    }

    void SpawnChunk()
    {
        GameObject c = Instantiate(chunkPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        c.transform.SetParent(this.transform);
        activeChunks.Enqueue(c);
        spawnZ += chunkLength;
    }

    void DeleteOldChunk() { Destroy(activeChunks.Dequeue()); }
}
```

### MorphController.cs
```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Tier Morph (Claude)
/// Player'a ekle. Tier prefablari Inspector'dan bagla (5 slot, 0=Tier1).
/// </summary>
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefablari (0=Tier1 ... 4=Tier5)")]
    public GameObject[] tierPrefabs;

    [Header("VFX")]
    public GameObject morphParticlePrefab;
    public float      flashDuration = 0.3f;

    GameObject currentModel;
    int        currentTierIndex = 0;

    void Start()
    {
        GameEvents.OnTierChanged += OnTierChanged;
        SpawnModel(0);
    }

    void OnDestroy() { GameEvents.OnTierChanged -= OnTierChanged; }

    void OnTierChanged(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, tierPrefabs.Length - 1);
        if (index == currentTierIndex) return;
        StartCoroutine(MorphSequence(index));
    }

    IEnumerator MorphSequence(int newIndex)
    {
        if (currentModel != null) currentModel.SetActive(false);
        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);
        yield return new WaitForSeconds(flashDuration);
        SpawnModel(newIndex);
        currentTierIndex = newIndex;
    }

    void SpawnModel(int index)
    {
        if (currentModel != null) Destroy(currentModel);

        if (tierPrefabs == null || index >= tierPrefabs.Length || tierPrefabs[index] == null)
        {
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentModel.transform.SetParent(transform);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localScale    = Vector3.one;
            Destroy(currentModel.GetComponent<Collider>());
            return;
        }

        currentModel = Instantiate(tierPrefabs[index], transform.position, transform.rotation);
        currentModel.transform.SetParent(transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localScale    = Vector3.one;
        foreach (Collider col in currentModel.GetComponentsInChildren<Collider>()) Destroy(col);
    }
}
```

### Enemy.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dushman (Claude)
/// Tag: "Enemy" | Capsule → Rigidbody(IsKinematic) + CapsuleCollider(IsTrigger)
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 120;

    [Header("Hareket")]
    public float moveSpeed   = 4.5f;
    public float trackSpeedX = 1.5f;
    public float xLimit      = 8f;

    [Header("Hasar")]
    public int contactDamage = 50;
    public int cpReward      = 15;

    int      currentHealth;
    Renderer bodyRenderer;
    bool     isDead           = false;
    bool     hasDamagedPlayer = false;

    void Start()
    {
        currentHealth = maxHealth;
        bodyRenderer  = GetComponentInChildren<Renderer>();
    }

    void Update()
    {
        if (isDead || PlayerStats.Instance == null) return;

        float playerZ = PlayerStats.Instance.transform.position.z;
        Vector3 pos   = transform.position;

        if (pos.z > playerZ + 0.5f)
            pos.z -= moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, trackSpeedX * Time.deltaTime),
            -xLimit, xLimit);

        transform.position = pos;

        if (pos.z < playerZ - 15f) Destroy(gameObject);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;
        if (bodyRenderer != null) bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);
        if (currentHealth <= 0) Die();
    }

    void ResetColor()
    {
        if (!isDead && bodyRenderer != null) bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(cpReward);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasDamagedPlayer || isDead) return;
        hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(contactDamage);
        Die();
    }
}
```

### Bullet.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Mermi (Claude)
/// SphereCollider(IsTrigger) + Rigidbody. ObjectPooler ile SetActive.
/// </summary>
public class Bullet : MonoBehaviour
{
    public int damage = 50;

    void OnEnable()  { Invoke(nameof(ReturnToPool), 3f); }
    void OnDisable() { CancelInvoke(); }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        other.GetComponent<Enemy>()?.TakeDamage(damage);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }
}
```

---

## DEGISIKLIK LOGU

```
[Mart 2026] Grok+Gemini v1: Python demo, Unity kurulum, ScriptableObject oneri
[Mart 2026] Claude v1: PlayerController, PlayerStats, Gate, GameHUD
[Mart 2026] Gemini v4-v5: ObjectPooler, ChunkManager, Z=44.8 bug fix
[Mart 2026] Claude v2: Drag input, Bullet→ObjectPooler
[Mart 2026] Claude v3: MorphController, EnemySpawner, AddCPFromKill
[Mart 2026] Claude v4: SpawnManager, DefaultExecOrder, GateSpawner kaldirildi
[Mart 2026] Claude v5: Serbest drag, coklu mermi, enemy grid
[Mart 2026] Claude v6: xLimit=8 (genis yol), triggered flag, transparan gate,
             unicode kaldirildi, hasar flash, bolum uzunlugu (bossDistance=1200),
             RoadChunk X scale=1.6 notu
```