using UnityEngine;

/// <summary>
/// Top End War — Dushman v2 (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic=true) + CapsuleCollider(IsTrigger=true)
///
/// IC ICE GECME COZUMU:
///   Update'de komsu dushmanlardan uzaklas (separation force).
///   OverlapSphere ile 1.8 birim icindeki dusmanlardan kac.
///   Bu sayede grid duzenini koruyarak birbirini iterler.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Varsayilan (DifficultyManager yoksa)")]
    public int   defaultHealth   = 120;
    public int   defaultDamage   = 50;
    public float defaultSpeed    = 4.5f;
    public int   defaultCPReward = 15;
    public float xLimit          = 8f;

    [Header("Ayirma Kuvveti")]
    public float separationRadius = 1.8f;  // Bu yaricap icinde baskalarini it
    public float separationForce  = 3.0f;  // Itme hizi

    int      _maxHealth;
    int      _currentHealth;
    int      _contactDamage;
    float    _moveSpeed;
    int      _cpReward;
    Renderer _bodyRenderer;
    bool     _isDead           = false;
    bool     _hasDamagedPlayer = false;

    void Awake()
    {
        _bodyRenderer = GetComponentInChildren<Renderer>();
        UseDefaults();
    }

    void OnEnable()
    {
        _isDead           = false;
        _hasDamagedPlayer = false;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
    }

    public void Initialize(DifficultyManager.EnemyStats stats)
    {
        _maxHealth        = stats.Health;
        _currentHealth    = _maxHealth;
        _contactDamage    = stats.Damage;
        _moveSpeed        = stats.Speed;
        _cpReward         = stats.CPReward;
        _isDead           = false;
        _hasDamagedPlayer = false;
        if (_bodyRenderer != null) _bodyRenderer.material.color = Color.white;
    }

    void UseDefaults()
    {
        _maxHealth     = defaultHealth;
        _currentHealth = defaultHealth;
        _contactDamage = defaultDamage;
        _moveSpeed     = defaultSpeed;
        _cpReward      = defaultCPReward;
    }

    void Update()
    {
        if (_isDead || PlayerStats.Instance == null) return;

        float   playerZ = PlayerStats.Instance.transform.position.z;
        Vector3 pos     = transform.position;

        // Z: Oyuncuya dogru ilerle
        if (pos.z > playerZ + 0.5f)
            pos.z -= _moveSpeed * Time.deltaTime;

        // X: Yavascna takip et
        pos.x = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, 1.5f * Time.deltaTime),
            -xLimit, xLimit);

        // ── AYIRMA KUVVETI (ic ice gecme onleme) ──────────────────────────
        Vector3 separation = Vector3.zero;
        int     count      = 0;
        Collider[] neighbors = Physics.OverlapSphere(pos, separationRadius);
        foreach (Collider col in neighbors)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.CompareTag("Enemy")) continue;

            Vector3 away = pos - col.transform.position;
            if (away.magnitude < 0.001f) away = Vector3.right * 0.1f; // Tam ustuste
            separation += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            count++;
        }

        if (count > 0)
        {
            separation /= count;
            // Sadece X ve Z ekseninde it (Y'yi dokundurma)
            separation.y = 0f;
            pos += separation * separationForce * Time.deltaTime;
        }

        // Sinir
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;

        // Geride kalirsa geri al
        if (pos.z < playerZ - 15f)
            gameObject.SetActive(false);
    }

    public void TakeDamage(int dmg)
    {
        if (_isDead) return;
        _currentHealth -= dmg;
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
        _isDead = true;
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

    void OnDisable() { CancelInvoke(); }
}