using UnityEngine;
using TMPro;

/// <summary>
/// Top End War — Kapi v2
///
/// Yeni kanonik veri tipi: GateConfig
/// </summary>
public class Gate : MonoBehaviour
{
    public GateConfig  gateConfig;
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

    public void Refresh()
    {
        ApplyVisuals();
        FitBoxCollider();
    }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) Destroy(col);
    }

    void ApplyVisuals()
    {
        if (gateConfig == null) return;

        if (labelText != null)
        {
            string sub = string.IsNullOrWhiteSpace(gateConfig.tag2)
                ? gateConfig.tag1
                : $"{gateConfig.tag1} • {gateConfig.tag2}";

            labelText.text               = $"{gateConfig.title}\n<size=55%>{sub}</size>";
            labelText.fontSize           = 5f;
            labelText.color              = Color.white;
            labelText.alignment          = TextAlignmentOptions.Center;
            labelText.fontStyle          = FontStyles.Bold;
            labelText.overflowMode       = TextOverflowModes.Overflow;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c = gateConfig.gateColor;
            c.a = 0.72f;
            mat.color = c;
            panelRenderer.material = mat;
        }
    }

    void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || panelRenderer == null) return;

        Vector3 s = panelRenderer.transform.localScale;
        box.size = new Vector3(s.x * 0.95f, s.y, 1.2f);
        box.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        PlayerStats ps = other.GetComponent<PlayerStats>();
        ps?.ApplyGateConfig(gateConfig);

        other.GetComponent<GateFeedback>()?.PlayGatePop();

        Debug.Log($"[Gate] {gateConfig.title}");
        Destroy(gameObject);
    }
}