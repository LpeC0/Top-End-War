using System;

/// <summary>
/// Top End War — Global Event Merkezi
/// Tüm sistemler buradan haberleşir. Hiçbir script diğerini doğrudan aramaz.
/// </summary>
public static class GameEvents
{
    public static Action<int>    OnCPUpdated;       // CP değişti → UI güncelle
    public static Action<int>    OnTierChanged;     // Tier atladı → Morph tetikle
    public static Action<string> OnPathBoosted;     // "Piyade" / "Mekanize" / "Teknoloji"
    public static Action         OnMergeTriggered;  // Merge kapısı → Tier atlama anı
    public static Action<string> OnSynergyFound;    // Sinerji tespit edildi
}
