using UnityEngine;

/// <summary>
/// Top End War — Dinamik Zorluk Yoneticisi (Claude + DDA tasarim)
/// Her frame degil, her updateInterval birimde hesaplama yapar (performans).
/// SpawnManager bu sinifin verilerini kullanir.
/// Hierarchy'e bos bir obje koy, bu scripti ekle, ProgressionConfig'i bagla.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Yapilandirma")]
    public ProgressionConfig config;

    [Header("Optimizasyon")]
    [Tooltip("Her kac birimde zorluk yeniden hesaplaniyor")]
    public float updateInterval = 50f;

    // ── Public Veriler (SpawnManager okur) ───────────────────────────────────
    public float CurrentDifficultyMultiplier { get; private set; } = 1f;
    public int   ExpectedCPAtDistance        { get; private set; }
    public float PlayerPowerRatio            { get; private set; } = 1f;

    // GC-friendly struct: enemy stats allocation olmadan tasimak icin
    public readonly struct EnemyStats
    {
        public readonly int   Health;
        public readonly int   Damage;
        public readonly float Speed;
        public readonly int   CPReward;

        public EnemyStats(int h, int d, float s, int r)
        { Health = h; Damage = d; Speed = s; CPReward = r; }
    }

    Transform _player;
    float     _lastUpdateZ = -9999f;
    float     _currentZ;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (PlayerStats.Instance != null)
            _player = PlayerStats.Instance.transform;

        if (config == null)
            Debug.LogError("[DifficultyManager] ProgressionConfig atanmamis!");
    }

    void Update()
    {
        if (_player == null || config == null) return;

        _currentZ = _player.position.z;

        if (Mathf.Abs(_currentZ - _lastUpdateZ) >= updateInterval)
        {
            RecalculateDifficulty();
            _lastUpdateZ = _currentZ;
        }
    }

    void RecalculateDifficulty()
    {
        CurrentDifficultyMultiplier = config.CalculateDifficultyMultiplier(_currentZ);
        ExpectedCPAtDistance        = config.CalculateExpectedCP(_currentZ);

        int actualCP = PlayerStats.Instance?.CP ?? config.baseStartCP;

        // Ham oran
        float rawRatio = (float)actualCP / Mathf.Max(1, ExpectedCPAtDistance);

        // Yumusatilmis oran (ChatGPT onerisi: ani spike'lari ezele)
        PlayerPowerRatio = Mathf.Lerp(PlayerPowerRatio, rawRatio, 0.08f);

        // PlayerStats'a bildir (SmoothedPowerRatio icin)
        PlayerStats.Instance?.SetExpectedCP(ExpectedCPAtDistance);

        GameEvents.OnDifficultyChanged?.Invoke(CurrentDifficultyMultiplier, PlayerPowerRatio);
    }

    /// <summary>
    /// Dusman stat'larini hesaplar. Struct return = GC allocation yok.
    /// </summary>
    public EnemyStats GetScaledEnemyStats()
    {
        if (config == null) return new EnemyStats(100, 25, 4.5f, 15);

        float diff    = CurrentDifficultyMultiplier;
        float pScale  = Mathf.Lerp(1f, PlayerPowerRatio, config.playerCPScalingFactor);
        float final   = diff * pScale;

        int   health  = Mathf.RoundToInt(config.baseEnemyHealth * final);
        int   damage  = Mathf.RoundToInt(config.baseEnemyDamage * final);
        float speed   = Mathf.Min(config.baseEnemySpeed * (1f + (diff - 1f) * 0.3f), config.enemyMaxSpeed);
        int   reward  = Mathf.RoundToInt(15 * diff);

        return new EnemyStats(health, damage, speed, reward);
    }

    /// <summary>Boss oncesi kotu kapi cikmamali mi?</summary>
    public bool IsInPityZone(float bossDistance)
    {
        if (config == null) return false;
        return _currentZ >= bossDistance - config.noBadGateZoneBeforeBoss;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}