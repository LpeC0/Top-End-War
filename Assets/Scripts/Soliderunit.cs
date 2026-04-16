using UnityEngine;

/// <summary>
/// Asker path tipleri — GateData ve ArmyManager ile eslesik olmali.
/// </summary>
public enum SoldierPath
{
    Piyade,
    Mekanik,
    Teknoloji
}

/// <summary>
/// Top End War — Bireysel Asker v4
///
/// PATCH OZETI:
/// - Eski follow + ates akisi korundu
/// - WeaponArchetypeConfig entegrasyonu eklendi
/// - Reservation hedef sistemi eklendi
/// - weaponConfig yoksa guvenli fallback ile calisir
/// </summary>
public class SoldierUnit : MonoBehaviour
{
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string biome = "Tas";
    [HideInInspector] public int mergeLevel = 1;

    [HideInInspector] public int maxHP;
    [HideInInspector] public int currentHP;

    [HideInInspector] public float chassisDamageMult = 1f;
    [HideInInspector] public float chassisFireRateMult = 1f;
    [HideInInspector] public int formationRank = 1;

    [HideInInspector] public WeaponArchetypeConfig weaponConfig;
    [HideInInspector] public int affinityPercent = 100;

    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED = 14f;
    const float RETARGET_INTERVAL = 0.20f;
    const float KEEP_TARGET_MULT = 1.15f;
    const float FALLBACK_RANGE = 18f;
    const float FALLBACK_PROJECTILE_SPEED = 32f;

    Renderer _rend;
    float _nextFire;
    bool _dead;

    Enemy _reservedEnemy;
    float _nextRetargetTime;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        _dead = false;
        _reservedEnemy = null;
        _nextRetargetTime = 0f;

        float fireRate = GetFinalFireRate();
        _nextFire = Time.time + Random.value / Mathf.Max(fireRate, 0.1f);
    }

    void Update()
    {
        if (_dead || PlayerStats.Instance == null) return;

        FollowPlayer();

        if (Time.time >= _nextFire)
            TryShoot();
    }

    void FollowPlayer()
    {
        Vector3 target = PlayerStats.Instance.transform.position + formationOffset;
        target.y = 1.2f;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_SPEED);
    }

    float GetAttackRange()
    {
        return weaponConfig != null ? weaponConfig.attackRange : FALLBACK_RANGE;
    }

    float GetProjectileSpeed()
    {
        return weaponConfig != null ? weaponConfig.projectileSpeed : FALLBACK_PROJECTILE_SPEED;
    }

    float GetFinalFireRate()
    {
        float baseRate = weaponConfig != null ? weaponConfig.fireRate : 1.5f;
        return Mathf.Max(0.05f, baseRate * chassisFireRateMult);
    }

    int GetFinalDamage()
    {
        float baseDamage = weaponConfig != null ? weaponConfig.baseDamage : 15f;

        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path)
            : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        {
            1 => 0f,
            2 => 0.10f,
            3 => 0.20f,
            4 => 0.30f,
            _ => 0.40f
        };

        float mergeMult = mergeLevel switch
        {
            2 => 1.8f,
            3 => 3.5f,
            4 => 7.0f,
            _ => 1.0f
        };

        float affinityMult = affinityPercent / 100f;

        float raw = baseDamage
            * chassisDamageMult
            * biomeMultiplier
            * (1f + cmdAura)
            * mergeMult
            * affinityMult;

        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    bool IsTargetStillValid(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return false;

        float keepRange = GetAttackRange() * KEEP_TARGET_MULT;
        Vector3 delta = enemy.transform.position - transform.position;

        if (delta.z < -1f) return false;
        return delta.sqrMagnitude <= keepRange * keepRange;
    }

    float ScoreTarget(Enemy enemy)
    {
        Vector3 pos = enemy.transform.position;
        Vector3 delta = pos - transform.position;

        float dist = delta.magnitude;
        float xOffset = Mathf.Abs(pos.x - transform.position.x);
        float zForward = Mathf.Max(0f, pos.z - transform.position.z);

        TargetProfile profile = weaponConfig != null
            ? weaponConfig.defaultTargetProfile
            : TargetProfile.Balanced;

        float score = dist + (enemy.ReservationCount * 2.5f);

        switch (profile)
        {
            case TargetProfile.NearestThreat:
                score += xOffset * 0.8f;
                score -= zForward * 0.35f;
                break;

            case TargetProfile.EliteHunter:
                score += enemy.IsElite ? -10f : 8f;
                score -= enemy.Armor * 0.15f;
                break;

            case TargetProfile.Finisher:
                score += enemy.HealthRatio * 8f;
                break;

            case TargetProfile.ClusterFocus:
                score += xOffset * 0.5f;
                break;

            case TargetProfile.Balanced:
            default:
                score += xOffset * 1.1f;
                score += zForward * 0.25f;
                break;
        }

        score -= enemy.ThreatWeight;
        return score;
    }

    Enemy AcquireTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, GetAttackRange());

        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Collider col in cols)
        {
            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            Vector3 delta = enemy.transform.position - transform.position;
            if (delta.z < -1f) continue;

            bool allowed = enemy == _reservedEnemy || enemy.CanAcceptReservation;
            if (!allowed) continue;

            float score = ScoreTarget(enemy);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    void RefreshReservedTarget()
    {
        if (IsTargetStillValid(_reservedEnemy) && Time.time < _nextRetargetTime)
            return;

        Enemy next = AcquireTarget();
        _nextRetargetTime = Time.time + RETARGET_INTERVAL;

        if (next == _reservedEnemy) return;

        if (_reservedEnemy != null)
            _reservedEnemy.ReleaseReservation();

        _reservedEnemy = null;

        if (next != null && next.TryReserve())
            _reservedEnemy = next;
    }

    void TryShoot()
    {
        float fireRate = GetFinalFireRate();
        _nextFire = Time.time + 1f / Mathf.Max(fireRate, 0.01f);

        RefreshReservedTarget();
        if (_reservedEnemy == null) return;

        int finalDmg = GetFinalDamage();
        Collider targetCol = _reservedEnemy.GetComponent<Collider>() ?? _reservedEnemy.GetComponentInChildren<Collider>();
        FireBullet(targetCol, finalDmg);
    }

    void FireBullet(Collider target, int dmg)
    {
        if (target == null) return;

        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 aimPoint = target.bounds.center;
        Vector3 dir = (aimPoint - pos).normalized;

        GameObject b = null;
        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null) return;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = path.ToString();
            bullet.bulletColor = GetPathColor() * 0.85f;

            int armorPen = weaponConfig != null ? weaponConfig.armorPen : 0;
            int pierceCount = weaponConfig != null ? weaponConfig.pierceCount : 0;

            bullet.SetCombatStats(
                dmg,
                armorPen,
                pierceCount,
                1f,
                1f
            );
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * GetProjectileSpeed();
    }

    public void TakeDamage(int dmg)
    {
        if (_dead) return;

        currentHP -= dmg;

        if (_rend) StartCoroutine(FlashRed());

        if (currentHP <= 0)
            Die();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (!_rend) yield break;

        Color orig = _rend.material.color;
        _rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);

        if (_rend && !_dead)
            _rend.material.color = orig;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        if (_reservedEnemy != null)
        {
            _reservedEnemy.ReleaseReservation();
            _reservedEnemy = null;
        }

        ArmyManager.Instance?.RemoveSoldier(this);
        gameObject.SetActive(false);
    }

    public void HealPercent(float pct)
    {
        currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * pct));
    }

    public Color GetPathColor() => path switch
    {
        SoldierPath.Piyade => new Color(0.2f, 0.85f, 0.2f),
        SoldierPath.Mekanik => new Color(0.65f, 0.65f, 0.65f),
        SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),
        _ => Color.white
    } * (mergeLevel switch { 2 => 1.2f, 3 => 1.5f, 4 => 2.0f, _ => 1.0f });
}