using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Top End War — Tier Morph v3 (Claude)
/// HATA DUZELTME: _currentModel Destroy edilince coroutine eski
/// referansi tutuyor ve SetActive crash yapiyordu.
/// Cozum: Destroy yerine SetActive(false) — model havuzu gibi calisir.
/// </summary>
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefablari (0=Tier1 .. 4=Tier5)")]
    public GameObject[] tierPrefabs;

    [Header("VFX")]
    public GameObject morphParticlePrefab;

    [Header("Animasyon")]
    public float shrinkDuration = 0.15f;
    public float popDuration    = 0.35f;
    public float popPeak        = 1.35f;

    // Tum tier modelleri onceden olusturulur, sadece aktif/pasif yapilir
    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged += HandleTierChange;
        ActivateTier(0); // Tier 1 ile basla
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= HandleTierChange;
    }

    /// <summary>Tum tier modellerini baslangicta olustur, hepsini gizle.</summary>
    void PrewarmModels()
    {
        int count = tierPrefabs != null ? tierPrefabs.Length : 0;
        if (count == 0) count = 5; // Placeholder

        _spawnedModels = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            GameObject model;

            if (tierPrefabs != null && i < tierPrefabs.Length && tierPrefabs[i] != null)
            {
                model = Instantiate(tierPrefabs[i], transform);
            }
            else
            {
                // Placeholder: Capsule
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.transform.SetParent(transform);
                Destroy(model.GetComponent<Collider>());
            }

            model.transform.localPosition = Vector3.zero;
            model.transform.localScale    = Vector3.one;

            // Alt collider'lari kaldir
            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Destroy(c);

            model.SetActive(false); // Hepsi baslangicta gizli
            _spawnedModels[i] = model;
        }
    }

    void HandleTierChange(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, _spawnedModels.Length - 1);
        if (index == _currentTierIndex || _isMorphing) return;
        StartCoroutine(MorphCoroutine(index));
    }

    IEnumerator MorphCoroutine(int targetIndex)
    {
        _isMorphing = true;

        // Mevcut modeli kucult ve gizle
        if (_currentTierIndex >= 0 && _currentTierIndex < _spawnedModels.Length)
        {
            GameObject prev = _spawnedModels[_currentTierIndex];
            if (prev != null)
            {
                // DOTween ile kucult
                yield return prev.transform
                    .DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(Ease.InBack)
                    .WaitForCompletion();

                prev.SetActive(false);
                prev.transform.localScale = Vector3.one; // Sonraki kullanim icin sifirla
            }
        }

        // Parcacik
        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

        // Yeni modeli goster ve pop
        ActivateTier(targetIndex);

        _isMorphing = false;
    }

    void ActivateTier(int index)
    {
        if (_spawnedModels == null || index >= _spawnedModels.Length) return;

        GameObject model = _spawnedModels[index];
        if (model == null) return;

        model.transform.localScale = Vector3.zero;
        model.SetActive(true);

        // Pop animasyonu
        model.transform
            .DOScale(Vector3.one * popPeak, popDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (model != null) // Null kontrol
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f).SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
}