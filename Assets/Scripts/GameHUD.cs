using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD v5 (Claude)
///
/// CANVAS KURULUMU:
///   Canvas (Screen Space - Overlay)
///   ├── CPText      TextMeshProUGUI  — Anchor: TopCenter, Pos: 0, -40
///   ├── TierText    TextMeshProUGUI  — Anchor: TopCenter, Pos: 0, -80  ← text BOSH bırak
///   ├── PopupText   TextMeshProUGUI  — Anchor: Center,    Pos: 0, 50
///   ├── SynergyText TextMeshProUGUI  — Anchor: Center,    Pos: 0, -50
///   ├── DamageFlash Image            — Anchor: Stretch-All, Alpha=0, RaycastTarget=false
///   ├── PiyadeBar   Slider           — sol alt
///   ├── MekanizeBar Slider
///   └── TeknolojiBar Slider
///
/// Bu HUD objesini Canvas'in ALTINA koy, tum referanslari Inspector'dan bagla.
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

    [Header("Hasar Flash (optional)")]
    public Image damageFlashImage;

    int lastCP = 0;

    void Start()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogError("GameHUD: PlayerStats.Instance NULL! Player objesinde PlayerStats.cs var mi?");
            return;
        }

        // Referans kontrolu
        if (cpText   == null) Debug.LogWarning("GameHUD: cpText atanmamis!");
        if (tierText == null) Debug.LogWarning("GameHUD: tierText atanmamis!");

        // Event baglantiları
        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;

        // Ilk degerler
        lastCP = stats.CP;

        if (cpText != null)
        {
            cpText.text  = stats.CP.ToString("N0");
            cpText.color = Color.white;
        }

        if (tierText != null)
        {
            tierText.text  = "TIER 1 | " + stats.GetTierName();
            tierText.color = Color.yellow;  // Belirgin renk
        }

        if (damageFlashImage != null)
            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);

        Debug.Log("GameHUD baslatildi. CP: " + stats.CP + " | Tier: " + stats.CurrentTier);
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

        if (cpText != null) cpText.text = cp.ToString("N0");

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
        if (tierText != null && stats != null)
        {
            tierText.text  = "TIER " + tier + " | " + stats.GetTierName();
            tierText.color = Color.yellow;
        }
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
        if (damageFlashImage == null) return;
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
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