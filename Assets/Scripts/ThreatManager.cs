using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — ThreatManager v1.0
///
/// Görev:
///   Aktif düşmanların oyuncuya yakınlığını ve tipini okuyarak
///   sürekli bir ThreatScore hesaplar. Bu score zone'lara ayrılır
///   ve Critical zone'da HP drain uygular.
///
/// Zone Tablosu:
///   0–20   → Normal   : ceza yok
///   20–50  → Pressure : sarı uyarı
///   50–80  → Danger   : kırmızı uyarı
///   80+    → Critical : HP drain (saniyede maxHP'nin %2'si)
///
/// Diğer sistemlerle bağlantı:
///   - PlayerStats  : HP drain için TryTakeContactDamage kullanır
///   - Enemy        : IsAlive + transform.position okur
///   - GameHUD      : ThreatScore ve ThreatZone'u event ile alır
///
/// Kurulum:
///   Sahneye boş GameObject ekle, bu script'i koy.
///   PlayerStats ve Enemy aynı sahnede olmalı.
/// </summary>
public class ThreatManager : MonoBehaviour
{
    public static ThreatManager Instance { get; private set; }

    // ── Ayarlar ──────────────────────────────────────────────────────────

    [Header("Mesafe Ayarı")]
    [Tooltip("Bu mesafenin dışındaki düşmanlar threat üretmez.")]
    [SerializeField] float maxThreatRange = 20f;

    [Header("Zone Eşikleri")]
    [SerializeField] float pressureThreshold = 20f;
    [SerializeField] float dangerThreshold   = 50f;
    [SerializeField] float criticalThreshold = 80f;

    [Header("HP Drain (Critical Zone)")]
    [Tooltip("Saniyede max HP'nin yüzde kaçı drain olur.")]
    [SerializeField] float drainPercentPerSecond = 2f;

    [Tooltip("HP drain'in minimum bekleme süresi. PlayerStats invincibility süresiyle uyumlu olmalı.")]
    [SerializeField] float drainTickInterval = 0.6f;

    [Header("Scan Aralığı")]
    [Tooltip("Her kaç saniyede bir aktif düşman listesi taranır. 0 = her frame.")]
    [SerializeField] float scanInterval = 0.1f;

    // ── baseThreat Tablosu ────────────────────────────────────────────────
    // EnemyThreatType → ne kadar baskı ürettiği.
    // Tasarım notu: Elite tek başına Danger'a çekebilmeli (4.5),
    // Swarm çok gelince tehlikeli olmalı ama tek başına hafif (0.5).

    static readonly Dictionary<EnemyThreatType, float> BaseThreatMap = new()
    {
        { EnemyThreatType.Standard,       1.0f },
        { EnemyThreatType.PackPressure,   0.5f },
        { EnemyThreatType.Durable,        2.0f },
        { EnemyThreatType.ElitePressure,  4.5f },
        { EnemyThreatType.Priority,       3.0f },
        { EnemyThreatType.Backline,       1.5f },
    };

    // ── Public State ─────────────────────────────────────────────────────

    public float       ThreatScore  { get; private set; }
    public ThreatZone  CurrentZone  { get; private set; } = ThreatZone.Normal;

    // ── Dahili ───────────────────────────────────────────────────────────

    readonly List<Enemy> _activeEnemies = new();
    float _scanTimer    = 0f;
    float _drainTimer   = 0f;
    bool  _isRunActive  = false;

    // ── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnRunStart     += HandleRunStart;
        GameEvents.OnGameOver     += HandleGameOver;
        GameEvents.OnStageCleared += HandleStageCleared;
    }

    void OnDisable()
    {
        GameEvents.OnRunStart     -= HandleRunStart;
        GameEvents.OnGameOver     -= HandleGameOver;
        GameEvents.OnStageCleared -= HandleStageCleared;
    }

    void HandleRunStart()                                    => SetRunActive(true);
    void HandleGameOver()                                    => SetRunActive(false);
    void HandleStageCleared(GameEvents.StageClearInfo info)  => SetRunActive(false);

    public void SetRunActive(bool active)
    {
        _isRunActive = active;
        if (!active)
        {
            ThreatScore = 0f;
            SetZone(ThreatZone.Normal);
        }
    }

    // ── Ana Döngü ────────────────────────────────────────────────────────

    void Update()
    {
        if (!_isRunActive) return;

        _scanTimer += Time.deltaTime;
        if (_scanTimer >= scanInterval)
        {
            _scanTimer = 0f;
            RefreshEnemyList();
        }

        ThreatScore = CalculateThreatScore();
        ThreatZone newZone = ScoreToZone(ThreatScore);

        if (newZone != CurrentZone)
            SetZone(newZone);

        if (CurrentZone == ThreatZone.Critical)
            TickDrain();
    }

    // ── Düşman Listesi ───────────────────────────────────────────────────

    void RefreshEnemyList()
    {
        _activeEnemies.Clear();

        // FindObjectsByType sadece scanInterval'da çağrılıyor, her frame değil.
        // Object pool kullanıyorsan bu yeterince verimli.
        var all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in all)
        {
            if (e.IsAlive)
                _activeEnemies.Add(e);
        }
    }

    // ── Score Hesabı ─────────────────────────────────────────────────────

    float CalculateThreatScore()
    {
        if (PlayerStats.Instance == null || _activeEnemies.Count == 0)
            return 0f;

        Vector3 playerPos = PlayerStats.Instance.transform.position;
        float total = 0f;

        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            float distance = Vector3.Distance(enemy.transform.position, playerPos);
            if (distance >= maxThreatRange) continue;

            float proximityFactor = 1f - (distance / maxThreatRange);

            // EnemyThreatType'ı Enemy'den okuyamıyoruz (private field),
            // bu yüzden ThreatWeight üzerinden yaklaşıyoruz ve ayrıca
            // tag/name üzerinden archetype okumak yerine bir extension noktası bırakıyoruz.
            // Şimdilik: ThreatWeight (1.0 normal, 1.35 elite) ve Enemy.IsElite kombinasyonu.
            float baseThreat = GetBaseThreat(enemy);

            total += baseThreat * proximityFactor;
        }

        return total;
    }

    /// <summary>
    /// Enemy'den baseThreat değerini çıkarır.
    /// Enemy.ThreatType property'si üzerinden BaseThreatMap'e direkt bakar.
    /// ConfigureArchetype çağrılmayan enemy'ler Standard (1.0) döner.
    /// </summary>
    float GetBaseThreat(Enemy enemy)
    {
        if (BaseThreatMap.TryGetValue(enemy.ThreatType, out float value))
            return value;

        return 1.0f;
    }

    // ── Zone ─────────────────────────────────────────────────────────────

    ThreatZone ScoreToZone(float score)
    {
        if (score >= criticalThreshold) return ThreatZone.Critical;
        if (score >= dangerThreshold)   return ThreatZone.Danger;
        if (score >= pressureThreshold) return ThreatZone.Pressure;
        return ThreatZone.Normal;
    }

    void SetZone(ThreatZone zone)
    {
        CurrentZone = zone;

        // HUD ve diğer sistemler bu event'i dinler.
        GameEvents.OnThreatZoneChanged?.Invoke(zone);

        Debug.Log($"[ThreatManager] Zone → {zone} | Score: {ThreatScore:F1}");
    }

    // ── HP Drain ─────────────────────────────────────────────────────────

    void TickDrain()
    {
        _drainTimer += Time.deltaTime;
        if (_drainTimer < drainTickInterval) return;

        _drainTimer = 0f;

        if (PlayerStats.Instance == null) return;

        float drainPercent = drainPercentPerSecond / 100f * drainTickInterval;
        int drainAmount = Mathf.Max(1, Mathf.RoundToInt(
            PlayerStats.Instance.CommanderMaxHP * drainPercent));

        // Anchor aktifse → AnchorCore'a drain et.
        // Runner modunda → Commander'a drain et.
        bool anchorActive = AnchorCore.Instance != null && !AnchorCore.Instance.IsDestroyed;

        if (anchorActive)
        {
            AnchorCore.Instance.TakeDamage(drainAmount);
            Debug.Log($"[ThreatManager] Anchor Drain: -{drainAmount} | AnchorHP: {AnchorCore.Instance.CurrentHP}");
        }
        else
        {
            PlayerStats.Instance.TryTakeContactDamage(drainAmount);
            Debug.Log($"[ThreatManager] Commander Drain: -{drainAmount} | HP: {PlayerStats.Instance.CommanderHP}");
        }
    }

    // ── Debug ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!_isRunActive) return;

        GUILayout.BeginArea(new Rect(10, 200, 260, 100));
        GUILayout.Label($"[ThreatManager]");
        GUILayout.Label($"Score : {ThreatScore:F2}");
        GUILayout.Label($"Zone  : {CurrentZone}");
        GUILayout.Label($"Enemies tracked: {_activeEnemies.Count}");
        GUILayout.EndArea();
    }
#endif
}

// ── Enum ─────────────────────────────────────────────────────────────────

public enum ThreatZone
{
    Normal,
    Pressure,
    Danger,
    Critical,
}