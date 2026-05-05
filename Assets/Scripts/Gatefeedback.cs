using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Kapi Gecis Efekti v2
/// Player objesine ekle. Coroutine ile calisir (DOTween'e gerek yok).
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
    public float  shakeStrength = 0.15f;
    public float  shakeDuration = 0.2f;

    Vector3 _originalScale;
    Coroutine _scaleRoutine;

    void Start()
    {
        _originalScale = transform.localScale;
        if (mainCamera == null) mainCamera = Camera.main;

        GameEvents.OnTierChanged += OnTierChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    public void PlayGatePop()
    {
        StartScalePop(gatePopScale, gatePopDuration);
    }

    public void PlayTierPop()
    {
        StartScalePop(tierPopScale, tierPopDuration);
        if (mainCamera != null)
        {
            // DEĞİŞİKLİK: Tier/gate feedback kamerayı eski localPosition'a döndürmez; follow shake kullanır.
            SimpleCameraFollow follow = mainCamera.GetComponent<SimpleCameraFollow>();
            if (follow != null)
                follow.AddShake(shakeStrength, shakeDuration);
        }
    }

    void OnTierChanged(int tier)
    {
        PlayTierPop();
    }

    void StartScalePop(float peak, float duration)
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScalePopRoutine(peak, duration));
    }

    IEnumerator ScalePopRoutine(float peak, float duration)
    {
        float upTime = duration * 0.4f;
        float downTime = duration * 0.6f;

        transform.localScale = _originalScale;
        Vector3 peakScale = _originalScale * peak;

        float t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / upTime);
            transform.localScale = Vector3.Lerp(_originalScale, peakScale, k);
            yield return null;
        }

        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / downTime);
            transform.localScale = Vector3.Lerp(peakScale, _originalScale, k);
            yield return null;
        }

        transform.localScale = _originalScale;
    }

}
