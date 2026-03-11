using UnityEngine;

/// <summary>
/// Top End War — Düşman (Claude)
/// Tag: "Enemy" olmalı. Ölünce CP verir.
/// EnemyPrefab'a ekle: Capsule Collider + Rigidbody (IsKinematic:true).
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 100;

    [Header("Ödül")]
    public int cpReward = 20; // Öldürünce oyuncuya bu kadar CP ver

    [Header("Görsel")]
    public Renderer bodyRenderer; // Inspector'dan sürükle ya da otomatik bulunur

    int currentHealth;

    static readonly Color hitColor  = Color.red;
    static readonly Color baseColor = Color.white;

    void Start()
    {
        currentHealth = maxHealth;

        // Renderer bulunamazsa otomatik ara
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        // Kısa renk flaşı
        if (bodyRenderer != null)
            bodyRenderer.material.color = hitColor;
        Invoke(nameof(ResetColor), 0.1f);

        if (currentHealth <= 0) Die();
    }

    void ResetColor()
    {
        if (bodyRenderer != null)
            bodyRenderer.material.color = baseColor;
    }

    void Die()
    {
        // CP ödülü ver
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddCPFromKill(cpReward);

        // İleride: ölüm particle + ses
        Destroy(gameObject);
    }

    // Eğer player üstünden geçerse (player koşarken düşman arkada kalır)
    void Update()
    {
        if (PlayerStats.Instance == null) return;

        // Player bu düşmanı 10 birim geçtiyse temizle
        float playerZ = PlayerStats.Instance.transform.position.z;
        if (transform.position.z < playerZ - 10f)
            Destroy(gameObject);
    }
}