using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD v6 (Claude)
///
/// Eger Canvas referanslari Inspector'dan baglanmadiysa
/// kod kendi minimal HUD'ini olusturur.
///
/// KURULUM: Canvas altindaki GameHUD objesine ekle.
/// Referanslar bossa bile calisir — hata vermez.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("CP / Tier (optional — boş bırakabilirsin)")]
    public TextMeshProUGUI cpText;
    public TextMeshProUGUI tierText;

    [Header("Path Barlari (optional)")]
    public Slider piyadebar;
    public Slider mekanizeBar;
    public Slider teknolojiBar;

    [Header("Popup (optional)")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash (optional)")]
    public Image damageFlashImage;

    // Auto-oluşturulan referanslar
    bool _autoBuilt = false;
    int  _lastCP    = 0;

    void Start()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("GameHUD: PlayerStats bulunamadi!");
            return;
        }

        // Referanslar bossa otomatik olsutur
        if (cpText == null || tierText == null)
            AutoBuildHUD();

        // Eventler
        GameEvents.OnCPUpdated     += OnCPUpdated;
        GameEvents.OnTierChanged   += OnTierChanged;
        GameEvents.OnSynergyFound  += OnSynergy;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated  += OnRiskBonus;
        GameEvents.OnBulletCountChanged  += OnBulletCount;

        // Ilk degerler
        _lastCP = PlayerStats.Instance.CP;
        if (cpText)   { cpText.text = PlayerStats.Instance.CP.ToString("N0"); cpText.color = Color.white; }
        if (tierText) { tierText.text = "TIER 1 | " + PlayerStats.Instance.GetTierName(); tierText.color = Color.yellow; }
        if (damageFlashImage) damageFlashImage.color = new Color(1, 0, 0, 0);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated          -= OnCPUpdated;
        GameEvents.OnTierChanged        -= OnTierChanged;
        GameEvents.OnSynergyFound       -= OnSynergy;
        GameEvents.OnPlayerDamaged      -= OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated  -= OnRiskBonus;
        GameEvents.OnBulletCountChanged  -= OnBulletCount;
    }

    // ── Otomatik HUD olustur ──────────────────────────────────────────────────
    void AutoBuildHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        if (cpText == null)
            cpText = CreateText(canvas.gameObject, "CP: 200", new Vector2(0.5f, 1f), new Vector2(0, -35), 36, Color.white);

        if (tierText == null)
            tierText = CreateText(canvas.gameObject, "TIER 1", new Vector2(0.5f, 1f), new Vector2(0, -75), 26, Color.yellow);

        if (popupText == null)
            popupText = CreateText(canvas.gameObject, "", new Vector2(0.5f, 0.5f), new Vector2(0, 60), 40, Color.cyan);

        // Hasar flash
        if (damageFlashImage == null)
        {
            var flashObj = new GameObject("DamageFlash");
            flashObj.transform.SetParent(canvas.transform, false);
            damageFlashImage = flashObj.AddComponent<Image>();
            damageFlashImage.color = new Color(1, 0, 0, 0);
            damageFlashImage.raycastTarget = false;
            var r = flashObj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        _autoBuilt = true;
        Debug.Log("[GameHUD] Otomatik HUD olusturuldu.");
    }

    TextMeshProUGUI CreateText(GameObject parent, string text,
        Vector2 anchor, Vector2 pos, float size, Color color)
    {
        var obj = new GameObject("AutoText");
        obj.transform.SetParent(parent.transform, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(500, 60);
        return tmp;
    }

    // ── Event Handler'lar ─────────────────────────────────────────────────────
    void OnCPUpdated(int cp)
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        if (cpText) cpText.text = cp.ToString("N0");

        float total = s.PiyadePath + s.MekanizePath + s.TeknolojiPath;
        if (total > 0)
        {
            if (piyadebar)    piyadebar.value    = s.PiyadePath    / total;
            if (mekanizeBar)  mekanizeBar.value  = s.MekanizePath  / total;
            if (teknolojiBar) teknolojiBar.value = s.TeknolojiPath / total;
        }

        int delta = cp - _lastCP;
        if (delta != 0)
            ShowPopup(delta > 0 ? "+" + delta : "" + delta, delta > 0 ? Color.cyan : Color.red);
        _lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        var s = PlayerStats.Instance;
        if (tierText && s != null)
        { tierText.text = "TIER " + tier + " | " + s.GetTierName(); tierText.color = Color.yellow; }
        ShowPopup("TIER " + tier + "!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) { ShowPopup(name, new Color(1, 0.84f, 0)); return; }
        StopCoroutine("HideSynergy");
        synergyText.text = name; synergyText.color = new Color(1, 0.84f, 0);
        StartCoroutine("HideSynergy");
    }

    void OnRiskBonus(int remaining)
    {
        ShowPopup("RISK! +" + remaining, new Color(1, 0.85f, 0));
    }

    void OnPlayerDamaged(int _)
    {
        if (damageFlashImage == null) return;
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    IEnumerator FlashDamage()
    {
        damageFlashImage.color = new Color(1, 0, 0, 0.55f);
        float t = 0;
        while (t < 0.4f) { t += Time.deltaTime; damageFlashImage.color = new Color(1, 0, 0, Mathf.Lerp(0.55f, 0, t / 0.4f)); yield return null; }
        damageFlashImage.color = new Color(1, 0, 0, 0);
    }

    void ShowPopup(string msg, Color color)
    {
        if (popupText == null) return;
        StopCoroutine("HidePopup");
        popupText.text = msg; popupText.color = color;
        StartCoroutine("HidePopup");
    }

    void OnBulletCount(int count)
    {
        ShowPopup("MERMI +" + count, new Color(0.5f, 0f, 0.9f));
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }
}