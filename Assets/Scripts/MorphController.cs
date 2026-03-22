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

    [Header("Shader Renkleri (Commander_BiomeShader icin)")]
    [SerializeField] Renderer characterRenderer;

    // Tier renkleri (T1=gri → T5=altin)
    static readonly Color[] TIER_COLORS =
    {
        new Color(0.55f, 0.55f, 0.55f), // T1 Gri
        new Color(0.20f, 0.45f, 0.90f), // T2 Mavi
        new Color(0.90f, 0.50f, 0.10f), // T3 Turuncu
        new Color(0.65f, 0.10f, 0.90f), // T4 Mor
        new Color(1.00f, 0.80f, 0.10f), // T5 Altin
    };

    // Biyom renkleri (BiomeManager currentBiome ile eslenik)
    static readonly System.Collections.Generic.Dictionary<string, Color> BIOME_COLORS =
        new System.Collections.Generic.Dictionary<string, Color>
    {
        ["Tas"]   = new Color(0.55f, 0.52f, 0.48f),
        ["Orman"] = new Color(0.20f, 0.60f, 0.20f),
        ["Cul"]   = new Color(0.85f, 0.70f, 0.30f),
        ["Karli"] = new Color(0.80f, 0.90f, 1.00f),
        ["Tarim"] = new Color(0.60f, 0.80f, 0.30f),
    };

    MaterialPropertyBlock _propBlock;
    static readonly int TierColorID = Shader.PropertyToID("_TierColor");
    static readonly int BiomeTintID = Shader.PropertyToID("_BiomeTint");

    GameObject[] _spawnedModels;
    int          _currentTierIndex = -1;
    bool         _isMorphing       = false;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        PrewarmModels();
        GameEvents.OnTierChanged  += HandleTierChange;
        GameEvents.OnBiomeChanged += HandleBiomeChange;
        ActivateTier(0);
        RefreshShader(1, BiomeManager.Instance?.currentBiome ?? "Tas");
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged  -= HandleTierChange;
        GameEvents.OnBiomeChanged -= HandleBiomeChange;
    }

    // ── Shader Guncelleme ─────────────────────────────────────────────────
    /// <summary>Tier ve biyom degistiginde Commander_BiomeShader'i gunceller.</summary>
    public void RefreshShader(int tier, string biome)
    {
        if (characterRenderer == null) return;

        Color tc = TIER_COLORS[Mathf.Clamp(tier - 1, 0, TIER_COLORS.Length - 1)];
        Color bc = BIOME_COLORS.TryGetValue(biome, out Color found) ? found : Color.white;

        characterRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(TierColorID, tc);
        _propBlock.SetColor(BiomeTintID, bc);
        characterRenderer.SetPropertyBlock(_propBlock);
    }

    void HandleTierChange(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, _spawnedModels.Length - 1);
        RefreshShader(newTier, BiomeManager.Instance?.currentBiome ?? "Tas");
        if (index == _currentTierIndex || _isMorphing) return;
        StartCoroutine(MorphCoroutine(index));
    }

    void HandleBiomeChange(string biome)
    {
        RefreshShader(PlayerStats.Instance?.CurrentTier ?? 1, biome);
    }

    // ── Model Yonetimi ────────────────────────────────────────────────────
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

    IEnumerator MorphCoroutine(int targetIndex)
    {
        _isMorphing = true;

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
                    model.transform.DOScale(Vector3.one, popDuration * 0.5f)
                        .SetEase(Ease.InOutQuad);
            });

        _currentTierIndex = index;
    }
}