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

        float finalFireRate = PlayerStats.Instance.GetBaseFireRate()
                            * (1f + PlayerStats.Instance.RunFireRatePercent / 100f);

        float totalDPS = PlayerStats.Instance.GetTotalDPS()
                       * (1f + PlayerStats.Instance.RunWeaponPowerPercent / 100f);

        int bCount = PlayerStats.Instance.BulletCount;

        // FIX: DPS asker sayisina degil, mermi sayisina ve fire rate'e bolunur.
        // Her mermi esit pay alir; toplam DPS korunur.
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * Mathf.Max(1, bCount))));

        Color tracerColor = GetWeaponTracerColor();

        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        int   armorPen        = GetCurrentArmorPen();
        int   pierceCount     = GetCurrentPierceCount();
        float eliteDamageMult = GetCurrentEliteDamageMultiplier();

        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, bulletDamage, armorPen, pierceCount, eliteDamageMult, tracerColor);
        }

        SpawnMuzzleFlash(firePoint.position, baseDir, tracerColor);
        _nextFire = Time.time + 1f / Mathf.Max(0.01f, finalFireRate);
    }

    void FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount, float eliteDamageMult, Color tracerColor)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        // FIX: Pool yoksa prefab'dan instantiate et — asla null kalmasın.
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b == null) return;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.hitterPath = "Commander";
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult);
            bullet.SetTracerColor(tracerColor);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
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
        EquipmentData w = PlayerStats.Instance?.equippedWeapon;
        return w != null && w.weaponArchetype != null
            ? w.weaponArchetype.family
            : WeaponFamily.Assault;
    }

    Transform FindTarget()
    {
        switch (GetCurrentWeaponFamily())
        {
            case WeaponFamily.SMG:
                return FindPackTarget();
            case WeaponFamily.Sniper:
                return FindPriorityTarget();
            default:
                return FindFrontTarget();
        }
    }

    // FIX: Eskiden Physics.BoxCast kullanılıyordu — sadece ilk physics hit'i döner.
    // Önde terrain/prop/başka collider varsa enemy hiç seçilemiyordu.
    // Physics.BoxCastAll ile tüm hitler taranır; geçerli en yakın enemy/boss seçilir.
    Transform FindFrontTarget()
    {
        if (_anchorMode)
            return FindClosestTargetInSphere(70f);

        RaycastHit[] hits = Physics.BoxCastAll(
            transform.position + Vector3.up,
            new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
            Vector3.forward,
            Quaternion.identity,
            detectRange);

        Transform best    = null;
        float     bestDist = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // FIX: Deaktif/ölü objeler physics'ten çıkar ama savunmacı kontrol.
            if (!hit.collider.gameObject.activeInHierarchy) continue;

            bool isEnemy = hit.collider.GetComponent<Enemy>() != null
                        || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null
                        || hit.collider.GetComponentInParent<BossHitReceiver>() != null;

            if (!isEnemy && !isBoss) continue;

            // En yakın geçerli hedefi seç (mesafe bazlı)
            float d = (hit.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best     = hit.transform;
            }
        }

        return best;
    }

    Transform FindPackTarget()
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(), scoreMode: TargetScoreMode.Cluster);
    }

    Transform FindPriorityTarget()
    {
        return FindBestEnemyByScore(DetectEnemyCandidates(), scoreMode: TargetScoreMode.Priority);
    }

    Transform FindClosestTargetInSphere(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
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

    Collider[] DetectEnemyCandidates()
    {
        return Physics.OverlapSphere(transform.position, detectRange);
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
        return best ?? FindClosestTargetInSphere(detectRange);
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
            return true;
        }

        if (enemy != null)
        {
            target = enemy.transform;
            return true;
        }

        return false;
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