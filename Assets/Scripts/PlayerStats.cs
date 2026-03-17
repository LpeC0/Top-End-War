using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi (Claude)
///
/// ONEMLI: GPT bu dosyayi "currentCP" public field ve "partial class" ile degistirmisti.
/// DOGRU versiyon: CP = property, normal class, GameEvents.On... Action pattern.
///
/// CP = can. 30'a dusunce GameOver.
/// Invincibility: 0.8s (daha zorlu).
/// Path skorlari (Piyade/Mekanize/Teknoloji) Merge kapisi icin kullanilir.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    // ── Public Properties ─────────────────────────────────────────────────
    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    // ── Private ────────────────────────────────────────────────────────────
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

    // ── Dusman carpma hasari ───────────────────────────────────────────────
    public void TakeContactDamage(int amount)
    {
        if (Time.time - lastDamageTime < invincibilityDuration) return;
        lastDamageTime = Time.time;

        int oldTier = CurrentTier;
        CP = Mathf.Max(30, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 30) GameEvents.OnGameOver?.Invoke();
    }

    // ── Oldurme odulu ──────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapi etkisi ────────────────────────────────────────────────────────
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
                HandleMerge(data); break;

            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                PiyadePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Piyade"); break;

            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                MekanizePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize"); break;

            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue * bonus);
                TeknolojiPath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji"); break;

            case GateEffectType.NegativeCP:
                CP = Mathf.Max(30, CP - Mathf.RoundToInt(data.effectValue)); break;

            case GateEffectType.RiskReward:
                int penalty = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(50, CP - penalty);
                riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(riskBonusLeft);
                Debug.Log("[PlayerStats] Risk! -" + penalty + " CP. 3 kapiya +50% bonus.");
                break;
        }

        // Risk bonus sayaci
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

    // ── Merge kapisi: path skoruna gore karar ─────────────────────────────
    void HandleMerge(GateData data)
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;

        if (total < 1f)
        {
            // Path yoksa x1.8 CP
            CP = Mathf.RoundToInt(CP * 1.8f);
            GameEvents.OnMergeTriggered?.Invoke();
            return;
        }

        float p = PiyadePath / total;
        float m = MekanizePath / total;
        float t = TeknolojiPath / total;
        float threshold = 0.5f;

        string role = "none";
        if (t >= threshold)      role = "Teknoloji";
        else if (p >= threshold) role = "Piyade";
        else if (m >= threshold) role = "Mekanize";

        if (role == "none")
        {
            // Belirgin path yok — x1.8 fallback
            CP = Mathf.RoundToInt(CP * 1.8f);
        }
        else
        {
            // Path bazli bonus: x1.8 CP + sinerji
            CP = Mathf.RoundToInt(CP * 1.8f);
            Debug.Log("[Merge] " + role + " rolune donusum! CP: " + CP);
            // Path skorlarini sifirla
            PiyadePath = MekanizePath = TeknolojiPath = 0f;
            // Gelecekte: companion spawn, MorphController'a rol gonder
        }
        GameEvents.OnMergeTriggered?.Invoke();
    }

    // ── DDA icin ───────────────────────────────────────────────────────────
    public void SetExpectedCP(float expected)
    {
        expectedCP = Mathf.Max(1f, expected);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
    {
        SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, (float)CP / expectedCP, 0.08f);
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
