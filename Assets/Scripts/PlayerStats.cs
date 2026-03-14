using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi (Claude)
/// - CP = can (sifira yaklasinca GameOver)
/// - RiskReward: Negatif kapidan sonra 3 kapiya %50 bonus
/// - PlayerPowerRatio: Son 30 saniye yumusatilmis (DDA icin)
/// [DefaultExecutionOrder(-10)] → TierText bos baslamaz.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int   startCP                = 200;
    public float invincibilityDuration  = 1.2f;

    // ── Public Properties ────────────────────────────────────────────────────
    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;

    // DDA icin — DifficultyManager okur
    public float SmoothedPowerRatio { get; private set; } = 1f;

    // ── Private ──────────────────────────────────────────────────────────────
    float lastDamageTime  = -99f;
    int   riskBonusLeft   = 0;   // RiskReward: kac kapiya bonus kaldi
    float expectedCP      = 200f; // DifficultyManager her updateInterval'da yazar

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
        CP = Mathf.Max(10, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 10) GameEvents.OnGameOver?.Invoke();
    }

    // ── Oldurme odulu ─────────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapi etkisi ───────────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int oldTier = CurrentTier;

        // RiskReward bonus varsa bu kapiya uygula
        float bonusMultiplier = (riskBonusLeft > 0) ? 1.5f : 1f;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * bonusMultiplier);
                break;
            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * data.effectValue);
                break;
            case GateEffectType.Merge:
                CP = Mathf.RoundToInt(CP * 1.8f);
                GameEvents.OnMergeTriggered?.Invoke();
                break;
            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue * bonusMultiplier);
                PiyadePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;
            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue * bonusMultiplier);
                MekanizePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize");
                break;
            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue * bonusMultiplier);
                TeknolojiPath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;
            case GateEffectType.NegativeCP:
                // Saf ceza — az cikacak (%2-3), bonus etkilemez
                CP = Mathf.Max(20, CP - Mathf.RoundToInt(data.effectValue));
                break;

            // ── RISK / REWARD (Claude) ────────────────────────────────────
            // Negatif kapidan gec: -30% CP kaybet
            // Karsilik: sonraki 3 AddCP/PathBoost kapısına +50% bonus
            case GateEffectType.RiskReward:
                int penalty = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(30, CP - penalty);
                riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft);
                Debug.Log("[PlayerStats] Risk alindi! Sonraki 3 kapiya +50% bonus. Ceza: " + penalty);
                break;
        }

        // Risk bonus sayacini azalt (cezali kapılar haric)
        if (riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            riskBonusLeft--;
            if (riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft);
        }

        CP = Mathf.Max(10, CP);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── DDA icin: DifficultyManager cagirır ───────────────────────────────────
    public void SetExpectedCP(float expected)
    {
        expectedCP = Mathf.Max(1f, expected);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
    {
        float rawRatio = (float)CP / expectedCP;
        // Son 30 saniye ortalamasını simule et — ani spike'lari yumusat (ChatGPT onerisi)
        SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, rawRatio, 0.08f);
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
        float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;

        if (Mathf.Min(p, Mathf.Min(m, t)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p > 0.5f && m > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");  return; }
        if (p > 0.5f && t > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");    return; }
        if (m > 0.4f && t > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");   return; }
    }

    public string GetTierName()  => tierNames[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => riskBonusLeft;
}