using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Tier Gorsel Evrimi v1 (Claude)
///
/// Tier atladikca boyut DEGISMEZ (eski morph sistemi).
/// Bunun yerine:
///   - Aktif model degisir (CommanderData.tierModels[tier-1])
///   - Aura degisir      (CommanderData.tierAuras[tier-1])
///   - Mermi VFX rengi degisir
///   - Tier-up mini event: DOTween scale punch + kisa slow-mo
///
/// KURULUM:
///   Player objesine ekle.
///   CommanderData SO'daki tierModels ve tierAuras dizilerini doldur
///   (bos birakilabilir — dizi yoksa sadece event tetiklenir).
/// </summary>
public class TierVisualizer : MonoBehaviour
{
    [Header("Baglanti (opsiyonel — CommanderData'dan da okunur)")]
    [Tooltip("Bos birakılırsa PlayerStats.activeCommander'dan alinir")]
    public CommanderData commanderOverride;

    [Header("Tier-Up Event Ayarlari")]
    [Tooltip("Scale punch siddeti")]
    public float punchStrength  = 0.25f;
    [Tooltip("Scale punch suresi (saniye)")]
    public float punchDuration  = 0.4f;
    [Tooltip("Slow-motion carpani (0.3 = %30 hiz)")]
    public float slowMoScale    = 0.3f;
    [Tooltip("Slow-motion suresi (saniye, gercek zaman)")]
    public float slowMoDuration = 0.5f;

    // ── Dahili ────────────────────────────────────────────────────────────
    int              _currentTier     = 0;
    CommanderData    _commander;
    ParticleSystem   _activeAura;

    void Start()
    {
        _commander = commanderOverride != null
            ? commanderOverride
            : PlayerStats.Instance?.activeCommander;

        GameEvents.OnTierChanged += OnTierChanged;

        // Baslangic tier'ini uygula (animasyonsuz)
        int startTier = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentTier : 1;
        ApplyTierVisuals(startTier, animated: false);
    }

    void OnDestroy() => GameEvents.OnTierChanged -= OnTierChanged;

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged(int newTier)
    {
        if (newTier <= _currentTier) return;   // Sadece yukari tier
        _currentTier = newTier;
        ApplyTierVisuals(newTier, animated: true);
    }

    void ApplyTierVisuals(int tier, bool animated)
    {
        _currentTier = tier;
        int idx      = Mathf.Clamp(tier - 1, 0, 4);

        // ── Model degisimi ────────────────────────────────────────────────
        if (_commander != null && _commander.tierModels != null &&
            _commander.tierModels.Length > 0)
        {
            for (int i = 0; i < _commander.tierModels.Length; i++)
            {
                if (_commander.tierModels[i] != null)
                    _commander.tierModels[i].SetActive(i == idx);
            }
        }

        // ── Aura degisimi ─────────────────────────────────────────────────
        if (_commander != null && _commander.tierAuras != null &&
            _commander.tierAuras.Length > 0)
        {
            // Onceki aurayi durdur
            _activeAura?.Stop(withChildren: true);

            if (idx < _commander.tierAuras.Length && _commander.tierAuras[idx] != null)
            {
                _activeAura = _commander.tierAuras[idx];
                _activeAura.Play();
            }
        }

        // ── Tier-up animasyon (sadece ilk kez atlandikta) ─────────────────
        if (animated) TierUpEvent();
    }

    // ── Tier-Up Mini Event ────────────────────────────────────────────────
    void TierUpEvent()
    {
        // Scale punch (DOTween)
        transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 6, 0.5f);

        // Kisa slow-motion (gercek zamanda geri doner)
        if (slowMoScale > 0f && slowMoDuration > 0f)
            SlowMo();

        Debug.Log($"[TierVisualizer] Tier {_currentTier} evrimi!");
    }

    void SlowMo()
    {
        Time.timeScale = slowMoScale;
        // UnscaledTime ile geri yukle
        DOVirtual.DelayedCall(slowMoDuration, ResetTimeScale, ignoreTimeScale: true);
    }

    static void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    // ── Getter ────────────────────────────────────────────────────────────
    public int CurrentTier => _currentTier;
}