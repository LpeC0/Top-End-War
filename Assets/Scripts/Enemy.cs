using UnityEngine;

/// <summary>
/// Top End War — Dushman (Claude)
/// Tag: "Enemy"
/// Prefab: Capsule → Rigidbody(IsKinematic:true) + CapsuleCollider(IsTrigger:true)
/// Oyuncuya dogru ilerler, sinir disina cikamaz, iç içe girmez.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 120;

    [Header("Hareket")]
    public float moveSpeed   = 4.5f;
    public float trackSpeedX = 1.5f;   // Oyuncuyu X'te takip hizi (dusuk = dalga formu korunur)
    public float xLimit      = 5.5f;   // PlayerController ile ayni olmali

    [Header("Hasar")]
    public int contactDamage = 50;
    public int cpReward      = 15;

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

        Vector3 pos       = transform.position;
        float   playerZ   = PlayerStats.Instance.transform.position.z;

        // Z: Oyuncuya dogru yuru
        if (pos.z > playerZ + 0.5f)
            pos.z -= moveSpeed * Time.deltaTime;

        // X: Oyuncuyu yavas takip et
        float targetX = Mathf.Clamp(
            Mathf.MoveTowards(pos.x, PlayerStats.Instance.transform.position.x, trackSpeedX * Time.deltaTime),
            -xLimit, xLimit);
        pos.x = targetX;

        // Sinir kontrolu
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);

        transform.position = pos;

        // 15 birim geride kaldiysa temizle
        if (pos.z < playerZ - 15f)
            Destroy(gameObject);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;
        if (bodyRenderer != null) bodyRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);
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
        PlayerStats.Instance?.AddCPFromKill(cpReward);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasDamagedPlayer || isDead) return;
        hasDamagedPlayer = true;
        other.GetComponent<PlayerStats>()?.TakeContactDamage(contactDamage);
        Die();
    }
}