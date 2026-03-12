using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi v4 (Claude)
/// Serbest surukleme. xLimit=8 ile genis harita siniri.
/// Tier'a gore 1-5 mermi.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Ileri Hareket")]
    public float forwardSpeed = 10f;

    [Header("Yatay Hareket")]
    public float dragSensitivity = 0.05f;
    public float smoothing       = 14f;
    public float xLimit          = 8f;   // RoadChunk genisligi ile uyumlu

    [Header("Ates")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      fireRate    = 2.5f;
    public float      detectRange = 30f;

    float targetX      = 0f;
    float nextFireTime = 0f;
    bool  isDragging   = false;
    float lastMouseX;

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
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
        if (Time.time < nextFireTime || !firePoint) return;

        int tier        = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        int bulletCount = tier;
        float spread    = 1.2f;

        RaycastHit hit;
        if (!Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(xLimit * 0.5f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, detectRange)
            || !hit.collider.CompareTag("Enemy")) return;

        for (int i = 0; i < bulletCount; i++)
        {
            float   offsetX  = (i - (bulletCount - 1) * 0.5f) * spread;
            Vector3 spawnPos = firePoint.position + new Vector3(offsetX, 0f, 0f);
            Vector3 dir      = (hit.transform.position - spawnPos).normalized;
            FireBullet(spawnPos, dir);
        }

        nextFireTime = Time.time + 1f / fireRate;
    }

    void FireBullet(Vector3 pos, Vector3 dir)
    {
        GameObject b = ObjectPooler.Instance?.SpawnFromPool("Bullet", pos, Quaternion.LookRotation(dir));
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