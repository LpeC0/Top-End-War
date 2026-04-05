using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Boss Yoneticisi v6 (Claude)
///
/// v6 degisiklikleri:
///   + Phase Shield: HP %60 ve %30'da 2sn dokunulmazlik
///   + Faz gecisleri coroutine ile yonetilir
///   + BossHitReceiver ayri component olarak ayrildi (Bullet.cs uyumu)
///   - Enemy.SetHP() bagimliligı kaldirildi (minyon spawn sadece ObjectPooler kullanir)
///
/// KURULUM:
///   1. Hierarchy'de bir Boss GameObject olustur.
///   2. BossHitReceiver.cs'i bu objeye ekle (Bullet.cs bunu arar).
///   3. BossManager.cs ayri bir sahne objesine (BossManager) ekle.
///   4. Inspector'dan bossMaxHP ayarla veya StageManager.SetupBoss() kullan.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Ayarlari")]
    public int   bossMaxHP           = 41000;
    public float phaseShieldDuration = 2f;
    public float enrageSpeedMult     = 2.2f;

    [Header("Minyon Spawn (Faz 2)")]
    [Tooltip("ObjectPooler 'Enemy' havuzundan cekilir. Pool bos ise spawn edilmez.")]
    public int   minionsPerWave  = 4;
    public float minionInterval  = 8f;
    [Tooltip("Minyon spawn pozisyonu icin bos referans noktalari (opsiyonel)")]
    public Transform[] minionSpawnPoints;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] private int  _currentHP;
    [SerializeField] private int  _currentPhase;   // 1=normal, 2=minyon, 3=enrage
    [SerializeField] private bool _invulnerable;
    [SerializeField] private bool _phase2Triggered;
    [SerializeField] private bool _phase3Triggered;
    [SerializeField] private bool _active;

    Coroutine _minionCoroutine;
    Coroutine _shieldCoroutine;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Boss Baslatma ─────────────────────────────────────────────────────
    /// <summary>
    /// SpawnManager veya StageManager bu metodu cagirir.
    /// hp = -1 ise Inspector'daki bossMaxHP kullanilir.
    /// </summary>
    public void StartBoss(int hp = -1)
    {
        if (hp > 0) bossMaxHP = hp;

        _currentHP       = bossMaxHP;
        _currentPhase    = 1;
        _invulnerable    = false;
        _phase2Triggered = false;
        _phase3Triggered = false;
        _active          = true;

        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);
        GameEvents.OnBossEncountered?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(true);

        Debug.Log($"[BossManager] Basliyor. HP: {bossMaxHP}");
    }

    // ── Hasar Al ─────────────────────────────────────────────────────────
    /// <summary>BossHitReceiver bu metodu cagirir.</summary>
    public void TakeDamage(int dmg)
    {
        if (!_active || _invulnerable || _currentHP <= 0) return;

        _currentHP = Mathf.Max(0, _currentHP - dmg);
        GameEvents.OnBossHPChanged?.Invoke(_currentHP, bossMaxHP);

        CheckPhaseTransitions();

        if (_currentHP <= 0) OnBossDefeated();
    }

    // ── Faz Kontrolu ─────────────────────────────────────────────────────
    void CheckPhaseTransitions()
    {
        float ratio = (float)_currentHP / bossMaxHP;

        if (!_phase2Triggered && ratio <= 0.60f)
        {
            _phase2Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(PhaseShieldRoutine(toPhase: 2));
        }

        if (!_phase3Triggered && ratio <= 0.30f)
        {
            _phase3Triggered = true;
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(PhaseShieldRoutine(toPhase: 3));
        }
    }

    // ── Phase Shield ─────────────────────────────────────────────────────
    IEnumerator PhaseShieldRoutine(int toPhase)
    {
        _invulnerable = true;
        Debug.Log($"[BossManager] Phase Shield aktif — Faz {toPhase} geliyor...");
        GameEvents.OnBossPhaseShield?.Invoke(toPhase);

        yield return new WaitForSeconds(phaseShieldDuration);

        _invulnerable = false;
        Debug.Log($"[BossManager] Phase Shield bitti — Faz {toPhase} aktif.");

        if      (toPhase == 2) EnterPhase2();
        else if (toPhase == 3) EnterPhase3();
    }

    // ── Faz 2: Minyon Dalgasi ────────────────────────────────────────────
    void EnterPhase2()
    {
        _currentPhase = 2;
        GameEvents.OnBossPhaseChanged?.Invoke(2);

        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        _minionCoroutine = StartCoroutine(MinionWaveRoutine());
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

        Debug.Log($"[BossManager] {minionsPerWave} minyon spawn edildi.");
    }

    // ── Faz 3: Enrage ────────────────────────────────────────────────────
    void EnterPhase3()
    {
        _currentPhase = 3;
        if (_minionCoroutine != null) { StopCoroutine(_minionCoroutine); _minionCoroutine = null; }

        GameEvents.OnBossPhaseChanged?.Invoke(3);
        GameEvents.OnBossEnraged?.Invoke(enrageSpeedMult);
        Debug.Log($"[BossManager] Faz 3: Enrage! Hiz x{enrageSpeedMult}");
    }

    // ── Boss Yenildi ─────────────────────────────────────────────────────
    void OnBossDefeated()
    {
        _active = false;
        if (_minionCoroutine != null) StopCoroutine(_minionCoroutine);
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);

        GameEvents.OnBossDefeated?.Invoke();
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        Debug.Log("[BossManager] Boss yenildi!");
    }

    // ── Yardimcilar ───────────────────────────────────────────────────────
    public float GetHPRatio() => bossMaxHP > 0 ? (float)_currentHP / bossMaxHP : 0f;
    public bool  IsActive()   => _active;
    public bool  IsInvulnerable() => _invulnerable;
}