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