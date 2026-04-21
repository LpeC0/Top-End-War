using UnityEngine;

/// <summary>
/// Top End War — Dusman v7 (Runtime Stabilite Patch)
///
/// PATCH OZETI:
/// - Eski calisan davranislar KORUNDU
/// - Reservation / Threat eklendi
/// - ConfigureCombat dolduruldu
/// - Elite gorsel tonu eklendi
///
/// v7 → Patch Delta:
///   • OnTriggerEnter: PlayerStats.Instance fallback eklendi.
///     other.GetComponent<PlayerStats>() child collider durumunda null donuyordu;
///     Instance uzerinden giderek contact damage garantilenir.
///   • OnTriggerEnter: null check log eklendi — sessiz kayip olmaz.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    [Header("Combat Flags (Debug / Fallback)")]
    [SerializeField] int _armor = 0;
    [SerializeField] bool _isElite = false;

    int _maxHealth;
    int _currentHealth;
    int _contactDamage;
    float _moveSpeed;
    int _cpReward;
    bool _initialized = false;
    bool _isDead = false;

    // DEĞİŞİKLİK: Player temasında enemy artık kendini öldürmüyor;
    // kontrollü aralıkla tekrar hasar denemesi yapıyor.
    float _nextPlayerDamageTime = 0f;
    [SerializeField] float playerTouchInterval = 0.20f;

    Renderer _bodyRenderer;
    EnemyHealthBar _hpBar;

    float _lastSepTime = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    // Reservation / threat
    int _reservationCount = 0;
    int _reservationCap = 2;
    float _threatWeight = 1f;
    Color _baseColor = Color.white;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();

        UseDefaults();
    }

    void OnEnable()
    {
        _isDead = false;
        _nextPlayerDamageTime = 0f;
        _separationVec = Vector3.zero;
        _reservationCount = 0;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = _baseColor;

        if (!_initialized)
            AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth = stats.Health;
        _currentHealth = _maxHealth;
        _contactDamage = stats.Damage;
        _moveSpeed = stats.Speed;
        _cpReward = stats.CPReward;
        _initialized = true;
        _isDead = false;
        _nextPlayerDamageTime = 0f;
        _reservationCount = 0;

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
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float pZ = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime = Time.time;
        }

        pos += _separationVec * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f)
            gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep = Vector3.zero;
        int count = 0;

        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;

            Vector3 away = pos - col.transform.position;
            away.y = 0f;

            if (away.magnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, 0f).normalized * 0.1f;

            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        return count > 0 ? (sep / count) * 3.5f : Vector3.zero;
    }

    public void TakeDamage(int rawDamage, int armorPenValue = 0, float eliteMultiplier = 1f, Color? hitColor = null)
    {
        if (_isDead) return;

        int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPenValue));
        int finalDamage    = Mathf.Max(1, rawDamage - effectiveArmor);

        if (_isElite)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, eliteMultiplier)));

        _currentHealth -= finalDamage;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.red;

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), 0.1f);

        bool isCrit = finalDamage > 200;
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

    // DEĞİŞİKLİK: Aynı enemy, player ile temas sürüyorsa tekrar hasar deneyebilir.
    void OnTriggerStay(Collider other)
    {
        if (_isDead) return;
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other);
    }

    // DEĞİŞİKLİK: Player temasında enemy kendini yok etmez; hasar gerçekten işlendiğinde
    // tekrar deneme aralığı PlayerStats invincibility ile birlikte doğal çalışır.
    void TryDamagePlayer(Collider other)
    {
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

_nextPlayerDamageTime = Time.time + playerTouchInterval;

if (applied)
    Debug.Log($"[Enemy] Contact damage APPLIED: {_contactDamage}");
else
    Debug.Log($"[Enemy] Contact damage BLOCKED");
    }

    void OnDisable()
    {
        CancelInvoke();
        _initialized  = false;
        _reservationCount = 0;
    }

    public int   Armor                => _armor;
    public bool  IsElite              => _isElite;
    public int   ReservationCount     => _reservationCount;
    public bool  CanAcceptReservation => _reservationCount < _reservationCap;
    public float ThreatWeight         => _threatWeight;
    public float HealthRatio          => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 1f;
}