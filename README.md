# Top End War — README & AI Master Prompt

> **Bu dosyayı herhangi bir yapay zekaya yapıştırarak projeyi sıfırdan anlatabilirsin.**

---

## PROJE ÖZETİ

**Oyun Adı:** Top End War  
**Motor:** Unity 6.3 LTS (URP, 3D)  
**Platform:** Android / iOS  
**Repo:** https://github.com/LpeC0/Top-End-War  
**Tür:** Runner / Auto-Shooter  

---

## CORE LOOP

1. **Koş:** Player Z ekseninde otomatik ilerler. Parmak/fare **sürükleyerek** şerit değiştirir.
2. **Kapıdan Geç:** Yolda matematiksel kapılar. Şerit seçerek kapı belirlenir.
3. **CP Artar:** Combat Power yükseldikçe ordu morph eder (Tier atlar).
4. **Otomatik Savaş:** Player önündeki düşmanlara otomatik ateş.
5. **Boss Savaşı:** Koşu sonu Boss. All-In veya Split Overload kapısı.
6. **Meta Döngü:** Boss loot → kalıcı gelişim → Türkiye haritasında yeni bölge.

---

## TEKNİK DURUM

### Hierarchy
```
SampleScene
  ├── Directional Light
  ├── PoolManager         (ObjectPooler.cs)
  ├── Player              (PlayerController + PlayerStats, Tag:"Player")
  │     └── FirePoint
  ├── Main Camera         (SimpleCameraFollow → target:Player)
  ├── GateSpawner         (GateSpawner.cs)
  ├── ChunkManager        (ChunkManager.cs)
  ├── BulletPreFab        (Assets/PreFabs/)
  ├── GatePreFab          (Assets/PreFabs/)
  ├── RoadChunk           (Assets/PreFabs/)
  └── Canvas
        └── HUD_Panel     (GameHUD.cs)
```

### Script Tablosu
| Script | Açıklama | Yazar |
|--------|----------|-------|
| PlayerController.cs | Drag input, transform hareketi, auto-shoot | Claude |
| PlayerStats.cs | CP, path, tier, event singleton | Claude |
| SimpleCameraFollow.cs | X sabit runner kamerası | Claude |
| Enemy.cs | Can, TakeDamage | Claude |
| Bullet.cs | Trigger, ObjectPooler ile SetActive | Claude |
| GameEvents.cs | Global event merkezi | Gemini+Claude |
| GateData.cs | ScriptableObject kapı verisi | Gemini+Claude |
| Gate.cs | Fiziksel kapı, TMP yazı | Gemini+Claude |
| GateSpawner.cs | Procedural kapı spawn | Claude |
| GameHUD.cs | Observer pattern UI | Claude |
| ObjectPooler.cs | Queue tabanlı nesne havuzu | Gemini |
| ChunkManager.cs | Sonsuz yol chunk sistemi | Gemini |

### Plugins
- **DOTween** — kurulu, henüz aktif kullanılmıyor

### Değiştirilmez Teknik Kararlar
- Player **Rigidbody YOK** — hareket `transform.position`
- Kamera **X sabit**, sadece Z takip — `SimpleCameraFollow` (LateUpdate)
- Şerit: **Drag (sürükleme)** — basılı tut + sürükle, 40px eşikte şerit değişir
- Ateş: `Physics.BoxCast` (OverlapSphere değil)
- Input: **Input Manager (Old/Legacy)**
- **Cinemachine YOK**
- Gate: **Rigidbody(IsKinematic=true)** + **BoxCollider(IsTrigger=true)**
- CP/path/tier → **PlayerStats singleton** — PlayerController dokunmaz
- UI → **Observer Pattern** (GameEvents) — PlayerStats fırlatır, GameHUD dinler
- Mermi → **ObjectPooler.SpawnFromPool("Bullet",...)** — Destroy değil
- Zemin → **ChunkManager** (chunkLength=50f, initialChunks=5)
- `forwardSpeed` = 10f, `spacingBetweenGates` = 50f
- Player objesinde **Bullet.cs ve Enemy.cs OLMAMALI** (Z=44.8 bug sebebi)

---

## OYUN MEKANİKLERİ

### Kapı Tipleri
| Tip | Etki |
|-----|------|
| AddCP | CP + effectValue |
| MultiplyCP | CP × effectValue |
| Merge | CP × 1.8 |
| PathBoost_Piyade | CP+60, PiyadePath+20 |
| PathBoost_Mekanize | CP+60, MekanizePath+20 |
| PathBoost_Teknoloji | CP+60, TeknolojiPath+20 |
| NegativeCP | CP - effectValue (min 20) |

### Hibrit Evrim (3 Yol)
- **Piyade:** Gönüllü Er → Elit Komando → Gatling Timi → Hava İndirme → Sürü Drone
- **Mekanize:** Zırhlı Jip → Hafif Tank → Ağır Tank → Kuşatma Tankı → Yürüyen Hisar
- **Teknoloji:** Enerji Eri → Plazma Tüfekli → Yürüyen Mech → Lazer Titanı → Yörüngesel Saldırı

### Sinerji Bonusları
| Kombinasyon | Bonus |
|-------------|-------|
| Piyade+Mekanize | "Exosuit Komutu" +%30 Direnç |
| Piyade+Teknoloji | "Drone Takımı" +%25 Alan |
| Mekanize+Teknoloji | "Füzyon Robotu" ×1.5 Boss |
| 3'ü dengeli >%25 | "PERFECT GENETICS" ×2.0 |

### Tier Eşikleri
| Tier | CP | İsim |
|------|----|------|
| 1 | 0 | Gönüllü Er |
| 2 | 300 | Elit Komando |
| 3 | 800 | Gatling Timi |
| 4 | 2000 | Hava İndirme |
| 5 | 5000 | Sürü Drone |

### Boss: Gökmedrese Muhafızı (Sivas)
- HP: 3.000 | Faz1: Taş Zırh | Faz2: Minyon | Faz3: Çekirdek
- Overload: All-In (Altın) vs Split (Mavi)

---

## YAPILACAKLAR

| # | Görev | Durum | Yazar |
|---|-------|-------|-------|
| 1 | Player hareket + Lerp şerit | ✅ | Claude |
| 2 | Kamera X sabit | ✅ | Claude |
| 3 | Auto-Shoot BoxCast | ✅ | Claude |
| 4 | Drag input (sürükle şerit değiştir) | ✅ | Claude |
| 5 | GameEvents + GateData ScriptableObject | ✅ | Gemini+Claude |
| 6 | Gate trigger + GateSpawner | ✅ | Claude |
| 7 | GameHUD CP+Tier+Path+Popup | ✅ | Claude |
| 8 | ObjectPooler entegrasyonu | ✅ | Gemini |
| 9 | ChunkManager sonsuz yol | ✅ | Gemini |
| 10 | Bullet → ObjectPooler tam bağlantı | ✅ | Claude |
| 11 | Kapı üstü TextMeshPro yazı | ✅ | Claude |
| 12 | Prefab Swap Morph (tier + particle) | 🔲 | - |
| 13 | Boss Savaşı (faz + Overload) | 🔲 | - |
| 14 | Meta UI (Türkiye haritası, upgrade) | 🔲 | - |

---

## YARDIM İSTEME ŞABLONU

```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
GitHub: https://github.com/LpeC0/Top-End-War
README (master prompt): [bu dosyanın tamamı]

Mevcut scriptler:
PlayerController, PlayerStats, SimpleCameraFollow, Enemy, Bullet,
GameEvents, GateData, Gate, GateSpawner, GameHUD, ObjectPooler, ChunkManager

Şu an [X] sistemini yazmak istiyorum.
Sadece [X] için C# kodu yaz, diğer scriptlere dokunma.
Unity 6.3 LTS, URP, Rigidbody YOK, Input Manager (Old), DOTween kurulu.
```

---

## TÜM SCRIPTLER

---

### PlayerController.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi
/// Rigidbody YOK. Drag (sürükleme) ile şerit değiştirme.
/// Parmak/fare basılı tutulup sürüklenince şerit değişir.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    public float laneSwitchSpeed = 10f;
    public float laneDistance    = 3.5f;

    [Header("Ateş")]
    public GameObject bulletPrefab;
    public Transform  firePoint;
    public float      fireRate   = 3f;

    [Header("Drag Ayarı")]
    public float dragThreshold = 40f;

    int   currentLane  = 1;
    float targetX      = 0f;
    float nextFireTime = 0f;

    bool    isDragging      = false;
    Vector2 lastDragPos;
    float   accumulatedDrag = 0f;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
    }

    void Update()
    {
        HandleDragInput();
        MovePlayer();
        AutoShoot();
    }

    void HandleDragInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))  ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(+1);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging      = true;
            lastDragPos     = Input.mousePosition;
            accumulatedDrag = 0f;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            float deltaX     = ((Vector2)Input.mousePosition).x - lastDragPos.x;
            accumulatedDrag += deltaX;
            lastDragPos      = Input.mousePosition;

            if (accumulatedDrag > dragThreshold)
            {
                ChangeLane(+1);
                accumulatedDrag = 0f;
            }
            else if (accumulatedDrag < -dragThreshold)
            {
                ChangeLane(-1);
                accumulatedDrag = 0f;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging      = false;
            accumulatedDrag = 0f;
        }
    }

    void ChangeLane(int dir)
    {
        currentLane = Mathf.Clamp(currentLane + dir, 0, 2);
        targetX     = (currentLane - 1) * laneDistance;
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * laneSwitchSpeed);
        p.y  = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (Time.time < nextFireTime) return;
        if (!firePoint) return;

        RaycastHit hit;
        if (Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(laneDistance * 0.4f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, 22f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Vector3 dir = (hit.transform.position - firePoint.position).normalized;

                GameObject b;
                if (ObjectPooler.Instance != null)
                    b = ObjectPooler.Instance.SpawnFromPool("Bullet", firePoint.position, Quaternion.LookRotation(dir));
                else
                {
                    b = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
                    Destroy(b, 3f);
                }

                if (b != null)
                {
                    Rigidbody rb = b.GetComponent<Rigidbody>();
                    if (rb) rb.linearVelocity = dir * 28f;
                }

                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }
}
```

---

### PlayerStats.cs
```csharp
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Başlangıç")]
    public int startCP = 100;

    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;

    static readonly int[]    tierCP    = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames = { "Gönüllü Er","Elit Komando","Gatling Timi","Hava İndirme","Sürü Drone" };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

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

        if (Mathf.Min(p,Mathf.Min(m,t)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p>0.5f && m>0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");  return; }
        if (p>0.5f && t>0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takımı");    return; }
        if (m>0.4f && t>0.3f)  { GameEvents.OnSynergyFound?.Invoke("Füzyon Robotu");   return; }
    }

    public string GetTierName() => tierNames[Mathf.Clamp(CurrentTier-1, 0, 4)];
}
```

---

### SimpleCameraFollow.cs
```csharp
using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Kamera Oturumu")]
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

---

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
}
```

---

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
    [Header("Görsel")]
    public string gateText  = "+60";
    public Color  gateColor = new Color(0f, 0.7f, 1f, 0.6f);

    [Header("Etki")]
    public GateEffectType effectType  = GateEffectType.AddCP;
    public float          effectValue = 60f;
}
```

---

### Gate.cs
```csharp
using UnityEngine;
using TMPro;

public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public TextMeshPro labelText;

    void Start()
    {
        if (gateData == null) return;
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.color     = Color.white;
            labelText.fontSize  = 8f;
            labelText.alignment = TextAlignmentOptions.Center;
        }
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(r.material);
            mat.color = gateData.gateColor;
            r.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log($"Kapıdan geçildi: {gateData.gateText} | Yeni CP: {stats.CP}");
        }
        Destroy(gameObject);
    }
}
```

---

### GateSpawner.cs
```csharp
using UnityEngine;

public class GateSpawner : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GateData[] gateDataList;

    [Header("Spawn Ayarları")]
    public float spawnAheadDistance  = 35f;
    public float spacingBetweenGates = 50f;
    public float laneOffset          = 3.5f;

    float nextSpawnZ = 30f;

    void Update()
    {
        if (playerTransform == null || gatePrefab == null) return;
        while (playerTransform.position.z + spawnAheadDistance >= nextSpawnZ)
        {
            SpawnGatePair(nextSpawnZ);
            nextSpawnZ += spacingBetweenGates;
        }
    }

    void SpawnGatePair(float zPos)
    {
        if (gateDataList == null || gateDataList.Length == 0) return;
        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];
        SpawnGate(left,  new Vector3(-laneOffset, 1.5f, zPos));
        SpawnGate(right, new Vector3( laneOffset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 30f);
    }
}
```

---

### GameHUD.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameHUD : MonoBehaviour
{
    [Header("CP Göstergesi")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barları")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;
        GameEvents.OnCPUpdated    += OnCPUpdated;
        GameEvents.OnTierChanged  += OnTierChanged;
        GameEvents.OnSynergyFound += OnSynergy;
        lastCP = stats.CP;
        if (cpText)   cpText.text   = stats.CP.ToString("N0");
        if (tierText) tierText.text = $"TİER {stats.CurrentTier} | {stats.GetTierName()}";
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated    -= OnCPUpdated;
        GameEvents.OnTierChanged  -= OnTierChanged;
        GameEvents.OnSynergyFound -= OnSynergy;
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
        if (delta != 0) ShowPopup(delta > 0 ? $"+{delta}" : $"{delta}", delta > 0 ? Color.cyan : Color.red);
        lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (tierText && stats != null) tierText.text = $"TİER {tier} | {stats.GetTierName()}";
        ShowPopup($"⭐ TİER {tier}!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) return;
        StopCoroutine("HideSynergy");
        synergyText.text  = name;
        synergyText.color = new Color(1f, 0.84f, 0f);
        StartCoroutine("HideSynergy");
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

---

### Bullet.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Mermi
/// ObjectPooler ile çalışır: Destroy yerine SetActive(false).
/// </summary>
public class Bullet : MonoBehaviour
{
    public int damage = 50;

    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), 3f);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(damage);
            ReturnToPool();
        }
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

### Enemy.cs
```csharp
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    int currentHealth;

    void Start() { currentHealth = maxHealth; }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0) Destroy(gameObject);
    }
}
```

---

### ObjectPooler.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

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
        else Destroy(gameObject);

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.parent = this.transform;
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectPool);
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

---

### ChunkManager.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [Header("Ayarlar")]
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
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        chunk.transform.SetParent(this.transform);
        activeChunks.Enqueue(chunk);
        spawnZ += chunkLength;
    }

    void DeleteOldChunk()
    {
        GameObject old = activeChunks.Dequeue();
        Destroy(old);
    }
}
```

---

## 💡 YAPAY ZEKA FİKİR ALIŞVERIŞI

### 🟡 TARTIŞMA #1 — Object Pooling
- **Claude:** Boss öncesi ekle. ✅ Uygulandı.
- **Gemini:** Kuruldu, entegre edildi. ✅
- **Grok:** Chunk ile birleştir, chunk silinince obje pool'a dönsün.

### 🟡 TARTIŞMA #2 — Sonsuz Yol
- **Claude:** Reverse movement önerdim.
- **Gemini (İtiraz):** BoxCast bozulur, Chunk Sistemi doğru. ✅ Uygulandı.
- **Grok:** Chunk + recycle (öne taşı). Transform ile stabil.

### 🟡 TARTIŞMA #3 — Morph Tekniği
- **Claude:** A (Prefab Swap) → C (Shader Dissolve).
- **Gemini:** A + particle. Mutabık.
- **Grok:** A + DOTween scale/position tween.

### 🟡 TARTIŞMA #4 — Dünya Haritası
- **Claude:** 2D PNG önce, globe sonra.
- **Gemini:** 2D destekliyorum.
- **Grok:** Canvas UI button'lar. Mapbox SDK ileride.

### 🟡 TARTIŞMA #5 — UI Optimizasyon
- **Grok:** TMPro + event güncelleme (Observer) ✅ zaten uygulandı.

### 🟡 TARTIŞMA #6 — DOTween Nerede?
- **Claude:** Popup uçuş, tier scale punch, boss HP bar. Henüz uygulanmadı.

---

## 🟢 KARARLAR

| Konu | Karar | Yazar |
|------|-------|-------|
| Kamera | X sabit, SimpleCameraFollow | Claude |
| Mimari | ScriptableObject + Observer + Singleton | Gemini→Claude |
| Başlangıç şehri | Sivas | Kullanıcı |
| Hareket | Rigidbody YOK | Claude |
| Input | Drag sürükleme, 40px eşik | Claude |
| Sonsuz yol | Chunk Sistemi | Gemini |
| Nesne havuzu | ObjectPooler Queue | Gemini |
| Animasyon | DOTween | Gemini |

---

## DEĞİŞİKLİK LOG

```
[Mart 2026] — İLK KURULUŞ (Grok + Gemini)
- Grok: Python CLI demo, core loop proof of concept
- Gemini: Unity kurulum, ScriptableObject+Observer önerisi

[Mart 2026] — CLAUDE v1-v2
- Tüm temel scriptler yazıldı (Claude)
- Swipe input eklendi (Claude)
- Gemini scriptleri ile çakışma temizlendi (Claude)

[Mart 2026] — GEMINI v4-v5
- ObjectPooler.cs Queue mimarisi (Gemini)
- forwardSpeed 10f, spacingBetweenGates 50f (Gemini)
- Z=44.8 bug fix: Player'daki yanlış scriptler silindi (Gemini)
- ChunkManager.cs sonsuz yol (Gemini)
- RoadChunk.prefab, DOTween eklendi (Gemini)

[Mart 2026] — CLAUDE v3 (Son)
- Drag input sistemi (swipe yerine sürükle) (Claude)
- Bullet.cs → ObjectPooler tam entegrasyon, SetActive (Claude)
- Gate.cs → TextMeshPro yazı düzeltildi (Claude)
- PlayerController → ObjectPooler.SpawnFromPool kullanımı (Claude)
- README.md oluşturuldu, tüm scriptler içine eklendi (Claude)
- MASTER_PROMPT v4 güncellendi (Claude)
```
