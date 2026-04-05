using UnityEngine;

/// <summary>
/// Top End War — Boss Isabet Alici (Claude)
/// Boss prefab'ine eklenir. Bullet.cs bu componenti bulur.
///
/// KURULUM:
///   Boss GameObject'ine ekle.
///   Inspector'dan bossManager alanina BossManager objesini sur.
///   (bos birakılırsa Instance'tan alir — fallback)
/// </summary>
public class BossHitReceiver : MonoBehaviour
{
    [Tooltip("BossManager objesi. Bos birakılırsa BossManager.Instance kullanilir.")]
    public BossManager bossManager;   // ← Bullet.cs bu field'i ariyordu

    void Awake()
    {
        if (bossManager == null)
            bossManager = BossManager.Instance;
    }

    /// <summary>Bullet.cs bu metodu cagirir.</summary>
    public void TakeDamage(int dmg)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(dmg);
    }
}