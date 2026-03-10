using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Fiziksel Kapı
/// GatePrefab'a ekle. Rigidbody(IsKinematic=true) + BoxCollider(IsTrigger=true) şart.
/// Inspector'dan GateData sürükle. LabelText için child'a TextMeshPro ekle.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public TextMeshPro labelText; // Prefab'ın child'ındaki 3D Text objesi

    void Start()
    {
        if (gateData == null) return;

        // Kapı yazısını ayarla
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.color     = Color.white;
            labelText.fontSize  = 8f;
            labelText.alignment = TextAlignmentOptions.Center;
        }

        // Kapı rengini ayarla
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // URP için material instance oluştur
            Material mat = new Material(r.material);
            mat.color = gateData.gateColor;
            r.material = mat;
        }
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

        Destroy(gameObject);
    }
}