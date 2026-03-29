using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Ana Menü (Claude)
///
/// UNITY KURULUM:
///   1. File → New Scene → "MainMenu" olarak kaydet
///   2. Hierarchy → Create Empty → "MainMenuManager" → bu scripti ekle
///   3. Build Settings'e MainMenu sahnesini 0. sıraya, SampleScene'i 1. sıraya ekle
///   4. gameSceneName = "SampleScene"
///
/// NE GÖSTERIR:
///   - Oyun adı + slogan
///   - En iyi skor + mesafe
///   - Toplam run sayısı
///   - OYNA butonu
///   - Ekipman butonu (EquipmentUI'yi açar — aynı sahnede)
///   - Sıfırla butonu (debug)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Sahne")]
    public string gameSceneName = "SampleScene";

    [Header("Arkaplan Rengi")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.12f);

    Canvas          _canvas;
    TextMeshProUGUI _bestCPText;
    TextMeshProUGUI _bestDistText;
    TextMeshProUGUI _totalRunsText;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        Camera.main.backgroundColor = bgColor;
        Camera.main.clearFlags      = CameraClearFlags.SolidColor;
        BuildUI();
        RefreshStats();
    }

    void BuildUI()
    {
        // Canvas
        var cObj = new GameObject("MainMenuCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)cObj.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);
        cObj.AddComponent<GraphicRaycaster>();

        // Arkaplan
        var bg = new GameObject("BG");
        bg.transform.SetParent(_canvas.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        Stretch(bg.GetComponent<RectTransform>());

        // Oyun adı
        MakeText(_canvas.gameObject, "TOP END WAR",
            new Vector2(0.5f, 1f), new Vector2(0, -160),
            72, new Color(1f, 0.85f, 0.1f), FontStyles.Bold);

        MakeText(_canvas.gameObject, "Sivas'ı Ele Geçir",
            new Vector2(0.5f, 1f), new Vector2(0, -250),
            30, new Color(0.7f, 0.7f, 0.9f), FontStyles.Italic);

        // İstatistikler
        _bestCPText    = MakeText(_canvas.gameObject, "En iyi CP: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 200),
            32, Color.white, FontStyles.Normal);

        _bestDistText  = MakeText(_canvas.gameObject, "En iyi Mesafe: —",
            new Vector2(0.5f, 0.5f), new Vector2(0, 155),
            28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        _totalRunsText = MakeText(_canvas.gameObject, "Toplam Sefer: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0, 110),
            24, new Color(0.6f, 0.6f, 0.6f), FontStyles.Normal);

        // OYNA butonu
        MakeButton(_canvas.gameObject, "OYNA",
            new Vector2(0.5f, 0.5f), new Vector2(0, -30),
            new Vector2(400, 110),
            new Color(0.15f, 0.75f, 0.25f),
            () => SceneManager.LoadScene(gameSceneName));

        // SIFIRLA butonu (debug — küçük, köşede)
        MakeButton(_canvas.gameObject, "Skoru Sifirla",
            new Vector2(0f, 0f), new Vector2(130, 60),
            new Vector2(220, 55),
            new Color(0.4f, 0.1f, 0.1f, 0.7f),
            () =>
            {
                SaveManager.Instance?.ResetAll();
                RefreshStats();
            });

        // Versiyon
        MakeText(_canvas.gameObject, "v0.4 — Top End War",
            new Vector2(1f, 0f), new Vector2(-80, 35),
            18, new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);
    }

    void RefreshStats()
    {
        var save = SaveManager.Instance;
        if (save == null) return;

        _bestCPText.text    = $"En iyi CP: {save.HighScoreCP:N0}";
        _bestDistText.text  = $"En iyi Mesafe: {save.HighScoreDistance:N0}m";
        _totalRunsText.text = $"Toplam Sefer: {save.TotalRuns}";
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("T_" + text.Substring(0, Mathf.Min(8, text.Length)));
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(900, 80);
        return t;
    }

    void MakeButton(GameObject parent, string label, Vector2 anchor,
        Vector2 pos, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent.transform, false);
        var img = obj.AddComponent<Image>(); img.color = bg;
        var btn = obj.AddComponent<Button>(); btn.targetGraphic = img;

        // Hover rengi
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f;
        cols.pressedColor     = bg * 0.7f;
        btn.colors = cols;
        btn.onClick.AddListener(onClick);

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(obj.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = size.y * 0.35f;
        t.color = Color.white; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}