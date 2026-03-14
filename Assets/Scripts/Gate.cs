using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v6 (Claude)
///
/// ═══════════════════════════════════════════════════════════
/// PREFAB YAPISI (tam olarak bu sekilde olmali):
///
///   GatePrefab  (empty GameObject — root)
///   ├── Gate.cs
///   ├── BoxCollider     IsTrigger = TRUE   ← boyut otomatik ayarlanır
///   ├── Rigidbody       IsKinematic = TRUE
///   ├── Panel           (3D Object → Quad, scale: 4, 5, 1)
///   │     └── Materyal: GateMat (asagiya bak)
///   └── Label           (3D Object → Text - TextMeshPro)
///         ├── Rect Transform: Width=3.5, Height=2
///         ├── Position: 0, 0, -0.1  (Panel'in hafif onunde)
///         └── TextMeshPro: FontSize=5, Overflow=Truncate, Alignment=Center
///
/// ═══════════════════════════════════════════════════════════
/// GATE MAT NASIL YAPILIR (tek seferlik, 2 dakika):
///
///   1. Project → sag tik → Create → Material → adi "GateMat"
///   2. Inspector'da Shader kutusuna tikla
///   3. "Universal Render Pipeline/Lit" sec
///   4. "Surface Type" → "Transparent" sec
///   5. "Base Map" rengi BEYAZ (255,255,255,165) — alpha 165 (transparan)
///   6. Bu materyali Panel Quad'ının uzerine surukle
///
///   KOD TARAFINDA: mat.SetColor("_BaseColor", ...) kullaniyoruz — bu dogru URP yolu.
///   mat.color veya mat.SetColor("_Color") URP'de CALISMAZ.
/// ═══════════════════════════════════════════════════════════
/// </summary>
public class Gate : MonoBehaviour
{
    public GateData    gateData;
    public Renderer    panelRenderer;   // Panel Quad'ini sur
    public TextMeshPro labelText;       // Label TMP'yi sur

    BoxCollider _col;
    bool        _triggered = false;

    void Awake()
    {
        _col = GetComponent<BoxCollider>();
    }

    void Start()
    {
        ApplyVisuals();
        FitCollider();
    }

    // SpawnManager runtime'da gateData atayinca da calis
    void OnEnable()
    {
        _triggered = false;
    }

    // SpawnManager gate.gateData = data yaptiktan sonra cagrilabilir
    public void Refresh()
    {
        ApplyVisuals();
        FitCollider();
    }

    // ── Gorsel ─────────────────────────────────────────────────────────────
    void ApplyVisuals()
    {
        if (gateData == null) return;

        // Sadece matematiksel etkiyi goster (+60, x2, MERGE, RISK, vb.)
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

        // Renk — URP'de _BaseColor kullanmak zorunlu
        if (panelRenderer != null)
        {
            Material mat = new Material(panelRenderer.sharedMaterial);

            Color c = gateData.gateColor;
            c.a = 0.55f; // Transparan

            // URP Lit/Unlit icin dogru property
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c; // Fallback (eski shader)

            panelRenderer.material = mat;
        }
    }

    // ── Hitbox Boyutu — Panel'in gercek boyutuna gore ─────────────────────
    void FitCollider()
    {
        if (_col == null || panelRenderer == null) return;

        // Panel'in world-space bounds'unu al, local'e cevir
        Bounds b    = panelRenderer.bounds;
        Vector3 sz  = b.size;

        // Biraz buyutur (oyuncu panel kenarina yakın gecebilsin)
        _col.size   = new Vector3(sz.x * 1.05f, sz.y * 1.1f, sz.z + 0.8f);
        _col.center = transform.InverseTransformPoint(b.center);
    }

    // ── Trigger ────────────────────────────────────────────────────────────
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