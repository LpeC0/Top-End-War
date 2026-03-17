using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Kapi Gecis Efekti (Claude)
/// Player objesine ekle. DOTween kurulu olmali.
///
/// Kapidan gecince: kucuk scale pop (1 → 1.25 → 1)
/// Tier atlayinca: buyuk scale pop (1 → 1.5 → 1) + kamera sallama
/// </summary>
public class GateFeedback : MonoBehaviour
{
    [Header("Gate Gecis")]
    public float gatePopDuration = 0.25f;
    public float gatePopScale    = 1.25f;

    [Header("Tier Atlama")]
    public float tierPopDuration = 0.4f;
    public float tierPopScale    = 1.5f;

    [Header("Kamera Sallama")]
    public Camera mainCamera;
    public float  shakeStrength = 0.3f;
    public float  shakeDuration = 0.3f;

    Vector3 _originalScale;
    Tweener _activeTween;

    void Start()
    {
        _originalScale = transform.localScale;

        GameEvents.OnCPUpdated   += OnCPUpdated;
        GameEvents.OnTierChanged += OnTierChanged;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated   -= OnCPUpdated;
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnCPUpdated(int cp)
    {
        // Her kapi gecisinde kucuk pop
        ScalePop(gatePopScale, gatePopDuration);
    }

    void OnTierChanged(int tier)
    {
        // Tier atlarken buyuk pop + kamera shake
        ScalePop(tierPopScale, tierPopDuration);

        if (mainCamera != null)
            mainCamera.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false);
    }

    void ScalePop(float peak, float duration)
    {
        _activeTween?.Kill();
        transform.localScale = _originalScale;

        _activeTween = transform
            .DOScale(_originalScale * peak, duration * 0.4f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(_originalScale, duration * 0.6f)
                         .SetEase(Ease.InOutQuad);
            });
    }
}
