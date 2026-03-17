using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v2
///
/// DEGİSİKLİK:
///   Tier esikleri guncellendi: {0, 500, 1500, 4000, 9000}
///   Simulate sonucu: %14 T3, %56 T4, %30 T5 boss karşısında — dengeli.
///   AddBullet gate: PlayerStats.BulletCount artirir.
///   MultiplyCP artik sadece x1.3.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    // ── Public Properties ──────────────────────────────────────────────────
    public int   CP              { get; private set; }
    public int   CurrentTier     { get; private set; } = 1;
    public int   BulletCount     { get; private set; } = 1;  // YENİ
    public float PiyadePath      { get; private set; } = 33f;
    public float MekanizePath    { get; private set; } = 33f;
    public float TeknolojiPath   { get; private set; } = 34f;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    // ── Private ────────────────────────────────────────────────────────────
    float _lastDmgTime  = -99f;
    int   _riskBonusLeft= 0;
    float _expectedCP   = 200f;

    // Guncellenmis tier esikleri — simülasyon ile onaylandi
    static readonly int[]    TIER_CP    = { 0, 500, 1500, 4000, 9000 };
    static readonly string[] TIER_NAMES =
        { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
    const int MAX_BULLETS = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP = startCP;
    }

    void Start() => GameEvents.OnCPUpdated?.Invoke(CP);

    // ── Dusman carpma ──────────────────────────────────────────────────────
    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        int oldTier = CurrentTier;
        CP = Mathf.Max(50, CP - amount);
        RefreshTier();

        GameEvents.OnPlayerDamaged?.Invoke(amount);
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (CP <= 50) GameEvents.OnGameOver?.Invoke();
    }

    // ── Kill odulu ─────────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP += amount;
        CP = Mathf.Min(CP, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapi etkisi ────────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;

        // Mesafeye gore kapı değer scale (boss'a yakın kapılar daha değerli)
        float dist  = transform.position.z;
        float scale = 1f + dist / 800f;   // 0m=1x, 800m=2x, 1200m=2.5x

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.MultiplyCP:
                // x1.3 sabit — data.effectValue kullanılmıyor artık
                CP = Mathf.RoundToInt(CP * 1.3f);
                break;

            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                }
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                Debug.Log("[PlayerStats] Mermi sayisi: " + BulletCount);
                break;

            case GateEffectType.Merge:
                HandleMerge(data);
                break;

            case GateEffectType.PathBoost_Piyade:
                CP      += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;

            case GateEffectType.PathBoost_Mekanize:
                CP       += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize");
                break;

            case GateEffectType.PathBoost_Teknoloji:
                CP        += Mathf.RoundToInt(data.effectValue * scale * bonus);
                TeknolojiPath += 20f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;

            case GateEffectType.NegativeCP:
                CP = Mathf.Max(50, CP - Mathf.RoundToInt(data.effectValue * scale));
                break;

            case GateEffectType.RiskReward:
                int penalty = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(100, CP - penalty);
                _riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
                Debug.Log("[PlayerStats] RISK! -" + penalty + " CP, 3 kapıya +%50 bonus.");
                break;
        }

        // Risk bonus sayaci
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0) GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        CP = Mathf.Max(50, Mathf.Min(CP, 99999));
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (BulletCount != oldBullet) GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    // ── Merge ─────────────────────────────────────────────────────────────
    void HandleMerge(GateData data)
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;

        if (total < 1f)
        {
            CP = Mathf.RoundToInt(CP * 1.8f);
            GameEvents.OnMergeTriggered?.Invoke();
            return;
        }

        float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;
        string role = t >= 0.5f ? "Teknoloji" : p >= 0.5f ? "Piyade" : m >= 0.5f ? "Mekanize" : "none";

        CP = Mathf.RoundToInt(CP * 1.8f);
        if (role != "none")
        {
            PiyadePath = MekanizePath = TeknolojiPath = 0f;
            Debug.Log("[Merge] " + role + " rolune donusum! CP: " + CP);
        }
        GameEvents.OnMergeTriggered?.Invoke();
    }

    // ── DDA ───────────────────────────────────────────────────────────────
    public void SetExpectedCP(float e)
    {
        _expectedCP = Mathf.Max(1f, e);
        UpdateSmoothedRatio();
    }

    void UpdateSmoothedRatio()
        => SmoothedPowerRatio = Mathf.Lerp(SmoothedPowerRatio, (float)CP / _expectedCP, 0.08f);

    void RefreshTier()
    {
        for (int i = TIER_CP.Length - 1; i >= 0; i--)
            if (CP >= TIER_CP[i]) { CurrentTier = i + 1; return; }
        CurrentTier = 1;
    }

    void CheckSynergy()
    {
        float t = PiyadePath + MekanizePath + TeknolojiPath;
        if (t == 0) return;
        float p = PiyadePath/t, m = MekanizePath/t, tk = TeknolojiPath/t;
        if (Mathf.Min(p, Mathf.Min(m, tk)) > 0.25f) { GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS"); return; }
        if (p > 0.5f && m > 0.25f)  { GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu"); return; }
        if (p > 0.5f && tk > 0.25f) { GameEvents.OnSynergyFound?.Invoke("Drone Takimi");  return; }
        if (m > 0.4f && tk > 0.3f)  { GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu"); return; }
    }

    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}