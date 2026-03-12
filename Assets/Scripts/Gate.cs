using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v5 (Claude)
///
/// PREFAB YAPISI:
///   GatePrefab (root)
///   ├── Gate.cs  +  BoxCollider(IsTrigger=true)  +  Rigidbody(IsKinematic=true)
///   ├── Panel  (Cube, scale 3x4x0.3)  ← panelRenderer slotuna sur
///   └── Label  (3D TextMeshPro)       ← labelText slotuna sur
///
/// GateMat materyali icin tek ayar (Inspector):
///   Shader: Particles/Standard Unlit
///   Rendering Mode: Transparent
///   Color Mode: COLOR  ← Multiply degil!
///   Albedo rengi: beyaz (kod halleder)
///
/// Bu script sadece material.color'u degistirir — baska hic bir sey yapmaz.
/// Shader property isimleriyle ugrasma yok.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool triggered = false;

    void Start()
    {
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (gateData == null) return;

        // ── Yazi ─────────────────────────────────────────────────────────
        if (labelText != null)
        {
            labelText.text      = gateData.gateText;
            labelText.fontSize  = 9f;
            labelText.color     = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
        }

        // ── Renk ─────────────────────────────────────────────────────────
        // Sadece material.color kullan — shader property'lerine dokunma
        if (panelRenderer != null)
        {
            // Prefab kirlenmesin diye instance al
            Material mat = Instantiate(panelRenderer.sharedMaterial);
            mat.color    = gateData.gateColor; // Alpha deger GateData'dan gelir
            panelRenderer.material = mat;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;                     // Cift tetiklenme engeli
        if (!other.CompareTag("Player")) return;

        triggered = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log("[Gate] " + gateData.gateText + " | Yeni CP: " + stats.CP);
        }

        Destroy(gameObject);
    }
}