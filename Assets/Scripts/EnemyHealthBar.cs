using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top End War — Dusman HP Bari (Claude)
///
/// KURULUM:
///   Enemy prefab'ina bu scripti ekle.
///   Kod kendi HP barini olusturur — elle Canvas yapma.
///   HP bar: Dusman kafasinin 1.8 birim ustunde, kameray a bakar.
///
/// Enemy.TakeDamage() sonrasi UpdateBar() cagrilir.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Gorsel")]
    public float barWidth   = 1.2f;
    public float barHeight  = 0.15f;
    public float barYOffset = 1.8f;

    public Color fullColor  = new Color(0.15f, 0.85f, 0.15f); // Yesil
    public Color halfColor  = new Color(0.95f, 0.75f, 0.05f); // Sari
    public Color lowColor   = new Color(0.9f, 0.15f, 0.15f);  // Kirmizi

    // Internal
    Canvas    _canvas;
    Image     _bgImage;
    Image     _fillImage;
    int       _maxHP;
    int       _currentHP;
    Transform _camTransform;

    void Awake()
    {
        BuildBar();
        _camTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        // HP bari kameraya bak
        if (_canvas != null && _camTransform != null)
        {
            _canvas.transform.position = transform.position + Vector3.up * barYOffset;
            _canvas.transform.LookAt(
                _canvas.transform.position + _camTransform.forward);
        }
    }

    public void Init(int maxHP)
    {
        _maxHP    = Mathf.Max(1, maxHP);
        _currentHP = _maxHP;
        UpdateBar();
    }

    public void UpdateBar(int currentHP)
    {
        _currentHP = Mathf.Clamp(currentHP, 0, _maxHP);
        UpdateBar();
    }

    void UpdateBar()
    {
        if (_fillImage == null) return;

        float ratio = (float)_currentHP / _maxHP;
        _fillImage.fillAmount = ratio;

        // Renk gecisi
        if      (ratio > 0.6f) _fillImage.color = fullColor;
        else if (ratio > 0.3f) _fillImage.color = Color.Lerp(halfColor, fullColor, (ratio - 0.3f) / 0.3f);
        else                   _fillImage.color = Color.Lerp(lowColor, halfColor, ratio / 0.3f);

        // HP sifirsa bari gizle
        _canvas.gameObject.SetActive(_currentHP > 0);
    }

    void BuildBar()
    {
        // World Space Canvas
        GameObject canvasObj = new GameObject("HPBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * barYOffset;

        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode    = RenderMode.WorldSpace;
        _canvas.sortingOrder  = 10;
        _canvas.worldCamera   = Camera.main;

        RectTransform cr = canvasObj.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(barWidth, barHeight * 2f);

        // Arka plan (koyu)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _bgImage = bgObj.AddComponent<Image>();
        _bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin     = Vector2.zero;
        bgR.anchorMax     = Vector2.one;
        bgR.offsetMin     = Vector2.zero;
        bgR.offsetMax     = Vector2.zero;

        // Dolgu
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = 0;
        _fillImage.color = fullColor;
        RectTransform fillR = fillObj.GetComponent<RectTransform>();
        fillR.anchorMin   = Vector2.zero;
        fillR.anchorMax   = Vector2.one;
        fillR.offsetMin   = Vector2.zero;
        fillR.offsetMax   = Vector2.zero;
    }
}