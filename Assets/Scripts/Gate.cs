using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v7 (Claude)
///
/// PREFAB YAPISI (tam):
///   GatePrefab  [root]
///   ├── Gate.cs
///   ├── BoxCollider    IsTrigger=true
///   ├── Rigidbody      IsKinematic=true
///   ├── Panel  (Quad, Scale 4,5,1)  ← panelRenderer slotuna sur
///   └── Label  (3D TMP)             ← labelText slotuna sur
///
/// MATERYAL (ARTIK KOD HALLEDIYOR — elle bir sey yapma):
///   GateMat sadece var olmali, herhangi bir shader olabilir.
///   Kod runtime'da shader'i "Sprites/Default"'a cevirir.
///   "Sprites/Default" = tam seffaf destekler, renk tam istedigin gibi gelir.
///   Panel uzerindeki MeshCollider otomatik silinir.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;
    public TextMeshPro labelText;

    bool _triggered = false;

    void Start()
    {
        // Panel'deki gereksiz collider'lari temizle
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable() { _triggered = false; }

    // SpawnManager runtime'da gateData atadiktan sonra cagirabilir
    public void Refresh() { ApplyVisuals(); FitBoxCollider(); }

    // ── Gorsel ────────────────────────────────────────────────────────────────
    void ApplyVisuals()
    {
        if (gateData == null) return;

        // Yazi
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

        // Renk — "Sprites/Default" shader her platformda, URP/Built-in ayirt etmeksizin
        // tam olarak istedigin rengi verir. Transparan destekler.
        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));

            Color c  = gateData.gateColor;
            c.a      = 0.72f;           // Transparan — 0=tamamen seffaf, 1=tam dolu
            mat.color = c;

            panelRenderer.material = mat;
        }
    }

    // ── Panel'deki gereksiz collider'lari sil ─────────────────────────────────
    void RemoveChildColliders()
    {
        // Root'taki BoxCollider (trigger) haric tum child collider'lari sil
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.gameObject == gameObject) continue; // Root'a dokunma
            Destroy(col);
        }
    }

    // ── Root BoxCollider'i Panel boyutuna gore ayarla ────────────────────────
    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;

        // Panel'in lokal boyutunu kullan (Quad Scale 4,5,1 → 4x5)
        Vector3 panelLocal = panelRenderer.transform.localScale;
        box.size   = new Vector3(panelLocal.x * 0.95f, panelLocal.y * 1.0f, 1.2f);
        box.center = Vector3.zero;
    }

    // ── Trigger ───────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateData);
            Debug.Log("[Gate] " + gateData.gateText + " → CP: " + stats.CP);
        }

        Destroy(gameObject);
    }
}