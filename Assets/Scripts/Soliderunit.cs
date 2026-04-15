using UnityEngine;

public enum SoldierPath { Piyade, Mekanik, Teknoloji }

public class SoldierUnit : MonoBehaviour
{
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string      biome      = "Tas";
    [HideInInspector] public int         mergeLevel = 1;

    [HideInInspector] public int   maxHP;
    [HideInInspector] public int   currentHP;
    [HideInInspector] public float baseAtk;
    [HideInInspector] public float atkSpeed;

    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED        = 14f;

    // DEĞİŞİKLİK
    const float DETECT_RADIUS       = 24f;
    const float KEEP_TARGET_RADIUS  = 28f;
    const float RETARGET_INTERVAL   = 0.18f;

    Renderer _rend;
    float    _nextFire;
    bool     _dead;

    // DEĞİŞİKLİK
    Collider _currentTarget;
    float    _nextRetargetTime;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        _dead = false;

        // DEĞİŞİKLİK
        _currentTarget = null;
        _nextRetargetTime = 0f;

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

        // DEĞİŞİKLİK
        if (!IsTargetValid(_currentTarget) || Time.time >= _nextRetargetTime)
        {
            _currentTarget = AcquireTarget();
            _nextRetargetTime = Time.time + RETARGET_INTERVAL;
        }

        if (_currentTarget == null) return;

        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path) : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        { 1 => 0f, 2 => 0.10f, 3 => 0.20f, 4 => 0.30f, _ => 0.40f };

        float mergeMult = mergeLevel switch { 2 => 1.8f, 3 => 3.5f, 4 => 7.0f, _ => 1.0f };
        int   finalDmg  = Mathf.RoundToInt(baseAtk * mergeMult * (1f + cmdAura) * biomeMultiplier);

        FireBullet(_currentTarget, finalDmg);
    }

    // DEĞİŞİKLİK
    bool IsTargetValid(Collider col)
    {
        if (col == null || !col.gameObject.activeInHierarchy) return false;
        if (!col.CompareTag("Enemy")) return false;

        Vector3 delta = col.bounds.center - transform.position;
        if (delta.z < -1f) return false;

        return delta.sqrMagnitude <= KEEP_TARGET_RADIUS * KEEP_TARGET_RADIUS;
    }

    // DEĞİŞİKLİK
    Collider AcquireTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, DETECT_RADIUS);

        Collider best = null;
        float bestScore = float.MaxValue;

        float playerZ = PlayerStats.Instance != null
            ? PlayerStats.Instance.transform.position.z
            : transform.position.z;

        foreach (Collider col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;

            Vector3 p = col.bounds.center;
            Vector3 delta = p - transform.position;

            if (delta.z < -1f) continue;

            float zToPlayer = Mathf.Max(0f, p.z - playerZ);
            float xOffset   = Mathf.Abs(p.x - transform.position.x);
            float distScore = delta.sqrMagnitude * 0.03f;

            float score = (zToPlayer * 2.2f) + (xOffset * 1.6f) + distScore;

            if (score < bestScore)
            {
                bestScore = score;
                best = col;
            }
        }

        return best;
    }

    void FireBullet(Collider target, int dmg)
    {
        // DEĞİŞİKLİK
        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 aimPoint = target.bounds.center;
        Vector3 dir = (aimPoint - pos).normalized;

        GameObject b = null;
        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null) return;

        Bullet bComp = b.GetComponent<Bullet>();
        if (bComp != null)
        {
            bComp.SetDamage(dmg);
            bComp.bulletColor = GetPathColor() * 0.85f;
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 38f;

        Bullet blt = b.GetComponent<Bullet>();
        if (blt != null) blt.hitterPath = path.ToString();
    }

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

    public Color GetPathColor() => path switch
    {
        SoldierPath.Piyade    => new Color(0.2f, 0.85f, 0.2f),
        SoldierPath.Mekanik   => new Color(0.65f, 0.65f, 0.65f),
        SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),
        _                     => Color.white
    } * (mergeLevel switch { 2 => 1.2f, 3 => 1.5f, 4 => 2.0f, _ => 1.0f });
}