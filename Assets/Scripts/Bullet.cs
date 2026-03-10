using UnityEngine;

/// <summary>
/// Top End War — Mermi
/// BulletPrefab'a ekle. SphereCollider(IsTrigger=true) + Rigidbody gerekli.
/// ObjectPooler ile çalışır: Destroy yerine SetActive(false).
/// </summary>
public class Bullet : MonoBehaviour
{
    public int damage = 50;

    // Pool'dan çıkınca kendini 3 saniye sonra geri gönder
    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), 3f);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(damage);
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        // Hızı sıfırla ki bir sonraki kullanımda sorun çıkmasın
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;

        gameObject.SetActive(false); // Destroy yerine havuza dön
    }
}