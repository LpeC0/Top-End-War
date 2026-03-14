using UnityEngine;

/// <summary>
/// Top End War — Mermi v3 (Claude)
/// Hasar tier'a gore artis:
///   Tier1: 60  | Tier2: 95  | Tier3: 145 | Tier4: 210 | Tier5: 300
///
/// Atis hizi (PlayerController'da):
///   Tier1:1.5/s | Tier2:2.5/s | Tier3:4.0/s | Tier4:6.0/s | Tier5:8.5/s
///
/// DPS tablosu:
///   Tier1: 90 DPS  → 120 HP dusmani 1.3sn
///   Tier2: 237 DPS → 120 HP dusmani 0.5sn
///   Tier3: 580 DPS → cok hizli
///   Tier4-5: neredeyse aninda
/// </summary>
public class Bullet : MonoBehaviour
{
    // Hasar dogrudan SetDamage() ile atanir — PlayerController cagırır
    public int damage = 60;

    public Color bulletColor = new Color(0.55f, 0f, 1f, 1f); // Mor

    Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        if (_renderer != null)
        {
            // URP: _BaseColor kullan, mat.color degil
            if (_renderer.material.HasProperty("_BaseColor"))
                _renderer.material.SetColor("_BaseColor", bulletColor);
            else
                _renderer.material.color = bulletColor;
        }
        Invoke(nameof(ReturnToPool), 2.5f);
    }

    void OnDisable() { CancelInvoke(); }

    /// <summary>PlayerController tarafindan spawn oncesi cagrilir.</summary>
    public void SetDamage(int d) { damage = d; }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        other.GetComponent<Enemy>()?.TakeDamage(damage);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }
}