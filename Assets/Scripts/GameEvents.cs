using System;

public static class GameEvents
{
    public static Action<int>         OnCPUpdated;
    public static Action<int>         OnTierChanged;
    public static Action<int>         OnBulletCountChanged;
    public static Action<string>      OnPathBoosted;
    public static Action              OnMergeTriggered;
    public static Action<string>      OnSynergyFound;
    public static Action<int>         OnPlayerDamaged;
    public static Action              OnGameOver;
    public static Action<int>         OnRiskBonusActivated;
    public static Action<float,float> OnDifficultyChanged;
    public static Action              OnBossEncountered;
    public static Action<bool>        OnAnchorModeChanged;  // true=boss sahnesi, false=normal
}