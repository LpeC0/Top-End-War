using UnityEngine;

/// <summary>
/// Asker path tipleri — GateData ve ArmyManager ile eslesik olmali.
/// </summary>
public enum SoldierPath { Piyade, Mekanik, Teknoloji }

/// <summary>
/// Top End War — Bireysel Asker v2 (Claude)
///
/// UNITY NOTU:
///   - "Soldier" TAG eklemenize GEREK YOK — tag kullanılmıyor.
///   - ArmyManager bu scripti otomatik yönetir, elle prefab gerekmez.
///   - Bullet pool "Bullet" etiketiyle çalışır — asker ateşi de aynı poolu kullanır.
/// </summary>
public class SoldierUnit : MonoBehaviour
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string      biome      = "Tas";
    [HideInInspector] public int         mergeLevel = 1;

    // ── Statlar ───────────────────────────────────────────────────────────
    [HideInInspector] public int   maxHP;
    [HideInInspector] public int   currentHP;
    [HideInInspector] public float baseAtk;
    [HideInInspector] public float atkSpeed;

    // ── Formasyon ─────────────────────────────────────────────────────────
    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED  = 14f;
    const float DETECT_RADIUS = 28f;

    Renderer _rend;
    float    _nextFire;
    bool     _dead;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        // TAG EKLEME — Unity'de Soldier tag'i varsayılan olarak yok,
        // ve buna ihtiyacımız yok. GetComponent<SoldierUnit>() kullanıyoruz.
    }

    void OnEnable()
    {
        _dead     = false;
        _nextFire = Time.time + Random.value / Mathf.Max(atkSpeed, 0.1f);
    }

    void Update()
    {
        if (_dead || PlayerStats.Instance == null) return;
        FollowPlayer();
        if (Time.time >= _nextFire) TryShoot();
    }

    void FollowPlayer()
    {
        Vector3 target = PlayerStats.Instance.transform.position + formationOffset;
        target.y = 1.2f;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_SPEED);
    }

    void TryShoot()
    {
        _nextFire = Time.time + 1f / Mathf.Max(atkSpeed, 0.01f);

        Collider best    = null;
        float    bestDist= DETECT_RADIUS * DETECT_RADIUS;

        foreach (Collider col in Physics.OverlapSphere(transform.position, DETECT_RADIUS))
        {
            if (!col.CompareTag("Enemy")) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = col; }
        }
        if (best == null) return;

        // Hasar hesapla
        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path) : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        { 1 => 0f, 2 => 0.10f, 3 => 0.20f, 4 => 0.30f, _ => 0.40f };

        float mergeMult = mergeLevel switch { 2 => 1.8f, 3 => 3.5f, 4 => 7.0f, _ => 1.0f };
        int   finalDmg  = Mathf.RoundToInt(baseAtk * mergeMult * (1f + cmdAura) * biomeMultiplier);

        FireBullet(best, finalDmg);
    }

    void FireBullet(Collider target, int dmg)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        Vector3 pos = transform.position + Vector3.up * 0.5f;

        // Bullet pool — null güvenli
        GameObject b = null;
        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null) return;  // pool doluysa veya yoksa atla

        Bullet bComp = b.GetComponent<Bullet>();
        if (bComp != null)
        {
            bComp.SetDamage(dmg);
            bComp.bulletColor = GetPathColor() * 0.85f;
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 32f;
    }

    // ── Hasar / Heal ──────────────────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (_dead) return;
        currentHP -= dmg;
        if (_rend) StartCoroutine(FlashRed());
        if (currentHP <= 0) Die();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (!_rend) yield break;
        Color orig = _rend.material.color;
        _rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (_rend && !_dead) _rend.material.color = orig;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        ArmyManager.Instance?.RemoveSoldier(this);
        gameObject.SetActive(false);
    }

    public void HealPercent(float pct)
        => currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * pct));

    // ── Renk ─────────────────────────────────────────────────────────────
    public Color GetPathColor() => path switch
    {
        SoldierPath.Piyade    => new Color(0.2f, 0.85f, 0.2f),  // yeşil
        SoldierPath.Mekanik   => new Color(0.65f, 0.65f, 0.65f), // gri
        SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),   // mavi
        _                     => Color.white
    } * (mergeLevel switch { 2 => 1.2f, 3 => 1.5f, 4 => 2.0f, _ => 1.0f });
}