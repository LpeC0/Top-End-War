using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v4 (Claude)
///
/// v4: EquipmentData.fireRateMultiplier ateş hızına uygulanıyor.
///     Komutan kuşandığı silahın hızını otomatik alıyor.
///
/// Anchor Modu: OnAnchorModeChanged(true) gelince forwardSpeed=0,
///   oyuncu sadece X ekseninde hareket eder, boss ile savaşır.
///
/// Spread formation (V şekli):
///   1 mermi: düz, 2: ±8°, 3: -12° 0° +12°,
///   4: -18° -6° +6° +18°, 5: -22° -11° 0° +11° +22°
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      detectRange = 35f;

    // Tier bazlı temel değerler
    static readonly float[] BASE_FIRE_RATES = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    static readonly int[]   DAMAGE          = { 60,   95,   145,  210,  300  };

    static readonly float[][] SPREAD = {
        new float[]{ 0f },
        new float[]{ -8f, 8f },
        new float[]{ -12f, 0f, 12f },
        new float[]{ -18f, -6f, 6f, 18f },
        new float[]{ -22f, -11f, 0f, 11f, 22f },
    };

    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
    }

    void OnDestroy() => GameEvents.OnAnchorModeChanged -= OnAnchorMode;

    void OnAnchorMode(bool active)
    {
        _anchorMode = active;
        forwardSpeed = active ? 0f : 10f;
        if (active) Debug.Log("[Player] Anchor modu aktif.");
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var c = gameObject.AddComponent<CapsuleCollider>();
        c.height = 2f; c.radius = 0.4f; c.isTrigger = false;
    }

    void Update()
    {
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

        if (Input.GetMouseButtonDown(0)) { _dragging = true; _lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))   _dragging = false;

        if (_dragging)
        {
            _targetX    = Mathf.Clamp(_targetX + (Input.mousePosition.x - _lastMouseX) * dragSensitivity, -xLimit, xLimit);
            _lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, _targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire) return;

        int tier   = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int bCount = PlayerStats.Instance != null ? PlayerStats.Instance.BulletCount  : 1;
        int idx    = Mathf.Clamp(tier - 1, 0, 4);

        // ── EquipmentData ateş hızı çarpanı ──────────────────────────────
        // Kuşanılan silah BASE_FIRE_RATES'i çarpar — silah yoksa 1x.
        float equipMult = 1f;
        if (PlayerStats.Instance?.equippedWeapon != null)
            equipMult = PlayerStats.Instance.equippedWeapon.fireRateMultiplier;

        float fireRate = BASE_FIRE_RATES[idx] * equipMult;

        // Silah hasar çarpanı
        float dmgMult = 1f;
        if (PlayerStats.Instance?.equippedWeapon != null)
            dmgMult = PlayerStats.Instance.equippedWeapon.damageMultiplier;
        int finalDamage = Mathf.RoundToInt(DAMAGE[idx] * dmgMult);

        // Hedef bul
        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        // Spread
        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, finalDamage);
        }

        _nextFire = Time.time + 1f / fireRate;
    }

    void FireOne(Vector3 pos, Vector3 dir, int dmg)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }
        if (b == null) return;

        b.GetComponent<Bullet>()?.SetDamage(dmg);
        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }

    /// <summary>
    /// En yakın Enemy'i bulur.
    /// Normal modda BoxCast (serit tarama), Anchor modda OverlapSphere (360 — boss kesin bulunur).
    /// </summary>
    Transform FindTarget()
    {
        if (_anchorMode)
        {
            float    bestDist = 70f * 70f;
            Collider best     = null;
            foreach (Collider c in Physics.OverlapSphere(transform.position, 70f))
            {
                if (!c.CompareTag("Enemy")) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best?.transform;
        }
        else
        {
            RaycastHit hit;
            bool found = Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 1.2f, 0.5f),
                Vector3.forward, out hit,
                Quaternion.identity, detectRange);
            return (found && hit.collider.CompareTag("Enemy")) ? hit.transform : null;
        }
    }

    public void ResumeRun() => OnAnchorMode(false);
}