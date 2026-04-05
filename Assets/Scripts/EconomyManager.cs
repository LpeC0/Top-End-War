using UnityEngine;
using System;

/// <summary>
/// Top End War — Ekonomi Yoneticisi v1 (Claude)
///
/// Uc para birimi:
///   Altin (Soft Currency)  — her yerden duser, slot leveling icin
///   TechCore               — sadece skill-based drop (boss fazi, combo, vs.)
///   Kristal (Hard Currency) — nadir drop veya satin alma
///
/// TECH CORE KURALI: Magazadan satin alinamaz. Sadece:
///   - Boss fazi atlatmak (Phase Shield gectikten sonra)
///   - Kusursuz kapi serisi
///   - Combo / ozel basari
///
/// Offline kazanc:
///   - Her gecilen stage altinHiz'i arttirir
///   - Uygulama kapaninca DateTime kaydedilir
///   - Tekrar acilinca gecen sure hesaplanir
///   - Offline cap: 15 saat
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // ── Para Birimleri ────────────────────────────────────────────────────
    public int Gold      { get; private set; }
    public int TechCore  { get; private set; }
    public int Crystal   { get; private set; }

    // ── Offline Kazanc ────────────────────────────────────────────────────
    [Header("Offline Kazanc")]
    [Tooltip("Baslangic altinHizi (stage gecmeden once)")]
    public int baseOfflineRate = 50;          // Altin / saat

    [Tooltip("Offline kazanc ust siniri (saat)")]
    public float offlineCapHours = 15f;

    private int _bonusOfflineRate = 0;        // Stage gecilenlerden gelen ek kazanc

    // ── PlayerPrefs Anahtarlari ───────────────────────────────────────────
    const string KEY_GOLD       = "Economy_Gold";
    const string KEY_TECHCORE   = "Economy_TechCore";
    const string KEY_CRYSTAL    = "Economy_Crystal";
    const string KEY_BONUS_RATE = "Economy_BonusRate";
    const string KEY_LAST_SAVE  = "Economy_LastSaveTime";

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
        CollectOfflineEarnings();
    }

    void OnApplicationPause(bool paused)  { if (paused)  SaveLastTime(); }
    void OnApplicationQuit()              { SaveLastTime(); }

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

    // ── Offline Kazanc ────────────────────────────────────────────────────
    /// <summary>Stage gecelince cagrilir. Kalici altinHiz artisi saglar.</summary>
    public void AddOfflineRate(int amountPerHour)
    {
        _bonusOfflineRate += amountPerHour;
        PlayerPrefs.SetInt(KEY_BONUS_RATE, _bonusOfflineRate);
        PlayerPrefs.Save();
    }

    public int GetTotalOfflineRate() => baseOfflineRate + _bonusOfflineRate;

    void CollectOfflineEarnings()
    {
        string savedTime = PlayerPrefs.GetString(KEY_LAST_SAVE, "");
        if (string.IsNullOrEmpty(savedTime)) return;

        if (!DateTime.TryParse(savedTime, out DateTime lastSave)) return;

        double hoursGone = (DateTime.UtcNow - lastSave).TotalHours;
        hoursGone = Mathf.Min((float)hoursGone, offlineCapHours);

        if (hoursGone < 0.01f) return;

        int earned = Mathf.RoundToInt((float)hoursGone * GetTotalOfflineRate());
        if (earned <= 0) return;

        Gold += earned;
        Save();

        Debug.Log($"[EconomyManager] Offline kazanc: {earned} Altin ({hoursGone:F1} saat)");
        OnOfflineEarningCollected?.Invoke(earned);
    }

    void SaveLastTime()
    {
        PlayerPrefs.SetString(KEY_LAST_SAVE, DateTime.UtcNow.ToString("o"));
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
        Gold             = PlayerPrefs.GetInt(KEY_GOLD,       0);
        TechCore         = PlayerPrefs.GetInt(KEY_TECHCORE,   0);
        Crystal          = PlayerPrefs.GetInt(KEY_CRYSTAL,    0);
        _bonusOfflineRate = PlayerPrefs.GetInt(KEY_BONUS_RATE, 0);
    }

    // ── Olaylar ───────────────────────────────────────────────────────────
    public static Action    OnEconomyChanged;
    public static Action<int> OnOfflineEarningCollected;  // (kazanilan altin)
}