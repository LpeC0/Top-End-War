using UnityEngine;

/// <summary>
/// Top End War — Zorluk Yoneticisi v4
///
/// Vertical slice icin compatibility shim:
/// - API korunur
/// - compile uyumu bozulmaz
/// - player gucune gore gizli scaling KAPALI
/// - fixed difficulty varsayilan
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

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

    [Header("Konfigurasyon (ProgressionConfig SO)")]
    public ProgressionConfig config;

    [Header("Temel Dusman Degerleri (Legacy fallback)")]
    public float baseEnemyHP     = 100f;
    public float baseEnemySpeed  = 4.5f;
    public float baseEnemyDamage = 25f;
    public int   baseEnemyReward = 15;

    [Header("Legacy Helper")]
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

    /// <summary>
    /// Vertical slice: fixed difficulty.
    /// Bu metod API uyumu icin korunur ama her zaman 1 dondurur.
    /// </summary>
    public float CalculateMultiplier(float z, int playerCP, float expectedCP)
    {
        _smoothedPowerRatio = 1f;
        _currentMultiplier  = 1f;

        // Event imzasi bozulmasin
        GameEvents.OnDifficultyChanged?.Invoke(_currentMultiplier, _smoothedPowerRatio);
        return _currentMultiplier;
    }

    /// <summary>
    /// Legacy fallback stats.
    /// Config-driven spawn varsa zaten kullanilmamali.
    /// </summary>
    public EnemyStats GetScaledEnemyStats()
    {
        return new EnemyStats(
            health:   Mathf.RoundToInt(baseEnemyHP),
            damage:   Mathf.RoundToInt(baseEnemyDamage),
            speed:    baseEnemySpeed,
            cpReward: baseEnemyReward
        );
    }

    /// <summary>
    /// Slice'ta risk gate/pity zone aktif degil.
    /// API uyumu icin false doner.
    /// </summary>
    public bool IsInPityZone(float bossDistance)
    {
        return false;
    }

    public float PlayerPowerRatio => 1f;

    public float GetCurrentMultiplier()  => _currentMultiplier;
    public float GetSmoothedPowerRatio() => _smoothedPowerRatio;

    /// <summary>
    /// API uyumu icin korunur. Fixed difficulty'de aktif etkisi yoktur.
    /// </summary>
    public void SetExpectedCP(float expected)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.SetExpectedCP(Mathf.Max(1f, expected));
    }
}