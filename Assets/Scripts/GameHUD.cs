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
    public TextMeshProUGUI threatWarningText; // DEĞİŞİKLİK: Threat Preview / wave warning görünür HUD metni.
    public TextMeshProUGUI buffSummaryText; // DEĞİŞİKLİK: Gate sonrası aktif hazırlık özetini HUD'da tutar.
    public TextMeshProUGUI prepSummaryText; // DEĞİŞİKLİK: Anchor girişinde kısa prepared loadout özeti gösterir.

    bool _autoBuilt = false;
    int  _lastCP    = 0;
    float _nextCombatReadoutRefresh = 0f;
    float _warningHideTime = 0f; // DEĞİŞİKLİK: Kısa warning paneli otomatik kapanır.
    float _prepHideTime = 0f; // DEĞİŞİKLİK: Anchor prep özeti birkaç saniye görünür.

    void Start()
    {
        if (PlayerStats.Instance == null)
        { Debug.LogError("GameHUD: PlayerStats yok!"); return; }

        if (cpText == null || tierText == null) AutoBuildHUD();
        EnsureCombatReadout();
        EnsureThreatWarningText(); // DEĞİŞİKLİK: Hazır HUD kullanılan sahnelerde de warning text otomatik oluşur.
        EnsureBuffSummaryText(); // DEĞİŞİKLİK: Gate/buff etkileri log yerine HUD'da da görünür.
        EnsurePrepSummaryText(); // DEĞİŞİKLİK: Anchor'a girerken hazırlık özeti için overlay text hazır olur.

        GameEvents.OnCPUpdated          += OnCPUpdated;
        GameEvents.OnTierChanged        += OnTierChanged;
        GameEvents.OnSynergyFound       += OnSynergy;
        GameEvents.OnPlayerDamaged      += OnPlayerDamaged;
        GameEvents.OnRiskBonusActivated += OnRiskBonus;
        GameEvents.OnBulletCountChanged += OnBulletCount;
        GameEvents.OnCommanderHPChanged += OnCommanderHP;
        GameEvents.OnSoldierAdded       += OnSoldierCount;
        GameEvents.OnSoldierRemoved     += OnSoldierCount;
        GameEvents.OnWaveWarning        += OnWaveWarning;
        GameEvents.OnThreatPreview      += OnThreatPreview;
        GameEvents.OnAnchorDamaged      += OnAnchorDamaged;
        GameEvents.OnAnchorModeChanged  += OnAnchorModeChanged;

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
        GameEvents.OnWaveWarning        -= OnWaveWarning;
        GameEvents.OnThreatPreview      -= OnThreatPreview;
        GameEvents.OnAnchorDamaged      -= OnAnchorDamaged;
        GameEvents.OnAnchorModeChanged  -= OnAnchorModeChanged;
    }

    void Update()
    {
        if (Time.time < _nextCombatReadoutRefresh) return;
        _nextCombatReadoutRefresh = Time.time + 0.35f;
        if (combatReadoutText == null)
            EnsureCombatReadout();
        RefreshCombatReadout();
        RefreshBuffSummary();
        if (threatWarningText != null && threatWarningText.gameObject.activeSelf && Time.time >= _warningHideTime)
            threatWarningText.gameObject.SetActive(false); // DEĞİŞİKLİK: Warning metni karar anından sonra ekranı kirletmez.
        if (prepSummaryText != null && prepSummaryText.gameObject.activeSelf && Time.time >= _prepHideTime)
            prepSummaryText.gameObject.SetActive(false); // DEĞİŞİKLİK: Prep özeti kalıcı kalıp ekranı kapatmaz.
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
        if (threatWarningText == null)
        {
            threatWarningText = MakeText(GetOverlayCanvas().gameObject, "", new Vector2(0.5f, 0.84f), Vector2.zero, 34, new Color(1f, 0.78f, 0.18f));
            threatWarningText.gameObject.SetActive(false); // DEĞİŞİKLİK: Warning sadece preview/wave anında görünür.
        }

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
        r.sizeDelta = new Vector2(360f, 330f); // DEĞİŞİKLİK: SGS debug metrikleri için panel yüksekliği artırıldı.
        combatReadoutText.alignment = TextAlignmentOptions.TopRight;
        combatReadoutText.raycastTarget = false;
    }

    void EnsureThreatWarningText()
    {
        // DEĞİŞİKLİK: Threat preview için mevcut Canvas'a minimal world-safe UI text eklenir.
        if (threatWarningText != null) return;

        Canvas canvas = GetOverlayCanvas();
        threatWarningText = MakeText(canvas.gameObject, "", new Vector2(0.5f, 0.84f), Vector2.zero, 34, new Color(1f, 0.78f, 0.18f));
        RectTransform r = threatWarningText.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(720f, 64f);
        threatWarningText.gameObject.SetActive(false);
    }

    void EnsureBuffSummaryText()
    {
        // DEĞİŞİKLİK: Aktif gate/buff özeti sahne objesi değil screen-space HUD olarak görünür.
        if (buffSummaryText != null) return;
        Canvas canvas = GetOverlayCanvas();
        buffSummaryText = MakeText(canvas.gameObject, "", new Vector2(0.02f, 0.78f), new Vector2(230f, 0f), 22, new Color(0.72f, 1f, 0.76f));
        RectTransform r = buffSummaryText.GetComponent<RectTransform>();
        r.pivot = new Vector2(0f, 0.5f);
        r.sizeDelta = new Vector2(520f, 96f);
        buffSummaryText.alignment = TextAlignmentOptions.Left;
    }

    void EnsurePrepSummaryText()
    {
        // DEĞİŞİKLİK: Anchor başında hazırlık/tehdit özeti kısa süre üst panelde gösterilir.
        if (prepSummaryText != null) return;
        Canvas canvas = GetOverlayCanvas();
        prepSummaryText = MakeText(canvas.gameObject, "", new Vector2(0.5f, 0.68f), Vector2.zero, 26, new Color(0.95f, 1f, 0.82f));
        RectTransform r = prepSummaryText.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(760f, 110f);
        prepSummaryText.gameObject.SetActive(false);
    }

    Canvas GetOverlayCanvas()
    {
        // DEĞİŞİKLİK: Preview/buff UI mevcut world-space canvas'a bağlanmaz, daima ScreenSpaceOverlay kalır.
        GameObject existing = GameObject.Find("GameplayOverlayCanvas");
        if (existing != null)
        {
            Canvas existingCanvas = existing.GetComponent<Canvas>();
            if (existingCanvas != null)
            {
                existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return existingCanvas;
            }
        }

        GameObject go = new GameObject("GameplayOverlayCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void RefreshCombatReadout()
    {
        if (combatReadoutText == null || PlayerStats.Instance == null) return;

        PlayerStats.RuntimeCombatSnapshot c = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        
        float soldierDps = ArmyManager.Instance != null ? ArmyManager.Instance.GetEstimatedSoldierDps() : 0f;
        float totalPlayerDps = c.DisplayedDPS + soldierDps;
        float soldierPct = totalPlayerDps > 0f ? soldierDps / totalPlayerDps * 100f : 0f;

        string line1 = $"DPS: {c.DisplayedDPS:0}+S{soldierDps:0} ({soldierPct:0}%) | FR: {c.FireRate:0.0}";
        string line2 = $"Bullet: {c.BulletDamage}x{c.ProjectileCount} | Range: {c.WeaponRange:0}";
        string line3 = $"Pen: {c.ArmorPen} | Pierce: {c.PierceCount}";
        string line4 = $"HP: {c.CurrentHP}/{c.MaxHP} | Pwr: {c.CombatPower:N0}";
        string line5 = $"Soldiers: {ArmyManager.Instance?.SoldierCount ?? 0} [{ArmyManager.Instance?.GetActiveSoldierTypesText() ?? "-"}]";
        
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
        
        combatReadoutText.text = line1 + "\n" + line2 + "\n" + line3 + "\n" + line4 + "\n" + line5 + stageInfo
            + "\n" + RunDebugMetrics.Instance.BuildDebugBlock(); // DEĞİŞİKLİK: Eğlence teşhisi için SGS metrikleri HUD'a eklenir.
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

    void OnWaveWarning(string warning)
    {
        // DEĞİŞİKLİK: Anchor wave warning artık oyuncuya görünür karar sinyali verir.
        ShowThreatWarning(warning, new Color(1f, 0.72f, 0.18f), 2.0f);
    }

    void OnThreatPreview(string warning)
    {
        // DEĞİŞİKLİK: Gate öncesi yaklaşan tehdit SGS döngüsünün GÖR adımıdır.
        RunDebugMetrics.Instance.RecordThreatPreview(warning);
        ShowThreatWarning(warning, new Color(0.35f, 0.9f, 1f), 2.2f);
    }

    void OnAnchorDamaged(int amount, int currentHp)
    {
        // DEĞİŞİKLİK: Anchor hasarı görünür consequence olarak popup/warning verir.
        ShowPopup($"ANCHOR -{amount}", new Color(1f, 0.25f, 0.15f));
        ShowThreatWarning("ANCHOR UNDER ATTACK", new Color(1f, 0.22f, 0.16f), 1.5f);
    }

    void ShowThreatWarning(string msg, Color color, float duration)
    {
        if (threatWarningText == null || string.IsNullOrWhiteSpace(msg)) return;
        threatWarningText.gameObject.SetActive(true);
        threatWarningText.text = msg;
        threatWarningText.color = color;
        _warningHideTime = Time.time + Mathf.Max(0.5f, duration);
    }

    void OnAnchorModeChanged(bool active)
    {
        // DEĞİŞİKLİK: Runner hazırlığının Anchor'da neye dönüşeceği oyuncuya kısa özetlenir.
        if (!active || prepSummaryText == null || PlayerStats.Instance == null) return;
        PlayerStats.RuntimeCombatSnapshot c = PlayerStats.Instance.GetRuntimeCombatSnapshot();
        string gates = PlayerStats.Instance.SelectedGateHistory.Count > 0
            ? string.Join(" / ", PlayerStats.Instance.SelectedGateHistory)
            : "-";
        string soldiers = ArmyManager.Instance != null ? ArmyManager.Instance.GetActiveSoldierTypesText() : "-";
        prepSummaryText.text = $"PREPARED: {gates}\nSQUAD: {soldiers} | DPS {c.DisplayedDPS:0} | PEN {c.ArmorPen}\nTEST: {RunDebugMetrics.Instance.LastThreatPreview}";
        prepSummaryText.gameObject.SetActive(true);
        _prepHideTime = Time.time + 4.0f;
    }

    void RefreshBuffSummary()
    {
        // DEĞİŞİKLİK: Gate etkisi alındıktan sonra oyuncu ne kazandığını ekranda görür.
        if (buffSummaryText == null || PlayerStats.Instance == null) return;
        PlayerStats ps = PlayerStats.Instance;
        string gates = ps.SelectedGateHistory.Count > 0 ? string.Join(" / ", ps.SelectedGateHistory) : "-";
        buffSummaryText.text =
            $"BUFFS: {gates}\n" +
            $"Power +{ps.RunWeaponPowerPercent:0}% | FR +{ps.RunFireRatePercent:0}% | Pen +{ps.RunArmorPenFlat} | Pierce +{ps.RunPierceCount}";
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
