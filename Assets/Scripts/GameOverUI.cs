using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Arayuzu v4 (Claude)
///
/// v3 → v4 Bootstrap Patch:
///   • mainMenuSceneName alani eklendi; OnMainMenuClicked artik sahneyi yukler.
///   • ShowGameOver → Time.timeScale = 0f  (oyun durur, UI calisir)
///   • Inspector referanslari null ise BuildFallbackPanel() otomatik cagrilir.
///     Tasarimci gercek paneli baglayinca fallback kodu hic calismaz.
///   • UpdateScoreDisplay: SaveManager null ise RunState.Instance.KillCount fallback.
///   • Revive sonrasi oyun resume edilir (zaten vardi, dokunulmadi).
///   • Retreat / Restart: null-safe, mevcut mantik KORUNDU.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel  (Bos birakılırsa kod otomatik olusturur)")]
    public GameObject gameOverPanel;

    [Header("Skor Gostergeleri")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI highScoreText;
    public GameObject      newRecordBadge;

    [Header("Revive")]
    public Button          reviveButton;
    public TextMeshProUGUI reviveInfoText;

    [Header("Retreat")]
    public Button          retreatButton;
    public TextMeshProUGUI retreatRewardText;

    [Header("Tekrar Oyna / Ana Menu")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Sahne Isimleri")]
    [Tooltip("Ana menu sahnesi — Inspector'dan ata veya default 'MainMenu' kullanilir")]
    public string mainMenuSceneName = "MainMenu";

    bool _reviveUsed    = false;
    int  _runGoldEarned = 0;
    int  _runTechEarned = 0;
    bool _fallbackBuilt = false;

    // ── Yasam Dongusu ─────────────────────────────────────────────────────

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        reviveButton?.onClick.AddListener(OnReviveClicked);
        retreatButton?.onClick.AddListener(OnRetreatClicked);
        restartButton?.onClick.AddListener(OnRestartClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
    }

    void OnEnable()  => GameEvents.OnGameOver += ShowGameOver;
    void OnDisable() => GameEvents.OnGameOver -= ShowGameOver;

    // ── Run Takibi ────────────────────────────────────────────────────────

    public void RegisterRunGold(int amount)    => _runGoldEarned += amount;
    public void RegisterRunTechCore(int amount) => _runTechEarned += amount;

    public void ResetRunTracking()
    {
        _runGoldEarned = 0;
        _runTechEarned = 0;
        _reviveUsed    = false;
    }

    // ── Game Over ─────────────────────────────────────────────────────────

    void ShowGameOver()
    {
        // Inspector paneli yoksa kod paneli oluştur (bootstrap)
        if (gameOverPanel == null && !_fallbackBuilt)
            BuildFallbackPanel();

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;  // Oyunu durdur; Revive/Restart geri acar

        UpdateScoreDisplay();
        UpdateReviveButton();
        UpdateRetreatButton();

        Debug.Log("[GameOverUI] Game Over ekrani gosterildi.");
    }

    // ── Skor Guncelleme ───────────────────────────────────────────────────

    void UpdateScoreDisplay()
    {
        int dist  = Mathf.RoundToInt(
            PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f);
        int cp    = PlayerStats.Instance != null ? PlayerStats.Instance.CP : 0;

        // SaveManager null ise RunState fallback
        int kills = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentRunKills
            : (RunState.Instance != null ? RunState.Instance.KillCount : 0);

        if (distanceText != null) distanceText.text = $"{dist} m";
        if (killText     != null) killText.text      = $"{kills}";
        if (cpText       != null) cpText.text        = $"{cp}";

        int  prevBest = PlayerPrefs.GetInt("HighScore_CP", 0);
        bool isRecord = cp > prevBest;
        if (isRecord) { PlayerPrefs.SetInt("HighScore_CP", cp); PlayerPrefs.Save(); }

        if (highScoreText  != null) highScoreText.text = $"{Mathf.Max(cp, prevBest)}";
        if (newRecordBadge != null) newRecordBadge.SetActive(isRecord);
    }

    void UpdateReviveButton()
    {
        if (reviveButton == null) return;
        reviveButton.interactable = !_reviveUsed;
        if (reviveInfoText != null)
            reviveInfoText.text = _reviveUsed ? "Kullanildi" : "Reklam izle";
    }

    void UpdateRetreatButton()
    {
        if (retreatButton == null) return;
        int goldBack = Mathf.RoundToInt(_runGoldEarned * 0.20f);
        if (retreatRewardText != null)
            retreatRewardText.text = $"Gold +{goldBack}";
    }

    // ── Revive ────────────────────────────────────────────────────────────

    void OnReviveClicked()
    {
        if (_reviveUsed) return;
        _reviveUsed = true;
        UpdateReviveButton();
        // TODO: Gercek reklam SDK buraya
        Debug.Log("[GameOverUI] Reklam placeholder — Revive verildi.");
        OnReviveGranted();
    }

    void OnReviveGranted()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // DEĞİŞİKLİK: Sadece HP doldurmak yetmez; ölüm ve hareket flagleri de temizlenmeli.
        PlayerStats.Instance?.ReviveFromGameOver();
        FindObjectOfType<Playercontroller>()?.ResumeRun();

        Time.timeScale = 1f;
        Debug.Log("[GameOverUI] Oyuncu diriltildi.");
    }

    // ── Retreat ───────────────────────────────────────────────────────────

    void OnRetreatClicked()
    {
        int goldBack = Mathf.RoundToInt(_runGoldEarned * 0.20f);
        EconomyManager.Instance?.AddGold(goldBack);
        Debug.Log($"[GameOverUI] Retreat: +{goldBack} Gold.");
        OnRestartClicked();
    }

    // ── Restart / Main Menu ───────────────────────────────────────────────

    void OnRestartClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnMainMenuClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[GameOverUI] mainMenuSceneName bos. Inspector'dan ata.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Fallback Panel (Inspector refs yoksa) ─────────────────────────────
    // Tasarimci gercek paneli baglayinca bu blok hic calismaz.

    void BuildFallbackPanel()
    {
        _fallbackBuilt = true;

        // Canvas
        var canvasGO = new GameObject("GameOver_FallbackCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;  // HUD'un ustune cik

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        gameOverPanel = canvasGO;  // panel referansini guncelle

        // Arkaplan
        var bg = MakeFBImage(canvasGO, "BG", new Color(0.05f, 0.05f, 0.12f, 0.92f));
        StretchRT(bg.GetComponent<RectTransform>());

        // Baslik
        MakeFBText(canvasGO, "GAME OVER",
            new Vector2(0.5f, 0.78f), new Vector2(0, 0), 80,
            new Color(1f, 0.25f, 0.25f), FontStyles.Bold);

        // Stat satirlari (referanslari ata — UpdateScoreDisplay bunlari kullanir)
        distanceText = MakeFBText(canvasGO, "— m",
            new Vector2(0.5f, 0.62f), new Vector2(0, 0), 34, Color.white, FontStyles.Normal);

        killText = MakeFBText(canvasGO, "0 kill",
            new Vector2(0.5f, 0.56f), new Vector2(0, 0), 30,
            new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        cpText = MakeFBText(canvasGO, "CP: 0",
            new Vector2(0.5f, 0.50f), new Vector2(0, 0), 30,
            new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);

        // Retry butonu
        restartButton = MakeFBButton(canvasGO, "TEKRAR OYNA",
            new Vector2(0.5f, 0.32f), new Vector2(400, 100),
            new Color(0.15f, 0.70f, 0.20f));
        restartButton.onClick.AddListener(OnRestartClicked);

        // Ana Menu butonu
        mainMenuButton = MakeFBButton(canvasGO, "ANA MENU",
            new Vector2(0.5f, 0.20f), new Vector2(400, 80),
            new Color(0.20f, 0.20f, 0.55f));
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        // Revive butonu (sade)
        reviveButton = MakeFBButton(canvasGO, "REKLAM: DEVAM ET",
            new Vector2(0.5f, 0.44f), new Vector2(400, 80),
            new Color(0.70f, 0.55f, 0.10f));
        reviveInfoText = MakeFBText(canvasGO, "Reklam izle",
            new Vector2(0.5f, 0.41f), new Vector2(0, 0), 22,
            new Color(0.7f, 0.7f, 0.7f), FontStyles.Italic);
        reviveButton.onClick.AddListener(OnReviveClicked);

        Debug.Log("[GameOverUI] Fallback panel olusturuldu. Inspector'dan gercek paneli bagla.");
    }

    // ── Fallback Yardimcilar ──────────────────────────────────────────────

    TextMeshProUGUI MakeFBText(GameObject parent, string text,
        Vector2 anchor, Vector2 offset, float size, Color color, FontStyles style)
    {
        var go = new GameObject("FBText_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = offset; r.sizeDelta = new Vector2(900, 80);
        return t;
    }

    Button MakeFBButton(GameObject parent, string label, Vector2 anchor,
        Vector2 size, Color bgColor)
    {
        var go = new GameObject("FBBtn_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = bgColor;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = size;
        var lbl = new GameObject("Label"); lbl.transform.SetParent(go.transform, false);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = size.y * 0.32f; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        return btn;
    }

    Image MakeFBImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = color;
        return img;
    }

    void StretchRT(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}