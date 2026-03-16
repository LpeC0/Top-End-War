using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Ekrani (Claude)
///
/// KURULUM (super basit):
///   Hierarchy'de bos bir obje olustur → adi "GameOverManager"
///   Bu scripti ekle → bitti.
///   Baska HIC BIR SEY yapma, kod kendi Canvas'ini olusturur.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Sahne Adi")]
    public string gameSceneName = "SampleScene";

    // Olusturulan UI referanslari
    Canvas         _canvas;
    GameObject     _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _cpText;
    TextMeshProUGUI _distText;
    bool            _shown = false;

    void Start()
    {
        BuildUI();
        GameEvents.OnGameOver += ShowGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver -= ShowGameOver;
    }

    // ── UI'yi programatik olustur ─────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("GameOverCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99; // Her seyin ustunde
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Koyu arka plan paneli
        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Baslik
        _titleText = CreateText(_panel, "SAVAS BITTI",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 80f),
            52, Color.red, FontStyles.Bold);

        // CP
        _cpText = CreateText(_panel, "",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            32, Color.white, FontStyles.Normal);

        // Mesafe
        _distText = CreateText(_panel, "",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -35f),
            28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        // Tekrar Dene butonu
        CreateButton(_panel, "TEKRAR DENE",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -100f),
            new Vector2(260f, 60f),
            new Color(0.2f, 0.8f, 0.2f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene(gameSceneName); });

        // Panel baslangicta gizli
        _panel.SetActive(false);
    }

    // ── Game Over tetiklenince ─────────────────────────────────────────────────
    void ShowGameOver()
    {
        if (_shown) return;
        _shown = true;

        Time.timeScale = 0f;
        _panel.SetActive(true);

        if (_cpText != null && PlayerStats.Instance != null)
            _cpText.text = "Son CP: " + PlayerStats.Instance.CP.ToString("N0");

        if (_distText != null && PlayerStats.Instance != null)
            _distText.text = "Mesafe: " + Mathf.RoundToInt(PlayerStats.Instance.transform.position.z) + "m";
    }

    // ── Yardimci: Text olustur ────────────────────────────────────────────────
    TextMeshProUGUI CreateText(GameObject parent, string text,
        Vector2 anchor, Vector2 anchoredPos,
        float fontSize, Color color, FontStyles style)
    {
        GameObject obj = new GameObject("Text_" + text.Substring(0, Mathf.Min(6, text.Length)));
        obj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(500f, 60f);

        return tmp;
    }

    // ── Yardimci: Button olustur ──────────────────────────────────────────────
    void CreateButton(GameObject parent, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        // Arka plan
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        // Yazi
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 24f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin      = Vector2.zero;
        tr.anchorMax      = Vector2.one;
        tr.offsetMin      = Vector2.zero;
        tr.offsetMax      = Vector2.zero;
    }
}