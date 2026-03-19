using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD v7 (Claude)
///
/// v7: Komutan HP bar + Asker sayisi gostergesi eklendi.
///
/// UNITY KURULUM:
///   Canvas -> HUDPanel -> GameHUD.cs ekle.
///   Yeni referanslar (opsiyonel, bos birakilabilir):
///     commanderHPSlider  : Slider (komutan HP bar)
///     commanderHPText    : TMP text (orn: "950/950")
///     soldierCountText   : TMP text (orn: "Asker: 12/20")
///   Boş birakilırsa kod kendi minimal UI'ini olusturur.
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

    [Header("Popup / Sinerjisi")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash")]
    public Image damageFlashImage;

    [Header("Komutan HP (v7 — opsiyonel)")]
    public Slider             commanderHPSlider;
    public TextMeshProUGUI    commanderHPText;

    [Header("Asker Sayisi (v7 — opsiyonel)")]
    public TextMeshProUGUI    soldierCountText;

    // Oto-olusturulan
    bool _autoBuilt = false;
    int  _lastCP    = 0;

    void Start()
    {
        if (PlayerStats.Instance == null)
        { Debug.LogError("GameHUD: PlayerStats bulunamadi!"); return; }

        if (cpText == null || tierText == null) AutoBuildHUD();

        // Event abone olma
        GameEvents.OnCPUpdated           += OnCPUpdated;
        GameEvents.OnTierChanged         += OnTierChanged;
        GameEvents.OnSynergyFound        += OnSynergy;
        GameEvents.OnPlayerDamaged       += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated  += OnRiskBonus;
        GameEvents.OnBulletCountChanged  += OnBulletCount;
        // v7
        GameEvents.OnCommanderHPChanged  += OnCommanderHP;
        GameEvents.OnSoldierAdded        += OnSoldierCount;
        GameEvents.OnSoldierRemoved      += OnSoldierCount;

        // Ilk degerler
        _lastCP = PlayerStats.Instance.CP;
        if (cpText)   cpText.text   = PlayerStats.Instance.CP.ToString("N0");
        if (tierText) tierText.text = "TIER 1 | " + PlayerStats.Instance.GetTierName();

        if (damageFlashImage) damageFlashImage.color = new Color(1, 0, 0, 0);

        // Komutan HP bar ilk deger
        OnCommanderHP(PlayerStats.Instance.CommanderHP,
                      PlayerStats.Instance.CommanderMaxHP);
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated           -= OnCPUpdated;
        GameEvents.OnTierChanged         -= OnTierChanged;
        GameEvents.OnSynergyFound        -= OnSynergy;
        GameEvents.OnPlayerDamaged       -= OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated  -= OnRiskBonus;
        GameEvents.OnBulletCountChanged  -= OnBulletCount;
        GameEvents.OnCommanderHPChanged  -= OnCommanderHP;
        GameEvents.OnSoldierAdded        -= OnSoldierCount;
        GameEvents.OnSoldierRemoved      -= OnSoldierCount;
    }

    // ── Oto HUD olustur ──────────────────────────────────────────────────
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

        if (cpText   == null) cpText   = CreateText(canvas.gameObject, "CP", new Vector2(0.5f, 1f), new Vector2(0, -35), 36, Color.white);
        if (tierText == null) tierText = CreateText(canvas.gameObject, "TIER 1", new Vector2(0.5f, 1f), new Vector2(0, -75), 26, Color.yellow);
        if (popupText == null) popupText = CreateText(canvas.gameObject, "", new Vector2(0.5f, 0.5f), new Vector2(0, 60), 40, Color.cyan);

        // Komutan HP bar (otomatik)
        if (commanderHPSlider == null)
        {
            var slGo = new GameObject("CommanderHPBar");
            slGo.transform.SetParent(canvas.transform, false);
            var sl = slGo.AddComponent<Slider>();
            sl.interactable = false;
            sl.minValue = 0; sl.maxValue = 1; sl.value = 1;
            var r = slGo.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.1f, 0.94f);
            r.anchorMax = new Vector2(0.9f, 0.98f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            // Fill image
            var bg  = CreateSliderBg(slGo);
            var fill= CreateSliderFill(slGo, new Color(0.2f, 0.8f, 0.2f));
            sl.fillRect = fill.GetComponent<RectTransform>();
            commanderHPSlider = sl;
        }

        // Asker sayisi text
        if (soldierCountText == null)
            soldierCountText = CreateText(canvas.gameObject, "Asker: 0/20",
                new Vector2(0f, 1f), new Vector2(10, -30), 20, Color.white);

        // Hasar flash
        if (damageFlashImage == null)
        {
            var flashGo = new GameObject("DamageFlash");
            flashGo.transform.SetParent(canvas.transform, false);
            damageFlashImage = flashGo.AddComponent<Image>();
            damageFlashImage.color = new Color(1, 0, 0, 0);
            damageFlashImage.raycastTarget = false;
            var fr = flashGo.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
        }

        _autoBuilt = true;
        Debug.Log("[GameHUD v7] Oto HUD olusturuldu.");
    }

    // ── Event Handler'lar ─────────────────────────────────────────────────
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
            ShowPopup(delta > 0 ? "+" + delta : "" + delta,
                      delta > 0 ? Color.cyan : Color.red);
        _lastCP = cp;
    }

    void OnTierChanged(int tier)
    {
        var s = PlayerStats.Instance;
        if (tierText && s != null)
            tierText.text = $"TIER {tier} | {s.GetTierName()}";
        ShowPopup($"TIER {tier}!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) { ShowPopup(name, new Color(1, 0.84f, 0)); return; }
        StopCoroutine("HideSynergy");
        synergyText.text = name;
        synergyText.color = new Color(1, 0.84f, 0);
        StartCoroutine("HideSynergy");
    }

    void OnRiskBonus(int remaining)
        => ShowPopup($"RISK! +{remaining}", new Color(1, 0.85f, 0));

    void OnPlayerDamaged(int _)
    {
        if (damageFlashImage == null) return;
        StopCoroutine("FlashDamage");
        StartCoroutine("FlashDamage");
    }

    void OnBulletCount(int count)
        => ShowPopup($"MERMI +{count}", new Color(0.5f, 0f, 0.9f));

    // ── v7: Komutan HP ────────────────────────────────────────────────────
    void OnCommanderHP(int current, int max)
    {
        if (commanderHPSlider) commanderHPSlider.value = max > 0 ? (float)current / max : 0f;
        if (commanderHPText)   commanderHPText.text    = $"{current}/{max}";

        // Slider rengi: HP durumuna gore
        if (commanderHPSlider)
        {
            var fillImg = commanderHPSlider.fillRect?.GetComponent<Image>();
            if (fillImg != null)
            {
                float ratio = max > 0 ? (float)current / max : 0f;
                fillImg.color = ratio > 0.6f ? new Color(0.2f, 0.8f, 0.2f)
                              : ratio > 0.3f ? new Color(1f, 0.7f, 0f)
                              :                new Color(0.9f, 0.1f, 0.1f);
            }
        }
    }

    // ── v7: Asker Sayisi ─────────────────────────────────────────────────
    void OnSoldierCount(int count)
    {
        if (soldierCountText)
            soldierCountText.text = $"Asker: {count}/20";
    }

    // ── Coroutine'ler ────────────────────────────────────────────────────
    IEnumerator FlashDamage()
    {
        damageFlashImage.color = new Color(1, 0, 0, 0.55f);
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            damageFlashImage.color = new Color(1, 0, 0, Mathf.Lerp(0.55f, 0, t / 0.4f));
            yield return null;
        }
        damageFlashImage.color = new Color(1, 0, 0, 0);
    }

    void ShowPopup(string msg, Color color)
    {
        if (popupText == null) return;
        StopCoroutine("HidePopup");
        popupText.text  = msg;
        popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text   = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }

    // ── Yardimci UI olusturucular ─────────────────────────────────────────
    TextMeshProUGUI CreateText(GameObject parent, string text,
        Vector2 anchor, Vector2 pos, float size, Color color)
    {
        var obj = new GameObject("HUD_" + text.Substring(0, Mathf.Min(6, text.Length)));
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

    Image CreateSliderBg(GameObject parent)
    {
        var go = new GameObject("BG"); go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>(); img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return img;
    }

    Image CreateSliderFill(GameObject parent, Color color)
    {
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(parent.transform, false);
        var far = fillArea.GetComponent<RectTransform>() ?? fillArea.AddComponent<RectTransform>();
        far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
        far.offsetMin = far.offsetMax = Vector2.zero;

        var go = new GameObject("Fill"); go.transform.SetParent(fillArea.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.type  = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return img;
    }
}