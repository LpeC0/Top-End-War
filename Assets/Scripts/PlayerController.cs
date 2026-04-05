using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v5 (Claude)
///
/// v5 degisiklikleri:
///   + AutoShoot: bulletDamage = GetTotalDPS() / (GetBaseFireRate() * BulletCount)
///   + DAMAGE[] ve BASE_FIRE_RATES[] dizileri KALDIRILDI — PlayerStats'ten gelir
///   + staticFire degiskeni kaldirildi
///   Onceki mantik (v4) aynen korundu: FindTarget, drag, spread, anchor.
///
/// HASAR FORMULU (Degismez Kural):
///   TotalDPS = BaseDMG[tier] * WeaponMult * SlotMult * RarityMult * GlobalMult
///   BulletDamage = TotalDPS / (FireRate * BulletCount)
///
///   NEDEN BulletCount boluyor:
///   5 mermi ayni hasar verirse toplam hasar 5x DPS olur.
///   Spread = daha genis alan, toplam hasar degil.
///
/// Spread formation (V sekli):
///   1 mermi: duz, 2: +-8, 3: -12 0 +12, 4: -18 -6 +6 +18, 5: -22 -11 0 +11 +22
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

    // ── Spread Tablosu ────────────────────────────────────────────────────
    static readonly float[][] SPREAD =
    {
        new[] {  0f },
        new[] { -8f,  8f },
        new[] { -12f, 0f, 12f },
        new[] { -18f, -6f, 6f, 18f },
        new[] { -22f, -11f, 0f, 11f, 22f },
    };

    // ── Dahili Durum ──────────────────────────────────────────────────────
    float _targetX    = 0f;
    float _nextFire   = 0f;
    bool  _dragging   = false;
    float _lastMouseX;
    bool  _anchorMode = false;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;
    }

    void OnDestroy() => GameEvents.OnAnchorModeChanged -= OnAnchorMode;

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
        c.height = 2f; c.radius = 0.4f; c.isTrigger = false;
    }

    void Update()
    {
        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    // ── Surukle / Hareket ─────────────────────────────────────────────────
    void HandleDrag()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            _targetX = Mathf.Clamp(_targetX - 10f * Time.deltaTime, -xLimit, xLimit);
        if (Input.GetKey(KeyCode.RightArrow))
            _targetX = Mathf.Clamp(_targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0)) { _dragging = true; _lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))    _dragging = false;

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

    // ── Otomatik Ates ─────────────────────────────────────────────────────
    void AutoShoot()
    {
        if (!firePoint || Time.time < _nextFire || PlayerStats.Instance == null) return;

        // ── Atis Hizi ────────────────────────────────────────────────────
        float finalFireRate = PlayerStats.Instance.GetBaseFireRate();

        // ── Hasar Hesabi (v5 formulu) ────────────────────────────────────
        // TotalDPS PlayerStats tarafindan hesaplandi:
        //   BaseDMG[tier] * WeaponMult * SlotMult * RarityMult * GlobalMult
        // BulletDamage = DPS / (FireRate * BulletCount)
        // BulletCount icin boluyoruz: 5 mermi = genis alan, toplam hasar x5 olmaz.
        int bCount      = PlayerStats.Instance.BulletCount;
        float totalDPS  = PlayerStats.Instance.GetTotalDPS();
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDPS / (finalFireRate * bCount)));

        // ── Hedef Bul ────────────────────────────────────────────────────
        Transform target = FindTarget();
        if (target == null) return;

        Vector3 aimPos  = target.position;
        Vector3 baseDir = (aimPos - firePoint.position).normalized;

        // ── Spread ile Ates ──────────────────────────────────────────────
        int spreadIdx = Mathf.Clamp(bCount - 1, 0, SPREAD.Length - 1);
        foreach (float angle in SPREAD[spreadIdx])
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            FireOne(firePoint.position, dir.normalized, bulletDamage);
        }

        _nextFire = Time.time + 1f / finalFireRate;
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

    // ── Hedef Bulma ───────────────────────────────────────────────────────
    /// <summary>
    /// Normal modda BoxCast (serit tarama).
    /// Anchor modda OverlapSphere 70 birim (boss kesin yakalanir).
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