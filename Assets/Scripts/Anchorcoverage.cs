using UnityEngine;

/// <summary>
/// Top End War — Anchor Coverage System v1.0
///
/// Anchor modda oyuncunun pozisyonu (stance) ile düşmanın
/// geldiği lane arasındaki hasar verimini tanımlar.
///
/// Kullanım:
///   float mult = AnchorCoverage.GetMultiplier(playerStance, enemyLane);
///   finalDamage = Mathf.RoundToInt(rawDamage * mult);
/// </summary>
public enum AnchorLane
{
    Left,
    Center,
    Right,
}

public enum AnchorStance
{
    Left,
    Center,
    Right,
}

public static class AnchorCoverage
{
    // DEĞİŞİKLİK: Center-only güvenliğini kırmak için yan/t ters lane verimi sertleştirildi.
    //           Enemy: Left   Center   Right
    // Left              1.00   0.45    0.10
    // Center            0.35   1.00    0.35
    // Right             0.10   0.45    1.00
    static readonly float[,] _table = new float[3, 3]
    {
        { 1.00f, 0.45f, 0.10f },   // Player Left
        { 0.35f, 1.00f, 0.35f },   // Player Center
        { 0.10f, 0.45f, 1.00f },   // Player Right
    };

    public static float GetMultiplier(AnchorStance stance, AnchorLane enemyLane)
    {
        return _table[(int)stance, (int)enemyLane];
    }

    public static AnchorStance LastStance { get; private set; } = AnchorStance.Center;
    public static AnchorLane LastEnemyLane { get; private set; } = AnchorLane.Center;
    public static float LastMultiplier { get; private set; } = 1f;
    public static float LastReportTime { get; private set; } = -999f;

    public static void ReportHit(AnchorStance stance, AnchorLane enemyLane, float multiplier)
    {
        LastStance = stance;
        LastEnemyLane = enemyLane;
        LastMultiplier = multiplier;
        LastReportTime = Time.time;
    }

    public static string GetQualityLabel(float multiplier)
    {
        if (multiplier >= 0.90f) return "FULL";
        if (multiplier >= 0.45f) return "MED";
        return "BAD";
    }

    /// <summary>
    /// Player X pozisyonundan stance hesaplar.
    /// x < -2.5 → Left | -2.5..2.5 → Center | x > 2.5 → Right
    /// </summary>
    public static AnchorStance StanceFromX(float x)
    {
        if (x < -2.5f) return AnchorStance.Left;
        if (x >  2.5f) return AnchorStance.Right;
        return AnchorStance.Center;
    }

    /// <summary>
    /// LaneBias → AnchorLane dönüşümü.
    /// Spread ve Random için dağıtım AnchorSpawnController'da yapılır.
    /// </summary>
    public static AnchorLane LaneFromBias(LaneBias bias)
    {
        return bias switch
        {
            LaneBias.Left   => AnchorLane.Left,
            LaneBias.Right  => AnchorLane.Right,
            LaneBias.Center => AnchorLane.Center,
            _               => AnchorLane.Center,
        };
    }

    /// <summary>
    /// Lane → Spawn X pozisyonu.
    /// </summary>
    public static float LaneToSpawnX(AnchorLane lane)
    {
        return lane switch
        {
            AnchorLane.Left  => -4.8f,
            AnchorLane.Right =>  4.8f,
            _                =>  0.0f,
        };
    }
}
