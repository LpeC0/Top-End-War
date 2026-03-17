using UnityEngine;

/// <summary>
/// Top End War — Mermi v3 (Claude)
///
/// SORUN (önceki): Bullet isTrigger=true + Enemy isTrigger=true
///   = Unity'de iki trigger birbiriyle çarpışmaz → hasar yok!
///
/// ÇÖZÜM: OnTriggerEnter yerine Update() Physics.OverlapSphere
///   Hem Enemy (trigger) hem Boss (non-trigger) için çalışır.
///   Performans: Max 5 mermi × her frame 1 OverlapSphere = ihmal edilebilir.
///
/// Prefab kurulumu:
///   SphereCollider(radius=0.25, isTrigger=TRUE kalabilir — sadece görsel)
///   Rigidbody(Gravity=false, Interpolate=Interpolate)
/// </summary>
public class Bullet : MonoBehaviour
{
    public int   damage      = 60;
    public Color bulletColor = new Color(0.55f, 0f, 1f); // Mor

    const float HIT_RADIUS   = 0.35f;  // Çarpışma yarıçapı
    const float LIFETIME     = 2.5f;

    Renderer _renderer;
    bool     _hit = false;             // Çift hasar önle

    void Awake() => _renderer = GetComponentInChildren<Renderer>();

    void OnEnable()
    {
        _hit = false;
        ApplyColor();
        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
    }

    public void SetDamage(int d) => damage = d;

    void Update()
    {
        if (_hit) return;

        // Her frame etrafındaki collider'ları tara
        Collider[] hits = Physics.OverlapSphere(transform.position, HIT_RADIUS);
        foreach (Collider col in hits)
        {
            // Enemy vur
            if (col.CompareTag("Enemy"))
            {
                // Boss mu normal enemy mi?
                BossHitReceiver boss = col.GetComponent<BossHitReceiver>();
                if (boss != null)
                    boss.bossManager?.TakeDamage(damage);
                else
                    col.GetComponent<Enemy>()?.TakeDamage(damage);

                Hit();
                return;
            }
        }
    }

    void Hit()
    {
        if (_hit) return;
        _hit = true;
        ReturnToPool();
    }

    void ReturnToPool()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    void ApplyColor()
    {
        if (_renderer == null) return;
        if (_renderer.material.HasProperty("_BaseColor"))
            _renderer.material.SetColor("_BaseColor", bulletColor);
        else
            _renderer.material.color = bulletColor;
    }
}