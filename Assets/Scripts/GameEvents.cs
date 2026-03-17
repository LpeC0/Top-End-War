using System;

/// <summary>
/// Top End War — Global Event Merkezi (Claude)
/// DIKKAT: GPT bu dosyayi event Action + Raise... metodlarina cevirmisti — YANLIS.
/// Bizim sistemimiz basit static Action<> kullaniyor, abonelik += ile yapiliyor.
/// </summary>
public static class GameEvents
{
    // Oyuncu
    public static Action<int>    OnCPUpdated;
    public static Action<int>    OnTierChanged;
    public static Action<string> OnPathBoosted;
    public static Action         OnMergeTriggered;
    public static Action<string> OnSynergyFound;
    public static Action<int>    OnPlayerDamaged;
    public static Action         OnGameOver;

    // Risk/Reward
    public static Action<int>    OnRiskBonusActivated;

    // Zorluk (DDA)
    public static Action<float, float> OnDifficultyChanged;

    // Boss
    public static Action OnBossEncountered;
}
