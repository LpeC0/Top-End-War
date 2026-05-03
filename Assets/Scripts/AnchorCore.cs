using UnityEngine;

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

        Debug.Log($"[AnchorCore] Heal +{amount} | HP: {_currentHP}/{_maxHP}");
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