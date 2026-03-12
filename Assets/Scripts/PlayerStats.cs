using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi (Claude)
/// [DefaultExecutionOrder(-10)] → TierText bos baslamaz.
/// DIKKAT: GameEvents.cs'de OnPlayerDamaged ve OnGameOver olmali!
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int startCP = 200;

    [Header("Hasar Korumasi")]
    public float invincibilityDuration = 1.2f;

    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;

    float lastDamageTime = -99f;

    static readonly int[] tierCP = { 0, 300, 800, 2000, 5000 };
    static readonly string[] tierNames =
    {
        "Gonullu Er",
        "Elit Komando",
        "Gatling Timi",
        "Hava Indirme",
        "Suru Drone"
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    // ── Dushmana carpma hasari ────────────────────────────────────────────
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

    // ── Oldurme odulu ─────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapi etkisi ───────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int oldTier = CurrentTier;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue); break;
            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * data.effectValue); break;
            case GateEffectType.Merge:
                CP = Mathf.RoundToInt(CP * 1.8f);
                GameEvents.OnMergeTriggered?.Invoke(); break;
            case GateEffectType.PathBoost_Piyade:
                CP += Mathf.RoundToInt(data.effectValue);
                PiyadePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Piyade"); break;
            case GateEffectType.PathBoost_Mekanize:
                CP += Mathf.RoundToInt(data.effectValue);
                MekanizePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize"); break;
            case GateEffectType.PathBoost_Teknoloji:
                CP += Mathf.RoundToInt(data.effectValue);
                TeknolojiPath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji"); break;
            case GateEffectType.NegativeCP:
                CP = Mathf.Max(20, CP - Mathf.RoundToInt(data.effectValue)); break;
        }

        CP = Mathf.Max(10, CP);
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
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
        float p = PiyadePath / total;
        float m = MekanizePath / total;
        float t = TeknolojiPath / total;

        if (Mathf.Min(p, Mathf.Min(m, t)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p > 0.5f && m > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");  return; }
        if (p > 0.5f && t > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");    return; }
        if (m > 0.4f && t > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");   return; }
    }

    public string GetTierName() => tierNames[Mathf.Clamp(CurrentTier - 1, 0, 4)];
}