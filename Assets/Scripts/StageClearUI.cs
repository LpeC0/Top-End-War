using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageClearUI : MonoBehaviour
{
    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    Canvas _canvas;
    GameObject _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _rewardText;
    Button _continueButton;
    Button _retryButton;
    Button _upgradeButton;
    Button _mainMenuButton;

    bool _visible;
    GameEvents.StageClearInfo _lastInfo;

    void OnEnable()
    {
        GameEvents.OnStageCleared += HandleStageCleared;
    }

    void OnDisable()
    {
        GameEvents.OnStageCleared -= HandleStageCleared;
    }

    void Start()
    {
        BuildUIIfNeeded();
        Hide();
    }

    void Update()
    {
        if (_visible)
            Time.timeScale = 0f;
    }

    void HandleStageCleared(GameEvents.StageClearInfo info)
    {
        _lastInfo = info;
        BuildUIIfNeeded();
        Show(info);
    }

    void Show(GameEvents.StageClearInfo info)
    {
        _visible = true;
        if (_panel != null) _panel.SetActive(true);

        if (_titleText != null)
            _titleText.text = info.worldCleared ? "WORLD CLEAR" : "STAGE CLEAR";

        if (_rewardText != null)
        {
            string nextText = info.hasNextStage ? "Continue ready" : "Run complete";
            _rewardText.text = $"{info.stageName}\nGold +{info.goldReward}\n{nextText}";
        }

        if (_continueButton != null)
            _continueButton.gameObject.SetActive(info.hasNextStage);

        Time.timeScale = 0f;
    }

    void Hide()
    {
        _visible = false;
        if (_panel != null) _panel.SetActive(false);
    }

    void BuildUIIfNeeded()
    {
        if (_panel != null) return;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            var canvasObj = new GameObject("StageClearCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 95;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        _panel = new GameObject("StageClearPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.10f, 0.95f);
        Stretch(_panel.GetComponent<RectTransform>());

        _titleText = MakeText(_panel, "STAGE CLEAR", new Vector2(0.5f, 0.78f), 64, new Color(1f, 0.85f, 0.15f), FontStyles.Bold);
        _rewardText = MakeText(_panel, "Reward", new Vector2(0.5f, 0.62f), 30, Color.white, FontStyles.Normal);

        _continueButton = MakeButton(_panel, "CONTINUE", new Vector2(0.5f, 0.38f), new Vector2(420, 96), new Color(0.15f, 0.75f, 0.25f), OnContinueClicked);
        _retryButton = MakeButton(_panel, "RETRY", new Vector2(0.5f, 0.28f), new Vector2(420, 88), new Color(0.55f, 0.20f, 0.20f), OnRetryClicked);
        _upgradeButton = MakeButton(_panel, "UPGRADE", new Vector2(0.5f, 0.18f), new Vector2(420, 88), new Color(0.20f, 0.35f, 0.65f), OnUpgradeClicked);
        _mainMenuButton = MakeButton(_panel, "MAIN MENU", new Vector2(0.5f, 0.08f), new Vector2(420, 80), new Color(0.25f, 0.25f, 0.35f), OnMainMenuClicked);
    }

    void OnContinueClicked()
    {
        Time.timeScale = 1f;
        Hide();
        StageManager.Instance?.ContinueAfterStageClear();
    }

    void OnRetryClicked()
    {
        Time.timeScale = 1f;
        Hide();
        StageManager.Instance?.RestartCurrentStage();
    }

    void OnUpgradeClicked()
    {
        EquipmentUI equipment = Object.FindAnyObjectByType<EquipmentUI>();
        if (equipment != null)
        {
            equipment.Toggle();
            Time.timeScale = 0f;
        }
    }

    void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        Hide();
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    TextMeshProUGUI MakeText(GameObject parent, string text, Vector2 anchor, float size, Color color, FontStyles style)
    {
        var go = new GameObject("Text_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900, 120);
        return tmp;
    }

    Button MakeButton(GameObject parent, string label, Vector2 anchor, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = size.y * 0.35f;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;

        return btn;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
