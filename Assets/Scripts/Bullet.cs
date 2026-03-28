using UnityEngine;

/// <summary>
/// Top End War — Mermi v4 (Claude)
///
/// DÜZELTMELER:
///   Çarptıktan sonra anında kaybolur (SetActive false + velocity=0)
///   Lead targeting KALDIRILDI — düz ileri ateş (görsel olarak daha temiz)
///   OverlapSphere radius 0.35 → 0.4 (daha güvenilir hit)
///   Lifetime 2.5s → 1.8s (daha az "havada kalan" mermi görüntüsü)
///
/// Komutan+Asker sistemine hazırlık:
///   SetDamage(int) public — asker mermileri farklı hasar verebilir
/// </summary>
public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);
    [HideInInspector]
    public string hitterPath  = "Commander"; // "Commander","Piyade","Mekanik","Teknoloji" 

    const float HIT_RADIUS = 0.4f;
    const float LIFETIME   = 1.8f;

    Renderer _rend;
    bool     _hit = false;

    void Awake() => _rend = GetComponentInChildren<Renderer>();

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

        Collider[] cols = Physics.OverlapSphere(transform.position, HIT_RADIUS);
        foreach (Collider col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;

            BossHitReceiver bossRecv = col.GetComponent<BossHitReceiver>();
            if (bossRecv != null)
            {
                bossRecv.bossManager?.TakeDamage(damage);
                DamagePopup.Show(col.transform.position, damage,
                    DamagePopup.GetColor(hitterPath), damage > 500);
            }
            else
            {
                col.GetComponent<Enemy>()?.TakeDamage(damage, DamagePopup.GetColor(hitterPath));
            }

            Hit();
            return;
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
        if (!gameObject.activeSelf) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    void ApplyColor()
    {
        if (_rend == null) return;
        if (_rend.material.HasProperty("_BaseColor"))
            _rend.material.SetColor("_BaseColor", bulletColor);
        else
            _rend.material.color = bulletColor;
    }
}