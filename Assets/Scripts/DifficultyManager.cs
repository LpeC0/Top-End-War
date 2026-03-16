using UnityEngine;

/// <summary>
/// Top End War — Zorluk Yoneticisi v2 (Claude)
///
/// ProgressionConfig OLMASA DA calisir — tum degerler kod icinde.
/// Inspector'dan config baglarsan override eder, baglamazsan
/// asagidaki sabitler devreye girer.
///
/// ZORLUK EGRISİ (matematik):
///   Distance 0:    HP=100,  Dmg=25,  Speed=4.0,  Count=2
///   Distance 300:  HP=185,  Dmg=42,  Speed=4.8,  Count=3
///   Distance 600:  HP=340,  Dmg=72,  Speed=5.7,  Count=5
///   Distance 900:  HP=520,  Dmg=100, Speed=6.5,  Count=7
///   Distance 1200: HP=700,  Dmg=130, Speed=7.2,  Count=8
///
/// Oyuncu Tier 1'de Z=400'de gezmek istiyorsa zorlanir.
/// Tier 2-3'te (300-800 CP) orta zorluk hissedilir.
/// Tier 4-5'te hizlanma hissedilir, boss icin hazirlanik gerekir.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Config (Opsiyonel — bos birakabilirsin)")]
    public ProgressionConfig config;

    [Header("Guncelleme Araligi (birim)")]
    public float updateInterval = 50f;

    // ── Sabit Degerler (config yoksa bunlar kullanilir) ───────────────────────
    const float BASE_HP        = 100f;
    const float BASE_DMG       = 25f;
    const float BASE_SPEED     = 4.0f;
    const float BASE_REWARD    = 15f;
    const float MAX_SPEED      = 7.5f;
    const float BOSS_DIST      = 1200f;

    // ── Public Okuma ──────────────────────────────────────────────────────────
    public float CurrentDifficultyMultiplier { get; private set; } = 1f;
    public float PlayerPowerRatio            { get; private set; } = 1f;

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
        if (PlayerStats.Instance != null)
            _player = PlayerStats.Instance.transform;
    }

    void Recalculate()
    {
        // Polinom zorluk: 1 + (z/1000)^1.3
        float norm = _currentZ / 1000f;
        CurrentDifficultyMultiplier = 1f + Mathf.Pow(norm, 1.3f);

        // Oyuncu guc orani
        int   expected = CalcExpectedCP(_currentZ);
        int   actual   = PlayerStats.Instance?.CP ?? 200;
        float raw      = (float)actual / Mathf.Max(1, expected);
        PlayerPowerRatio = Mathf.Lerp(PlayerPowerRatio, raw, 0.08f);

        PlayerStats.Instance?.SetExpectedCP(expected);
        GameEvents.OnDifficultyChanged?.Invoke(CurrentDifficultyMultiplier, PlayerPowerRatio);
    }

    // Her 100 birimde %15 buyume
    int CalcExpectedCP(float z) => Mathf.RoundToInt(200f * Mathf.Pow(1.15f, z / 100f));

    /// <summary>Dusman statlari — config varsa ondan, yoksa dahili hesap.</summary>
    public EnemyStats GetScaledEnemyStats()
    {
        float diff   = CurrentDifficultyMultiplier;

        // Oyuncu cok gucluyse dusmanlar biraz daha sert (ama fazla degil)
        float pBonus = Mathf.Lerp(1f, PlayerPowerRatio, 0.7f);
        float final  = diff * pBonus;

        int   hp     = Mathf.RoundToInt(BASE_HP    * final);
        int   dmg    = Mathf.RoundToInt(BASE_DMG   * final);
        float spd    = Mathf.Min(BASE_SPEED * (1f + (diff - 1f) * 0.35f), MAX_SPEED);
        int   reward = Mathf.RoundToInt(BASE_REWARD * diff);

        // Config varsa override et
        if (config != null)
        {
            hp     = Mathf.RoundToInt(config.baseEnemyHealth  * final);
            dmg    = Mathf.RoundToInt(config.baseEnemyDamage  * final);
            spd    = Mathf.Min(config.baseEnemySpeed * (1f + (diff - 1f) * 0.35f), config.enemyMaxSpeed);
            reward = Mathf.RoundToInt(15 * diff);
        }

        return new EnemyStats(hp, dmg, spd, reward);
    }

    public bool IsInPityZone(float bossDistance)
    {
        float noZone = config != null ? config.noBadGateZoneBeforeBoss : 200f;
        return _currentZ >= bossDistance - noZone;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}