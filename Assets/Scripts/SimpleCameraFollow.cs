using UnityEngine;

/// <summary>
/// Top End War — Runner Kamera v2 (Claude)
///
/// v2 DEĞİŞİKLİKLER:
///   - LookAt kaldırıldı — sabit pitch açısı, sallantı yok
///   - X ekseni tamamen sabit (0) — şerit değiştirince kamera sallanmaz
///   - Y: oyuncuyla birlikte kayar ama hızlı değişmez (followSpeed ile)
///   - Z: oyuncunun arkasında sabit mesafe
///   - pitchAngle: kameranın aşağı bakış açısı (Inspector'dan ayarla)
///
/// UNITY KURULUM:
///   Main Camera → bu scripti ekle → target = Player transform
///   Önerilen: heightOffset=8, backOffset=12, pitchAngle=22
///
/// İPUCU:
///   pitchAngle artarsa kamera daha fazla aşağı bakar (top-down hissi)
///   backOffset artarsa daha geniş alan görünür
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Pozisyon")]
    public float heightOffset = 8f;
    public float backOffset   = 12f;
    public float followSpeed  = 10f;

    [Header("Açı (sabit — LookAt yok)")]
    [Tooltip("Kameranın aşağı bakış açısı. 20-30 arası runner için idealdir.")]
    [Range(10f, 50f)]
    public float pitchAngle = 22f;

    // Sabit rotasyonu bir kere hesapla
    Quaternion _fixedRotation;

    void Start()
    {
        // Pitch açısına göre sabit rotasyon — oyun boyunca değişmez
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = _fixedRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Hedef konum: X=0 (sabit), Y=target+offset, Z=target-back
        Vector3 desired = new Vector3(
            0f,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        // Yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position, desired,
            Time.deltaTime * followSpeed
        );

        // Rotasyon hiç değişmez — LookAt yok
        transform.rotation = _fixedRotation;
    }

    /// <summary>Pitch açısı çalışma zamanında değiştirilirse rotasyonu güncelle.</summary>
    public void SetPitch(float angle)
    {
        pitchAngle     = Mathf.Clamp(angle, 10f, 50f);
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}