using UnityEngine;

/// <summary>
/// Top End War — Dushman v4 (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic=true) + CapsuleCollider(IsTrigger=true)
///
/// PERFORMANS: OverlapSphere her frame degil, her 0.2s bir guncellenir.
/// IC ICE GECME: Separation force hala aktif.
/// HP BAR: EnemyHealthBar otomatik eklenir.
/// ZORLUK: Initialize yoksa mesafeye gore kendi hesaplar.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    // Runtime degerler — Initialize veya Awake'den gelir
    int    _maxHealth;
    int    _currentHealth;
    int    _contactDamage;
    float  _moveSpeed;
    int    _cpReward;
    bool   _initialized = false;

    Renderer        _bodyRenderer;
    EnemyHealthBar  _hpBar;
    bool            _isDead           = false;
    bool            _hasDamagedPlayer = false;

    // Separation icin cache
    float     _lastSepTime = 0f;
    Vector3   _separationVec = Vector3.zero;
    const float SEP_INTERVAL = 0.15f; // Saniyede ~6 kez hesapla

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _hpBar = GetComponent<EnemyHealthBar>();
        if (_hpBar == null) _hpBar = gameObject.AddComponent<EnemyHealthBar>();
    }

    void OnEnable()
    {
        _isDead           = false;
        _hasDamagedPlayer = false;
        _separationVec    = Vector3.zero;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;

        // Initialize edilmediyse mesafeye gore hesapla
        if (!_initialized) AutoInit();

        _hpBar?.Init(_maxHealth);
    }

    /// <summary>SpawnManager cagirır — DDA statlari uygular.</summary>
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
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
        _hpBar?.Init(_maxHealth);
    }

    /// <summary>DifficultyManager yoksa basit mesafe tabanli hesap.</summary>
    void AutoInit()
    {
        float z    = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f;
        float mult = 1f + Mathf.Pow(z / 1000f, 1.3f);

        _maxHealth     = Mathf.RoundToInt(100f * mult);
        _currentHealth = _maxHealth;
        _contactDamage = Mathf.RoundToInt(25f  * mult);
        _moveSpeed     = Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f);
        _cpReward      = Mathf.RoundToInt(15f  * mult);
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   pZ  = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        // Z hareketi
        if (pos.z > pZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        // X: oyuncuyu takip
        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        // Separation (her frame degil, cache)
        if (Time.time - _lastSepTime > SEP_INTERVAL)
        {
            _separationVec = CalcSeparation(pos);
            _lastSepTime   = Time.time;
        }
        pos += _separationVec * Time.deltaTime;

        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        if (pos.z < pZ - 15f) gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep   = Vector3.zero;
        int     count = 0;
        Collider[] neighbors = Physics.OverlapSphere(pos, 1.8f);
        foreach (Collider col in neighbors)
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            if (away.magnitude < 0.001f) away = new Vector3(Random.Range(-1f, 1f), 0, 0).normalized * 0.1f;
            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }
        if (count > 0) sep = (sep / count) * 3.5f;
        return sep;
    }

    public void TakeDamage(int dmg)
    {
        if (_isDead) return;
        _currentHealth -= dmg;
        _hpBar?.UpdateBar(_currentHealth);

        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);

        if (_currentHealth <= 0) Die();
    }

    void ResetColor()
    {
        if (!_isDead && _bodyRenderer != null)
            _bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead      = true;
        _initialized = false; // Sonraki spawn icin sifirla
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(_cpReward);
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _hasDamagedPlayer || _isDead) return;
        _hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

    void OnDisable()
    {
        CancelInvoke();
        _initialized = false;
    }
}