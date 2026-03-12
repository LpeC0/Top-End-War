using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD (Claude)
/// Hasar alinca ekran kirmizi flash yapar.
/// NOT: TierText Inspector text kutusunu BOŞ birak — kod doldurur.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("CP / Tier")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barlari")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash")]
    public Image damageFlashImage; // Canvas'ta tam ekran Image, alpha=0 baslar

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogWarning("HUD: PlayerStats bulunamadi!"); return; }

        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;

        lastCP = stats.CP;
        if (cpText)   cpText.text   = stats.CP.ToString("N0");
        if (tierText) tierText.text = "TIER 1 | " + stats.GetTierName();

        if (damageFlashImage != null)
            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated     -= OnCPUpdated;
        GameEvents.OnTierChanged   -= OnTierChanged;
        GameEvents.OnSynergyFound  -= OnSynergy;
        GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
    }

    void OnCPUpdated(int cp)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;

        if (cpText) cpText.text = cp.ToString("N0");

        float total = stats.PiyadePath + stats.MekanizePath + stats.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = stats.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = stats.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = stats.TeknolojiPath / total;
        }

        int delta = cp - lastCP;
        if (delta != 0)
            ShowPopup(delta > 0 ? "+" + delta : "" + delta,
                      delta > 0 ? Color.cyan : Color.red);
        lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        PlayerStats stats = PlayerStats.Instance;
        if (tierText && stats != null)
            tierText.text = "TIER " + tier + " | " + stats.GetTierName();
        ShowPopup("TIER " + tier + "!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) return;
        StopCoroutine("HideSynergy");
        synergyText.text  = name;
        synergyText.color = new Color(1f, 0.84f, 0f);
        StartCoroutine("HideSynergy");
    }

    void OnPlayerDamaged(int amount)
    {
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
        if (damageFlashImage == null) yield break;
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.5f);
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            damageFlashImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.5f, 0f, t / 0.45f));
            yield return null;
        }
        damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
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