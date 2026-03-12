using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi (Claude)
///
/// PREFAB YAPISI:
///   GatePrefab
///   ├── [Gate.cs] [BoxCollider IsTrigger=true] [Rigidbody IsKinematic=true]
///   ├── Panel   (Cube, scale 3x4x0.2)
///   └── Label   (3D TextMeshPro)
///
/// Seffaflik icin Panel materyali:
///   1. Project → Create → Material → adi "GateMat"
///   2. Inspector → Shader: Universal Render Pipeline/Particles/Unlit
///      (VEYA Built-in: Standard, Rendering Mode: Transparent)
///   3. Bu script renk+alfayi otomatik ayarlar — baska bir sey yapmana gerek yok.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool triggered = false;

    void Start()      { if (gateData != null) ApplyVisuals(); }
    void OnEnable()   { if (gateData != null) ApplyVisuals(); }

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
            Material mat = new Material(panelRenderer.sharedMaterial);

            // URP Lit shader keyword'leri
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",   0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            Color c = gateData.gateColor;
            c.a     = 0.65f;
            mat.color = c;

            panelRenderer.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        other.GetComponent<PlayerStats>()?.ApplyGateEffect(gateData);
        Debug.Log("[Gate] " + gateData.gateText + " | CP: " + PlayerStats.Instance?.CP);
        Destroy(gameObject);
    }
}