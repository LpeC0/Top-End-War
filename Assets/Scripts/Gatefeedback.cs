using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Kapi Gecis Efekti (Claude + DOTween)
///
/// Player objesine ekle.
/// Kapidan gecince: kisa scale pop (0.8→1.3→1.0) — "hissettiren" morph ani.
/// Tier atlayinca: daha buyuk pop + kamera shake.
///
/// DOTween kurulu olmali (Package Manager'dan).
/// </summary>
public class GateFeedback : MonoBehaviour
{
    [Header("Gate Gecis")]
    public float gatePopDuration = 0.25f;
    public float gatePopScale    = 1.25f;

    [Header("Tier Atlama")]
    public float tierPopDuration = 0.4f;
    public float tierPopScale    = 1.5f;

    [Header("Kamera Sallama (Tier)")]
    public Camera mainCamera;
    public float  shakeStrength  = 0.3f;
    public float  shakeDuration  = 0.3f;

    Vector3 _originalScale;
    Tweener _currentTween;

    void Start()
    {
        _originalScale = transform.localScale;

        GameEvents.OnCPUpdated   += OnCPUpdated;
        GameEvents.OnTierChanged += OnTierChanged;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated   -= OnCPUpdated;
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnCPUpdated(int cp)
    {
        // Her kapida kucuk pop
        ScalePop(gatePopScale, gatePopDuration);
    }

    void OnTierChanged(int tier)
    {
        // Tier atlarken daha buyuk pop + kamera shake
        ScalePop(tierPopScale, tierPopDuration);

        if (mainCamera != null)
            mainCamera.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false);
    }

    void ScalePop(float peak, float duration)
    {
        _currentTween?.Kill();

        transform.localScale = _originalScale;

        _currentTween = transform
            .DOScale(_originalScale * peak, duration * 0.4f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(_originalScale, duration * 0.6f)
                         .SetEase(Ease.InOutQuad);
            });
    }
}