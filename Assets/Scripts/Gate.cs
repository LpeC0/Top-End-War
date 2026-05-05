using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Top End War - Gate runtime
/// Spawn aninda config snapshot alir ve sonrasinda degismez.
/// </summary>
public class Gate : MonoBehaviour
{
    public GateConfig gateConfig;
    public Renderer panelRenderer;
    public TextMeshPro labelText;

    static readonly Dictionary<int, int> ConsumedChoiceGroups = new Dictionary<int, int>();

    bool _triggered;
    int _choiceGroupId;
    GateRuntimeData _runtimeData;

    void Start()
    {
        RemoveChildColliders();
        ApplyVisuals();
        FitBoxCollider();
    }

    void OnEnable()
    {
        _triggered = false;
    }

    public static void ResetChoiceState()
    {
        ConsumedChoiceGroups.Clear();
    }

    public static bool TryConsumeGroup(int choiceGroupId, int gateInstanceId)
    {
        if (choiceGroupId <= 0)
        {
            // DEĞİŞİKLİK: Standalone tutorial/sahne gate'leri log spam üretmeden tek seçim gibi çalışır.
            return true;
        }

        if (ConsumedChoiceGroups.ContainsKey(choiceGroupId))
            return false;

        ConsumedChoiceGroups.Add(choiceGroupId, gateInstanceId);
        return true;
    }

    public void SetChoiceGroup(int choiceGroupId)
    {
        _choiceGroupId = Mathf.Max(0, choiceGroupId);
    }

    public void BindGateConfig(GateConfig config)
    {
        gateConfig = config;
        _runtimeData = GateRuntimeData.FromConfig(config);
        Refresh();
    }

    public void Refresh()
    {
        ApplyVisuals();
        FitBoxCollider();
    }

    GateRuntimeData GetRuntimeData()
    {
        if (_runtimeData != null) return _runtimeData;
        if (gateConfig == null) return null;
        _runtimeData = GateRuntimeData.FromConfig(gateConfig);
        return _runtimeData;
    }

    void RemoveChildColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.gameObject != gameObject)
                Destroy(col);
        }
    }

    void ApplyVisuals()
    {
        GateRuntimeData data = GetRuntimeData();
        if (data == null) return;

        if (labelText != null)
        {
            string sub = string.IsNullOrWhiteSpace(data.tag2)
                ? data.tag1
                : $"{data.tag1} • {data.tag2}";

            labelText.text = $"{data.title}\n<size=55%>{sub}</size>";
            labelText.fontSize = 5f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (panelRenderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            Color c = data.gateColor;
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
        TryApplyGate(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryApplyGate(other);
    }

    void TryApplyGate(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;

        if (!TryConsumeGroup(_choiceGroupId, GetInstanceID()))
        {
            _triggered = true;
            DisablePassiveGate();
            return;
        }

        _triggered = true;
        DisableOtherGatesInGroup();

        GateRuntimeData data = GetRuntimeData();
        if (data == null)
        {
            DisablePassiveGate();
            return;
        }

        PlayerStats ps = PlayerStats.Instance
                      ?? other.GetComponent<PlayerStats>()
                      ?? other.GetComponentInParent<PlayerStats>();

        if (ps != null)
            ps.ApplyGateConfig(data);
        else
            Debug.LogWarning("[Gate] PlayerStats not found - gate effect skipped.");

        other.GetComponent<GateFeedback>()?.PlayGatePop();

        Debug.Log($"[Gate] {data.title}");
        Destroy(gameObject);
    }

    void DisableOtherGatesInGroup()
    {
        if (_choiceGroupId <= 0) return;

        foreach (Gate gate in FindObjectsByType<Gate>(FindObjectsSortMode.None))
        {
            if (gate == null || gate == this) continue;
            if (gate._choiceGroupId != _choiceGroupId) continue;
            gate.DisarmFromGroup();
        }
    }

    void DisarmFromGroup()
    {
        if (_triggered) return;
        _triggered = true;
        DisablePassiveGate();
    }

    void DisablePassiveGate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (panelRenderer != null)
            panelRenderer.enabled = false;

        if (labelText != null)
            labelText.gameObject.SetActive(false);

        Destroy(gameObject, 0.25f);
    }
}

public class GateRuntimeData
{
    public string gateId;
    public string title;
    public string tag1;
    public string tag2;
    public Color gateColor;
    public bool isRisk;
    public bool isRecovery;
    public List<GateModifier2> modifiers = new List<GateModifier2>();
    public List<GateModifier2> penaltyModifiers = new List<GateModifier2>();

    public static GateRuntimeData FromConfig(GateConfig config)
    {
        if (config == null) return null;

        return new GateRuntimeData
        {
            gateId = config.gateId,
            title = config.title,
            tag1 = config.tag1,
            tag2 = config.tag2,
            gateColor = config.gateColor,
            isRisk = config.IsRisk,
            isRecovery = config.IsRecovery,
            modifiers = CloneModifiers(config.modifiers),
            penaltyModifiers = CloneModifiers(config.penaltyModifiers),
        };
    }

    static List<GateModifier2> CloneModifiers(List<GateModifier2> source)
    {
        var result = new List<GateModifier2>();
        if (source == null) return result;

        foreach (GateModifier2 mod in source)
        {
            if (mod == null) continue;
            result.Add(new GateModifier2
            {
                targetType = mod.targetType,
                statType = mod.statType,
                operation = mod.operation,
                value = mod.value,
            });
        }

        return result;
    }
}
