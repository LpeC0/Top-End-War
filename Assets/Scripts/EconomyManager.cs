using UnityEngine;
using System;

/// <summary>
/// Top End War — Ekonomi Yoneticisi v2 (Claude)
///
/// v2: EconomyConfig SO entegre edildi.
///   SlotUpgrade() — Gold + TechCore harcar, basarili ise true dondurur.
///   Pity timer — N bos stage sonra Basic Scroll garantisi.
///   Reklam politikasi — TechCore ve Hard Currency reklamla bypass edilemez.
///
/// Para birimleri: Altin (Soft) | TechCore (Skill-based) | Kristal (Hard)
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Konfigurasyon")]
    public EconomyConfig config;

    // ── Para Birimleri ────────────────────────────────────────────────────
    public int Gold      { get; private set; }
    public int TechCore  { get; private set; }
    public int Crystal   { get; private set; }

    // ── Offline Gelir ─────────────────────────────────────────────────────
    private int _bonusOfflineRate = 0;

    // ── Pity Sayaci ───────────────────────────────────────────────────────
    private int _emptyStageCount = 0;  // Scroll dusmeyen stage sayisi

    // ── Gunluk Reklam Sayaclari ───────────────────────────────────────────
    private int  _doubleGoldAdsToday = 0;
    private int  _bonusChestAdsToday = 0;
    private string _lastAdResetDate  = "";

    // ── PlayerPrefs Anahtarlari ───────────────────────────────────────────
    const string KEY_GOLD         = "Economy_Gold";
    const string KEY_TECHCORE     = "Economy_TechCore";
    const string KEY_CRYSTAL      = "Economy_Crystal";
    const string KEY_BONUS_RATE   = "Economy_BonusRate";
    const string KEY_LAST_SAVE    = "Economy_LastSaveTime";
    const string KEY_PITY         = "Economy_PityCount";
    const string KEY_AD_DATE      = "Economy_AdResetDate";
    const string KEY_AD_DGOLD     = "Economy_DoubleGoldAds";
    const string KEY_AD_CHEST     = "Economy_BonusChestAds";

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
        ResetDailyAdsIfNeeded();
        CollectOfflineEarnings();
    }

    void OnApplicationPause(bool paused) { if (paused) SaveLastTime(); }
    void OnApplicationQuit()             { SaveLastTime(); }

    // ── Altin ─────────────────────────────────────────────────────────────
    public void AddGold(int amount)
    {
        Gold = Mathf.Max(0, Gold + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── TechCore ─────────────────────────────────────────────────────────
    public void AddTechCore(int amount)
    {
        TechCore = Mathf.Max(0, TechCore + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendTechCore(int amount)
    {
        if (TechCore < amount) return false;
        TechCore -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── Kristal ───────────────────────────────────────────────────────────
    public void AddCrystal(int amount)
    {
        Crystal = Mathf.Max(0, Crystal + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendCrystal(int amount)
    {
        if (Crystal < amount) return false;
        Crystal -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // ── Slot Yukseltme ────────────────────────────────────────────────────
    /// <summary>
    /// Belirtilen slot seviyesi icin Gold + TechCore harcar.
    /// EconomyConfig formulune gore maliyet hesaplanir.
    /// Basarili ise true, yetersiz kaynak ise false dondurur.
    ///
    /// currentLevel: MEVCUT seviye. Yeni seviye = currentLevel + 1.
    /// </summary>
    public bool TryUpgradeSlot(int currentLevel, out string failReason)
    {
        failReason = "";
        if (config == null) { failReason = "EconomyConfig atanmamis."; return false; }

        int nextLevel = currentLevel + 1;
        if (nextLevel > 50) { failReason = "Maksimum slot seviyesi."; return false; }

        int goldCost = config.GetSlotGoldCost(currentLevel);      // mevcut level maliyeti
        int tcCost   = config.GetSlotTechCoreCost(currentLevel);

        if (Gold < goldCost)
        {
            failReason = $"Yetersiz altin. Gerekli: {goldCost}, Mevcut: {Gold}";
            return false;
        }
        if (TechCore < tcCost)
        {
            failReason = $"Yetersiz TechCore. Gerekli: {tcCost}, Mevcut: {TechCore}";
            return false;
        }

        Gold     -= goldCost;
        TechCore -= tcCost;
        Save();
        OnEconomyChanged?.Invoke();
        Debug.Log($"[Economy] Slot Lv{currentLevel}→{nextLevel} | -{goldCost}G -{tcCost}TC");
        return true;
    }

    /// <summary>Bir sonraki slot yukseltmesinin maliyetini dondurur (bilgi icin).</summary>
    public (int gold, int tc) GetUpgradeCost(int currentLevel)
    {
        if (config == null) return (0, 0);
        return (config.GetSlotGoldCost(currentLevel), config.GetSlotTechCoreCost(currentLevel));
    }

    // ── Pity Timer ────────────────────────────────────────────────────────
    /// <summary>
    /// Scroll dusmeyen her stage'de cagirilir.
    /// Esige ulasilirsa Basic Scroll garantisi tetiklenir.
    /// </summary>
    public void RegisterEmptyStage()
    {
        _emptyStageCount++;
        int threshold = config != null ? config.pityStagThreshold : 20;

        if (_emptyStageCount >= threshold)
        {
            _emptyStageCount = 0;
            OnGuaranteedScroll?.Invoke();
            Debug.Log("[Economy] Pity Timer: Guaranteed Basic Scroll!");
        }

        PlayerPrefs.SetInt(KEY_PITY, _emptyStageCount);
        PlayerPrefs.Save();
    }

    public void ResetPityCounter()
    {
        _emptyStageCount = 0;
        PlayerPrefs.SetInt(KEY_PITY, 0);
    }

    // ── Reklam ───────────────────────────────────────────────────────────
    public bool TryDoubleGoldAd()
    {
        int limit = config != null ? config.doubleGoldAdsDaily : 3;
        if (_doubleGoldAdsToday >= limit) return false;
        _doubleGoldAdsToday++;
        SaveAds();
        return true;
    }

    public bool TryBonusChestAd()
    {
        int limit = config != null ? config.bonusChestAdsDaily : 4;
        if (_bonusChestAdsToday >= limit) return false;
        _bonusChestAdsToday++;
        SaveAds();
        return true;
    }

    // TechCore reklamla alinamaz — bu metot kasitli olarak yok.

    // ── Offline Gelir ─────────────────────────────────────────────────────
    public void AddOfflineRate(int amountPerHour)
    {
        _bonusOfflineRate += amountPerHour;
        PlayerPrefs.SetInt(KEY_BONUS_RATE, _bonusOfflineRate);
        PlayerPrefs.Save();
    }

    public int GetTotalOfflineRate()
    {
        int baseRate = config != null ? config.baseOfflineRate : 50;
        return baseRate + _bonusOfflineRate;
    }

    void CollectOfflineEarnings()
    {
        string savedTime = PlayerPrefs.GetString(KEY_LAST_SAVE, "");
        if (string.IsNullOrEmpty(savedTime)) return;
        if (!DateTime.TryParse(savedTime, out DateTime lastSave)) return;

        float capHours = config != null ? config.offlineCapHours : 15f;
        double hoursGone = Mathf.Min((float)(DateTime.UtcNow - lastSave).TotalHours, capHours);
        if (hoursGone < 0.01f) return;

        int earned = Mathf.RoundToInt((float)hoursGone * GetTotalOfflineRate());
        if (earned <= 0) return;

        Gold += earned;
        Save();
        Debug.Log($"[Economy] Offline: +{earned} Altin ({hoursGone:F1} saat)");
        OnOfflineEarningCollected?.Invoke(earned);
    }

    void SaveLastTime()
    {
        PlayerPrefs.SetString(KEY_LAST_SAVE, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    // ── Gunluk Reklam Sifirla ─────────────────────────────────────────────
    void ResetDailyAdsIfNeeded()
    {
        string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        if (_lastAdResetDate != today)
        {
            _doubleGoldAdsToday = 0;
            _bonusChestAdsToday = 0;
            _lastAdResetDate    = today;
            SaveAds();
        }
    }

    void SaveAds()
    {
        PlayerPrefs.SetString(KEY_AD_DATE,  _lastAdResetDate);
        PlayerPrefs.SetInt(KEY_AD_DGOLD,    _doubleGoldAdsToday);
        PlayerPrefs.SetInt(KEY_AD_CHEST,    _bonusChestAdsToday);
        PlayerPrefs.Save();
    }

    // ── Save / Load ───────────────────────────────────────────────────────
    void Save()
    {
        PlayerPrefs.SetInt(KEY_GOLD,       Gold);
        PlayerPrefs.SetInt(KEY_TECHCORE,   TechCore);
        PlayerPrefs.SetInt(KEY_CRYSTAL,    Crystal);
        PlayerPrefs.SetInt(KEY_BONUS_RATE, _bonusOfflineRate);
        PlayerPrefs.Save();
    }

    void Load()
    {
        Gold              = PlayerPrefs.GetInt(KEY_GOLD,       0);
        TechCore          = PlayerPrefs.GetInt(KEY_TECHCORE,   0);
        Crystal           = PlayerPrefs.GetInt(KEY_CRYSTAL,    0);
        _bonusOfflineRate = PlayerPrefs.GetInt(KEY_BONUS_RATE, 0);
        _emptyStageCount  = PlayerPrefs.GetInt(KEY_PITY,       0);
        _lastAdResetDate  = PlayerPrefs.GetString(KEY_AD_DATE, "");
        _doubleGoldAdsToday = PlayerPrefs.GetInt(KEY_AD_DGOLD, 0);
        _bonusChestAdsToday = PlayerPrefs.GetInt(KEY_AD_CHEST, 0);
    }

    // ── Olaylar ───────────────────────────────────────────────────────────
    public static Action      OnEconomyChanged;
    public static Action<int> OnOfflineEarningCollected;
    public static Action      OnGuaranteedScroll;
}