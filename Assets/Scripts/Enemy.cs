using UnityEngine;

/// <summary>
/// Top End War — Dusman v5 (Claude)
///
/// DEGISIKLIKLER (v5):
///   OnTriggerEnter: Soldier tagini da taniyor.
///   Dusman Soldier'a carparsa: Soldier hasar alir, dusman olur.
///   Dusman Player'a carparsa: CommanderHP.TakeDamage (CP etkilenmiyor).
///   HP degerleri: Normal 1100, Zirh 2250, Elite 3750 (ordu DPS'e gore).
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (Initialize cagrilmazsa)")]
    public float xLimit = 8f;

    int    _maxHealth;
    int    _currentHealth;
    int    _contactDamage;
    float  _moveSpeed;
    int    _cpReward;
    bool   _initialized      = false;
    bool   _isDead            = false;
    bool   _hasDamagedPlayer  = false;

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
        _isDead          = false;
        _hasDamagedPlayer= false;
        _separationVec   = Vector3.zero;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
        if (!_initialized) AutoInit();
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
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
        _hpBar?.Init(_maxHealth);
    }

    void AutoInit()
    {
        float z    = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f;
        float mult = 1f + Mathf.Pow(z / 1000f, 1.3f);
        // Yeni HP degerlerine gore (ordu DPS kalibre)
        _maxHealth     = Mathf.RoundToInt(1100f * mult);
        _currentHealth = _maxHealth;
        _contactDamage = Mathf.RoundToInt(30f   * mult);
        _moveSpeed     = Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f);
        _cpReward      = Mathf.RoundToInt(15f   * mult);
    }

    void UseDefaults()
    {
        _maxHealth = _currentHealth = 1100;
        _contactDamage = 30; _moveSpeed = 4.5f; _cpReward = 15;
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   pZ  = PlayerStats.Instance.transform.position.z;
        Vector3 pos = transform.position;

        if (pos.z > pZ + 0.5f) pos.z -= _moveSpeed * Time.deltaTime;

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

        if (pos.z < pZ - 15f) gameObject.SetActive(false);
    }

    Vector3 CalcSeparation(Vector3 pos)
    {
        Vector3 sep   = Vector3.zero;
        int     count = 0;
        foreach (Collider col in Physics.OverlapSphere(pos, 1.8f))
        {
            if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
            Vector3 away = pos - col.transform.position;
            away.y = 0f;
            if (away.magnitude < 0.001f) away = new Vector3(Random.Range(-1f, 1f), 0, 0).normalized * 0.1f;
            sep += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }
        return count > 0 ? (sep / count) * 3.5f : Vector3.zero;
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
        if (!_isDead && _bodyRenderer != null) _bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = _initialized = false;
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(_cpReward);
        SaveManager.Instance?.RegisterKill();  // kill sayaci
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isDead) return;

        // --- Askere carparsa ---
        if (other.CompareTag("Soldier"))
        {
            other.GetComponent<SoldierUnit>()?.TakeDamage(_contactDamage);
            Die(); // dusman da olur
            return;
        }

        // --- Oyuncuya carparsa ---
        if (!other.CompareTag("Player") || _hasDamagedPlayer) return;
        _hasDamagedPlayer = true;
        // PlayerStats.TakeContactDamage → OnPlayerDamaged event → CommanderHP alir
        other.GetComponent<PlayerStats>()?.TakeContactDamage(_contactDamage);
        Die();
    }

    void OnDisable() { CancelInvoke(); _initialized = false; }
}