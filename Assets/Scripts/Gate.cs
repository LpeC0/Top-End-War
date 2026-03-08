using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Tek Kapı
/// GatePrefab'a ekle. Rigidbody (IsKinematic=true) + BoxCollider (IsTrigger=true) gerekli.
/// Inspector'dan GateData sürükle bırak.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData     gateData;
    public TextMeshPro  labelText;   // Kapının üstündeki 3D yazı (isteğe bağlı)

    void Start()
    {
        if (gateData == null) return;

        // Yazıyı ayarla
        if (labelText) labelText.text = gateData.gateText;

        // Rengi ayarla
        Renderer r = GetComponent<Renderer>();
        if (r) r.material.color = gateData.gateColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log($"Kapıdan geçildi: {gateData.gateText} | Yeni CP: {stats.CP}");
        }

        Destroy(gameObject); // İleride: Object Pool ile değiştirilecek
    }
}
