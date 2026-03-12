using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi (Claude)
///
/// PREFAB YAPISI (onemli!):
///   GatePrefab  [Gate.cs] [BoxCollider IsTrigger=true] [Rigidbody IsKinematic=true]
///   ├── Panel   [MeshRenderer — URP/Lit veya Unlit Shader]
///   └── Label   [TextMeshPro 3D]
///
/// URP'de seffaflik icin: Panel'in material'i
///   Surface Type: Transparent
///   Blend Mode: Alpha
/// ayarlanmali. Gate.cs rengi ve alfayi otomatik ayarlar.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;   // Inspector'dan Panel objesini surukle
    public TextMeshPro labelText;       // Inspector'dan Label'i surukle

    bool triggered = false;            // Cift tetiklenmeyi onle

    void Start()
    {
        if (gateData == null) return;
        ApplyVisuals();
    }

    // Sahneye ilk konuldugunda da guncelle (runtime spawn)
    void OnEnable()
    {
        if (gateData != null) ApplyVisuals();
    }

    void ApplyVisuals()
    {
        // Yazi
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.fontSize  = 9f;
            labelText.color     = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
        }

        // Renk + Seffaflik
        if (panelRenderer != null)
        {
            // Material instance olustur — prefab'i kirletme
            Material mat = new Material(panelRenderer.sharedMaterial);

            // Seffaf yapabilmek icin URP rendering mode ayarla
            mat.SetFloat("_Surface", 1);            // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend",   0);            // Alpha blend
            mat.SetFloat("_SrcBlend",  (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend",  (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",    0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            // GateData rengini ata ama alfayi 0.6'ya sabitle
            Color c = gateData.gateColor;
            c.a     = 0.6f;
            mat.color = c;

            panelRenderer.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;                    // Zaten tetiklendi
        if (!other.CompareTag("Player")) return;

        triggered = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log($"[Gate] {gateData.gateText} | CP: {stats.CP}");
        }

        Destroy(gameObject);
    }
}