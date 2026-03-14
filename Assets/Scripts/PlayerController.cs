using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v6 (Claude)
///
/// MATEMATIKAL ILERLEME:
///   Tier | Hasar | Atis/sn | DPS
///   1    |  60   |  1.5    |  90
///   2    |  95   |  2.5    | 237
///   3    | 145   |  4.0    | 580
///   4    | 210   |  6.0    |1260
///   5    | 300   |  8.5    |2550
///
/// Tier 2'ye gecince oyuncu hemen fark eder — dusmanlar cok daha hizli oluyor.
///
/// HEDEFLEME: Dusmanin Z hizina gore hafif "lead" (ileriden nisan) alir.
/// Dusmanlar kacarsa mermi geri kalandir degil, gidecekleri yere gider.
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
    static readonly float[] tierFireRates  = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    // Tier bazli hasar
    static readonly int[]   tierDamage     = {  60,   95,  145,  210,  300  };

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        EnsureCollider();
    }

    /// <summary>Gate trigger icin Player'da Collider OLMALI (IsTrigger=false).</summary>
    void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
        col.height    = 2f;
        col.radius    = 0.4f;
        col.isTrigger = false;
        Debug.LogWarning("[Player] CapsuleCollider eklendi! Inspector'dan kaydet.");
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

        Transform target = hit.transform;

        // Lead hedefleme: Dusmanin hizina gore biraz onunu al
        // Mermi hizi 30, mesafe farkina gore gecikme hesapla
        float   dist    = Vector3.Distance(firePoint.position, target.position);
        float   travelT = dist / 30f; // Mermi kac saniyede ulasir
        Vector3 aimPos  = target.position + Vector3.back * (travelT * 4f); // Dusman Z'de -4/sn geliyor
        Vector3 dir     = (aimPos - firePoint.position).normalized;

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

        // Hasari ata
        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null) bullet.SetDamage(damage);

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 30f;
    }
}