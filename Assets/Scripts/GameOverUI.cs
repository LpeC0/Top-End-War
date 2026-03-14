using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Top End War — Game Over Ekrani (Claude)
///
/// CANVAS KURULUMU:
///   Canvas altina "GameOverPanel" adli bir Panel ekle
///   GameOverPanel:
///   ├── Bu script
///   ├── Background Image (koyu yari seffaf, full stretch)
///   ├── TitleText       TextMeshProUGUI  "SAVAS BITTI"
///   ├── CPText          TextMeshProUGUI  "Toplam CP: 480"
///   ├── DistanceText    TextMeshProUGUI  "Mesafe: 342m"
///   ├── RetryButton     Button           "TEKRAR DENE"
///   └── MainMenuButton  Button           "ANA MENU"
///
///   GameOverPanel baslangicta SetActive(false) olmali!
///   GameEvents.OnGameOver tetikleyince acilir.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject gameOverPanel;

    [Header("Metinler")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI cpResultText;
    public TextMeshProUGUI distanceText;

    [Header("Butonlar")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Sahne Adlari")]
    public string gameSceneName = "SampleScene";
    public string menuSceneName = "MainMenu";

    void Start()
    {
        // Panel baslangicta gizli
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Event dinle
        GameEvents.OnGameOver += ShowGameOver;

        // Butonlar
        if (retryButton    != null) retryButton.onClick.AddListener(Retry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMenu);
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver -= ShowGameOver;
    }

    void ShowGameOver()
    {
        if (gameOverPanel == null) return;

        // Oyunu durdur
        Time.timeScale = 0f;

        // Panel'i ac
        gameOverPanel.SetActive(true);

        // Bilgileri doldur
        if (cpResultText != null && PlayerStats.Instance != null)
            cpResultText.text = "Son CP: " + PlayerStats.Instance.CP;

        if (distanceText != null && PlayerStats.Instance != null)
            distanceText.text = "Mesafe: " + Mathf.RoundToInt(PlayerStats.Instance.transform.position.z) + "m";

        Debug.Log("[GameOver] Ekran acildi.");
    }

    void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        // MainMenu sahnesi yoksa sadece yeniden yukle
        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }
}