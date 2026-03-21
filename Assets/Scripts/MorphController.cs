using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Top End War — Tier Morph (Claude)
/// CRASH FIX: Destroy yerine SetActive. Tum modeller baslangicta olusturulur.
/// DOTween ile scale pop animasyonu.
/// Player objesine ekle. Tier prefablari 0=Tier1...4=Tier5.
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

    [Header("Visual Components")]
    [SerializeField] private Renderer characterRenderer; // Komutanın veya Askerin Mesh Renderer'ı
    
    // Performans için Property Block tanımlıyoruz
    private MaterialPropertyBlock _propBlock;

    // Shader referans ID'lerini (String yerine ID kullanmak çok daha hızlıdır) önbelleğe alıyoruz
    private static readonly int TierColorID = Shader.PropertyToID("_TierColor");
    private static readonly int BiomeTintID = Shader.PropertyToID("_BiomeTint");

    // Tum tier modelleri onceden olusturulur, sadece aktif/pasif yapilir
    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged += HandleTierChange;
        ActivateTier(0);
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= HandleTierChange;
    }

    void PrewarmModels()
    {
        int count = tierPrefabs != null ? tierPrefabs.Length : 5;
        _spawnedModels = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            GameObject model;
            if (tierPrefabs != null && i < tierPrefabs.Length && tierPrefabs[i] != null)
                model = Instantiate(tierPrefabs[i], transform);
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.transform.SetParent(transform);
                Destroy(model.GetComponent<Collider>());
            }

            model.transform.localPosition = Vector3.zero;
            model.transform.localScale    = Vector3.one;

            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Destroy(c);

            model.SetActive(false);
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

        // Mevcut modeli kucult
        if (_currentTierIndex >= 0 && _currentTierIndex < _spawnedModels.Length)
        {
            GameObject prev = _spawnedModels[_currentTierIndex];
            if (prev != null)
            {
                yield return prev.transform.DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(Ease.InBack).WaitForCompletion();
                prev.SetActive(false);
                prev.transform.localScale = Vector3.one;
            }
        }

        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

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

        model.transform.DOScale(Vector3.one * popPeak, popDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (model != null)
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f).SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
private void Awake()
    {
        // Bellek tahsisini oyun başında sadece bir kere yapıyoruz (Garbage Collector'ı yormamak için)
        _propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Karakterin görsel renklerini günceller. Tier atladığında veya Biyom değiştiğinde çağrılır.
    /// </summary>
    public void UpdateVisuals(Color tierColor, Color biomeTint)
    {
        if (characterRenderer == null) return;

        // 1. O anki render ayarlarını bloğa al
        characterRenderer.GetPropertyBlock(_propBlock);
        
        // 2. Yeni renkleri bloğa işle
        _propBlock.SetColor(TierColorID, tierColor);
        _propBlock.SetColor(BiomeTintID, biomeTint);
        
        // 3. Bloğu tekrar renderer'a geri ver (Materyal kopyalanmadan renk değişir!)
        characterRenderer.SetPropertyBlock(_propBlock);
    }

}
