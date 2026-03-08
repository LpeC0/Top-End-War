using UnityEngine;

/// <summary>
/// Army Gate Siege – Player Controller
/// Rigidbody YOK. Tamamen transform tabanlı. Unity 6 URP uyumlu.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== HAREKET ===")]
    public float forwardSpeed   = 10f;   // İleri koşu hızı
    public float laneSwitchSpeed = 10f;  // Şerit geçiş yumuşaklığı
    public float laneDistance   = 3.5f; // Şeritler arası mesafe

    [Header("=== ATEŞ ===")]
    public GameObject bulletPrefab;
    public Transform  firePoint;
    public float      fireRate = 3f;    // Saniyede kaç mermi

    // ── Özel değişkenler ──────────────────────────────────────────────────────
    private int   currentLane  = 1;     // 0=Sol  1=Orta  2=Sağ
    private float targetX      = 0f;
    private float nextFireTime = 0f;

    // ── Combat Power (Gelecekte: kapı sistemi, morph, tier ──────────────────
    [HideInInspector] public int cp = 100;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Başlangıç pozisyonunu güvenli yap
        transform.position = new Vector3(0f, 1.2f, 0f);
        targetX = 0f;
        Debug.Log(">>> PlayerController START çalıştı!");
    }

    void Update()
    {
        HandleInput();
        MovePlayer();
        AutoShoot();
    }

    // ── Girdi ────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        // PC: ok tuşları
        if (Input.GetKeyDown(KeyCode.LeftArrow))  ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(+1);

        // Mobil / Mouse: ekranın sol/sağ yarısına tıklama
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x < Screen.width * 0.5f) ChangeLane(-1);
            else                                               ChangeLane(+1);
        }
    }

    void ChangeLane(int dir)
    {
        currentLane = Mathf.Clamp(currentLane + dir, 0, 2);
        targetX = (currentLane - 1) * laneDistance;
    }

    // ── Hareket ──────────────────────────────────────────────────────────────
   void MovePlayer()
{
    // EĞER PLAYER SİLİNİYORSA: Hierarchy'de isminin yanına (Deleted) yazıyor mu bak.
    // Eğer siliniyorsa, sahnedeki hiçbir scriptte "Destroy(other.gameObject)" 
    // veya "Destroy(target)" kodunun Player'ı hedeflemediğinden emin olmalıyız.

    Vector3 p = transform.position;
    
    // Z ilerlemesini sadece oyun aktifken yap (veya sınırlama koyma)
    p.z += forwardSpeed * Time.deltaTime;
    
    p.x  = Mathf.Lerp(p.x, targetX, Time.deltaTime * laneSwitchSpeed);
    p.y  = 1.2f; // Y ekseninde sabit tutuyoruz
    
    transform.position = p;

    // Konsolda takip edelim, karakter gerçekten duruyor mu yoksa sadece görsel mi kayboluyor?
    if(Time.frameCount % 100 == 0) 
        Debug.Log("Sistem Raporu: Player Z = " + transform.position.z);
}

    // ── Otomatik Ateş (Object Pool Güncellemesi - Gemini) ──────────────────
    void AutoShoot()
    {
        if (Time.time < nextFireTime) return;
        if (!bulletPrefab || !firePoint) return;

        RaycastHit hit;
        if (Physics.BoxCast(
                transform.position + Vector3.up,
                new Vector3(laneDistance * 0.4f, 1f, 0.5f),
                Vector3.forward, out hit, Quaternion.identity, 22f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Vector3 dir = (hit.transform.position - firePoint.position).normalized;
                
                // Instantiate yerine havuzdan çekiyoruz! Tag'i "Bullet" olmalı.
                GameObject b = ObjectPooler.Instance.SpawnFromPool("Bullet", firePoint.position, Quaternion.LookRotation(dir));
                
                if (b != null)
                {
                    Rigidbody rb = b.GetComponent<Rigidbody>();
                    if (rb) rb.linearVelocity = dir * 28f;
                    
                    // Otomatik kaybolması için (eski Destroy yerine) özel bir Invoke veya Coroutine yazılabilir,
                    // şimdilik Bullet içindeki bir zamanlayıcı ile Disable yapacağız.
                }
                
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    void SpawnBullet(Transform target)
    {
        Vector3 dir = (target.position - firePoint.position).normalized;
        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = dir * 28f;

        Destroy(b, 3f); // 3 saniyede temizle (pool olmadığı için)
    }
}