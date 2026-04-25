using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Top End War main menu bootstrap.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Color bgColor = default;

    void Awake()
    {
        if (bgColor == default)
        {
            bgColor = UITheme.DeepNavy;
        }

        EnsureSaveManager();
        EnsureEventSystem();
        EnsureScreenManager();
        ApplyCameraTheme();
    }

    void EnsureSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            return;
        }

        // DEGISIKLIK: MainMenu can now preview progression-driven UI even before gameplay loads.
        new GameObject("SaveManager").AddComponent<SaveManager>();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void EnsureScreenManager()
    {
        GameObject canvasObject = GameObject.Find("MainMenuCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform uiRoot = canvasObject.transform.Find("UIRoot");
        if (uiRoot == null)
        {
            GameObject root = new GameObject("UIRoot", typeof(RectTransform));
            uiRoot = root.transform;
            uiRoot.SetParent(canvasObject.transform, false);
        }

        RectTransform rect = (RectTransform)uiRoot;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UIScreenManager screenManager = uiRoot.GetComponent<UIScreenManager>();
        if (screenManager == null)
        {
            screenManager = uiRoot.gameObject.AddComponent<UIScreenManager>();
        }

        screenManager.Bootstrap();
    }

    void ApplyCameraTheme()
    {
        if (Camera.main == null)
        {
            return;
        }

        Camera.main.backgroundColor = bgColor;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
    }
}
