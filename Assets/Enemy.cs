using UnityEngine;

/// <summary>
/// Army Gate Siege – Düşman
/// Tag: "Enemy" olmalı. Mermi vurduğunda TakeDamage çağırır.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("=== DÜŞMAN ===")]
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // İleride: patlama particle, loot drop, CP artışı buraya
        Destroy(gameObject);
    }
}
