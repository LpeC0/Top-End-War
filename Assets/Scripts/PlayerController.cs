using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi (Claude)
/// Serbest surukleme. xLimit=8.
/// Tier = atis HIZI (spread degil). Hasar tablosu Bullet.SetDamage() ile gider.
///
/// ATIS MATEMATIGI:
///   Tier1: 60 hasar, 1.5/sn  = 90 DPS
///   Tier2: 95 hasar, 2.5/sn  = 237 DPS
///   Tier3: 145 hasar, 4.0/sn = 580 DPS
///   Tier4: 210 hasar, 6.0/sn = 1260 DPS
///   Tier5: 300 hasar, 8.5/sn = 2550 DPS
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Ileri Hareket")]
    public float forwardSpeed = 10f;

    [Header("Yatay Hareket")]
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      detectRange = 35f;

    // Tier bazli atis hizi (atis/saniye)
    static readonly float[] tierFireRates = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    // Tier bazli mermi hasari
    static readonly int[]   tierDamage    = { 60,   95,   145,  210,  300  };

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
    }

    // Gate trigger icin Player'da Collider olmali (IsTrigger = false)
    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
        col.height    = 2f;
        col.radius    = 0.4f;
        col.isTrigger = false;
        Debug.LogWarning("[Player] CapsuleCollider eklendi. Prefab'a kaydet.");
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
            targetX = Mathf.Clamp(targetX - 10f * Time.deltaTime, -xLimit, xLimit);
        if (Input.GetKey(KeyCode.RightArrow))
            targetX = Mathf.Clamp(targetX + 10f * Time.deltaTime, -xLimit, xLimit);

        if (Input.GetMouseButtonDown(0)) { isDragging = true;  lastMouseX = Input.mousePosition.x; }
        if (Input.GetMouseButtonUp(0))   { isDragging = false; }

        if (isDragging)
        {
            float delta = (Input.mousePosition.x - lastMouseX) * dragSensitivity;
            targetX    = Mathf.Clamp(targetX + delta, -xLimit, xLimit);
            lastMouseX = Input.mousePosition.x;
        }
    }

    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.y  = 1.2f;
        transform.position = p;
    }

    void AutoShoot()
    {
        if (!firePoint) return;

        int   tier     = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int   idx      = Mathf.Clamp(tier - 1, 0, 4);
        float fireRate = tierFireRates[idx];
        int   damage   = tierDamage[idx];

        if (Time.time < nextFireTime) return;

        // Hedef bul
        RaycastHit hit;
        if (!Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.55f, 1.2f, 0.5f),
                Vector3.forward, out hit,
                Quaternion.identity, detectRange)
            || !hit.collider.CompareTag("Enemy")) return;

        // Lead hedefleme: dusman hareket yonunu tahmin et
        float   dist   = Vector3.Distance(firePoint.position, hit.transform.position);
        float   travelT= dist / 30f;
        Vector3 aimPos = hit.transform.position + Vector3.back * (travelT * 4f);
        Vector3 dir    = (aimPos - firePoint.position).normalized;

        FireBullet(firePoint.position, dir, damage);
        nextFireTime = Time.time + 1f / fireRate;
    }

    void FireBullet(Vector3 pos, Vector3 dir, int damage)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }
        if (b == null) return;

        b.GetComponent<Bullet>()?.SetDamage(damage);

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }
}
