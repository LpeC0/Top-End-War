using UnityEngine;

/// <summary>
/// Top End War — Düşman (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody (IsKinematic:true) + Capsule Collider (IsTrigger:true)
/// Oyuncuya doğru yürür. Çarparsa CP düşer. Vurulunca CP kazanılır.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 100;

    [Header("Hareket")]
    public float moveSpeed = 4f;         // Oyuncuya doğru yaklaşma hızı

    [Header("Hasar (Oyuncu ile çarpışma)")]
    public int contactDamage = 30;       // Oyuncuya çarpınca düşülecek CP

    [Header("Ödül")]
    public int cpReward = 20;            // Öldürülünce oyuncuya verilecek CP

    int      currentHealth;
    Renderer bodyRenderer;
    bool     isDead           = false;
    bool     hasDamagedPlayer = false;

    void Start()
    {
        currentHealth = maxHealth;
        bodyRenderer  = GetComponentInChildren<Renderer>();
    }

    void Update()
    {
        if (isDead || PlayerStats.Instance == null) return;

        // Oyuncuya doğru sadece Z ekseninde yürü
        float playerZ = PlayerStats.Instance.transform.position.z;
        if (transform.position.z > playerZ)
        {
            transform.position -= new Vector3(0f, 0f, moveSpeed * Time.deltaTime);
        }

        // Oyuncu düşmanı 15 birim geçtiyse temizle (kaçırıldı)
        if (transform.position.z < playerZ - 15f)
            Destroy(gameObject);
    }

    // ── Mermi hasarı ─────────────────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;

        if (bodyRenderer != null) bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.12f);

        if (currentHealth <= 0) Die();
    }

    void ResetColor()
    {
        if (!isDead && bodyRenderer != null)
            bodyRenderer.material.color = Color.white;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        CancelInvoke();

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddCPFromKill(cpReward);

        Destroy(gameObject);
    }

    // ── Oyuncuya çarpma ──────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasDamagedPlayer || isDead) return;
        hasDamagedPlayer = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null) stats.TakeContactDamage(contactDamage);

        Die(); // Çarptıktan sonra düşman yok olur
    }
}