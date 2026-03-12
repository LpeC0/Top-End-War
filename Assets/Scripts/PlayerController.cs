using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v4 (Claude)
/// Serbest sürükleme. xLimit ile harita sınırı. Tier'a göre çoklu mermi.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Ileri Hareket")]
    public float forwardSpeed = 10f;

    [Header("Yatay Hareket")]
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 5.5f;   // Harita sınırı — SpawnManager.roadHalfWidth ile ESİT olmalı

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;        // ObjectPooler yoksa fallback
    public float      fireRate    = 2.5f;
    public float      detectRange = 30f;

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
        targetX = 0f;
    }

    void Update()
    {
        HandleDrag();
        MovePlayer();
        AutoShoot();
    }

    // ── Serbest Surukleme ─────────────────────────────────────────────────
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

    // ── Hareket + Sinir ───────────────────────────────────────────────────
    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * smoothing);
        p.x  = Mathf.Clamp(p.x, -xLimit, xLimit); // Hard sınır
        p.y  = 1.2f;
        transform.position = p;
    }

    // ── Tier Bazli Coklu Mermi ────────────────────────────────────────────
    void AutoShoot()
    {
        if (Time.time < nextFireTime || !firePoint) return;

        // Tier'a gore mermi sayisi: Tier1=1, Tier2=2, Tier3=3, Tier4=4, Tier5=5
        int tier        = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int bulletCount = tier;
        float spread    = 1.2f; // Mermiler arasi yatay offset

        bool fired = false;

        // Önce hedef var mi diye kontrol et
        RaycastHit hit;
        if (Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.6f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, detectRange)
            && hit.collider.CompareTag("Enemy"))
        {
            // Mermi sayisi kadar farkli offsette at
            for (int i = 0; i < bulletCount; i++)
            {
                float offsetX = (i - (bulletCount - 1) * 0.5f) * spread;
                Vector3 spawnPos = firePoint.position + new Vector3(offsetX, 0f, 0f);
                Vector3 dir      = (hit.transform.position + new Vector3(offsetX * 0.3f, 0f, 0f)
                                   - spawnPos).normalized;

                FireBullet(spawnPos, dir);
                fired = true;
            }
        }

        if (fired) nextFireTime = Time.time + 1f / fireRate;
    }

    void FireBullet(Vector3 pos, Vector3 dir)
    {
        GameObject b = null;

        if (ObjectPooler.Instance != null)
            b = ObjectPooler.Instance.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));

        if (b == null && bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
            Destroy(b, 3f);
        }

        if (b != null)
        {
            Rigidbody rb = b.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = dir * 28f;
        }
    }
}