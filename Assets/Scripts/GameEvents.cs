using System;

/// <summary>
/// Top End War — Global Event Merkezi (Claude)
/// </summary>
public static class GameEvents
{
    public static Action<int>    OnCPUpdated;
    public static Action<int>    OnTierChanged;
    public static Action<string> OnPathBoosted;
    public static Action         OnMergeTriggered;
    public static Action<string> OnSynergyFound;
    public static Action<int>    OnPlayerDamaged;   // Hasar flash
    public static Action         OnGameOver;
}