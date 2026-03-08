using UnityEngine;

/// <summary>
/// Army Gate Siege – Kamera Takip
/// Runner mantığı: X sabit (şerit değiştirince dünya sallanmaz),
/// sadece Z ve Y ekseninde Player'ı takip eder.
/// Cinemachine GEREKMİYOR. Main Camera'ya attach et, Target'a Player sürükle.
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    [Header("=== HEDEF ===")]
    public Transform target;          // Inspector'dan Player sürükle

    [Header("=== KAMERA OTURUMU ===")]
    public float heightOffset  =  9f; // Yukarı ne kadar
    public float backOffset    = 11f; // Arkaya ne kadar
    public float followSpeed   = 12f; // Takip yumuşaklığı (düşürünce daha "drone" hissi)

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("SimpleCameraFollow: Target atanmadı! Main Camera Inspector'ında Player'ı sürükle.");
            return;
        }

        // Hedef pozisyon:
        //   X = 0 (sabit, şerit değiştirince kamera sallanmaz)
        //   Y = Player Y + yükseklik
        //   Z = Player Z - geri mesafe
        Vector3 desired = new Vector3(
            0f,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        // Yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * followSpeed
        );

        // Her zaman Player'a bak (biraz yukarısına, boynun görünsün)
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}