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
