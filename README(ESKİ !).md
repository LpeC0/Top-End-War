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
/// Top End War — Oyuncu Hareketi v6 (Claude)
///
/// MATEMATIKAL ILERLEME:
///   Tier | Hasar | Atis/sn | DPS
///   1    |  60   |  1.5    |  90
///   2    |  95   |  2.5    | 237
///   3    | 145   |  4.0    | 580
///   4    | 210   |  6.0    |1260
///   5    | 300   |  8.5    |2550
///
/// Tier 2'ye gecince oyuncu hemen fark eder — dusmanlar cok daha hizli oluyor.
///
/// HEDEFLEME: Dusmanin Z hizina gore hafif "lead" (ileriden nisan) alir.
/// Dusmanlar kacarsa mermi geri kalandir degil, gidecekleri yere gider.
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
    public float      detectRange = 35f;

    // Tier bazli atis hizi (atis/saniye)
    static readonly float[] tierFireRates  = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    // Tier bazli hasar
    static readonly int[]   tierDamage     = {  60,   95,  145,  210,  300  };

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
    }

    /// <summary>Gate trigger icin Player'da Collider OLMALI (IsTrigger=false).</summary>
    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
        col.height    = 2f;
        col.radius    = 0.4f;
        col.isTrigger = false;
        Debug.LogWarning("[Player] CapsuleCollider eklendi! Inspector'dan kaydet.");
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
        if (!firePoint) return;

        int   tier     = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int   idx      = Mathf.Clamp(tier - 1, 0, 4);
        float fireRate = tierFireRates[idx];
        int   damage   = tierDamage[idx];

        if (Time.time < nextFireTime) return;

        // Hedef bul
        RaycastHit hit;
        if (!Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.55f, 1.2f, 0.5f),
                Vector3.forward, out hit,
                Quaternion.identity, detectRange)
            || !hit.collider.CompareTag("Enemy")) return;

        Transform target = hit.transform;

        // Lead hedefleme: Dusmanin hizina gore biraz onunu al
        // Mermi hizi 30, mesafe farkina gore gecikme hesapla
        float   dist    = Vector3.Distance(firePoint.position, target.position);
        float   travelT = dist / 30f; // Mermi kac saniyede ulasir
        Vector3 aimPos  = target.position + Vector3.back * (travelT * 4f); // Dusman Z'de -4/sn geliyor
        Vector3 dir     = (aimPos - firePoint.position).normalized;

        FireBullet(firePoint.position, dir, damage);
        nextFireTime = Time.time + 1f / fireRate;
    }

    void FireBullet(Vector3 pos, Vector3 dir, int damage)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }
        if (b == null) return;

        // Hasari ata
        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null) bullet.SetDamage(damage);

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }
}
```

### PlayerStats.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v5 (Claude)
/// [DefaultExecutionOrder(-10)] → TierText bos baslamaz.
///
/// ZORLUK DENGESI:
///   startCP = 200 (Tier 1)
///   invincibility = 0.8s (dusuruldu — daha az affedici)
///   Dusman hasar CP'nin %20-40'ini alabilir (mesafeye gore)
///   Oyun Over: CP 30'un altina dusunce (10 degil)
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f; // 1.2'den dusuruldu

    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    float lastDamageTime = -99f;
    int   riskBonusLeft  = 0;
    float expectedCP     = 200f;

    static readonly int[]    tierCP    = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames =
        { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    // ── Dusman carpma hasari ──────────────────────────────────────────────────
    public void TakeContactDamage(int amount)
    {
        if (Time.time - lastDamageTime < invincibilityDuration) return;
        lastDamageTime = Time.time;

        int oldTier = CurrentTier;
        // Minimum CP = 30 (10 degil — daha gercekci game over)
        CP = Mathf.Max(30, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 30) GameEvents.OnGameOver?.Invoke();
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
        if (data == null) return;
        int   oldTier = CurrentTier;
        float bonus   = riskBonusLeft > 0 ? 1.5f : 1f;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * bonus); break;
            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * data.effectValue); break;
            case GateEffectType.Merge:
                CP = Mathf.RoundToInt(CP * 1.8f);
                GameEvents.OnMergeTriggered?.Invoke(); break;
            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                PiyadePath += 20f; GameEvents.OnPathBoosted?.Invoke("Piyade"); break;
            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                MekanizePath += 20f; GameEvents.OnPathBoosted?.Invoke("Mekanize"); break;
            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                TeknolojiPath += 20f; GameEvents.OnPathBoosted?.Invoke("Teknoloji"); break;
            case GateEffectType.NegativeCP:
                CP = Mathf.Max(30, CP - Mathf.RoundToInt(data.effectValue)); break;
            case GateEffectType.RiskReward:
                int penalty = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(50, CP - penalty);
                riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft); break;
        }

        if (riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            riskBonusLeft--;
            if (riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft);
        }

        CP = Mathf.Max(30, CP);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    public void SetExpectedCP(float expected)
    {
        expectedCP = Mathf.Max(1f, expected);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
    {
        float raw = (float)CP / expectedCP;
        SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, raw, 0.08f);
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
        if (p > 0.5f && m > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu"); return; }
        if (p > 0.5f && t > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");   return; }
        if (m > 0.4f && t > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");  return; }
    }

    public string GetTierName()  => tierNames[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => riskBonusLeft;
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
/// Observer Pattern: Tum sistemler bu static event'ler uzerinden haberlesir.
/// Namespace yok — Unity'de en basit kullanim.
/// </summary>
public static class GameEvents
{
    // ── Oyuncu ───────────────────────────────────────────────────────────────
    public static Action<int>    OnCPUpdated;          // CP degisti (yeni deger)
    public static Action<int>    OnTierChanged;        // Tier atladi (yeni tier)
    public static Action<string> OnPathBoosted;        // Path guclendirildi
    public static Action         OnMergeTriggered;     // Merge kapisi gecildi
    public static Action<string> OnSynergyFound;       // Sinerji aktif
    public static Action<int>    OnPlayerDamaged;      // Hasar alindi (HUD flash)
    public static Action         OnGameOver;           // CP min'e dustu

    // ── Risk/Reward (YENI - Claude) ──────────────────────────────────────────
    // Negatif kapidan gectikten sonra sonraki 3 kapiya %50 bonus verilir
    public static Action<int>    OnRiskBonusActivated; // kalan bonus kapı sayısı

    // ── Zorluk & Spawn (YENI - DDA sistemi icin) ────────────────────────────
    // DifficultyManager her updateInterval'da bu event'i ateşler
    public static Action<float, float> OnDifficultyChanged; // (multiplier, playerPowerRatio)

    // ── Boss ─────────────────────────────────────────────────────────────────
    public static Action OnBossEncountered;
}
```

### GateData.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Kapi Etki Tipleri
/// RiskReward (YENI): CP -30% ANCAK sonraki 3 kapiya +50% bonus uygulanir.
/// Oyuncu "risk mi alayim?" diye dusunur — oyun derinligi artar.
/// </summary>
public enum GateEffectType
{
    AddCP,               // +deger  — guvenlii
    MultiplyCP,          // xdeger  — risk/odul
    Merge,               // x1.8    — nadir, guclu
    PathBoost_Piyade,    // Strateji yolu
    PathBoost_Mekanize,
    PathBoost_Teknoloji,
    NegativeCP,          // -deger  — saf ceza (az cikacak, %2-3)
    RiskReward           // -30% CP + sonraki 3 kapiya +50% bonus (Claude)
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

    // ── Hazir Renkler (Inspector icin referans) ──────────────────────────────
    // Yesil  = AddCP       new Color(0.2f, 0.8f, 0.2f, 0.65f)
    // Mavi   = MultiplyCP  new Color(0.1f, 0.4f, 1.0f, 0.65f)
    // Mor    = Merge       new Color(0.6f, 0.1f, 0.9f, 0.65f)
    // Turuncu= PathBoost   new Color(1.0f, 0.5f, 0.0f, 0.65f)
    // Kirmizi= Negative    new Color(0.9f, 0.1f, 0.1f, 0.65f)
    // Sari   = RiskReward  new Color(1.0f, 0.8f, 0.0f, 0.65f)
}

```

### Gate.cs
```csharp
using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v7 (Claude)
///
/// PREFAB YAPISI (tam):
///   GatePrefab  [root]
///   ├── Gate.cs
///   ├── BoxCollider    IsTrigger=true
///   ├── Rigidbody      IsKinematic=true
///   ├── Panel  (Quad, Scale 4,5,1)  ← panelRenderer slotuna sur
///   └── Label  (3D TMP)             ← labelText slotuna sur
///
/// MATERYAL (ARTIK KOD HALLEDIYOR — elle bir sey yapma):
///   GateMat sadece var olmali, herhangi bir shader olabilir.
///   Kod runtime'da shader'i "Sprites/Default"'a cevirir.
///   "Sprites/Default" = tam seffaf destekler, renk tam istedigin gibi gelir.
///   Panel uzerindeki MeshCollider otomatik silinir.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool _triggered = false;

    void Start()
    {
        // Panel'deki gereksiz collider'lari temizle
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable() { _triggered = false; }

    // SpawnManager runtime'da gateData atadiktan sonra cagirabilir
    public void Refresh() { ApplyVisuals(); FitBoxCollider(); }

    // ── Gorsel ────────────────────────────────────────────────────────────────
    void ApplyVisuals()
    {
        if (gateData == null) return;

        // Yazi
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

        // Renk — "Sprites/Default" shader her platformda, URP/Built-in ayirt etmeksizin
        // tam olarak istedigin rengi verir. Transparan destekler.
        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));

            Color c  = gateData.gateColor;
            c.a      = 0.72f;           // Transparan — 0=tamamen seffaf, 1=tam dolu
            mat.color = c;

            panelRenderer.material = mat;
        }
    }

    // ── Panel'deki gereksiz collider'lari sil ─────────────────────────────────
    void RemoveChildColliders()
    {
        // Root'taki BoxCollider (trigger) haric tum child collider'lari sil
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.gameObject == gameObject) continue; // Root'a dokunma
            Destroy(col);
        }
    }

    // ── Root BoxCollider'i Panel boyutuna gore ayarla ────────────────────────
    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;

        // Panel'in lokal boyutunu kullan (Quad Scale 4,5,1 → 4x5)
        Vector3 panelLocal = panelRenderer.transform.localScale;
        box.size   = new Vector3(panelLocal.x * 0.95f, panelLocal.y * 1.0f, 1.2f);
        box.center = Vector3.zero;
    }

    // ── Trigger ───────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log("[Gate] " + gateData.gateText + " → CP: " + stats.CP);
        }

        Destroy(gameObject);
    }
}
```

### SpawnManager.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v6 (Claude)
///
/// TAMAMEN BAGIMSIZ calisir:
///   - DifficultyManager yoksa mesafe bazli kendi hesabini yapar
///   - GateDataList bossa kendi ScriptableObject'lerini olusturur
///   - EnemyPrefab bossa primitive capsule kullanir
///   - GatePrefab bossa primitive quad kullanir
///
/// ZORLUK (standalone):
///   0-300m:    2-3 dusman, yavaş
///   300-800m:  4-6 dusman, orta
///   800-1200m: 6-8 dusman, hizli
///   1200m+:    Boss
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
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;

    float nextGateZ    = 40f;
    float nextWaveZ    = 55f;
    bool  bossSpawned  = false;

    // DDA
    DifficultyManager.EnemyStats _currentStats;
    bool _statsReady = false;

    // Runtime gate data (GateDataList bossa bunlar kullanilir)
    GateData[] _runtimeGates;

    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        BuildRuntimeGates();
        RefreshStats();
        GameEvents.OnDifficultyChanged += (m, r) => RefreshStats();
    }

    void OnDestroy()
    {
        GameEvents.OnDifficultyChanged -= (m, r) => RefreshStats();
    }

    void RefreshStats()
    {
        _currentStats = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetScaledEnemyStats()
            : FallbackStats();
        _statsReady = true;
    }

    /// <summary>DifficultyManager yoksa mesafeye gore hesapla.</summary>
    DifficultyManager.EnemyStats FallbackStats()
    {
        float z    = playerTransform != null ? playerTransform.position.z : 0f;
        float mult = 1f + Mathf.Pow(z / 1000f, 1.3f);
        return new DifficultyManager.EnemyStats(
            Mathf.RoundToInt(100f * mult),
            Mathf.RoundToInt(25f  * mult),
            Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f),
            Mathf.RoundToInt(15f  * mult));
    }

    // ── Kapi verilerini runtime olustur (elle yapmana gerek yok) ─────────────
    void BuildRuntimeGates()
    {
        // Inspector'dan baglandiysa kullan
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            MakeGate("+60",       GateEffectType.AddCP,              60f,  new Color(0.2f,0.85f,0.2f,0.7f)),
            MakeGate("+100",      GateEffectType.AddCP,              100f, new Color(0.2f,0.85f,0.2f,0.7f)),
            MakeGate("x2",        GateEffectType.MultiplyCP,         2f,   new Color(0.1f,0.4f,1.0f,0.7f)),
            MakeGate("x1.5",      GateEffectType.MultiplyCP,         1.5f, new Color(0.1f,0.4f,1.0f,0.7f)),
            MakeGate("MERGE",     GateEffectType.Merge,              0f,   new Color(0.6f,0.1f,0.9f,0.7f)),
            MakeGate("+Piyade",   GateEffectType.PathBoost_Piyade,   60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("+Mekanize", GateEffectType.PathBoost_Mekanize, 60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("+Teknoloji",GateEffectType.PathBoost_Teknoloji,60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("RISK",      GateEffectType.RiskReward,         0f,   new Color(1.0f,0.85f,0.0f,0.7f)),
            MakeGate("-80",       GateEffectType.NegativeCP,         80f,  new Color(0.9f,0.1f,0.1f,0.7f)),
        };
        Debug.Log("[SpawnManager] Runtime gate verileri olusturuldu.");
    }

    GateData MakeGate(string text, GateEffectType type, float value, Color color)
    {
        GateData d      = ScriptableObject.CreateInstance<GateData>();
        d.gateText      = text;
        d.effectType    = type;
        d.effectValue   = value;
        d.gateColor     = color;
        return d;
    }

    GateData[] ActiveGates => (gateDataList != null && gateDataList.Length > 0)
        ? gateDataList : _runtimeGates;

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
            Debug.Log("[SpawnManager] BOSS ZAMANI!");
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

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;
    }

    // ── Kapi ─────────────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        bool pity = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;

        GateData left  = PickGate(pity);
        GateData right = PickGate(pity);

        float offset = ROAD_HALF_WIDTH * 0.45f;
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    GateData PickGate(bool pity)
    {
        GateData[] pool = ActiveGates;
        if (!pity) return pool[Random.Range(0, pool.Length)];

        // Pity: sadece pozitif
        var safe = new List<GateData>(pool.Length);
        foreach (var g in pool)
            if (g.effectType != GateEffectType.NegativeCP &&
                g.effectType != GateEffectType.RiskReward)
                safe.Add(g);
        return safe.Count > 0 ? safe[Random.Range(0, safe.Count)] : pool[0];
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj;

        if (gatePrefab != null)
        {
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        }
        else
        {
            // GatePrefab yoksa quad olustur
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(3f, 4f, 1f);
            Destroy(obj.GetComponent<Collider>());

            // Collider ekle
            BoxCollider bc  = obj.AddComponent<BoxCollider>();
            bc.isTrigger    = true;
            bc.size         = new Vector3(1f, 1.1f, 1.2f);

            // Rigidbody
            Rigidbody rb    = obj.AddComponent<Rigidbody>();
            rb.isKinematic  = true;

            // Gate script
            Gate gate       = obj.AddComponent<Gate>();
            gate.panelRenderer = obj.GetComponent<Renderer>();
            gate.gateData   = data;
        }

        // Gate varsa data ata
        Gate g2 = obj.GetComponent<Gate>();
        if (g2 != null)
        {
            g2.gateData = data;
            g2.Refresh();
        }

        Destroy(obj, 40f);
    }

    // ── Dusman Dalgasi ────────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float pz       = playerTransform.position.z;
        float progress = Mathf.Clamp01(pz / bossDistance);

        // Dusman sayisi: 2'den 8'e lineer + oyuncu gucluyse +1
        int count = Mathf.RoundToInt(Mathf.Lerp(2f, 8f, progress));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f)
            count = Mathf.Min(count + 1, 9);

        // Dalga tipi
        int waveType = PickWaveType(progress);
        switch (waveType)
        {
            case 0: NormalWave(zPos, count);  break;
            case 1: HeavyWave(zPos, count);   break;
            case 2: FlankWave(zPos, count);   break;
        }

        // Stats'i her dalga guncelle (DDA icin)
        RefreshStats();
    }

    int PickWaveType(float progress)
    {
        if (progress < 0.25f) return 0;
        float r = Random.value;
        if (r < 0.5f) return 0;
        if (r < 0.75f) return 1;
        return 2;
    }

    void NormalWave(float z, int count)
    {
        int   cols   = Mathf.Min(count, 4);
        int   rows   = Mathf.CeilToInt((float)count / cols);
        float colGap = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float startX = -(colGap * (cols - 1)) * 0.5f;
        int   placed = 0;
        for (int r = 0; r < rows && placed < count; r++)
            for (int c = 0; c < cols && placed < count; c++)
            {
                PlaceEnemy(new Vector3(
                    Mathf.Clamp(startX + c * colGap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                    1.2f, z + r * 3f));
                placed++;
            }
    }

    void HeavyWave(float z, int count)
    {
        for (int i = 0; i < count; i++)
            PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
    }

    void FlankWave(float z, int count)
    {
        int half = count / 2;
        for (int i = 0; i < half; i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
        }
        if (count % 2 == 1) PlaceEnemy(new Vector3(0f, 1.2f, z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        // Cakisma onle
        Collider[] nearby = Physics.OverlapSphere(pos, 1.2f);
        foreach (Collider col in nearby)
            if (col.CompareTag("Enemy")) { pos.x += 2.4f; break; }
        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);

        GameObject obj;
        if (enemyPrefab != null)
        {
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
        else
        {
            // EnemyPrefab yoksa capsule olustur
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            CapsuleCollider cc = obj.AddComponent<CapsuleCollider>();
            cc.isTrigger = true;
            Rigidbody rb  = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            obj.tag = "Enemy";
            obj.AddComponent<Enemy>();
            obj.AddComponent<EnemyHealthBar>();
        }

        obj.GetComponent<Enemy>()?.Initialize(_currentStats);
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
/// Top End War — HUD v6 (Claude)
///
/// Eger Canvas referanslari Inspector'dan baglanmadiysa
/// kod kendi minimal HUD'ini olusturur.
///
/// KURULUM: Canvas altindaki GameHUD objesine ekle.
/// Referanslar bossa bile calisir — hata vermez.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("CP / Tier (optional — boş bırakabilirsin)")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barlari (optional)")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup (optional)")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash (optional)")]
    public Image damageFlashImage;

    // Auto-oluşturulan referanslar
    bool _autoBuilt = false;
    int  _lastCP    = 0;

    void Start()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("GameHUD: PlayerStats bulunamadi!");
            return;
        }

        // Referanslar bossa otomatik olsutur
        if (cpText == null || tierText == null)
            AutoBuildHUD();

        // Eventler
        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated += OnRiskBonus;

        // Ilk degerler
        _lastCP = PlayerStats.Instance.CP;
        if (cpText)   { cpText.text = PlayerStats.Instance.CP.ToString("N0"); cpText.color = Color.white; }
        if (tierText) { tierText.text = "TIER 1 | " + PlayerStats.Instance.GetTierName(); tierText.color = Color.yellow; }
        if (damageFlashImage) damageFlashImage.color = new Color(1, 0, 0, 0);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated          -= OnCPUpdated;
        GameEvents.OnTierChanged        -= OnTierChanged;
        GameEvents.OnSynergyFound       -= OnSynergy;
        GameEvents.OnPlayerDamaged      -= OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated -= OnRiskBonus;
    }

    // ── Otomatik HUD olustur ──────────────────────────────────────────────────
    void AutoBuildHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        if (cpText == null)
            cpText = CreateText(canvas.gameObject, "CP: 200", new Vector2(0.5f, 1f), new Vector2(0, -35), 36, Color.white);

        if (tierText == null)
            tierText = CreateText(canvas.gameObject, "TIER 1", new Vector2(0.5f, 1f), new Vector2(0, -75), 26, Color.yellow);

        if (popupText == null)
            popupText = CreateText(canvas.gameObject, "", new Vector2(0.5f, 0.5f), new Vector2(0, 60), 40, Color.cyan);

        // Hasar flash
        if (damageFlashImage == null)
        {
            var flashObj = new GameObject("DamageFlash");
            flashObj.transform.SetParent(canvas.transform, false);
            damageFlashImage = flashObj.AddComponent<Image>();
            damageFlashImage.color = new Color(1, 0, 0, 0);
            damageFlashImage.raycastTarget = false;
            var r = flashObj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        _autoBuilt = true;
        Debug.Log("[GameHUD] Otomatik HUD olusturuldu.");
    }

    TextMeshProUGUI CreateText(GameObject parent, string text,
        Vector2 anchor, Vector2 pos, float size, Color color)
    {
        var obj = new GameObject("AutoText");
        obj.transform.SetParent(parent.transform, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(500, 60);
        return tmp;
    }

    // ── Event Handler'lar ─────────────────────────────────────────────────────
    void OnCPUpdated(int cp)
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

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
        if (tierText && s != null)
        { tierText.text = "TIER " + tier + " | " + s.GetTierName(); tierText.color = Color.yellow; }
        ShowPopup("TIER " + tier + "!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) { ShowPopup(name, new Color(1, 0.84f, 0)); return; }
        StopCoroutine("HideSynergy");
        synergyText.text = name; synergyText.color = new Color(1, 0.84f, 0);
        StartCoroutine("HideSynergy");
    }

    void OnRiskBonus(int remaining)
    {
        ShowPopup("RISK! +" + remaining, new Color(1, 0.85f, 0));
    }

    void OnPlayerDamaged(int _)
    {
        if (damageFlashImage == null) return;
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
        damageFlashImage.color = new Color(1, 0, 0, 0.55f);
        float t = 0;
        while (t < 0.4f) { t += Time.deltaTime; damageFlashImage.color = new Color(1, 0, 0, Mathf.Lerp(0.55f, 0, t / 0.4f)); yield return null; }
        damageFlashImage.color = new Color(1, 0, 0, 0);
    }

    void ShowPopup(string msg, Color color)
    {
        if (popupText == null) return;
        StopCoroutine("HidePopup");
        popupText.text = msg; popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }
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
using DG.Tweening;

/// <summary>
/// Top End War — Tier Morph v3 (Claude)
/// HATA DUZELTME: _currentModel Destroy edilince coroutine eski
/// referansi tutuyor ve SetActive crash yapiyordu.
/// Cozum: Destroy yerine SetActive(false) — model havuzu gibi calisir.
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

    // Tum tier modelleri onceden olusturulur, sadece aktif/pasif yapilir
    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged += HandleTierChange;
        ActivateTier(0); // Tier 1 ile basla
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= HandleTierChange;
    }

    /// <summary>Tum tier modellerini baslangicta olustur, hepsini gizle.</summary>
    void PrewarmModels()
    {
        int count = tierPrefabs != null ? tierPrefabs.Length : 0;
        if (count == 0) count = 5; // Placeholder

        _spawnedModels = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            GameObject model;

            if (tierPrefabs != null && i < tierPrefabs.Length && tierPrefabs[i] != null)
            {
                model = Instantiate(tierPrefabs[i], transform);
            }
            else
            {
                // Placeholder: Capsule
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.transform.SetParent(transform);
                Destroy(model.GetComponent<Collider>());
            }

            model.transform.localPosition = Vector3.zero;
            model.transform.localScale    = Vector3.one;

            // Alt collider'lari kaldir
            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Destroy(c);

            model.SetActive(false); // Hepsi baslangicta gizli
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

        // Mevcut modeli kucult ve gizle
        if (_currentTierIndex >= 0 && _currentTierIndex < _spawnedModels.Length)
        {
            GameObject prev = _spawnedModels[_currentTierIndex];
            if (prev != null)
            {
                // DOTween ile kucult
                yield return prev.transform
                    .DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(Ease.InBack)
                    .WaitForCompletion();

                prev.SetActive(false);
                prev.transform.localScale = Vector3.one; // Sonraki kullanim icin sifirla
            }
        }

        // Parcacik
        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

        // Yeni modeli goster ve pop
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

        // Pop animasyonu
        model.transform
            .DOScale(Vector3.one * popPeak, popDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (model != null) // Null kontrol
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f).SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
}
```

### Enemy.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Dushman v4 (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic=true) + CapsuleCollider(IsTrigger=true)
///
/// PERFORMANS: OverlapSphere her frame degil, her 0.2s bir guncellenir.
/// IC ICE GECME: Separation force hala aktif.
/// HP BAR: EnemyHealthBar otomatik eklenir.
/// ZORLUK: Initialize yoksa mesafeye gore kendi hesaplar.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    // Runtime degerler — Initialize veya Awake'den gelir
    int    _maxHealth;
    int    _currentHealth;
    int    _contactDamage;
    float  _moveSpeed;
    int    _cpReward;
    bool   _initialized = false;

    Renderer        _bodyRenderer;
    EnemyHealthBar  _hpBar;
    bool            _isDead           = false;
    bool            _hasDamagedPlayer = false;

    // Separation icin cache
    float     _lastSepTime = 0f;
    Vector3   _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f; // Saniyede ~6 kez hesapla

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();
    }

    void OnEnable()
    {
        _isDead           = false;
        _hasDamagedPlayer = false;
        _separationVec    = Vector3.zero;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;

        // Initialize edilmediyse mesafeye gore hesapla
        if (!_initialized) AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    /// <summary>SpawnManager cagirır — DDA statlari uygular.</summary>
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

    /// <summary>DifficultyManager yoksa basit mesafe tabanli hesap.</summary>
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

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   pZ  = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        // Z hareketi
        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        // X: oyuncuyu takip
        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        // Separation (her frame degil, cache)
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
        Collider[] neighbors = Physics.OverlapSphere(pos, 1.8f);
        foreach (Collider col in neighbors)
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            if (away.magnitude < 0.001f) away = new Vector3(Random.Range(-1f, 1f), 0, 0).normalized * 0.1f;
            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }
        if (count > 0) sep = (sep / count) * 3.5f;
        return sep;
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
        if (!_isDead && _bodyRenderer != null)
            _bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead      = true;
        _initialized = false; // Sonraki spawn icin sifirla
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

    void OnDisable()
    {
        CancelInvoke();
        _initialized = false;
    }
}
```

### Bullet.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Mermi v3 (Claude)
/// Hasar tier'a gore artis:
///   Tier1: 60  | Tier2: 95  | Tier3: 145 | Tier4: 210 | Tier5: 300
///
/// Atis hizi (PlayerController'da):
///   Tier1:1.5/s | Tier2:2.5/s | Tier3:4.0/s | Tier4:6.0/s | Tier5:8.5/s
///
/// DPS tablosu:
///   Tier1: 90 DPS  → 120 HP dusmani 1.3sn
///   Tier2: 237 DPS → 120 HP dusmani 0.5sn
///   Tier3: 580 DPS → cok hizli
///   Tier4-5: neredeyse aninda
/// </summary>
public class Bullet : MonoBehaviour
{
    // Hasar dogrudan SetDamage() ile atanir — PlayerController cagırır
    public int damage = 60;

    public Color bulletColor = new Color(0.55f, 0f, 1f, 1f); // Mor

    Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        if (_renderer != null)
        {
            // URP: _BaseColor kullan, mat.color degil
            if (_renderer.material.HasProperty("_BaseColor"))
                _renderer.material.SetColor("_BaseColor", bulletColor);
            else
                _renderer.material.color = bulletColor;
        }
        Invoke(nameof(ReturnToPool), 2.5f);
    }

    void OnDisable() { CancelInvoke(); }

    /// <summary>PlayerController tarafindan spawn oncesi cagrilir.</summary>
    public void SetDamage(int d) { damage = d; }

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
### EnemyHealthBar.cs
```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top End War — Dusman HP Bari (Claude)
///
/// KURULUM:
///   Enemy prefab'ina bu scripti ekle.
///   Kod kendi HP barini olusturur — elle Canvas yapma.
///   HP bar: Dusman kafasinin 1.8 birim ustunde, kameray a bakar.
///
/// Enemy.TakeDamage() sonrasi UpdateBar() cagrilir.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Gorsel")]
    public float barWidth   = 1.2f;
    public float barHeight  = 0.15f;
    public float barYOffset = 1.8f;

    public Color fullColor  = new Color(0.15f, 0.85f, 0.15f); // Yesil
    public Color halfColor  = new Color(0.95f, 0.75f, 0.05f); // Sari
    public Color lowColor   = new Color(0.9f, 0.15f, 0.15f);  // Kirmizi

    // Internal
    Canvas    _canvas;
    Image     _bgImage;
    Image     _fillImage;
    int       _maxHP;
    int       _currentHP;
    Transform _camTransform;

    void Awake()
    {
        BuildBar();
        _camTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        // HP bari kameraya bak
        if (_canvas != null && _camTransform != null)
        {
            _canvas.transform.position = transform.position + Vector3.up * barYOffset;
            _canvas.transform.LookAt(
                _canvas.transform.position + _camTransform.forward);
        }
    }

    public void Init(int maxHP)
    {
        _maxHP    = Mathf.Max(1, maxHP);
        _currentHP = _maxHP;
        UpdateBar();
    }

    public void UpdateBar(int currentHP)
    {
        _currentHP = Mathf.Clamp(currentHP, 0, _maxHP);
        UpdateBar();
    }

    void UpdateBar()
    {
        if (_fillImage == null) return;

        float ratio = (float)_currentHP / _maxHP;
        _fillImage.fillAmount = ratio;

        // Renk gecisi
        if      (ratio > 0.6f) _fillImage.color = fullColor;
        else if (ratio > 0.3f) _fillImage.color = Color.Lerp(halfColor, fullColor, (ratio - 0.3f) / 0.3f);
        else                   _fillImage.color = Color.Lerp(lowColor, halfColor, ratio / 0.3f);

        // HP sifirsa bari gizle
        _canvas.gameObject.SetActive(_currentHP > 0);
    }

    void BuildBar()
    {
        // World Space Canvas
        GameObject canvasObj = new GameObject("HPBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * barYOffset;

        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode    = RenderMode.WorldSpace;
        _canvas.sortingOrder  = 10;
        _canvas.worldCamera   = Camera.main;

        RectTransform cr = canvasObj.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(barWidth, barHeight * 2f);

        // Arka plan (koyu)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _bgImage = bgObj.AddComponent<Image>();
        _bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin     = Vector2.zero;
        bgR.anchorMax     = Vector2.one;
        bgR.offsetMin     = Vector2.zero;
        bgR.offsetMax     = Vector2.zero;

        // Dolgu
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = 0;
        _fillImage.color = fullColor;
        RectTransform fillR = fillObj.GetComponent<RectTransform>();
        fillR.anchorMin   = Vector2.zero;
        fillR.anchorMax   = Vector2.one;
        fillR.offsetMin   = Vector2.zero;
        fillR.offsetMax   = Vector2.zero;
    }
}

```

### Gatefeedback.cs
```csharp
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Kapi Gecis Efekti (Claude + DOTween)
///
/// Player objesine ekle.
/// Kapidan gecince: kisa scale pop (0.8→1.3→1.0) — "hissettiren" morph ani.
/// Tier atlayinca: daha buyuk pop + kamera shake.
///
/// DOTween kurulu olmali (Package Manager'dan).
/// </summary>
public class GateFeedback : MonoBehaviour
{
    [Header("Gate Gecis")]
    public float gatePopDuration = 0.25f;
    public float gatePopScale    = 1.25f;

    [Header("Tier Atlama")]
    public float tierPopDuration = 0.4f;
    public float tierPopScale    = 1.5f;

    [Header("Kamera Sallama (Tier)")]
    public Camera mainCamera;
    public float  shakeStrength  = 0.3f;
    public float  shakeDuration  = 0.3f;

    Vector3 _originalScale;
    Tweener _currentTween;

    void Start()
    {
        _originalScale = transform.localScale;

        GameEvents.OnCPUpdated   += OnCPUpdated;
        GameEvents.OnTierChanged += OnTierChanged;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated   -= OnCPUpdated;
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnCPUpdated(int cp)
    {
        // Her kapida kucuk pop
        ScalePop(gatePopScale, gatePopDuration);
    }

    void OnTierChanged(int tier)
    {
        // Tier atlarken daha buyuk pop + kamera shake
        ScalePop(tierPopScale, tierPopDuration);

        if (mainCamera != null)
            mainCamera.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false);
    }

    void ScalePop(float peak, float duration)
    {
        _currentTween?.Kill();

        transform.localScale = _originalScale;

        _currentTween = transform
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
### Progressionconfig.cs
```csharp
using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Dengeleme Konfigurasyonu (Claude + Gemini DDA)
/// Assets/ProgressionConfig klasorunde olustur:
///   Project → Create → TopEndWar → Progression Config
///
/// Bu ScriptableObject oyunun matematiksel dengesinin merkezi.
/// Inspector'dan tweak et, DifficultyManager okur.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Temel Ilerleme")]
    [Tooltip("Her 100 birimdeki buyume carpani. 1.15 = %15")]
    [Range(1.05f, 1.5f)]
    public float growthRate = 1.15f;

    [Tooltip("Zorluk egrisi ussu. 1.3 = dengeli, 2.0 = cok sert")]
    [Range(1.0f, 3.0f)]
    public float difficultyExponent = 1.3f;

    public int baseStartCP = 200;

    [Header("Dusman Olcekleme")]
    public int   baseEnemyHealth    = 100;
    public int   baseEnemyDamage    = 25;
    public float baseEnemySpeed     = 4.5f;
    public float enemyMaxSpeed      = 8f;    // Hiz tavan (Gemini onerisi)

    [Tooltip("Oyuncu CP'sine gore dushman guclenme faktoru")]
    [Range(0.5f, 1.5f)]
    public float playerCPScalingFactor = 0.9f;

    [Header("Kapi Dengeleme")]
    public float gateValueGrowthRate = 1.12f;
    public int   minGateValue        = 20;
    public int   maxGateValue        = 500;

    [Tooltip("Boss oncesi bu mesafede negatif/risk kapi cikmasın (Pity Timer)")]
    public float noBadGateZoneBeforeBoss = 200f;

    [Header("Tier Eslikleri")]
    public int[] tierThresholds = { 0, 300, 800, 2000, 5000 };

    // ── Hesaplama Metodlari (GC-friendly, allocation yok) ────────────────────

    /// <summary>Belirli mesafedeki beklenen CP.</summary>
    public int CalculateExpectedCP(float distance)
    {
        float segments   = distance / 100f;
        float multiplier = Mathf.Pow(growthRate, segments);
        return Mathf.RoundToInt(baseStartCP * multiplier);
    }

    /// <summary>Mesafeye gore zorluk carpani.</summary>
    public float CalculateDifficultyMultiplier(float distance)
    {
        float normalized = distance / 1000f;
        return 1f + Mathf.Pow(normalized, difficultyExponent);
    }

    /// <summary>Kapi degerini mesafeye gore olcekle.</summary>
    public int ScaleGateValue(int baseValue, float distance)
    {
        float segments = distance / 150f;
        float mult     = Mathf.Pow(gateValueGrowthRate, segments);
        int   scaled   = Mathf.RoundToInt(baseValue * mult);
        if (scaled < minGateValue) return minGateValue;
        if (scaled > maxGateValue) return maxGateValue;
        return scaled;
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