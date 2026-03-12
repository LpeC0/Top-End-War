using System;

/// <summary>
/// Top End War — Global Event Merkezi (Claude)
/// Bu dosya degismeden kalsin — tum scriptler buraya bagli.
/// </summary>
public static class GameEvents
{
    public static Action<int>    OnCPUpdated;       // CP degisti
    public static Action<int>    OnTierChanged;     // Tier atladi
    public static Action<string> OnPathBoosted;     // Path degisti
    public static Action         OnMergeTriggered;  // Merge kapisi
    public static Action<string> OnSynergyFound;    // Sinerji
    public static Action<int>    OnPlayerDamaged;   // Hasar flash (GameHUD dinler)
    public static Action         OnGameOver;        // CP bitti
}