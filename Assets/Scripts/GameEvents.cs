using System;

/// <summary>
/// Top End War — Global Event Merkezi v4
/// KURAL: Raise...() metod YOK. Kullanim: GameEvents.OnXxx?.Invoke(param);
/// v4: Komutan HP + Asker + Biyom eventleri eklendi.
/// </summary>
public static class GameEvents
{
    // ── Temel ─────────────────────────────────────────────────────────────
    public static Action<int>          OnCPUpdated;
    public static Action<int>          OnTierChanged;
    public static Action<int>          OnBulletCountChanged;
    public static Action<string>       OnPathBoosted;
    public static Action               OnMergeTriggered;
    public static Action<string>       OnSynergyFound;
    public static Action<int>          OnPlayerDamaged;       // flash icin
    public static Action               OnGameOver;
    public static Action<int>          OnRiskBonusActivated;
    public static Action<float, float> OnDifficultyChanged;
    public static Action               OnBossEncountered;
    public static Action<bool>         OnAnchorModeChanged;   // true=boss sahnesi

    // ── Komutan HP (v4) ──────────────────────────────────────────────────
    public static Action<int, int>     OnCommanderDamaged;    // (hasar, kalanHP)
    public static Action<int>          OnCommanderHealed;     // kalanHP
    public static Action<int, int>     OnCommanderHPChanged;  // (current, max)

    // ── Asker (v4) ───────────────────────────────────────────────────────
    public static Action<int>          OnSoldierAdded;        // toplam asker sayisi
    public static Action<int>          OnSoldierRemoved;      // toplam asker sayisi
    public static Action<string, int>  OnSoldierMerged;       // (path, yeni level)
    public static Action<int>          OnSoldierHPRestored;   // geri yuklenen HP toplami

    // ── Biyom (v4) ───────────────────────────────────────────────────────
    public static Action<string>       OnBiomeChanged;        // "Tas", "Orman" vb.
}