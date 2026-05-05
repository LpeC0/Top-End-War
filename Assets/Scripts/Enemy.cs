using UnityEngine;

/// <summary>
/// Top End War — Dusman v7.2 (Gameplay Fix Patch)
///
/// v7.1 → v7.2 Fix Delta:
///   • playerTouchInterval: 0.20f → 0.50f
///     0.20f = 5 vuruş/saniye, çok agresif ve tutarsız hissettiriyordu.
///     0.50f = 2 vuruş/saniye, kontrollü tick damage hissi verir.
///     OnTriggerStay mekanizması değişmedi; sadece interval güncellendi.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    [Header("Combat Flags (Debug / Fallback)")]
    [SerializeField] int _armor = 0;
    [SerializeField] bool _isElite = false;

    EnemyThreatType _threatType = EnemyThreatType.Standard;
    int _maxHealth;
    int _currentHealth;
    int _contactDamage;
    float _moveSpeed;
    int _cpReward;
    bool _initialized = false;
    bool _isDead = false;
    bool _anchorMoveActive = false;

    float _nextPlayerDamageTime = 0f;
    float _lifeStartTime = 0f; // DEĞİŞİKLİK: Ortalama TTK ölçümü için spawn zamanı tutulur.

    // FIX: 0.20f → 0.50f  (5 hit/s → 2 hit/s — kontrollü tick damage)
    [SerializeField] float playerTouchInterval = 0.50f;
    [SerializeField] bool logContactDamage = false;

    [SerializeField] float engageDistance = 14f;
    [SerializeField] float hardEngageDistance = 8f;
    [SerializeField] float preEngageLateralSpeed = 0.45f;
    [SerializeField] float engageLateralSpeed = 1.8f;
    [SerializeField] float hardEngageLateralBoost = 1.35f;
    [SerializeField] float outerLaneInwardFactor = 0.72f;

    float _spawnLaneX = 0f;
    bool _spawnLaneCaptured = false;

    Renderer _bodyRenderer;
    EnemyHealthBar _hpBar;

    float _lastSepTime = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    int _reservationCount = 0;
    int _reservationCap = 2;
    float _threatWeight = 1f;
    Color _baseColor = Color.white;

    float _speedMult = 1f;
    float _engageDistanceMult = 1f;
    float _hardEngageDistanceMult = 1f;
    float _preLateralMult = 1f;
    float _engageLateralMult = 1f;
    float _hardLateralBoostMult = 1f;
    float _outerLaneMult = 1f;
    float _separationMult = 1f;
    Vector3 _baseScale = Vector3.one;
    bool _baseScaleCaptured = false;
    float _spawnIntroEndTime = -1f;
    const float SPAWN_INTRO_DURATION = 0.4f;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();

        CaptureBaseScale();
        UseDefaults();
    }

    void OnEnable()
    {
        _isDead = false;
        _lifeStartTime = Time.time; // DEĞİŞİKLİK: Fallback init düşmanlarında da TTK başlangıcı güvenli kalır.
        _nextPlayerDamageTime = 0f;
        _separationVec = Vector3.zero;
        _reservationCount = 0;
        CaptureSpawnLane();
        BeginSpawnIntro();
        ApplyBehaviorProfile(EnemyThreatType.Standard, EnemyClass.Normal);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        if (!_initialized)
            AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    void OnDisable()
    {
        CancelInvoke();
        _initialized          = false;
        _reservationCount     = 0;
        _isDead               = false;
        _nextPlayerDamageTime = 0f;
        if (_baseScaleCaptured)
            transform.localScale = _baseScale;
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        // DEĞİŞİKLİK: Enemy spawn sayısı ve TTK başlangıcı debug metriklerine yazılır.
        _lifeStartTime = Time.time;
        RunDebugMetrics.Instance.RecordEnemySpawn();
        _maxHealth     = stats.Health;
        _currentHealth = _maxHealth;
        _contactDamage = stats.Damage;
        _moveSpeed     = stats.Speed;
        _cpReward      = stats.CPReward;
        _initialized   = true;
        _isDead        = false;
        _nextPlayerDamageTime = 0f;
        _reservationCount = 0;
        CaptureSpawnLane();

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats, int armor, bool isElite)
    {
        Initialize(stats);
        ConfigureCombat(armor, isElite);
    }

    void AutoInit()
    {
        UseDefaults();
        _initialized = true;
    }

    void CaptureSpawnLane()
    {
        _spawnLaneX = transform.position.x;
        _spawnLaneCaptured = true;
    }

    void CaptureBaseScale()
    {
        if (_baseScaleCaptured) return;
        _baseScale = transform.localScale;
        _baseScaleCaptured = true;
    }

    void BeginSpawnIntro()
    {
        CaptureBaseScale();
        _spawnIntroEndTime = Time.time + SPAWN_INTRO_DURATION;
        transform.localScale = _baseScale * 0.75f;
    }

    float UpdateSpawnIntro()
    {
        if (_spawnIntroEndTime <= 0f)
            return 1f;

        float t = 1f - ((_spawnIntroEndTime - Time.time) / SPAWN_INTRO_DURATION);
        t = Mathf.Clamp01(t);
        transform.localScale = Vector3.Lerp(_baseScale * 0.75f, _baseScale, t);
        return t;
    }

    bool IsInSpawnIntro()
    {
        return Time.time < _spawnIntroEndTime;
    }

    void UseDefaults()
    {
        _maxHealth = _currentHealth = 100;
        _contactDamage = 25;
        _moveSpeed = 4.5f;
        _cpReward = 15;
        _armor = 0;
        _isElite = false;
        _reservationCap = 2;
        _threatWeight = 1f;
        _baseColor = Color.white;
        ApplyBehaviorProfile(EnemyThreatType.Standard, EnemyClass.Normal);
    }

    void Update()
    {
        
        if (_isDead || PlayerStats.Instance == null) return;

        if (_anchorMoveActive) return;

        if (!_spawnLaneCaptured)
            CaptureSpawnLane();

        float dt = Time.deltaTime;
        float introT = UpdateSpawnIntro();
        float introMoveMult = Mathf.Lerp(0.35f, 1f, introT);

        Transform playerTf = PlayerStats.Instance.transform;
        float pZ = playerTf.position.z;
        float pX = playerTf.position.x;

        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * _speedMult * introMoveMult * dt;

        float distanceAhead = pos.z - pZ;

        float approachTargetX = _spawnLaneX;
        if (Mathf.Abs(_spawnLaneX) > xLimit * 0.45f)
            approachTargetX = _spawnLaneX * Mathf.Clamp01(outerLaneInwardFactor * _outerLaneMult);

        float engageT = Mathf.InverseLerp(
            engageDistance * _engageDistanceMult,
            hardEngageDistance * _hardEngageDistanceMult,
            distanceAhead);
        float targetX = Mathf.Lerp(approachTargetX, pX, engageT);

        float lateralSpeed = Mathf.Lerp(
            preEngageLateralSpeed * _preLateralMult,
            engageLateralSpeed * _engageLateralMult,
            engageT);

        if (distanceAhead <= hardEngageDistance && Mathf.Abs(pos.x - pX) > 2.5f)
            lateralSpeed *= hardEngageLateralBoost * _hardLateralBoostMult;

        pos.x = Mathf.MoveTowards(pos.x, targetX, lateralSpeed * introMoveMult * dt);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime   = Time.time;
        }

        pos.x += _separationVec.x * dt;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f)
{
    PlayerStats.Instance?.TryTakeContactDamage(_contactDamage);
    gameObject.SetActive(false);
}
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep = Vector3.zero;
        int count   = 0;

        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;

            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            away.z = 0f;

            if (away.magnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, 0f).normalized * 0.1f;

            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        return count > 0 ? Vector3.ClampMagnitude(sep / count, 1f) * 1.4f * _separationMult : Vector3.zero;
    }

    public void TakeDamage(int rawDamage, int armorPenValue = 0, float eliteMultiplier = 1f, Color? hitColor = null)
    {
        if (_isDead) return;

        int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPenValue));
        float armorMult    = 100f / (100f + effectiveArmor);

        // FIX: Mathf.Max(1,...) zırh sıfır hasar yapmasını engeller.
        int finalDamage    = Mathf.Max(1, Mathf.RoundToInt(rawDamage * armorMult));

        if (_isElite)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, eliteMultiplier)));

        _currentHealth -= finalDamage;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.red;

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), 0.1f);

        bool isCrit   = finalDamage > 200;
        Color popupColor = hitColor ?? DamagePopup.GetColor("Commander");
        DamagePopup.Show(transform.position, finalDamage, popupColor, isCrit);

        if (_currentHealth <= 0)
            Die();
    }

    public void ConfigureCombat(int armor, bool isElite)
    {
        _armor   = Mathf.Max(0, armor);
        _isElite = isElite;

        _reservationCap = _isElite ? 3 : 2;
        _threatWeight   = _isElite ? 1.35f : 1f;
        _baseColor      = _isElite ? new Color(1f, 0.92f, 0.35f) : Color.white;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;
    }

    public void ConfigureArchetype(EnemyArchetypeConfig archetype)
{
    if (archetype == null) return;

    // DEĞİŞİKLİK: Archetype threatType sonradan okunabilsin diye saklanıyor.
    _threatType = archetype.threatType;

    ApplyBehaviorProfile(_threatType, archetype.enemyClass);
}

    void ApplyBehaviorProfile(EnemyThreatType threatType, EnemyClass enemyClass)
    {
        _speedMult = 1f;
        _engageDistanceMult = 1f;
        _hardEngageDistanceMult = 1f;
        _preLateralMult = 1f;
        _engageLateralMult = 1f;
        _hardLateralBoostMult = 1f;
        _outerLaneMult = 1f;
        _separationMult = 1f;

        switch (threatType)
        {
            case EnemyThreatType.PackPressure:
                _speedMult = 1.18f;
                _engageDistanceMult = 1.25f;
                _hardEngageDistanceMult = 1.15f;
                _preLateralMult = 1.15f;
                _engageLateralMult = 1.25f;
                _outerLaneMult = 0.90f;
                _separationMult = 0.75f;
                break;

            case EnemyThreatType.Durable:
                _speedMult = 0.78f;
                _engageDistanceMult = 0.85f;
                _hardEngageDistanceMult = 0.90f;
                _preLateralMult = 0.55f;
                _engageLateralMult = 0.60f;
                _hardLateralBoostMult = 0.70f;
                _outerLaneMult = 1.15f;
                _separationMult = 0.60f;
                break;

            case EnemyThreatType.ElitePressure:
                _speedMult = 1.35f;
                _engageDistanceMult = 1.35f;
                _hardEngageDistanceMult = 1.20f;
                _preLateralMult = 1.10f;
                _engageLateralMult = 1.55f;
                _hardLateralBoostMult = 1.55f;
                _separationMult = 0.70f;
                break;

            case EnemyThreatType.Priority:
            case EnemyThreatType.Backline:
                _speedMult = 0.92f;
                _engageDistanceMult = 0.95f;
                _hardEngageDistanceMult = 0.90f;
                _preLateralMult = 0.65f;
                _engageLateralMult = 0.75f;
                _hardLateralBoostMult = 0.85f;
                _outerLaneMult = 1.10f;
                _separationMult = 0.80f;
                break;
        }

        if (enemyClass == EnemyClass.BossSupport)
        {
            _speedMult *= 0.88f;
            _engageLateralMult *= 0.80f;
            _outerLaneMult *= 1.10f;
        }
    }

    public bool TryReserve()
    {
        if (_reservationCount >= _reservationCap) return false;
        _reservationCount++;
        return true;
    }

    public void ReleaseReservation()
    {
        _reservationCount = Mathf.Max(0, _reservationCount - 1);
    }

    void ResetColor()
    {
        if (!_isDead && _bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;
    }

    void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _initialized = false;
        _reservationCount = 0;
        CancelInvoke();

        PlayerStats.Instance?.AddCPFromKill(_cpReward);
        RunDebugMetrics.Instance.RecordEnemyKilled(Time.time - _lifeStartTime); // DEĞİŞİKLİK: Enemy ölümü TTK metriğine eklenir.
        SaveManager.Instance?.RegisterKill();
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isDead) return;

        SoldierUnit soldier = other.GetComponent<SoldierUnit>();
        if (soldier != null)
        {
            soldier.TakeDamage(_contactDamage);
            Die();
            return;
        }

        if (other.CompareTag("Player"))
            TryDamagePlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (_isDead) return;
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other);
    }

    void TryDamagePlayer(Collider other)
    {
        if (IsInSpawnIntro()) return;
        if (Time.time < _nextPlayerDamageTime) return;

        PlayerStats ps = PlayerStats.Instance
                      ?? other.GetComponent<PlayerStats>()
                      ?? other.GetComponentInParent<PlayerStats>();

        if (ps == null)
        {
            Debug.LogWarning($"[Enemy] PlayerStats bulunamadi — contact damage uygulanamadi. " +
                             $"Player objesinin Tag'i 'Player' ve PlayerStats script'i root'ta olmali.");
            _nextPlayerDamageTime = Time.time + playerTouchInterval;
            return;
        }

        bool applied = ps.TryTakeContactDamage(_contactDamage);

        // FIX: interval 0.5s — kontrollü tick damage, 5 vuruş/s değil 2 vuruş/s
        _nextPlayerDamageTime = Time.time + playerTouchInterval;

        if (!logContactDamage) return;

        if (applied)
            Debug.Log($"[Enemy] Contact damage APPLIED: {_contactDamage}");
        else
            Debug.Log($"[Enemy] Contact damage BLOCKED");
    }

    public EnemyThreatType ThreatType => _threatType;
    public int   Armor                => _armor;
    public bool  IsElite              => _isElite;
    public bool  IsAlive              => !_isDead && gameObject.activeInHierarchy && _currentHealth > 0;
    public int   ReservationCount     => _reservationCount;
    public bool  CanAcceptReservation => _reservationCount < _reservationCap;
    public float ThreatWeight         => _threatWeight;
    public float HealthRatio          => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 1f;
    public void EnableAnchorMovement() => _anchorMoveActive = true;
    public void DisableAnchorMovement() => _anchorMoveActive = false;
}
