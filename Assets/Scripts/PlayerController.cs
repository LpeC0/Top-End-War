using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v9 (Runtime Stabilite Patch)
///
/// v8 → v9 Delta:
///   • playerY alani eklendi: Y yüksekligini Inspector'dan ayarlayabilirsin,
///     MovePlayer() artık hardcode 1.2f kullanmiyor.
///   • Start(): Z sifirlanmiyor — sahne pozisyonu korunur, sadece Y ayarlanir.
///   • _gameOver flag + OnGameOver subscribe: Update tamamen bloklanir.
///   • OnAnchorMode: BossManager null veya aktif degil ise forwardSpeed sifirlanmaz.
///     (BossManager sahnede olmadan anchor event geldigi zaman 1200 civarinda tikanma yasaniyordu)
///   • ResumeRun(): _gameOver sifirlanir (Revive icin).
/// </summary>
public class Playercontroller : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    [Tooltip("Oyuncunun sabit Y yuksekligi — artik Inspector'dan degistirilebilir.")]
    public float playerY         = 0.1f;
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 6.8f;

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

    // DEĞİŞİKLİK: Anchor yanlış/erken açılırsa oyuncu sonsuza kadar kilitlenmesin.
    float _baseForwardSpeed;
    float _anchorStartTime = -99f;
    public float anchorFailSafeDelay = 1.0f;
    public float anchorBossDetectRadius = 90f;

    void Start()
    {
        _baseForwardSpeed = forwardSpeed;

        // PATCH: sadece Y'yi ayarla; X=0 (merkez serit), Z sahne pozisyonundan kalsin.
        Vector3 p = transform.position;
        p.x = 0f;
        p.y = playerY;
        transform.position = p;

        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
        GameEvents.OnGameOver          += OnGameOver;     // PATCH
    }

    void OnDestroy()
    {
        GameEvents.OnAnchorModeChanged -= OnAnchorMode;
        GameEvents.OnGameOver          -= OnGameOver;     // PATCH
    }

    // PATCH: game over — Update komple bloklanir.
    void OnGameOver()
    {
        _gameOver = true;
        _dragging = false;
        _nextFire = float.MaxValue;
        Debug.Log("[PlayerController] Game Over — hareket durduruldu.");
    }

    void OnAnchorMode(bool active)
{
    if (active)
    {
        bool bossReady =
            BossManager.Instance != null &&
            BossManager.Instance.IsActive() &&
            FindObjectOfType<BossHitReceiver>() != null;

        if (!bossReady)
        {
            Debug.LogWarning("[Player] Anchor geldi ama boss sahada hazir degil. Ignore edildi.");
            return;
        }
    }

    _anchorMode = active;
    forwardSpeed = active ? 0f : 10f;

    if (active)
        Debug.Log("[Player] Anchor modu aktif.");
}

    // DEĞİŞİKLİK: BossManager active olsa bile sahada gerçek boss receiver yoksa anchor kilidi koyma.
    bool BossFightActuallyReady()
    {
        if (PlayerStats.Instance == null) return false;

        foreach (Collider c in Physics.OverlapSphere(PlayerStats.Instance.transform.position, anchorBossDetectRadius))
        {
            if (c.GetComponent<BossHitReceiver>() != null || c.GetComponentInParent<BossHitReceiver>() != null)
                return true;
        }

        return false;
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height    = 2f;
        c.radius    = 0.4f;
        c.isTrigger = false;
    }

    void Update()
    {
        if (_gameOver) return;   // PATCH

        // DEĞİŞİKLİK: Anchor açıldı ama boss sahada değilse kısa süre sonra kendini aç.
        if (_anchorMode && forwardSpeed <= 0f)
        {
            if (!BossFightActuallyReady() && Time.time - _anchorStartTime >= anchorFailSafeDelay)
            {
                _anchorMode = false;
                forwardSpeed = _baseForwardSpeed;
                Debug.LogWarning("[Player] Anchor failsafe devreye girdi — hareket tekrar acildi.");
            }
        }

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
        p.y  = playerY;    // PATCH: hardcode 1.2f yerine field
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

        int bCount       = PlayerStats.Instance.BulletCount;
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * bCount)));

        float bossDamageMult = GetCurrentBossDamageMultiplier();

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
            FireOne(firePoint.position, dir.normalized, bulletDamage,
                    armorPen, pierceCount, eliteDamageMult, bossDamageMult);
        }

        _nextFire = Time.time + 1f / finalFireRate;
    }

    void FireOne(Vector3 pos, Vector3 dir, int dmg, int armorPen, int pierceCount,
                 float eliteDamageMult, float bossDamageMult)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
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
            bullet.SetCombatStats(dmg, armorPen, pierceCount, eliteDamageMult, bossDamageMult);
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 50f;
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
        EquipmentData w       = PlayerStats.Instance?.equippedWeapon;
        float         equipM  = w != null ? w.eliteDamageMultiplier : 1f;
        float         gateM   = PlayerStats.Instance != null
                                 ? 1f + PlayerStats.Instance.RunEliteDamagePercent / 100f : 1f;
        return equipM * gateM;
    }

    float GetCurrentBossDamageMultiplier()
    {
        return PlayerStats.Instance != null
            ? 1f + PlayerStats.Instance.RunBossDamagePercent / 100f : 1f;
    }

    Transform FindTarget()
    {
        if (_anchorMode)
        {
            float     bestDist = 70f * 70f;
            Transform best     = null;
            foreach (Collider c in Physics.OverlapSphere(transform.position, 70f))
            {
                bool isEnemy = c.GetComponent<Enemy>() != null || c.GetComponentInParent<Enemy>() != null;
                bool isBoss  = c.GetComponent<BossHitReceiver>() != null || c.GetComponentInParent<BossHitReceiver>() != null;
                if (!isEnemy && !isBoss) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c.transform; }
            }
            return best;
        }
        else
        {
            bool found = Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 0.2f, 0.5f),
                Vector3.forward, out RaycastHit hit,
                Quaternion.identity, detectRange);
            if (!found) return null;
            bool isEnemy = hit.collider.GetComponent<Enemy>() != null || hit.collider.GetComponentInParent<Enemy>() != null;
            bool isBoss  = hit.collider.GetComponent<BossHitReceiver>() != null || hit.collider.GetComponentInParent<BossHitReceiver>() != null;
            return (isEnemy || isBoss) ? hit.transform : null;
        }
    }

    public void ResumeRun()
    {
        _gameOver = false;
        _anchorMode = false;
        _anchorStartTime = -99f;
        forwardSpeed = _baseForwardSpeed;
    }
}