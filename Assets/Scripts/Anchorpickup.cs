using TMPro;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anchor modda lane uzerinde okunur pickup prototipi.
/// Kendi world-space ikon/label/timer goruntusunu uretir ve alindiginda feedback basar.
/// </summary>
public class AnchorPickup : MonoBehaviour
{
    [Header("Pickup Tipi")]
    public AnchorPickupType pickupType = AnchorPickupType.FireRateBoost;
    public int choiceGroupId = 0; // DEĞİŞİKLİK: Aynı anda çıkan pickup çiftinden sadece biri alınabilir.

    [Header("Degerler")]
    [Tooltip("Soldier icin kac asker. Boost icin yuzde degeri (orn: 30 = +30%).")]
    public float value = 1f;
    [Tooltip("Sureli boost'lar icin saniye.")]
    public float duration = 6f;
    [Tooltip("Anchor heal icin maxHP yuzdesi (0.15 = %15).")]
    public float healPercent = 0.15f;

    [Header("Toplama")]
    [Tooltip("Oyuncu bu X mesafesine girince pickup alinir.")]
    public float collectRadius = 1.85f; // DEĞİŞİKLİK: Capture zone okunur kalır ama hızlı swipe ile bedava alınmaz.
    [Tooltip("Oyuncu pickup alanında bu kadar kalınca alınır.")]
    public float captureDuration = 1.1f; // DEĞİŞİKLİK: Lane capture kararı için test süresi 1.1 sn.
    [Tooltip("Alinmazsa kac saniyede kaybolur.")]
    public float lifetime = 4.5f;

    [Header("Okunurluk")]
    public string labelText = "PICKUP";
    public string iconText = "+";
    public string feedbackText = "PICKUP!";
    public Color pickupColor = Color.white;
    public Color labelColor = Color.white;

    [Header("Gorsel")]
    public GameObject visualRoot;

    float _spawnTime;
    bool _collected;
    float _captureProgress;
    Transform _billboardRoot;
    Transform _pulseRoot;
    Transform _timerBar;
    TextMeshPro _label;
    TextMeshPro _icon;
    TextMeshPro _timer;
    static readonly HashSet<int> ConsumedChoiceGroups = new HashSet<int>();

    void Awake()
    {
        EnsureVisuals();
    }

    void OnEnable()
    {
        _collected = false;
        _captureProgress = 0f;
        _spawnTime = Time.time;
        EnsureVisuals();
        RefreshVisualText();
        if (visualRoot != null) visualRoot.SetActive(true);
    }

    void Update()
    {
        if (_collected) return;

        float elapsed = Time.time - _spawnTime;
        float normalized = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;
        UpdateReadability(normalized);

        if (elapsed >= lifetime)
        {
            Despawn();
            return;
        }

        if (PlayerStats.Instance == null) return;

        float px = PlayerStats.Instance.transform.position.x;
        bool inCapture = Mathf.Abs(px - transform.position.x) <= collectRadius;
        if (inCapture)
            _captureProgress += Time.deltaTime / Mathf.Max(0.1f, captureDuration);
        else
            _captureProgress = Mathf.Max(0f, _captureProgress - Time.deltaTime * 1.5f);

        _captureProgress = Mathf.Clamp01(_captureProgress);
        if (_captureProgress >= 1f)
            Collect();
    }

    void Collect()
    {
        if (_collected) return;

        if (!TryConsumeChoiceGroup())
        {
            DismissFromChoiceGroup();
            return;
        }

        _collected = true;

        ApplyEffect();
        SpawnFeedback();
        GameEvents.OnAnchorPickupCollected?.Invoke(pickupType);
        RunDebugMetrics.Instance.RecordPickupCollected(); // DEĞİŞİKLİK: Pickup alma kararı debug metriklerine yazılır.
        Debug.Log($"[AnchorPickup] Toplandi: {pickupType} | deger={value}");

        DisableOtherPickupsInGroup(); // DEĞİŞİKLİK: Bir pickup alınca karşı seçim kapanır.
        Despawn();
    }

    public static void ResetChoiceState()
    {
        // DEĞİŞİKLİK: Anchor pickup mutual exclusion state'i yeni anchor'da temizlenir.
        ConsumedChoiceGroups.Clear();
    }

    bool TryConsumeChoiceGroup()
    {
        // DEĞİŞİKLİK: Pickup pair seçimleri atomik olur; iki pickup birden alınamaz.
        if (choiceGroupId <= 0) return true;
        if (ConsumedChoiceGroups.Contains(choiceGroupId)) return false;
        ConsumedChoiceGroups.Add(choiceGroupId);
        return true;
    }

    void DisableOtherPickupsInGroup()
    {
        // DEĞİŞİKLİK: Aynı pair'deki diğer pickup alınmış sayılmaz, sadece kapanır.
        if (choiceGroupId <= 0) return;
        foreach (AnchorPickup pickup in FindObjectsByType<AnchorPickup>(FindObjectsSortMode.None))
        {
            if (pickup == null || pickup == this) continue;
            if (pickup.choiceGroupId != choiceGroupId) continue;
            pickup.DismissFromChoiceGroup();
        }
    }

    public void DismissFromChoiceGroup()
    {
        // DEĞİŞİKLİK: Karşı pickup seçimi kapatılırken missed metriği kirletilmez.
        _collected = true;
        if (visualRoot != null) visualRoot.SetActive(false);
        Destroy(gameObject);
    }

    void ApplyEffect()
    {
        switch (pickupType)
        {
            case AnchorPickupType.AddSoldier:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: Mathf.RoundToInt(value));
                break;

            case AnchorPickupType.FireRateBoost:
                AnchorBuffManager.EnsureInstance()?.ApplyBuff(AnchorBuffType.FireRate, value, duration);
                break;

            case AnchorPickupType.ArmorPenBoost:
                AnchorBuffManager.EnsureInstance()?.ApplyBuff(AnchorBuffType.ArmorPen, value, duration);
                break;

            case AnchorPickupType.RepairAnchor:
                if (AnchorCore.Instance != null)
                {
                    int heal = Mathf.RoundToInt(AnchorCore.Instance.MaxHP * healPercent);
                    AnchorCore.Instance.Heal(heal);
                }
                break;

            case AnchorPickupType.RepairSquad:
                ArmyManager.Instance?.HealAll(healPercent);
                break;
        }
    }

    void EnsureVisuals()
    {
        if (visualRoot != null && _label != null && _icon != null && _timer != null) return;

        if (visualRoot == null)
        {
            visualRoot = new GameObject("PickupVisual");
            visualRoot.transform.SetParent(transform, false);
        }

        _billboardRoot = visualRoot.transform;

        if (_pulseRoot == null)
        {
            GameObject pulse = new GameObject("PulseRoot");
            pulse.transform.SetParent(_billboardRoot, false);
            _pulseRoot = pulse.transform;
        }

        if (_pulseRoot.Find("Beacon") == null)
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon";
            beacon.transform.SetParent(_pulseRoot, false);
            beacon.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            beacon.transform.localScale = new Vector3(2.05f, 0.035f, 2.75f); // DEĞİŞİKLİK: Pickup artık küçük obje değil, lane capture zone olarak okunur.
            Destroy(beacon.GetComponent<Collider>());

            Renderer renderer = beacon.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = pickupColor;
            }
        }

        _icon = EnsureText("Icon", new Vector3(0f, 1.65f, 0f), 2.1f, labelColor, FontStyles.Bold);
        _label = EnsureText("Label", new Vector3(0f, 1.05f, 0f), 0.9f, Color.white, FontStyles.Bold);
        _timer = EnsureText("Timer", new Vector3(0f, 0.55f, 0f), 0.72f, Color.white, FontStyles.Bold);

        if (_timerBar == null)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "TimerBar";
            bar.transform.SetParent(_billboardRoot, false);
            bar.transform.localPosition = new Vector3(0f, 0.33f, 0f);
            bar.transform.localScale = new Vector3(2.4f, 0.08f, 0.08f); // DEĞİŞİKLİK: Capture progress bar zone üstünde daha net okunur.
            Destroy(bar.GetComponent<Collider>());

            Renderer renderer = bar.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.white;
            }
            _timerBar = bar.transform;
        }
    }

    TextMeshPro EnsureText(string name, Vector3 localPosition, float fontSize, Color color, FontStyles style)
    {
        Transform existing = _billboardRoot.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(_billboardRoot, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null) tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap; // DEĞİŞİKLİK: Obsolete TMP wrapping API temizlendi.
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 80;
        return tmp;
    }

    void RefreshVisualText()
    {
        if (_icon != null)
        {
            _icon.text = iconText;
            _icon.color = labelColor;
        }

        if (_label != null)
        {
            _label.text = labelText;
            _label.color = Color.white;
        }
    }

    void UpdateReadability(float normalizedLifetime)
    {
        if (_billboardRoot != null && Camera.main != null)
            _billboardRoot.rotation = Quaternion.LookRotation(_billboardRoot.position - Camera.main.transform.position);

        if (_pulseRoot != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.08f;
            float urgency = normalizedLifetime > 0.7f ? 1.15f : 1f;
            _pulseRoot.localScale = Vector3.one * pulse * urgency;
        }

        float secondsLeft = Mathf.Max(0f, lifetime - (Time.time - _spawnTime));
        if (_timer != null)
            _timer.text = _captureProgress > 0f
                ? $"CAP {Mathf.RoundToInt(_captureProgress * 100f)}%"
                : Mathf.CeilToInt(secondsLeft).ToString(); // DEĞİŞİKLİK: Pickup capture progress TMP ile okunur.

        if (_timerBar != null)
        {
            float fill = _captureProgress > 0f ? _captureProgress : 1f - normalizedLifetime;
            _timerBar.localScale = new Vector3(Mathf.Max(0.05f, 2.4f * fill), 0.08f, 0.08f);
            Renderer renderer = _timerBar.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = _captureProgress > 0f
                    ? Color.Lerp(new Color(0.25f, 1f, 0.45f), Color.white, _captureProgress)
                    : Color.Lerp(new Color(1f, 0.2f, 0.15f), Color.white, 1f - normalizedLifetime); // DEĞİŞİKLİK: Bar capture sırasında progress bar gibi davranır.
        }
    }

    void SpawnFeedback()
    {
        Vector3 pos = transform.position + new Vector3(0f, 3.2f, -0.4f);
        GameObject go = new GameObject($"PickupFeedback_{pickupType}");
        go.transform.position = pos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = feedbackText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 1.25f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = labelColor;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 100;

        AnchorPickupFeedback feedback = go.AddComponent<AnchorPickupFeedback>();
        feedback.Init(labelColor);
    }

    void Despawn()
    {
        if (!_collected)
            RunDebugMetrics.Instance.RecordPickupMissed(); // DEĞİŞİKLİK: Süresi dolan pickup risk/fırsat metriğine yazılır.
        if (visualRoot != null) visualRoot.SetActive(false);
        Destroy(gameObject); // DEĞİŞİKLİK: Pickup mock objeleri toplandıktan/süresi dolduktan sonra sahnede inactive birikmez.
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, collectRadius);
    }
#endif
}

public class AnchorPickupFeedback : MonoBehaviour
{
    float _startTime;
    float _duration = 1.15f;
    Color _baseColor = Color.white;
    TextMeshPro _tmp;

    public void Init(Color color)
    {
        _baseColor = color;
    }

    void Awake()
    {
        _startTime = Time.time;
        _tmp = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        float t = Mathf.Clamp01((Time.time - _startTime) / _duration);
        transform.position += Vector3.up * (Time.deltaTime * 1.4f);

        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        if (_tmp != null)
        {
            Color c = _baseColor;
            c.a = 1f - t;
            _tmp.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}

public enum AnchorPickupType
{
    AddSoldier,
    RepairSquad,
    FireRateBoost,
    ArmorPenBoost,
    RepairAnchor,
}

public enum AnchorBuffType
{
    FireRate,
    ArmorPen,
}
