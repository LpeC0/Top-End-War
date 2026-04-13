using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top End War — Game Over Arayuzu v3 (Claude)
/// Revive (reklam, run basina 1x) + Retreat (%20 loot koruma)
/// SaveManager.CurrentRunKills kullanilir (SessionKills degil).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
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

    bool _reviveUsed    = false;
    int  _runGoldEarned = 0;
    int  _runTechEarned = 0;

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
    public void RegisterRunGold(int amount)     => _runGoldEarned += amount;
    public void RegisterRunTechCore(int amount)  => _runTechEarned += amount;

    public void ResetRunTracking()
    {
        _runGoldEarned = 0;
        _runTechEarned = 0;
        _reviveUsed    = false;
    }

    // ── Game Over ─────────────────────────────────────────────────────────
    void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        UpdateScoreDisplay();
        UpdateReviveButton();
        UpdateRetreatButton();
    }

    void UpdateScoreDisplay()
    {
        int dist  = Mathf.RoundToInt(
            PlayerStats.Instance != null ? PlayerStats.Instance.transform.position.z : 0f);
        int cp    = PlayerStats.Instance != null ? PlayerStats.Instance.CP : 0;

        // SaveManager.CurrentRunKills  ← dogru property adi
        int kills = SaveManager.Instance != null ? SaveManager.Instance.CurrentRunKills : 0;

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
        PlayerStats.Instance?.HealCommander(PlayerStats.Instance.CommanderMaxHP);
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

    // ── Tekrar / Menu ─────────────────────────────────────────────────────
    void OnRestartClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void OnMainMenuClicked()
    {
        ResetRunTracking();
        Time.timeScale = 1f;
        Debug.Log("[GameOverUI] Ana menu sahne adi henuz tanimsiz.");
    }
}