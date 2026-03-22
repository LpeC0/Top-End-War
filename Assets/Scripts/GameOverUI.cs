using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Ekrani v2 (Claude)
///
/// v2: SaveManager entegrasyonu.
///   En iyi CP, en iyi mesafe, bu oyun kill sayisi gosterilir.
///   Yeni rekor varsa altin rengi vurgu yapar.
///
/// KURULUM:
///   Hierarchy -> Create Empty -> "GameOverManager" -> ekle -> bitti.
///   Kod kendi Canvas'ini olusturur.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Sahne Adi")]
    public string gameSceneName = "SampleScene";

    Canvas          _canvas;
    GameObject      _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _cpText;
    TextMeshProUGUI _distText;
    TextMeshProUGUI _killText;
    TextMeshProUGUI _bestText;
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

    void BuildUI()
    {
        // Canvas
        var canvasObj = new GameObject("GameOverCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasObj.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Arkaplan
        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        _panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        var pr = _panel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        // Baslik
        _titleText = MakeText(_panel, "SAVAS BITTI",   new Vector2(0.5f,0.5f), new Vector2(0, 130), 52, Color.red, FontStyles.Bold);

        // Mevcut oyun sonuclari
        _cpText   = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0,  60), 32, Color.white,           FontStyles.Normal);
        _distText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0,  15), 28, new Color(0.8f,0.8f,1f), FontStyles.Normal);
        _killText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0, -25), 24, new Color(1f,0.7f,0.3f), FontStyles.Normal);

        // En iyi skor
        _bestText = MakeText(_panel, "", new Vector2(0.5f,0.5f), new Vector2(0, -70), 22, new Color(0.6f,0.6f,0.6f), FontStyles.Normal);

        // Butonlar
        MakeButton(_panel, "TEKRAR DENE", new Vector2(0,-130), new Vector2(260,60),
            new Color(0.2f,0.8f,0.2f), () => { Time.timeScale = 1f; SceneManager.LoadScene(gameSceneName); });

        // Gelecekte: Ana Menü butonu buraya gelecek
        // MakeButton(_panel, "ANA MENU", new Vector2(0,-210), new Vector2(260,60),
        //     new Color(0.3f,0.3f,0.8f), () => SceneManager.LoadScene("MainMenu"));

        _panel.SetActive(false);
    }

    void ShowGameOver()
    {
        if (_shown) return;
        _shown = true;

        Time.timeScale = 0f;
        _panel.SetActive(true);

        var ps   = PlayerStats.Instance;
        var save = SaveManager.Instance;
        var army = ArmyManager.Instance;

        int   cp      = ps?.CP ?? 0;
        float dist    = ps != null ? Mathf.RoundToInt(ps.transform.position.z) : 0f;
        int   kills   = save?.CurrentRunKills ?? 0;
        int   soldiers= army?.SoldierCount ?? 0;

        _cpText.text   = $"CP: {cp:N0}";
        _distText.text = $"Mesafe: {dist:N0}m  |  Asker: {soldiers}/20";
        _killText.text = $"Dusmanlar: {kills}";

        // En iyi skor goster
        if (save != null)
        {
            bool newCP   = cp   >= save.HighScoreCP   && save.TotalRuns > 1;
            bool newDist = dist >= save.HighScoreDistance && save.TotalRuns > 1;

            string bestStr = $"En iyi: {save.HighScoreCP:N0} CP  |  {save.HighScoreDistance:N0}m";
            _bestText.text  = bestStr;

            // Yeni rekor vurgusu
            if (newCP || newDist)
            {
                _bestText.text  = "YENİ REKOR!";
                _bestText.color = new Color(1f, 0.85f, 0f);
                _cpText.color   = new Color(1f, 0.85f, 0f);
            }
        }
    }

    // ── Yardimci ─────────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor,
        Vector2 pos, float size, Color color, FontStyles style)
    {
        var obj = new GameObject("T_" + text.Substring(0, Mathf.Min(6, text.Length)));
        obj.transform.SetParent(parent.transform, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(700, 60);
        return tmp;
    }

    void MakeButton(GameObject parent, string label, Vector2 pos, Vector2 size,
        Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var btn = new GameObject("Btn_" + label);
        btn.transform.SetParent(parent.transform, false);
        var img = btn.AddComponent<Image>(); img.color = bg;
        var b = btn.AddComponent<Button>(); b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f,0.5f); r.anchorMax = new Vector2(0.5f,0.5f);
        r.pivot = new Vector2(0.5f,0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(btn.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 24; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }
}