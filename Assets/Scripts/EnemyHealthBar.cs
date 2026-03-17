using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top End War — Dusman HP Bari (Claude)
/// Enemy prefabina ekle — veya Enemy.Awake() otomatik ekler.
/// Kod kendi Canvas'ini olusturur, elle kurulum yok.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public float barWidth   = 1.2f;
    public float barHeight  = 0.15f;
    public float barYOffset = 1.8f;

    public Color fullColor = new Color(0.15f, 0.85f, 0.15f);
    public Color halfColor = new Color(0.95f, 0.75f, 0.05f);
    public Color lowColor  = new Color(0.9f,  0.15f, 0.15f);

    Canvas    _canvas;
    Image     _fillImage;
    int       _maxHP;
    Transform _camTransform;

    void Awake()
    {
        BuildBar();
        _camTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        if (_canvas == null || _camTransform == null) return;
        _canvas.transform.position = transform.position + Vector3.up * barYOffset;
        _canvas.transform.LookAt(_canvas.transform.position + _camTransform.forward);
    }

    public void Init(int maxHP)
    {
        _maxHP = Mathf.Max(1, maxHP);
        UpdateBar(maxHP);
    }

    public void UpdateBar(int currentHP)
    {
        if (_fillImage == null) return;
        float ratio = (float)Mathf.Max(0, currentHP) / _maxHP;
        _fillImage.fillAmount = ratio;

        if      (ratio > 0.6f) _fillImage.color = fullColor;
        else if (ratio > 0.3f) _fillImage.color = Color.Lerp(halfColor, fullColor, (ratio - 0.3f) / 0.3f);
        else                   _fillImage.color = Color.Lerp(lowColor,  halfColor,  ratio / 0.3f);

        if (_canvas != null) _canvas.gameObject.SetActive(currentHP > 0);
    }

    void BuildBar()
    {
        GameObject cObj = new GameObject("HPBarCanvas");
        cObj.transform.SetParent(transform);
        cObj.transform.localPosition = Vector3.up * barYOffset;

        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 10;

        RectTransform cr = cObj.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(barWidth, barHeight * 2f);

        // Arka plan
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(cObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;

        // Dolucu
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(cObj.transform, false);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.color = fullColor;
        RectTransform fillR = fillObj.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one;
        fillR.offsetMin = Vector2.zero; fillR.offsetMax = Vector2.zero;
    }
}
