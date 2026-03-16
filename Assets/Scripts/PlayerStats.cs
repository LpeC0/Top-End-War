using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v5 (Claude)
/// [DefaultExecutionOrder(-10)] → TierText bos baslamaz.
///
/// ZORLUK DENGESI:
///   startCP = 200 (Tier 1)
///   invincibility = 0.8s (dusuruldu — daha az affedici)
///   Dusman hasar CP'nin %20-40'ini alabilir (mesafeye gore)
///   Oyun Over: CP 30'un altina dusunce (10 degil)
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f; // 1.2'den dusuruldu

    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    float lastDamageTime = -99f;
    int   riskBonusLeft  = 0;
    float expectedCP     = 200f;

    static readonly int[]    tierCP    = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames =
        { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    // ── Dusman carpma hasari ──────────────────────────────────────────────────
    public void TakeContactDamage(int amount)
    {
        if (Time.time - lastDamageTime < invincibilityDuration) return;
        lastDamageTime = Time.time;

        int oldTier = CurrentTier;
        // Minimum CP = 30 (10 degil — daha gercekci game over)
        CP = Mathf.Max(30, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 30) GameEvents.OnGameOver?.Invoke();
    }

    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier = CurrentTier;
        float bonus   = riskBonusLeft > 0 ? 1.5f : 1f;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * bonus); break;
            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * data.effectValue); break;
            case GateEffectType.Merge:
                CP = Mathf.RoundToInt(CP * 1.8f);
                GameEvents.OnMergeTriggered?.Invoke(); break;
            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                PiyadePath += 20f; GameEvents.OnPathBoosted?.Invoke("Piyade"); break;
            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                MekanizePath += 20f; GameEvents.OnPathBoosted?.Invoke("Mekanize"); break;
            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                TeknolojiPath += 20f; GameEvents.OnPathBoosted?.Invoke("Teknoloji"); break;
            case GateEffectType.NegativeCP:
                CP = Mathf.Max(30, CP - Mathf.RoundToInt(data.effectValue)); break;
            case GateEffectType.RiskReward:
                int penalty = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(50, CP - penalty);
                riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft); break;
        }

        if (riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            riskBonusLeft--;
            if (riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft);
        }

        CP = Mathf.Max(30, CP);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    public void SetExpectedCP(float expected)
    {
        expectedCP = Mathf.Max(1f, expected);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
    {
        float raw = (float)CP / expectedCP;
        SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, raw, 0.08f);
    }

    void RefreshTier()
    {
        for (int i = tierCP.Length - 1; i >= 0; i--)
            if (CP >= tierCP[i]) { CurrentTier = i + 1; return; }
        CurrentTier = 1;
    }

    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total == 0) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if (Mathf.Min(p, Mathf.Min(m, t)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p > 0.5f && m > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu"); return; }
        if (p > 0.5f && t > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");   return; }
        if (m > 0.4f && t > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");  return; }
    }

    public string GetTierName()  => tierNames[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => riskBonusLeft;
}