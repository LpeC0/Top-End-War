using UnityEngine;

/// <summary>
/// Top End War — Dusman v6
///
/// DEĞİŞİKLİK:
///   - Armor / Elite alanlari eklendi
///   - TakeDamage armorPen ve elite multiplier alabiliyor
///   - Eski Initialize(stats) korunuyor
///   - Yeni Initialize(stats, armor, isElite) eklendi
///   - AutoInit gizli scaling yerine sabit fallback oldu
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    // DEĞİŞİKLİK
    [Header("Combat Flags (Debug / Fallback)")]
    [SerializeField] int  _armor = 0;
    [SerializeField] bool _isElite = false;

    int    _maxHealth;
    int    _currentHealth;
    int    _contactDamage;
    float  _moveSpeed;
    int    _cpReward;
    bool   _initialized      = false;
    bool   _isDead           = false;
    bool   _hasDamagedPlayer = false;

    Renderer       _bodyRenderer;
    EnemyHealthBar _hpBar;

    float   _lastSepTime   = 0f;
    Vector3 _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar        = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();
        UseDefaults();
    }

    void OnEnable()
    {
        _isDead           = false;
        _hasDamagedPlayer = false;
        _separationVec    = Vector3.zero;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.white;

        if (!_initialized)
            AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth        = stats.Health;
        _currentHealth    = _maxHealth;
        _contactDamage    = stats.Damage;
        _moveSpeed        = stats.Speed;
        _cpReward         = stats.CPReward;
        _initialized      = true;
        _isDead           = false;
        _hasDamagedPlayer = false;

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.white;

        _hpBar?.Init(_maxHealth);
    }

    // DEĞİŞİKLİK
    public void Initialize(DifficultyManager.EnemyStats stats, int armor, bool isElite)
    {
        Initialize(stats);
        _armor = Mathf.Max(0, armor);
        _isElite = isElite;
    }

    // DEĞİŞİKLİK
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
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   pZ  = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime   = Time.time;
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
                away = new Vector3(Random.Range(-1f, 1f), 0, 0).normalized * 0.1f;

            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        return count > 0 ? (sep / count) * 3.5f : Vector3.zero;
    }

    // DEĞİŞİKLİK
    public void TakeDamage(int rawDamage, int armorPenValue = 0, float eliteMultiplier = 1f, Color? hitColor = null)
    {
        if (_isDead) return;

        int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPenValue));

        // Minimum kirilim icin basit ve anlasilir armor cozumu:
        // finalDamage = rawDamage - effectiveArmor
        // en az 1 hasar
        int finalDamage = Mathf.Max(1, rawDamage - effectiveArmor);

        if (_isElite)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * Mathf.Max(1f, eliteMultiplier)));

        _currentHealth -= finalDamage;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.red;

        Invoke(nameof(ResetColor), 0.1f);

        bool isCrit = finalDamage > 200;
        Color popupColor = hitColor ?? DamagePopup.GetColor("Commander");
        DamagePopup.Show(transform.position, finalDamage, popupColor, isCrit);

        if (_currentHealth <= 0)
            Die();
    }

    void ResetColor()
    {
        if (!_isDead && _bodyRenderer != null)
            _bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _initialized = false;
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

        if (!other.CompareTag("Player") || _hasDamagedPlayer) return;

        _hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

// Enemy.cs içine eklenecek bridge metot
public void ConfigureCombat(int armor, bool isElite)
{
    // Buraya zırh ve elite görsel mantığını bağlayabilirsin
    // Şimdilik boş kalsa bile hata vermesini engeller.
}
    void OnDisable()
    {
        CancelInvoke();
        _initialized = false;
    }

    // DEĞİŞİKLİK
    public int  Armor => _armor;
    public bool IsElite => _isElite;
}