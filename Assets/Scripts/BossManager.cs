using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Boss Yoneticisi v7 (Claude & Patch Integrated)
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Ayarlari")]
    public int bossMaxHP = 41000;

    [Header("Vertical Slice Mini-Boss")]
    public float transitionLockDuration = 1.6f;
    [Range(0.1f, 0.9f)] public float phase2Threshold = 0.50f;

    [Header("Legacy (Slice'ta kapali tutulur)")]
    public bool enableMinionPhase = false;
    public bool enableEnragePhase = false;

    [Header("Minyon Spawn (Legacy)")]
    [Tooltip("ObjectPooler 'Enemy' havuzundan cekilir. Pool bos ise spawn edilmez.")]
    public int minionsPerWave = 4;
    public float minionInterval = 8f;
    [Tooltip("Minyon spawn pozisyonu icin bos referans noktalari (opsiyonel)")]
    public Transform[] minionSpawnPoints;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] private int _currentHP;
    [SerializeField] private int _currentPhase;   // 1=normal, 2=phase2
    [SerializeField] private bool _invulnerable;
    [SerializeField] private bool _phase2Triggered;
    [SerializeField] private bool _active;

    Coroutine _minionCoroutine;
    Coroutine _shieldCoroutine;

    // --- YENİ ALANLAR ---
    BossConfig _currentBossConfig;
    int _currentBossArmor = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // --- YENİ START OVERLOAD ---
    public void StartBoss(BossConfig config, float targetDps)
    {
        _currentBossConfig = config;

        if (config != null)
        {
            bossMaxHP = config.GetHP(targetDps);
            _currentBossArmor = config.armor;
            transitionLockDuration = config.GetFirstTransitionLock(transitionLockDuration);
            phase2Threshold = config.GetFirstTransitionRatio(phase2Threshold);
        }
        else
        {
            _currentBossArmor = 0;
        }

        StartBoss(bossMaxHP);
    }

    public void StartBoss(int hp = -1)
    {
        if (hp > 0) bossMaxHP = hp;
        _currentHP = bossMaxHP;
        _currentPhase = 1;
        _invulnerable = false;
        _phase2Triggered = false;
        _active = true;

        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);
        GameEvents.OnBossEncountered?.Invoke();
        //GameEvents.OnAnchorModeChanged?.Invoke(true);

        Debug.Log($"[BossManager] Basliyor. HP: {bossMaxHP}");
    }

    // --- HASAR UYGULAMA (GÜNCELLENDİ) ---
    void ApplyFinalDamage(int finalDamage)
    {
        if (!_active || _invulnerable || _currentHP <= 0) return;

        _currentHP = Mathf.Max(0, _currentHP - finalDamage);
        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);

        CheckPhaseTransitions();

        if (_currentHP <= 0) OnBossDefeated();
    }

    public void TakeDamage(int dmg)
    {
        ApplyFinalDamage(dmg);
    }

    public void TakeDamage(int rawDamage, int armorPen, float bossDamageMult)
    {
        int effectiveArmor = Mathf.Max(0, _currentBossArmor - Mathf.Max(0, armorPen));
        int finalDamage = Mathf.Max(1, rawDamage - effectiveArmor);
        finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, bossDamageMult)));

        ApplyFinalDamage(finalDamage);
    }

    void CheckPhaseTransitions()
    {
        float ratio = (float)_currentHP / bossMaxHP;
        
        if (!_phase2Triggered && ratio <= phase2Threshold)
        {
            _phase2Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(TransitionLockRoutine());
        }
    }

    IEnumerator TransitionLockRoutine()
    {
        _invulnerable = true;
        GameEvents.OnBossPhaseShield?.Invoke(2); 

        yield return new WaitForSeconds(transitionLockDuration);

        _invulnerable = false;
        EnterPhase2();
    }

    void EnterPhase2()
    {
        _currentPhase = 2;
        GameEvents.OnBossPhaseChanged?.Invoke(2);

        if (enableMinionPhase)
        {
            if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
            _minionCoroutine = StartCoroutine(MinionWaveRoutine());
        }
    }

    IEnumerator MinionWaveRoutine()
    {
        while (_active && _currentPhase == 2)
        {
            SpawnMinions();
            yield return new WaitForSeconds(minionInterval);
        }
    }

    void SpawnMinions()
    {
        if (!_active || ObjectPooler.Instance == null) return;
        for (int i = 0; i < minionsPerWave; i++)
        {
            Vector3 spawnPos;
            if (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
                spawnPos = minionSpawnPoints[i % minionSpawnPoints.Length].position;
            else
                spawnPos = transform.position + new Vector3(
                    Random.Range(-5f, 5f), 0f, Random.Range(-3f, 3f));
            
            ObjectPooler.Instance.SpawnFromPool("Enemy", spawnPos, Quaternion.identity);
        }
    }

    void OnBossDefeated()
    {
        _active = false;
        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);

        GameEvents.OnBossDefeated?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        Debug.Log("[BossManager] Boss yenildi!");
    }

    public float GetHPRatio() => bossMaxHP > 0 ? (float)_currentHP / bossMaxHP : 0f;
    public bool IsActive() => _active;
    public bool IsInvulnerable() => _invulnerable;
}
/// KURULUM:
///   1. Hierarchy'de bir Boss GameObject olustur.
///   2. BossHitReceiver.cs'i bu objeye ekle (Bullet.cs bunu arar).
///   3. BossManager.cs ayri bir sahne objesine (BossManager) ekle.
///   4. Inspector'dan bossMaxHP ayarla veya StageManager.SetupBoss() kullan.
/// </summary>