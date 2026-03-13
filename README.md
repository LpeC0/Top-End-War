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

TIER / CP:(Değiştirilir muhtemelen)
Tier1=0CP "Gonullu Er" 1 mermi
Tier2=300CP "Elit Komando" 2 mermi
Tier3=800CP "Gatling Timi" 3 mermi
Tier4=2000CP "Hava Indirme" 4 mermi
Tier5=5000CP "Suru Drone" 5 mermi

BOLUM (Sivas):(Ayarları yapılacak)
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
/// Serbest surukleme. xLimit=8 ile genis harita siniri.
/// Tier'a gore 1-5 mermi.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Ileri Hareket")]
    public float forwardSpeed = 10f;

    [Header("Yatay Hareket")]
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;   // RoadChunk genisligi ile uyumlu

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
/// Top End War — Oyuncu Veri Merkezi (Claude)
/// [DefaultExecutionOrder(-10)] → TierText bos baslamaz.
/// DIKKAT: GameEvents.cs'de OnPlayerDamaged ve OnGameOver olmali!
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

    static readonly int[] tierCP = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames =
    {
        "Gonullu Er",
        "Elit Komando",
        "Gatling Timi",
        "Hava Indirme",
        "Suru Drone"
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    // ── Dushmana carpma hasari ────────────────────────────────────────────
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

    // ── Oldurme odulu ─────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapi etkisi ───────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
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
        float p = PiyadePath / total;
        float m = MekanizePath / total;
        float t = TeknolojiPath / total;

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

/// <summary>
/// Army Gate Siege – Kamera Takip
/// Runner mantığı: X sabit (şerit değiştirince dünya sallanmaz),
/// sadece Z ve Y ekseninde Player'ı takip eder.
/// Cinemachine GEREKMİYOR. Main Camera'ya attach et, Target'a Player sürükle.
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    [Header("=== HEDEF ===")]
    public Transform target;          // Inspector'dan Player sürükle

    [Header("=== KAMERA OTURUMU ===")]
    public float heightOffset  =  9f; // Yukarı ne kadar
    public float backOffset    = 11f; // Arkaya ne kadar
    public float followSpeed   = 12f; // Takip yumuşaklığı (düşürünce daha "drone" hissi)

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("SimpleCameraFollow: Target atanmadı! Main Camera Inspector'ında Player'ı sürükle.");
            return;
        }

        // Hedef pozisyon:
        //   X = 0 (sabit, şerit değiştirince kamera sallanmaz)
        //   Y = Player Y + yükseklik
        //   Z = Player Z - geri mesafe
        Vector3 desired = new Vector3(
            0f,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        // Yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * followSpeed
        );

        // Her zaman Player'a bak (biraz yukarısına, boynun görünsün)
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
```

### GameEvents.cs
```csharp
using System;

/// <summary>
/// Top End War — Global Event Merkezi (Claude)
/// Bu dosya degismeden kalsin — tum scriptler buraya bagli.
/// </summary>
public static class GameEvents
{
    public static Action<int>    OnCPUpdated;       // CP degisti
    public static Action<int>    OnTierChanged;     // Tier atladi
    public static Action<string> OnPathBoosted;     // Path degisti
    public static Action         OnMergeTriggered;  // Merge kapisi
    public static Action<string> OnSynergyFound;    // Sinerji
    public static Action<int>    OnPlayerDamaged;   // Hasar flash (GameHUD dinler)
    public static Action         OnGameOver;        // CP bitti
}
```

### GateData.cs
```csharp
using UnityEngine;

public enum GateEffectType
{
    AddCP,
    MultiplyCP,
    Merge,
    PathBoost_Piyade,
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
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

### Gate.cs
```csharp
using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v5 (Claude)
///
/// PREFAB YAPISI:
///   GatePrefab (root)
///   ├── Gate.cs  +  BoxCollider(IsTrigger=true)  +  Rigidbody(IsKinematic=true)
///   ├── Panel  (Cube, scale 3x4x0.3)  ← panelRenderer slotuna sur
///   └── Label  (3D TextMeshPro)       ← labelText slotuna sur
///
/// GateMat materyali icin tek ayar (Inspector):
///   Shader: Particles/Standard Unlit
///   Rendering Mode: Transparent
///   Color Mode: COLOR  ← Multiply degil!
///   Albedo rengi: beyaz (kod halleder)
///
/// Bu script sadece material.color'u degistirir — baska hic bir sey yapmaz.
/// Shader property isimleriyle ugrasma yok.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool triggered = false;

    void Start()
    {
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (gateData == null) return;

        // ── Yazi ─────────────────────────────────────────────────────────
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.fontSize  = 9f;
            labelText.color     = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
        }

        // ── Renk ─────────────────────────────────────────────────────────
        // Sadece material.color kullan — shader property'lerine dokunma
        if (panelRenderer != null)
        {
            // Prefab kirlenmesin diye instance al
            Material mat = Instantiate(panelRenderer.sharedMaterial);
            mat.color    = gateData.gateColor; // Alpha deger GateData'dan gelir
            panelRenderer.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;                     // Cift tetiklenme engeli
        if (!other.CompareTag("Player")) return;

        triggered = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log("[Gate] " + gateData.gateText + " | Yeni CP: " + stats.CP);
        }

        Destroy(gameObject);
    }
}
```

### SpawnManager.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v4 (Claude)
/// Dushman iç içe gecmez: Physics.OverlapSphere ile doluluk kontrolu yapilir.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn Mesafeleri")]
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
            Debug.Log("[SpawnManager] BOSS ZAMANI! Z=" + pz);
            // TODO: BossManager.Instance.StartBoss();
            return;
        }

        while (pz + spawnAhead >= nextGateZ)
        {
            SpawnGatePair(nextGateZ);
            nextGateZ += gateSpacing;
        }

        while (pz + spawnAhead >= nextWaveZ)
        {
            SpawnEnemyWave(nextWaveZ);
            nextWaveZ += waveSpacing;
        }
    }

    // ── Kapi Cifti ────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];

        float offset = ROAD_HALF_WIDTH * 0.45f; // ~3.6 birim
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate      = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 45f);
    }

    // ── Dushman Dalgasi ───────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        float progress  = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   count     = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));

        // Grid boyutlari
        int   cols      = Mathf.Min(count, 4);
        int   rows      = Mathf.CeilToInt((float)count / cols);

        // Ic ice gecmemesi icin yeterli aralik:
        // Dushman capsule radius ~0.5, min aralik = 2x radius + bosluk = 2.0
        float minGap    = 2.2f;
        float roadWidth = ROAD_HALF_WIDTH * 1.6f; // Kullanılabilir genişlik
        float colGap    = Mathf.Max(roadWidth / Mathf.Max(cols, 1), minGap);
        float rowGap    = 3.0f; // Satirlar arasi — iç içe girmesin

        // Grid'i ortala
        float startX = -(colGap * (cols - 1)) * 0.5f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = Mathf.Clamp(
                    startX + c * colGap,
                    -ROAD_HALF_WIDTH + 1f,
                     ROAD_HALF_WIDTH - 1f);
                float z = zPos + r * rowGap;

                Vector3 spawnPos = new Vector3(x, 1.2f, z);

                // OVERLAP KONTROLU: Cok yakinda baska dushman var mi?
                Collider[] nearby = Physics.OverlapSphere(spawnPos, 1.5f);
                bool tooClose = false;
                foreach (Collider col in nearby)
                    if (col.CompareTag("Enemy")) { tooClose = true; break; }

                if (!tooClose)
                {
                    Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                    spawned++;
                }
                else
                {
                    // Cok yakin — X'i kaydirarak tekrar dene
                    Vector3 altPos = new Vector3(
                        Mathf.Clamp(x + colGap * 0.5f, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                        1.2f, z + 1f);
                    Instantiate(enemyPrefab, altPos, Quaternion.identity);
                    spawned++;
                }
            }
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

/// <summary>
/// Top End War — HUD v5 (Claude)
///
/// CANVAS KURULUMU:
///   Canvas (Screen Space - Overlay)
///   ├── CPText      TextMeshProUGUI  — Anchor: TopCenter, Pos: 0, -40
///   ├── TierText    TextMeshProUGUI  — Anchor: TopCenter, Pos: 0, -80  ← text BOSH bırak
///   ├── PopupText   TextMeshProUGUI  — Anchor: Center,    Pos: 0, 50
///   ├── SynergyText TextMeshProUGUI  — Anchor: Center,    Pos: 0, -50
///   ├── DamageFlash Image            — Anchor: Stretch-All, Alpha=0, RaycastTarget=false
///   ├── PiyadeBar   Slider           — sol alt
///   ├── MekanizeBar Slider
///   └── TeknolojiBar Slider
///
/// Bu HUD objesini Canvas'in ALTINA koy, tum referanslari Inspector'dan bagla.
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

    [Header("Popup")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash (optional)")]
    public Image damageFlashImage;

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogError("GameHUD: PlayerStats.Instance NULL! Player objesinde PlayerStats.cs var mi?");
            return;
        }

        // Referans kontrolu
        if (cpText   == null) Debug.LogWarning("GameHUD: cpText atanmamis!");
        if (tierText == null) Debug.LogWarning("GameHUD: tierText atanmamis!");

        // Event baglantiları
        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;

        // Ilk degerler
        lastCP = stats.CP;

        if (cpText != null)
        {
            cpText.text  = stats.CP.ToString("N0");
            cpText.color = Color.white;
        }

        if (tierText != null)
        {
            tierText.text  = "TIER 1 | " + stats.GetTierName();
            tierText.color = Color.yellow;  // Belirgin renk
        }

        if (damageFlashImage != null)
            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);

        Debug.Log("GameHUD baslatildi. CP: " + stats.CP + " | Tier: " + stats.CurrentTier);
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

        if (cpText != null) cpText.text = cp.ToString("N0");

        float total = stats.PiyadePath + stats.MekanizePath + stats.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = stats.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = stats.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = stats.TeknolojiPath / total;
        }

        int delta = cp - lastCP;
        if (delta != 0)
            ShowPopup(delta > 0 ? "+" + delta : "" + delta,
                      delta > 0 ? Color.cyan : Color.red);
        lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (tierText != null && stats != null)
        {
            tierText.text  = "TIER " + tier + " | " + stats.GetTierName();
            tierText.color = Color.yellow;
        }
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
        if (damageFlashImage == null) return;
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
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

    IEnumerator HidePopup()
    {
        yield return new WaitForSeconds(1.2f);
        if (popupText) popupText.text = "";
    }

    IEnumerator HideSynergy()
    {
        yield return new WaitForSeconds(2.5f);
        if (synergyText) synergyText.text = "";
    }
}
```

### ObjectPooler.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Nesne Havuzu (Gemini)
/// Instantiate ve Destroy yerine SetActive(true/false) kullanarak performansı kurtarır.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
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
                obj.transform.parent = this.transform; // Hierarchy temiz kalsın
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Yeniden kuyruğa ekle ki döngü sağlansın
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}
```

### ChunkManager.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Sonsuz Yol Yöneticisi
/// Zemin parçalarını (Chunk) Player'ın önüne dizer, arkada kalanları siler.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject chunkPrefab;      // Hazırladığımız RoadChunk prefabı
    public Transform playerTransform;   // Karakterimiz
    public int initialChunks = 5;       // Ekranda aynı anda kaç zemin olacak?
    public float chunkLength = 50f;     // Plane Z scale 5 ise uzunluk 50'dir.

    private float spawnZ = 0f;          // Bir sonraki zeminin çıkacağı Z konumu
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    void Start()
    {
        // Oyun başlarken ilk zeminleri döşe
        for (int i = 0; i < initialChunks; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Player yeterince ilerlediyse, yeni chunk üret ve en eskisini sil
        // player Z konumu, arkada kalan chunk'ı geçtiğinde tetiklenir
        if (playerTransform.position.z - (chunkLength * 1.5f) > (spawnZ - (initialChunks * chunkLength)))
        {
            SpawnChunk();
            DeleteOldChunk();
        }
    }

    void SpawnChunk()
    {
        // Yeni zemini spawnla
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        
        // Zeminleri gruplamak için bu objenin (ChunkManager) altına koyalım
        chunk.transform.SetParent(this.transform);
        
        activeChunks.Enqueue(chunk);
        
        // Bir sonraki spawn noktasını ileri taşı
        spawnZ += chunkLength;
    }

    void DeleteOldChunk()
    {
        // Kuyruktan en baştakini (en eskisini) al ve yok et
        GameObject oldChunk = activeChunks.Dequeue();
        Destroy(oldChunk);
        // İleride performans için bunu da Object Pool'a çevirebiliriz ama zeminler için Destroy şu an mobilde bile çok dert değil.
    }
}
```

### MorphController.cs
```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Tier Morph Sistemi (Claude)
/// Tier atladığında Player modeli değişir + VFX oynar.
/// Player objesine ekle. Tier prefabları Inspector'dan bağla.
/// </summary>
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefabları (1'den 5'e sıralı)")]
    public GameObject[] tierPrefabs; // 0=Tier1, 1=Tier2 ... 4=Tier5

    [Header("VFX")]
    public GameObject morphParticlePrefab; // Patlama efekti (isteğe bağlı)
    public float      flashDuration = 0.3f;

    GameObject currentModel;
    int        currentTierIndex = 0;

    void Start()
    {
        // Oyun başlarken Tier 1 modelini göster
        GameEvents.OnTierChanged += OnTierChanged;
        SpawnModel(0);
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnTierChanged(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, tierPrefabs.Length - 1);
        if (index == currentTierIndex) return;

        StartCoroutine(MorphSequence(index));
    }

    IEnumerator MorphSequence(int newIndex)
    {
        // 1) Ekran flaşı — Player'ı gizle
        if (currentModel != null)
            currentModel.SetActive(false);

        // 2) Parçacık efekti
        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

        yield return new WaitForSeconds(flashDuration);

        // 3) Yeni modeli göster
        SpawnModel(newIndex);
        currentTierIndex = newIndex;

        Debug.Log($"Morph: Tier {newIndex + 1} modeli aktif");
    }

    void SpawnModel(int index)
    {
        // Eskiyi sil
        if (currentModel != null)
            Destroy(currentModel);

        // Prefab yoksa küp göster (placeholder)
        if (tierPrefabs == null || index >= tierPrefabs.Length || tierPrefabs[index] == null)
        {
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentModel.transform.SetParent(transform);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localScale    = Vector3.one;
            // Collider çakışmasın
            Destroy(currentModel.GetComponent<Collider>());
            return;
        }

        currentModel = Instantiate(tierPrefabs[index], transform.position, transform.rotation);
        currentModel.transform.SetParent(transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localScale    = Vector3.one;

        // Morph modelinin kendi collider'ı varsa kaldır (Player'ın collider'ı yeterli)
        foreach (Collider c in currentModel.GetComponentsInChildren<Collider>())
            Destroy(c);
    }
}
```

### Enemy.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dushman (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic:true) + CapsuleCollider(IsTrigger:true)
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 120;

    [Header("Hareket")]
    public float moveSpeed   = 4.5f;
    public float trackSpeedX = 1.5f;
    public float xLimit      = 8f;     // PlayerController ile AYNI

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

        if (pos.z < playerZ - 15f)
            Destroy(gameObject);
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
        if (!isDead && bodyRenderer != null)
            bodyRenderer.material.color = Color.white;
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
/// Top End War — Mermi
/// BulletPrefab'a ekle. SphereCollider(IsTrigger=true) + Rigidbody gerekli.
/// ObjectPooler ile çalışır: Destroy yerine SetActive(false).
/// </summary>
public class Bullet : MonoBehaviour
{
    public int damage = 50;

    // Pool'dan çıkınca kendini 3 saniye sonra geri gönder
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
        // Hızı sıfırla ki bir sonraki kullanımda sorun çıkmasın
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;

        gameObject.SetActive(false); // Destroy yerine havuza dön
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