using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Pet Sistemi (Claude)
///
/// UNITY KURULUM:
///   Player objesine ekle.
///   Inspector'da petData slotuna PetData ScriptableObject sur.
///   petData.petPrefab doluysa onu kullanir, yoksa kucuk altin kure.
///
/// DAVRANIS:
///   - Normal: Karakterin sol-arkasindan smooth takip eder
///   - Anchor modu: Sabit kalir, "Hasar Azaltma" aura aktif olur
///     (PetData.anchorDamageReduction PlayerStats'a uygulanir)
///
/// GELECEK (ileride):
///   - Her pet tipine ozel efekt (heal aura, ates hizi, sekici mermi)
///   - Ana menu'den secilen pet buraya inject edilir
/// </summary>
public class PetController : MonoBehaviour
{
    [Header("Pet Verisi (PlayerStats'tan otomatik alinabilir)")]
    public PetData petData;

    [Header("Takip Ayarlari")]
    public float followSpeed     = 8f;
    public float followDistance  = 2.2f;  // sol-arkayi mesafesi
    public float sideOffset      = 1.4f;  // saga/sola offset

    [Header("Ziplama (idle animasyon)")]
    public float bobHeight       = 0.18f;
    public float bobSpeed        = 2.2f;

    GameObject _petModel;
    bool       _anchorMode  = false;
    bool       _auraActive  = false;
    float      _bobTimer    = 0f;
    Vector3    _baseOffset;

    // Anchor DR degeri — TakeContactDamage'a carpilir
    float _currentDR = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // PlayerStats'ta equippedPet varsa onu al
        if (petData == null && PlayerStats.Instance?.equippedPet != null)
            petData = PlayerStats.Instance.equippedPet;

        _baseOffset = new Vector3(-sideOffset, 1.2f, -followDistance);

        SpawnPetModel();
        GameEvents.OnAnchorModeChanged += OnAnchorMode;

        Debug.Log($"[Pet] {(petData != null ? petData.petName : "Varsayilan Pet")} aktif.");
    }

    void OnDestroy()
    {
        GameEvents.OnAnchorModeChanged -= OnAnchorMode;
        DeactivateAura();
    }

    // ── Model Olustur ─────────────────────────────────────────────────────
    void SpawnPetModel()
    {
        if (_petModel != null) Destroy(_petModel);

        if (petData != null && petData.petPrefab != null)
        {
            _petModel = Instantiate(petData.petPrefab);
        }
        else
        {
            // Fallback: altin kure
            _petModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _petModel.transform.localScale = Vector3.one * 0.35f;
            Destroy(_petModel.GetComponent<Collider>());

            var rend = _petModel.GetComponent<Renderer>();
            if (rend != null)
            {
                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", new Color(1f, 0.85f, 0.1f));
                else
                    rend.material.color = new Color(1f, 0.85f, 0.1f);
            }
        }
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (_petModel == null || PlayerStats.Instance == null) return;

        if (_anchorMode)
        {
            // Anchor modda sabit kal, hafifce titres
            _bobTimer += Time.deltaTime * bobSpeed * 2f;
            _petModel.transform.position = transform.position
                + _baseOffset
                + Vector3.up * Mathf.Sin(_bobTimer) * bobHeight * 0.5f;
        }
        else
        {
            // Runner modda smooth takip
            _bobTimer += Time.deltaTime * bobSpeed;
            Vector3 target = transform.position + _baseOffset
                + Vector3.up * Mathf.Sin(_bobTimer) * bobHeight;

            _petModel.transform.position = Vector3.Lerp(
                _petModel.transform.position, target,
                Time.deltaTime * followSpeed);
        }

        // Pet oyuncuya baksın
        Vector3 lookDir = (transform.position - _petModel.transform.position);
        if (lookDir != Vector3.zero)
            _petModel.transform.rotation = Quaternion.Slerp(
                _petModel.transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 8f);
    }

    // ── Anchor Modu ───────────────────────────────────────────────────────
    void OnAnchorMode(bool active)
    {
        _anchorMode = active;

        if (active && petData != null && petData.anchorDamageReduction > 0f)
        {
            ActivateAura();
        }
        else
        {
            DeactivateAura();
        }
    }

    void ActivateAura()
    {
        if (_auraActive) return;
        _auraActive  = true;
        _currentDR   = petData?.anchorDamageReduction ?? 0f;

        // Parlama efekti
        var rend = _petModel?.GetComponentInChildren<Renderer>();
        if (rend != null)
            _petModel.transform.DOScale(Vector3.one * 1.35f, 0.3f).SetEase(Ease.OutBack);

        Debug.Log($"[Pet] Aura aktif — Hasar Azaltma: %{_currentDR * 100:.0f}");
        GameEvents.OnSynergyFound?.Invoke($"Pet Aurası +%{Mathf.RoundToInt(_currentDR * 100)}");
    }

    void DeactivateAura()
    {
        if (!_auraActive) return;
        _auraActive = false;
        _currentDR  = 0f;

        if (_petModel != null)
            _petModel.transform.DOScale(Vector3.one, 0.2f);
    }

    // ── DR Getter (PlayerStats.TakeContactDamage'dan kullanilabilir) ──────
    /// <summary>
    /// Hasar azaltma carpani. 0 = hasar azaltma yok, 0.1 = %10 azaltir.
    /// PlayerStats.TakeContactDamage icinde:
    ///   float dr = PetController.Instance?.DamageReduction ?? 0f;
    ///   int final = Mathf.RoundToInt(amount * (1f - dr));
    /// </summary>
    public static PetController Instance { get; private set; }
    public float DamageReduction => _currentDR;

    void Awake()
    {
        // Singleton (bir pet olacak)
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
}