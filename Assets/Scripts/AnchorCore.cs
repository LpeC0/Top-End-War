using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Top End War — AnchorCore v1.0
///
/// Anchor modun korunan nesnesi. Commander HP'sinden bağımsız,
/// ayrı bir hayat barına sahip. Bu sıfırlanırsa stage biter (Defeat).
///
/// HP Formülü:
///   anchorMaxHP = stageBaseHP + PlayerStats.TotalEquipmentHPBonus()
///   stageBaseHP → AnchorModeManager üzerinden StageBlueprint'ten gelir.
///   Equipment bonusu → oyuncunun ekipman yatırımını anchor savunmasına yansıtır.
///
/// ThreatManager bağlantısı:
///   Critical zone'da drain bu nesneye gelir, Commander'a değil.
///   ThreatManager.Instance.SetDrainTarget(ThreatDrainTarget.Anchor) ile ayarlanır.
///
/// Kurulum:
///   Sahneye bir GameObject ekle (görsel: kalkan, üs, kale vb.)
///   Bu scripti koy. AnchorModeManager başlarken InitAnchor() çağırır.
/// </summary>
public class AnchorCore : MonoBehaviour
{
    public static AnchorCore Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Bağlantılar")]
    [Tooltip("HP bar gibi görsel bileşenler buraya bağlanır. Zorunlu değil.")]
    [SerializeField] AnchorHPBar _hpBar;   // isteğe bağlı — henüz yoksa null kalabilir

    [Header("Debug (Salt Okunur)")]
    [SerializeField] int _currentHP;
    [SerializeField] int _maxHP;
    [SerializeField] bool _isDestroyed;

    Transform _visualRoot;
    Renderer _coreRenderer;
    TextMeshPro _coreLabel;
    Coroutine _flashCo;

    // ── Public State ─────────────────────────────────────────────────────

    public int  CurrentHP    => _currentHP;
    public int  MaxHP        => _maxHP;
    public bool IsDestroyed  => _isDestroyed;
    public float HPRatio     => _maxHP > 0 ? (float)_currentHP / _maxHP : 0f;

    // ── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Init ─────────────────────────────────────────────────────────────

    /// <summary>
    /// AnchorModeManager tarafından çağrılır.
    /// stageBaseHP: StageBlueprint'ten gelen temel HP değeri.
    /// Equipment bonusu otomatik eklenir.
    /// </summary>
    public void InitAnchor(int stageBaseHP)
    {
        int equipBonus = PlayerStats.Instance != null
            ? PlayerStats.Instance.TotalEquipmentHPBonus()
            : 0;

        _maxHP      = Mathf.Max(100, stageBaseHP + equipBonus);
        _currentHP  = _maxHP;
        _isDestroyed = false;

        _hpBar?.Init(_maxHP);
        GameEvents.OnAnchorHPChanged?.Invoke(_currentHP, _maxHP);
        EnsureTempVisuals(); // DEĞİŞİKLİK: AnchorCore placeholder görünür hale getirilir.
        SetTempVisualsVisible(true); // DEĞİŞİKLİK: Önceki anchor sonunda gizlenen Core mock'u yeni anchor başında tekrar açılır.
        RefreshCoreLabel();

        Debug.Log($"[AnchorCore] Init | stageBase={stageBaseHP} equipBonus={equipBonus} maxHP={_maxHP}");
    }

    // ── Hasar ────────────────────────────────────────────────────────────

    /// <summary>
    /// ThreatManager veya düşman temas hasarı buraya gelir.
    /// Dönen değer: gerçekten hasar uygulandı mı?
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (_isDestroyed) return false;
        if (amount <= 0)  return false;

        int oldHP  = _currentHP;
        _currentHP = Mathf.Max(0, _currentHP - amount);

        _hpBar?.UpdateBar(_currentHP);
        GameEvents.OnAnchorHPChanged?.Invoke(_currentHP, _maxHP);
        GameEvents.OnAnchorDamaged?.Invoke(amount, _currentHP);
        RunDebugMetrics.Instance.RecordAnchorDamage(amount); // DEĞİŞİKLİK: Anchor hasarı W1-01 debug metriklerine yazılır.
        PlayCoreHitFeedback(amount); // DEĞİŞİKLİK: Breach consequence Core üzerinde görünür flash/text/shake verir.
        RefreshCoreLabel();

        Debug.Log($"[AnchorCore] Hasar -{amount} | HP: {oldHP} → {_currentHP}");

        if (_currentHP <= 0)
            DestroyAnchor();

        return true;
    }

    /// <summary>
    /// Onarım gate'i veya özel event ile iyileşme.
    /// </summary>
    public void Heal(int amount)
    {
        if (_isDestroyed) return;

        _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        _hpBar?.UpdateBar(_currentHP);
        GameEvents.OnAnchorHPChanged?.Invoke(_currentHP, _maxHP);
        RefreshCoreLabel(); // DEĞİŞİKLİK: Repair sonrası Core label güncellenir.

        Debug.Log($"[AnchorCore] Heal +{amount} | HP: {_currentHP}/{_maxHP}");
    }

    void EnsureTempVisuals()
    {
        // DEĞİŞİKLİK: Final asset yokken savunulan Core objesi primitive placeholder ile okunur.
        if (_visualRoot != null) return;

        GameObject root = new GameObject("Temp_AnchorCoreVisual");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        _visualRoot = root.transform;

        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "CoreBase";
        baseObj.transform.SetParent(_visualRoot, false);
        baseObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        baseObj.transform.localScale = new Vector3(1.8f, 0.12f, 1.8f);
        Destroy(baseObj.GetComponent<Collider>());

        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "CoreOrb";
        coreObj.transform.SetParent(_visualRoot, false);
        coreObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        coreObj.transform.localScale = Vector3.one * 1.25f;
        Destroy(coreObj.GetComponent<Collider>());
        _coreRenderer = coreObj.GetComponent<Renderer>();
        if (_coreRenderer != null)
        {
            _coreRenderer.material = new Material(Shader.Find("Standard"));
            _coreRenderer.material.color = new Color(0.1f, 0.95f, 1f, 1f);
            _coreRenderer.material.EnableKeyword("_EMISSION");
            _coreRenderer.material.SetColor("_EmissionColor", new Color(0.05f, 0.55f, 0.65f));
        }

        GameObject labelObj = new GameObject("CoreLabel");
        labelObj.transform.SetParent(_visualRoot, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        _coreLabel = labelObj.AddComponent<TextMeshPro>();
        _coreLabel.alignment = TextAlignmentOptions.Center;
        _coreLabel.fontSize = 1.1f;
        _coreLabel.fontStyle = FontStyles.Bold;
        _coreLabel.color = Color.cyan;
        _coreLabel.outlineWidth = 0.18f;
        _coreLabel.outlineColor = Color.black;
    }

    public void SetTempVisualsVisible(bool visible)
    {
        // DEĞİŞİKLİK: Anchor dışına çıkınca Core placeholder runner ekranında kalmaz.
        if (_visualRoot != null)
            _visualRoot.gameObject.SetActive(visible);
    }

    void RefreshCoreLabel()
    {
        // DEĞİŞİKLİK: Core label savunulan hedefi ve HP'yi sahne içinde de gösterir.
        if (_coreLabel == null) return;
        _coreLabel.text = $"ANCHOR CORE\n{_currentHP}/{_maxHP}";
        if (Camera.main != null)
            _coreLabel.transform.rotation = Quaternion.LookRotation(_coreLabel.transform.position - Camera.main.transform.position);
    }

    void PlayCoreHitFeedback(int amount)
    {
        // DEĞİŞİKLİK: Enemy breach olduğunda oyuncu hatayı Core üzerinde görür.
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(CoreFlashCo());
        SpawnCoreHitText(amount);
        PlayCameraShake(); // DEĞİŞİKLİK: Shake artık camera follow üzerinden uygulanır, transform reset oluşturmaz.
    }

    IEnumerator CoreFlashCo()
    {
        if (_coreRenderer == null) yield break;
        Color baseColor = new Color(0.1f, 0.95f, 1f, 1f);
        _coreRenderer.material.color = new Color(1f, 0.12f, 0.08f, 1f);
        yield return new WaitForSeconds(0.12f);
        if (_coreRenderer != null)
            _coreRenderer.material.color = baseColor;
    }

    void SpawnCoreHitText(int amount)
    {
        // DEĞİŞİKLİK: Core hit feedback world-space TMP ile kısa süre görünür.
        GameObject go = new GameObject("CoreHitText");
        go.transform.position = transform.position + new Vector3(0f, 3.0f, 0f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = $"CORE HIT -{amount}";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 1.2f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.red;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        go.AddComponent<AnchorCoreHitText>();
    }

    void PlayCameraShake()
    {
        // DEĞİŞİKLİK: Hafif camera shake Core breach consequence'ını güçlendirir.
        if (Camera.main == null) return;
        SimpleCameraFollow follow = Camera.main.GetComponent<SimpleCameraFollow>();
        if (follow != null)
            follow.AddShake(0.08f, 0.16f);
    }

    // ── Yok Oluş ────────────────────────────────────────────────────────

    void DestroyAnchor()
    {
        if (_isDestroyed) return;

        _isDestroyed = true;
        _currentHP   = 0;

        Debug.Log("[AnchorCore] Anchor yıkıldı → Defeat");
        GameEvents.OnAnchorDestroyed?.Invoke();
        GameEvents.OnGameOver?.Invoke();
    }

    // ── Debug ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnGUI()
    {
        if (Instance == null) return;

        GUILayout.BeginArea(new Rect(10, 310, 260, 60));
        GUILayout.Label($"[AnchorCore]");
        GUILayout.Label($"HP: {_currentHP} / {_maxHP}  ({HPRatio * 100f:F0}%)");
        GUILayout.EndArea();
    }
#endif
}

/// <summary>
/// AnchorCore HP bar arayüzü.
/// Görsel HP bar scriptin implement etmesi gereken minimum kontrat.
/// Henüz görsel yoksa bu interface boş bir MonoBehaviour ile doldurulabilir.
/// </summary>
public interface AnchorHPBar
{
    void Init(int maxHP);
    void UpdateBar(int currentHP);
}

public class AnchorCoreHitText : MonoBehaviour
{
    float _startTime;
    const float DURATION = 1.0f;
    TextMeshPro _tmp;

    void Awake()
    {
        // DEĞİŞİKLİK: Core hit world-space text kendini fade ederek temizler.
        _startTime = Time.time;
        _tmp = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        float t = Mathf.Clamp01((Time.time - _startTime) / DURATION);
        transform.position += Vector3.up * (Time.deltaTime * 1.1f);
        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        if (_tmp != null)
        {
            Color c = _tmp.color;
            c.a = 1f - t;
            _tmp.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
