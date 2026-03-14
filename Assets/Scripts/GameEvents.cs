using System;

/// <summary>
/// Top End War — Global Event Merkezi (Claude)
/// Observer Pattern: Tum sistemler bu static event'ler uzerinden haberlesir.
/// Namespace yok — Unity'de en basit kullanim.
/// </summary>
public static class GameEvents
{
    // ── Oyuncu ───────────────────────────────────────────────────────────────
    public static Action<int>    OnCPUpdated;          // CP degisti (yeni deger)
    public static Action<int>    OnTierChanged;        // Tier atladi (yeni tier)
    public static Action<string> OnPathBoosted;        // Path guclendirildi
    public static Action         OnMergeTriggered;     // Merge kapisi gecildi
    public static Action<string> OnSynergyFound;       // Sinerji aktif
    public static Action<int>    OnPlayerDamaged;      // Hasar alindi (HUD flash)
    public static Action         OnGameOver;           // CP min'e dustu

    // ── Risk/Reward (YENI - Claude) ──────────────────────────────────────────
    // Negatif kapidan gectikten sonra sonraki 3 kapiya %50 bonus verilir
    public static Action<int>    OnRiskBonusActivated; // kalan bonus kapı sayısı

    // ── Zorluk & Spawn (YENI - DDA sistemi icin) ────────────────────────────
    // DifficultyManager her updateInterval'da bu event'i ateşler
    public static Action<float, float> OnDifficultyChanged; // (multiplier, playerPowerRatio)

    // ── Boss ─────────────────────────────────────────────────────────────────
    public static Action OnBossEncountered;
}