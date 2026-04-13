using UnityEngine;

/// <summary>
/// Top End War — Boss Isabet Alici v2
///
/// DEĞİŞİKLİK:
///   - ArmorPen / BossDamageMult tasir
///   - Eski TakeDamage(int) korunur
/// </summary>
public class BossHitReceiver : MonoBehaviour
{
    [Tooltip("BossManager objesi. Bos birakılırsa BossManager.Instance kullanilir.")]
    public BossManager bossManager;

    void Awake()
    {
        if (bossManager == null)
            bossManager = BossManager.Instance;
    }

    public void TakeDamage(int dmg)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(dmg);
    }

    // DEĞİŞİKLİK
    public void TakeDamage(int rawDamage, int armorPen, float bossDamageMult)
    {
        if (bossManager == null) bossManager = BossManager.Instance;
        bossManager?.TakeDamage(rawDamage, armorPen, bossDamageMult);
    }
}