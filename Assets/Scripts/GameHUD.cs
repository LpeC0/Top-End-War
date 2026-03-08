using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — Oyun İçi HUD
/// Canvas altındaki UI elemanlarına bağlan.
/// PlayerStats event'lerini dinler, otomatik güncellenir.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("CP Göstergesi")]
    public TextMeshProUGUI cpText;       // "3.132" gibi büyük sayı
    public TextMeshProUGUI tierText;     // "TIER 2 — Elit Komando"

    [Header("Path Barları (0-1 arası Slider)")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup Yazı (kapıdan geçince uçan)")]
    public TextMeshProUGUI popupText;    // "+60", "×2", "TIER 3!" gibi
    public TextMeshProUGUI synergyText;  // "PERFECT GENETICS" gibi

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogWarning("HUD: PlayerStats bulunamadı!"); return; }

        // Event'lere abone ol
        GameEvents.OnCPUpdated    += OnCPUpdated;
        GameEvents.OnTierChanged  += OnTierChanged;
        GameEvents.OnSynergyFound += OnSynergy;

        // İlk değerleri göster
        lastCP = stats.CP;
        RefreshAll(stats);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated    -= OnCPUpdated;
        GameEvents.OnTierChanged  -= OnTierChanged;
        GameEvents.OnSynergyFound -= OnSynergy;
    }

    // ── Event Dinleyicileri ───────────────────────────────────────────────
    void OnCPUpdated(int cp)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;

        // CP yazısı
        if (cpText) cpText.text = cp.ToString("N0");

        // Path barları
        float total = stats.PiyadePath + stats.MekanizePath + stats.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = stats.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = stats.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = stats.TeknolojiPath / total;
        }

        // Popup: +60 veya -80
        int delta = cp - lastCP;
        if (delta != 0)
        {
            string txt   = delta > 0 ? $"+{delta}" : $"{delta}";
            Color  color = delta > 0 ? Color.cyan : Color.red;
            ShowPopup(txt, color);
        }
        lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (tierText && stats != null)
            tierText.text = $"TİER {tier}  |  {stats.GetTierName()}";

        ShowPopup($"⭐ TİER {tier}!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) return;
        StopCoroutine("HideSynergy");
        synergyText.text  = name;
        synergyText.color = new Color(1f, 0.84f, 0f);
        StartCoroutine("HideSynergy");
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    void RefreshAll(PlayerStats stats)
    {
        if (cpText)   cpText.text   = stats.CP.ToString("N0");
        if (tierText) tierText.text = $"TİER {stats.CurrentTier}  |  {stats.GetTierName()}";
    }

    void ShowPopup(string msg, Color color)
    {
        if (popupText == null) return;
        StopCoroutine("HidePopup");
        popupText.text  = msg;
        popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator HidePopup()
    {
        yield return new WaitForSeconds(1.2f);
        if (popupText) popupText.text = "";
    }

    IEnumerator HideSynergy()
    {
        yield return new WaitForSeconds(2.5f);
        if (synergyText) synergyText.text = "";
    }
}
