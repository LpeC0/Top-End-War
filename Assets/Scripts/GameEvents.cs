using System;

/// <summary>
/// Top End War — Oyun Olaylari v5 (Claude)
/// Tum v4 eventleri korundu + Boss/Dunya eventleri eklendi.
/// KURAL: Raise() yok — dogrudan ?.Invoke() kullan.
/// </summary>
public static class GameEvents
{
    public struct StageClearInfo
    {
        public int worldID;
        public int stageID;
        public string stageName;
        public int goldReward;
        public bool hasNextStage;
        public bool worldCleared;
    }

    // ── Oyuncu / Komutan ─────────────────────────────────────────────────
    public static Action<int>        OnCPUpdated;
    public static Action<int>        OnBulletCountChanged;
    public static Action<int>        OnTierChanged;
    public static Action<int, int>   OnCommanderHPChanged;    // (current, max)
    public static Action<int, int>   OnCommanderDamaged;      // (finalDmg, currentHP)
    public static Action<int>        OnCommanderHealed;
    public static Action<int>        OnPlayerDamaged;

    // ── Ordu ────────────────────────────────────────────────────────────
    public static Action<int>        OnSoldierAdded;          // (toplam asker sayisi)
    public static Action<int>        OnSoldierRemoved;        // (toplam asker sayisi)
    public static Action<string,int> OnSoldierMerged;         // (path adı, yeni level) ← DUZELTILDI
    public static Action<int>        OnSoldierHPRestored;
    public static Action<int>        OnSoldierCountChanged;

    // ── Yol / Sinerji ────────────────────────────────────────────────────
    public static Action             OnMergeTriggered;
    public static Action<string>     OnPathBoosted;
    public static Action<string>     OnSynergyFound;

    // ── Kapi / Risk ──────────────────────────────────────────────────────
    public static Action<int>        OnRiskBonusActivated;

    // ── Zorluk / Spawn ───────────────────────────────────────────────────
    // SpawnManager (float multiplier, float powerRatio) olarak kullaniyor
    public static Action<float,float> OnDifficultyChanged;    // ← DUZELTILDI (2 param)
    public static Action              OnBossEncountered;

    // ── Anchor / Boss ────────────────────────────────────────────────────
    public static Action<bool>       OnAnchorModeChanged;
    public static Action<int, int>   OnBossHPChanged;         // (current, max)
    public static Action<int>        OnBossPhaseShield;       // (gelen faz: 2 veya 3)
    public static Action<int>        OnBossPhaseChanged;
    public static Action<float>      OnBossEnraged;
    public static Action             OnBossDefeated;

    // ── Oyun Akisi ────────────────────────────────────────────────────────
    public static Action             OnGameOver;
    public static Action             OnVictory;
    public static Action<StageClearInfo> OnStageCleared;

    // ── Biyom / Dunya ────────────────────────────────────────────────────
    public static Action<string>     OnBiomeChanged;
    public static Action<int>        OnWorldChanged;
    public static Action<int, int>   OnStageChanged;          // (worldID, stageID)
}
