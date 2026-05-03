using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v7.2 (Gameplay Fix Patch)
///
/// v7.1 → v7.2 Fix Delta:
///   • FindFrontTarget(): Physics.BoxCast → Physics.BoxCastAll
///     BoxCast sadece ilk çarpışmayı döner; terrain/prop önde varsa
///     enemy hiç hedeflenemiyordu. BoxCastAll tüm hitleri tarar,
///     geçerli ilk enemy/boss seçilir.
///   • IsCombatTarget(): activeInHierarchy kontrolü eklendi.
///     Ölü/deaktif enemy'lerin frame-edge durumlarına karşı savunmacı hat.
/// </summary>
public class Playercontroller : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    [Tooltip("Oyuncunun sabit Y yuksekligi — Inspector'dan degistirilebilir.")]
    public float playerY         = 1.2f;
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;
public AnchorStance GetAnchorStance()
    => AnchorCoverage.StanceFromX(transform.position.x);
    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      detectRange = 35f;

    static readonly float[][] SPREAD =
    {
        new[] {  0f },
        new[] { -8f,  8f },
        new[] { -12f, 0f, 12f },
        new[] { -18f, -6f, 6f, 18f },
        new[] { -22f, -11f, 0f, 11f, 22f },
    };

    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;
    bool  _gameOver   = false;
    static Material _flashMaterial;

    void Start()
    {
        Vector3 p = transform.position;
        p.x = 0f;
        p.y = playerY;
        transform.position = p;

        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
        GameEvents.OnGameOver          += OnGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnAnchorModeChanged -= OnAnchorMode;
        GameEvents.OnGameOver          -= OnGameOver;
    }

    void OnGameOver()
    {
        _gameOver = true;
        _dragging = false;
        _nextFire = float.MaxValue;
        Debug.Log("[PlayerController] Game Over — hareket durduruldu.");
    }

    void OnAnchorMode(bool active)
    {
        _anchorMode  = active;
        forwardSpeed = active ? 0f : 10f;
        if (active) Debug.Log("[Player] Anchor modu aktif.");
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height = 2f;
        c.radius = 0.4f;
        c.isTrigger = false;
    }

    void Update()
    {
        if (_gameOver) return;

        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    void HandleDrag()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            _targetX = Mathf.Clamp(_targetX - 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetKey(KeyCode.RightArrow))
            _targetX = Mathf.Clamp(_targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0))
        {
            _dragging   = true;
            _lastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (_dragging)
        {
            _targetX = Mathf.Clamp(
                _targetX + (Input.mousePosition.x - _lastMouseX) * dragSensitivity,
                -xLimit, xLimit);
            _lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = playerY;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null)
            return;

        PlayerStats.RuntimeCombatSnapshot combat = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        float finalFireRate = combat.FireRate;
        int bCount = combat.ProjectileCount;
        int bulletDamage = combat.BulletDamage;

        Color tracerColor = GetWeaponTracerColor();

        Transform target = FindTarget();
        if (target == null) return;

        if (target.position.z <= transform.position.z + 1f)
            return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = aimPos - firePoint.position;
        if (baseDir.sqrMagnitude <= 0.0001f || baseDir.z <= 0.05f)
            return;

        baseDir.Normalize();

        int   armorPen        = combat.ArmorPen;
        int   pierceCount     = combat.PierceCount;
        float eliteDamageMult = GetCurrentEliteDamageMultiplier();
        float weaponRange     = combat.WeaponRange;
        float projectileSpeed = GetCurrentProjectileSpeed();
        WeaponFamily family   = GetCurrentWeaponFamily();

        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        bool firedAny = false;
        foreach (float angle in SPREAD[spreadIdx])
        {
            float finalAngle = GetWeaponSpreadAngle(angle);
            Vector3 dir = Quaternion.Euler(0f, finalAngle, 0f) * baseDir;
            firedAny |= FireOne(firePoint.position, dir.normalized, bulletDamage, armorPen, pierceCount, eliteDamageMult, tracerColor, weaponRange, projectileSpeed, family);
        }

        if (firedAny)
        {
            SpawnMuzzleFlash(firePoint.position, baseDir, tracerColor);
            _nextFire = Time.time + 1f / Mathf.Max(0.01f, finalFireRate);
        }
    }

    bool FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount, float eliteDamageMult, Color tracerColor, float weaponRange, float projectileSpeed, WeaponFamily family)
    {
        if (dir.sqrMagnitude <= 0.0001f || dir.z <= 0.05f)
            return false;

        dir.Normalize();
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        // FIX: Pool yoksa prefab'dan instantiate et — asla null kalmasın.
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b == null) return false;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = "Commander";
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult);
            bullet.SetMaxRange(weaponRange);
            bullet.SetTrailProfile(family);
            bullet.SetTracerColor(tracerColor);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * projectileSpeed;
        return true;
    }

    Color GetWeaponTracerColor()
    {
        switch (GetCurrentWeaponFamily())
        {
            case WeaponFamily.SMG:
                return new Color(0.30f, 0.95f, 1.00f, 1f);
            case WeaponFamily.Sniper:
                return new Color(1.00f, 0.35f, 0.85f, 1f);
            default:
                return new Color(1.00f, 0.88f, 0.25f, 1f);
        }
    }

    float GetWeaponSpreadAngle(float angle)
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        float bonus = w != null ? w.spreadBonus : 0f;
        float sign = angle < 0f ? -1f : (angle > 0f ? 1f : 0f);
        float signedAngle = angle + sign * bonus;

        if (GetCurrentWeaponFamily() == WeaponFamily.SMG)
            return Mathf.Clamp(signedAngle, -3f, 3f);

        return signedAngle;
    }

    float GetCurrentWeaponRange()
    {
        return PlayerStats.Instance != null ? PlayerStats.Instance.GetRuntimeWeaponRange() : 24f;
    }

    float GetCurrentProjectileSpeed()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        if (w != null && w.weaponArchetype != null)
            return Mathf.Max(1f, w.weaponArchetype.projectileSpeed);

        return 30f;
    }

    void SpawnMuzzleFlash(Vector3 pos, Vector3 dir, Color color)
    {
        var go = new GameObject("MuzzleFlash");
        go.transform.position = pos + dir.normalized * 0.08f;
        go.transform.rotation = Quaternion.LookRotation(dir.normalized);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.5f;
        light.intensity = 1.8f;
        light.color = color;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.18f;
        lr.endWidth = 0.02f;
        lr.material = GetFlashMaterial();
        lr.startColor = new Color(color.r, color.g, color.b, 0.95f);
        lr.endColor = new Color(color.r, color.g, color.b, 0f);
        lr.SetPosition(0, pos);
        lr.SetPosition(1, pos - dir.normalized * 0.7f);

        Destroy(go, 0.06f);
    }

    static Material GetFlashMaterial()
    {
        if (_flashMaterial == null)
            _flashMaterial = new Material(Shader.Find("Sprites/Default"));
        return _flashMaterial;
    }

    int GetCurrentArmorPen()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        return (w != null ? w.armorPen : 0)
             + (PlayerStats.Instance != null ? PlayerStats.Instance.RunArmorPenFlat : 0);
    }

    int GetCurrentPierceCount()
    {
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        return (w != null ? w.pierceCount : 0)
             + (PlayerStats.Instance != null ? PlayerStats.Instance.RunPierceCount : 0);
    }

    float GetCurrentEliteDamageMultiplier()
    {
        EquipmentData w      = PlayerStats.Instance?.equippedWeapon;
        float  equipMult     = w != null ? w.eliteDamageMultiplier : 1f;
        float  gateMult      = PlayerStats.Instance != null
                                ? (1f + PlayerStats.Instance.RunEliteDamagePercent / 100f) : 1f;
        return equipMult * gateMult;
    }

    WeaponFamily GetCurrentWeaponFamily()
    {
        return PlayerStats.Instance != null ? PlayerStats.Instance.GetRuntimeWeaponFamily() : WeaponFamily.Assault;
    }

    Transform FindTarget()
    {
        float weaponRange = GetCurrentWeaponRange();
        switch (GetCurrentWeaponFamily())
        {
            case WeaponFamily.SMG:
                return FindPackTarget(weaponRange);
            case WeaponFamily.Sniper:
                return FindPriorityTarget(weaponRange);
            default:
                return FindFrontTarget(weaponRange);
        }
    }

    // FIX: Eskiden Physics.BoxCast kullanılıyordu — sadece ilk physics hit'i döner.
    // Önde terrain/prop/başka collider varsa enemy hiç seçilemiyordu.
    // Physics.BoxCastAll ile tüm hitler taranır; geçerli en yakın enemy/boss seçilir.
    Transform FindFrontTarget(float weaponRange)
    {
        if (_anchorMode)
            return FindClosestTargetInSphere(weaponRange);

        RaycastHit[] hits = Physics.BoxCastAll(
            transform.position + Vector3.up,
            new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
            Vector3.forward,
            Quaternion.identity,
            weaponRange);

        Transform best    = null;
        float     bestDist = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // FIX: Deaktif/ölü objeler physics'ten çıkar ama savunmacı kontrol.
            if (!IsCombatTarget(hit.collider, out Transform target)) continue;

            bool isEnemy = hit.collider.GetComponent<Enemy>() != null
                        || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null
                        || hit.collider.GetComponentInParent<BossHitReceiver>() != null;

            if (!isEnemy && !isBoss) continue;

            // En yakın geçerli hedefi seç (mesafe bazlı)
            float d = (target.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best     = target;
            }
        }

        return best;
    }

    Transform FindPackTarget(float weaponRange)
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(weaponRange), scoreMode: TargetScoreMode.Cluster);
    }

    Transform FindPriorityTarget(float weaponRange)
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(weaponRange), scoreMode: TargetScoreMode.Priority);
    }

    Transform FindClosestTargetInSphere(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(GetTargetRangeOrigin(), radius);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Collider c in cols)
        {
            if (!IsCombatTarget(c, out Transform target)) continue;
            float d = (target.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = target;
            }
        }

        return best;
    }

    Collider[] DetectEnemyCandidates(float weaponRange)
    {
        return Physics.OverlapSphere(GetTargetRangeOrigin(), weaponRange);
    }

    enum TargetScoreMode
    {
        Cluster,
        Priority,
    }

    Transform FindBestEnemyByScore(Collider[] cols, TargetScoreMode scoreMode)
    {
        Transform best = null;
        float bestScore = float.MinValue;

        foreach (Collider c in cols)
        {
            if (!IsCombatTarget(c, out Transform target, out Enemy enemy, out bool isBoss)) continue;

            float dist = Vector3.Distance(transform.position, target.position);
            float score = 0f;

            if (scoreMode == TargetScoreMode.Cluster)
            {
                int neighborCount = CountNearbyEnemies(target.position, 4f);
                score = neighborCount * 100f - dist * 2f;
                if (enemy != null && enemy.Armor > 0) score += enemy.Armor * 1.5f;
            }
            else
            {
                score = isBoss ? 5000f : 0f;
                if (enemy != null)
                {
                    score += enemy.IsElite ? 1500f : 0f;
                    score += enemy.Armor * 40f;
                    score += Mathf.Clamp(10f - dist, -10f, 10f) * 15f;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        // Fallback: en yakın geçerli hedef
        return best ?? FindClosestTargetInSphere(GetCurrentWeaponRange());
    }

    int CountNearbyEnemies(Vector3 center, float radius)
    {
        int count = 0;
        Collider[] cols = Physics.OverlapSphere(center, radius);
        foreach (Collider c in cols)
            if (IsCombatTarget(c, out _))
                count++;
        return count;
    }

    bool IsCombatTarget(Collider col, out Transform target)
    {
        return IsCombatTarget(col, out target, out _, out _);
    }

    // FIX: activeInHierarchy kontrolü eklendi.
    // SetActive(false) physics'ten kaldırır ama aynı frame içinde edge case olabilir.
    bool IsCombatTarget(Collider col, out Transform target, out Enemy enemy, out bool isBoss)
    {
        target = null;
        isBoss = false;
        enemy  = null;

        // FIX: Deaktif obje hedeflenemez.
        if (col == null || !col.gameObject.activeInHierarchy) return false;

        enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
        BossHitReceiver boss = col.GetComponent<BossHitReceiver>() ?? col.GetComponentInParent<BossHitReceiver>();
        isBoss = boss != null;

        if (boss != null)
        {
            target = boss.transform;
            return IsTargetInWeaponWindow(target);
        }

        if (enemy != null)
        {
            if (!enemy.IsAlive) return false;
            target = enemy.transform;
            return IsTargetInWeaponWindow(target);
        }

        return false;
    }

    bool IsTargetInWeaponWindow(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = GetTargetRangeOrigin();
        Vector3 delta = target.position - origin;
        // Anchor modda düşman player'a DOĞRU geliyor (Z azalıyor) — yön filtresi olmamalı.
if (!_anchorMode && delta.z <= 0.5f) return false;

        float range = GetCurrentWeaponRange();
        return delta.sqrMagnitude <= range * range;
    }

    Vector3 GetTargetRangeOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    public void ResumeRun()
    {
        _gameOver    = false;
        _anchorMode  = false;
        _dragging    = false;
        _targetX     = 0f;
        _nextFire    = 0f;
        forwardSpeed = 10f;
    }

    public void ResetForStage(float startZ = 0f)
    {
        ResumeRun();

        Vector3 p = transform.position;
        p.x = 0f;
        p.y = playerY;
        p.z = startZ;
        transform.position = p;
    }
}
