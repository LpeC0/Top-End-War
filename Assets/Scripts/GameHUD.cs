using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — HUD v8 (Claude)
///
/// v8 DÜZELTMELER:
///   - CommanderHP Slider fill rect düzgün oluşturuluyor (v7'de bozuktu)
///   - Slider hierarchy: Bar BG → FillArea → Fill (Unity standart yapısı)
///   - SoldierCountText sol üstte, net okunur
///
/// UNITY KURULUM:
///   Canvas → HUDPanel → GameHUD bileşeni zaten bağlı.
///   Inspector'da commanderHPSlider / commanderHPText / soldierCountText
///   referanslarını bağlayabilirsin VEYA boş bırak (auto-build çalışır).
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

    [Header("Popup / Sinerji")]
    public TextMeshProUGUI popupText;
    public TextMeshProUGUI synergyText;

    [Header("Hasar Flash")]
    public Image damageFlashImage;

    [Header("Komutan HP (opsiyonel — bos birakilabilir)")]
    public Slider          commanderHPSlider;
    public TextMeshProUGUI commanderHPText;

    [Header("Asker Sayisi (opsiyonel)")]
    public TextMeshProUGUI soldierCountText;

    [Header("Runtime Combat Readout (debug)")]
    public TextMeshProUGUI combatReadoutText;

    bool _autoBuilt = false;
    int  _lastCP    = 0;
    float _nextCombatReadoutRefresh = 0f;

    void Start()
    {
        if (PlayerStats.Instance == null)
        { Debug.LogError("GameHUD: PlayerStats yok!"); return; }

        if (cpText == null || tierText == null) AutoBuildHUD();
        EnsureCombatReadout();

        GameEvents.OnCPUpdated          += OnCPUpdated;
        GameEvents.OnTierChanged        += OnTierChanged;
        GameEvents.OnSynergyFound       += OnSynergy;
        GameEvents.OnPlayerDamaged      += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated += OnRiskBonus;
        GameEvents.OnBulletCountChanged += OnBulletCount;
        GameEvents.OnCommanderHPChanged += OnCommanderHP;
        GameEvents.OnSoldierAdded       += OnSoldierCount;
        GameEvents.OnSoldierRemoved     += OnSoldierCount;

        _lastCP = PlayerStats.Instance.CP;
        if (cpText)   cpText.text   = PlayerStats.Instance.CP.ToString("N0");
        if (tierText) tierText.text = "TIER 1 | " + PlayerStats.Instance.GetTierName();
        if (damageFlashImage) damageFlashImage.color = new Color(1,0,0,0);

        // Komutan HP bar ilk değer
        OnCommanderHP(PlayerStats.Instance.CommanderHP, PlayerStats.Instance.CommanderMaxHP);
        if (soldierCountText) soldierCountText.text = "Asker: 0/20";
        RefreshCombatReadout();
    }

    void OnDestroy()
    {
        GameEvents.OnCPUpdated          -= OnCPUpdated;
        GameEvents.OnTierChanged        -= OnTierChanged;
        GameEvents.OnSynergyFound       -= OnSynergy;
        GameEvents.OnPlayerDamaged      -= OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated -= OnRiskBonus;
        GameEvents.OnBulletCountChanged -= OnBulletCount;
        GameEvents.OnCommanderHPChanged -= OnCommanderHP;
        GameEvents.OnSoldierAdded       -= OnSoldierCount;
        GameEvents.OnSoldierRemoved     -= OnSoldierCount;
    }

    void Update()
    {
        if (Time.time < _nextCombatReadoutRefresh) return;
        _nextCombatReadoutRefresh = Time.time + 0.35f;
        if (combatReadoutText == null)
            EnsureCombatReadout();
        RefreshCombatReadout();
    }

    // ── AUTO BUILD ────────────────────────────────────────────────────────
    void AutoBuildHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>(); go.AddComponent<GraphicRaycaster>();
        }

        if (cpText   == null) cpText   = MakeText(canvas.gameObject, "CP", new Vector2(0.5f,1f), new Vector2(0,-50),  52, Color.white);
        if (tierText == null) tierText = MakeText(canvas.gameObject, "TIER 1", new Vector2(0.5f,1f), new Vector2(0,-105), 32, Color.yellow);
        if (popupText== null) popupText= MakeText(canvas.gameObject, "", new Vector2(0.5f,0.5f), new Vector2(0,80), 52, Color.cyan);

        // ── Komutan HP Bar ────────────────────────────────────────────────
        // Unity Slider standart yapısı: Slider → Background + Fill Area → Fill
        if (commanderHPSlider == null)
            commanderHPSlider = BuildHPBar(canvas,
                new Vector2(0.03f, 0.90f), new Vector2(0.72f, 0.96f),
                new Color(0.2f, 0.8f, 0.2f), "KomutanHP");

        // HP text (slider'ın yanında)
        if (commanderHPText == null)
            commanderHPText = MakeText(canvas.gameObject, "HP",
                new Vector2(0.74f, 0.93f), Vector2.zero, 24, Color.white);

        // ── Asker Sayısı ──────────────────────────────────────────────────
        if (soldierCountText == null)
            soldierCountText = MakeText(canvas.gameObject, "Asker: 0/20",
                new Vector2(0.0f, 0.88f), new Vector2(100, 0), 28, new Color(0.9f,0.9f,0.9f));

        // ── Hasar Flash ───────────────────────────────────────────────────
        if (damageFlashImage == null)
        {
            var fg = new GameObject("DamageFlash");
            fg.transform.SetParent(canvas.transform, false);
            damageFlashImage = fg.AddComponent<Image>();
            damageFlashImage.color = new Color(1,0,0,0);
            damageFlashImage.raycastTarget = false;
            var fr = fg.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
        }

        EnsureCombatReadout();

        _autoBuilt = true;
        Debug.Log("[GameHUD v8] AutoBuild tamamlandi.");
    }

    void EnsureCombatReadout()
    {
        if (combatReadoutText != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("AutoCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        combatReadoutText = MakeText(canvas.gameObject, "Combat", new Vector2(1f, 1f), new Vector2(-145f, -130f), 20, new Color(0.86f, 0.96f, 1f));
        RectTransform r = combatReadoutText.GetComponent<RectTransform>();
        r.pivot = new Vector2(1f, 1f);
        r.sizeDelta = new Vector2(270f, 190f);
        combatReadoutText.alignment = TextAlignmentOptions.TopRight;
        combatReadoutText.raycastTarget = false;
    }

    void RefreshCombatReadout()
    {
        if (combatReadoutText == null || PlayerStats.Instance == null) return;

        PlayerStats.RuntimeCombatSnapshot c = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        
        string line1 = $"DPS: {c.DisplayedDPS:0} | FR: {c.FireRate:0.0}";
        string line2 = $"Bullet: {c.BulletDamage}x{c.ProjectileCount} | Range: {c.WeaponRange:0}";
        string line3 = $"Pen: {c.ArmorPen} | Pierce: {c.PierceCount}";
        string line4 = $"HP: {c.CurrentHP}/{c.MaxHP} | Pwr: {c.CombatPower:N0}";
        
        // Stage info (optional, tutarlılık check)
        string stageInfo = "";
        StageManager sm = StageManager.Instance;
        if (sm != null && sm.GetActiveStageConfig() != null)
        {
            StageConfig stage = sm.GetActiveStageConfig();
            int targetPower = stage.GetEffectiveTargetPower();
            float targetDps = stage.targetDps;
            
            // Durum: Underpowered / Risky / Ready / Overkill
            string state = "Ready";
            if (c.CombatPower < targetPower * 0.7f)
                state = "Underpowered";
            else if (c.CombatPower < targetPower)
                state = "Risky";
            else if (c.CombatPower >= targetPower * 1.3f)
                state = "Overkill";
            
            stageInfo = $"\nTarget: {targetDps:0} DPS / {targetPower:N0} Pwr\n{state}";
        }
        
        combatReadoutText.text = line1 + "\n" + line2 + "\n" + line3 + "\n" + line4 + stageInfo;
    }

    /// <summary>
    /// Unity Slider standart hiyerarşisini elle oluşturur:
    ///   Slider root → Background → Fill Area → Fill → Handle Slide Area → Handle
    /// Fill Rect doğru şekilde atanır — bu v7'deki hatanın düzeltmesi.
    /// </summary>
    Slider BuildHPBar(Canvas canvas, Vector2 anchorMin, Vector2 anchorMax,
                      Color fillColor, string name)
    {
        // Root
        var root = new GameObject(name);
        root.transform.SetParent(canvas.transform, false);
        var sl = root.AddComponent<Slider>();
        sl.interactable = false;
        sl.minValue = 0f; sl.maxValue = 1f; sl.value = 1f;
        var rootR = root.GetComponent<RectTransform>();
        rootR.anchorMin = anchorMin; rootR.anchorMax = anchorMax;
        rootR.offsetMin = rootR.offsetMax = Vector2.zero;

        // Background
        var bg = new GameObject("Background"); bg.transform.SetParent(root.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.08f,0.08f,0.08f,0.88f);
        StretchRect(bg.GetComponent<RectTransform>());

        // Fill Area
        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(root.transform, false);
        var faR = fillArea.GetComponent<RectTransform>() ?? fillArea.AddComponent<RectTransform>();
        faR.anchorMin = new Vector2(0,0.25f); faR.anchorMax = new Vector2(1,0.75f);
        faR.offsetMin = new Vector2(5,0); faR.offsetMax = new Vector2(-5,0);

        // Fill
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
        var fillR = fill.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = new Vector2(0,1);
        fillR.sizeDelta  = new Vector2(10,0); fillR.anchoredPosition = Vector2.zero;

        // Slider referanslari
        sl.fillRect       = fillR;           // ← kritik satır, v7'de eksikti
        sl.targetGraphic  = bgImg;

        return sl;
    }

    // ── EVENT HANDLER'LAR ─────────────────────────────────────────────────
    void OnCPUpdated(int cp)
    {
        var s = PlayerStats.Instance; if (s == null) return;
        if (cpText) cpText.text = cp.ToString("N0");
        RefreshCombatReadout();

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
        if (tierText && s != null) tierText.text = $"TIER {tier} | {s.GetTierName()}";
        RefreshCombatReadout();
        ShowPopup($"TIER {tier}!", Color.yellow);
    }

    void OnSynergy(string name)
    {
        if (synergyText == null) { ShowPopup(name, new Color(1,0.84f,0)); return; }
        StopCoroutine("HideSynergy");
        synergyText.text = name; synergyText.color = new Color(1,0.84f,0);
        StartCoroutine("HideSynergy");
    }

    void OnRiskBonus(int r) => ShowPopup($"RISK! +{r}", new Color(1,0.85f,0));

    void OnPlayerDamaged(int _)
    {
        if (!damageFlashImage) return;
        StopCoroutine("FlashDamage"); StartCoroutine("FlashDamage");
    }

    void OnBulletCount(int c)
    {
        RefreshCombatReadout();
        ShowPopup($"+MERMI {c}", new Color(0.5f,0,0.9f));
    }

    // ── KOMUTAN HP ────────────────────────────────────────────────────────
    void OnCommanderHP(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;

        if (commanderHPSlider)
        {
            commanderHPSlider.value = ratio;

            // Fill rengini güncelle
            Image fillImg = commanderHPSlider.fillRect?.GetComponent<Image>();
            if (fillImg)
                fillImg.color = ratio > 0.6f ? new Color(0.2f,0.8f,0.2f)
                              : ratio > 0.3f ? new Color(1f,0.7f,0f)
                              :                new Color(0.9f,0.1f,0.1f);
        }

        if (commanderHPText) commanderHPText.text = $"{current}/{max}";
        RefreshCombatReadout();
    }

    // ── ASKER SAYISI ─────────────────────────────────────────────────────
    void OnSoldierCount(int count)
    {
        if (soldierCountText) soldierCountText.text = $"Asker: {count}/20";
    }

    // ── POPUP ─────────────────────────────────────────────────────────────
    void ShowPopup(string msg, Color color)
    {
        if (!popupText) return;
        StopCoroutine("HidePopup");
        popupText.text = msg; popupText.color = color;
        StartCoroutine("HidePopup");
    }

    IEnumerator FlashDamage()
    {
        if (!damageFlashImage) yield break;
        damageFlashImage.color = new Color(1,0,0,0.55f);
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (!damageFlashImage) yield break;
            damageFlashImage.color = new Color(1,0,0, Mathf.Lerp(0.55f,0,t/0.4f));
            yield return null;
        }
        if (!damageFlashImage) yield break;
        damageFlashImage.color = new Color(1,0,0,0);
    }

    IEnumerator HidePopup()   { yield return new WaitForSeconds(1.2f); if (popupText)   popupText.text   = ""; }
    IEnumerator HideSynergy() { yield return new WaitForSeconds(2.5f); if (synergyText) synergyText.text = ""; }

    // ── YARDIMCI ─────────────────────────────────────────────────────────
    TextMeshProUGUI MakeText(GameObject parent, string txt, Vector2 anchor,
                             Vector2 pos, float size, Color color)
    {
        var obj = new GameObject("HUD_" + txt.Substring(0, Mathf.Min(8, txt.Length)));
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(500, 60);
        return t;
    }

    void StretchRect(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
