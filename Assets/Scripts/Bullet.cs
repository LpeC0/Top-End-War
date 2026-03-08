using UnityEngine;

/// <summary>
/// Top End War — Mermi (Gemini - Pool Uyumlu)
/// Destroy yerine gameObject.SetActive(false) kullanır.
/// </summary>
public class Bullet : MonoBehaviour
{
    public int damage = 50;
    private float lifeTimer = 0f;

    void OnEnable()
    {
        // Havuzdan her çıktığında ömrünü sıfırla (Mermi sonsuza gitmesin)
        lifeTimer = 3f; 
    }

    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            gameObject.SetActive(false); // Destroy yerine havuza geri döner
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy e = other.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(damage);

            gameObject.SetActive(false); // Çarptıktan sonra uyu
        }
    }
}