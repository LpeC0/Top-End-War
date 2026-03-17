using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi (Claude)
///
/// PREFAB:
///   GatePrefab (root)
///   ├── Gate.cs + BoxCollider(IsTrigger=true) + Rigidbody(IsKinematic=true)
///   ├── Panel (3D Quad, Scale 4,5,1)  → panelRenderer slotuna sur
///   └── Label (3D TextMeshPro)        → labelText slotuna sur
///
/// MATERYAL: Herhangi bir materyal olabilir — kod runtime'da Sprites/Default'a cevirir.
/// Panel'deki MeshCollider otomatik silinir.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool _triggered = false;

    void Start()
    {
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable() { _triggered = false; }

    public void Refresh() { ApplyVisuals(); FitBoxCollider(); }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) Destroy(col);
    }

    void ApplyVisuals()
    {
        if (gateData == null) return;

        if (labelText != null)
        {
            labelText.text               = gateData.gateText;
            labelText.fontSize           = 5f;
            labelText.color              = Color.white;
            labelText.alignment          = TextAlignmentOptions.Center;
            labelText.fontStyle          = FontStyles.Bold;
            labelText.overflowMode       = TextOverflowModes.Truncate;
            labelText.enableWordWrapping = false;
        }

        if (panelRenderer != null)
        {
            // Sprites/Default: her shader'da calisir, tam transparan destekler
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c      = gateData.gateColor;
            c.a          = 0.72f;
            mat.color    = c;
            panelRenderer.material = mat;
        }
    }

    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;
        Vector3 s  = panelRenderer.transform.localScale;
        box.size   = new Vector3(s.x * 0.95f, s.y, 1.2f);
        box.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        other.GetComponent<PlayerStats>()?.ApplyGateEffect(gateData);
        Debug.Log("[Gate] " + gateData.gateText + " | CP: " + PlayerStats.Instance?.CP);
        Destroy(gameObject);
    }
}
