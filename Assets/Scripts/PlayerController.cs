using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Hareketi
/// Rigidbody YOK. Drag (sürükleme) ile şerit değiştirme.
/// Parmak/fare basılı tutulup sürüklenince şerit değişir — anlık, doğal his.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public float forwardSpeed    = 10f;
    public float laneSwitchSpeed = 10f;
    public float laneDistance    = 3.5f;

    [Header("Ateş")]
    public GameObject bulletPrefab;
    public Transform  firePoint;
    public float      fireRate   = 3f;

    [Header("Drag Ayarı")]
    public float dragThreshold = 40f; // Kaç piksel sürüklenince şerit değişsin

    int   currentLane  = 1;
    float targetX      = 0f;
    float nextFireTime = 0f;

    // ── Drag takibi ──────────────────────────────────────────────────────
    bool    isDragging     = false;
    Vector2 dragStartPos;
    Vector2 lastDragPos;
    float   accumulatedDrag = 0f; // Biriken sürükleme miktarı

    void Start()
    {
        transform.position = new Vector3(0f, 1.2f, 0f);
    }

    void Update()
    {
        HandleDragInput();
        MovePlayer();
        AutoShoot();
    }

    // ── Sürükle ile şerit değiştir ───────────────────────────────────────
    void HandleDragInput()
    {
        // PC klavye (test)
        if (Input.GetKeyDown(KeyCode.LeftArrow))  ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(+1);

        // Dokunma / Mouse basıldı
        if (Input.GetMouseButtonDown(0))
        {
            isDragging      = true;
            dragStartPos    = Input.mousePosition;
            lastDragPos     = Input.mousePosition;
            accumulatedDrag = 0f;
        }

        // Basılı tutulurken sürükle
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentPos = Input.mousePosition;
            float   deltaX     = currentPos.x - lastDragPos.x;
            accumulatedDrag   += deltaX;
            lastDragPos        = currentPos;

            // Eşik aşılınca şerit değiştir, sayacı sıfırla
            if (accumulatedDrag > dragThreshold)
            {
                ChangeLane(+1);
                accumulatedDrag = 0f;
            }
            else if (accumulatedDrag < -dragThreshold)
            {
                ChangeLane(-1);
                accumulatedDrag = 0f;
            }
        }

        // Bırakıldı
        if (Input.GetMouseButtonUp(0))
        {
            isDragging      = false;
            accumulatedDrag = 0f;
        }
    }

    void ChangeLane(int dir)
    {
        currentLane = Mathf.Clamp(currentLane + dir, 0, 2);
        targetX     = (currentLane - 1) * laneDistance;
    }

    // ── İleri hareket ────────────────────────────────────────────────────
    void MovePlayer()
    {
        Vector3 p = transform.position;
        p.z += forwardSpeed * Time.deltaTime;
        p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * laneSwitchSpeed);
        p.y  = 1.2f;
        transform.position = p;
    }

    // ── Otomatik ateş ────────────────────────────────────────────────────
    void AutoShoot()
    {
        if (Time.time < nextFireTime) return;
        if (!firePoint) return;

        RaycastHit hit;
        if (Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(laneDistance * 0.4f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, 22f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Vector3 dir = (hit.transform.position - firePoint.position).normalized;

                // ObjectPooler varsa kullan, yoksa Instantiate
                GameObject b;
                if (ObjectPooler.Instance != null)
                    b = ObjectPooler.Instance.SpawnFromPool("Bullet", firePoint.position, Quaternion.LookRotation(dir));
                else
                {
                    b = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
                    Destroy(b, 3f);
                }

                if (b != null)
                {
                    Rigidbody rb = b.GetComponent<Rigidbody>();
                    if (rb) rb.linearVelocity = dir * 28f;
                }

                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }
}