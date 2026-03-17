using UnityEngine;

/// <summary>
/// Top End War — Mermi (Claude)
/// SphereCollider(IsTrigger=true) + Rigidbody(Gravity=false)
/// ObjectPooler ile SetActive — Destroy degil.
/// </summary>
public class Bullet : MonoBehaviour
{
    public int   damage      = 60;
    public Color bulletColor = new Color(0.55f, 0f, 1f); // Mor

    Renderer _renderer;

    void Awake() { _renderer = GetComponentInChildren<Renderer>(); }

    void OnEnable()
    {
        if (_renderer != null)
        {
            if (_renderer.material.HasProperty("_BaseColor"))
                _renderer.material.SetColor("_BaseColor", bulletColor);
            else
                _renderer.material.color = bulletColor;
        }
        Invoke(nameof(ReturnToPool), 2.5f);
    }

    void OnDisable() { CancelInvoke(); }

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
