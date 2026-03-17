using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v3
///
/// DENGE DEĞİŞİKLİKLERİ:
///   Kapı scale: dist/800 → dist/2400 (SpawnManager ile tutarlı)
///   Merge: x1.8 → x1.5
///   MultiplyCP: her zaman x1.2 (data.effectValue kullanılmıyor)
///   PathBoost: PiyadePath += 20f → artık sınırsız birikmiyor,
///     CheckSynergy için normalized kullanılıyor
/// </thinking>
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Başlangıç")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    public int   CP              { get; private set; }
    public int   CurrentTier     { get; private set; } = 1;
    public int   BulletCount     { get; private set; } = 1;
    public float PiyadePath      { get; private set; } = 0f;  // Sıfırdan başlar
    public float MekanizePath    { get; private set; } = 0f;
    public float TeknolojiPath   { get; private set; } = 0f;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    float _lastDmgTime   = -99f;
    int   _riskBonusLeft = 0;
    float _expectedCP    = 200f;

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

    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP = Mathf.Min(CP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;

        // DÜZELTME: Scale yavaş artıyor (dist/2400)
        float dist  = transform.position.z;
        float scale = 1f + dist / 2400f;   // 0m=1x, 1200m=1.5x

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.MultiplyCP:
                // SABIT x1.2 — data.effectValue ile manipülasyon yok
                CP = Mathf.RoundToInt(CP * 1.2f);
                break;

            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                    Debug.Log("[Player] Mermi: " + BulletCount);
                }
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.Merge:
                HandleMerge();
                break;

            case GateEffectType.PathBoost_Piyade:
                CP        += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath += 1f;   // Sayaç — CheckSynergy normalize eder
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;

            case GateEffectType.PathBoost_Mekanize:
                CP          += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Mekanize");
                break;

            case GateEffectType.PathBoost_Teknoloji:
                CP            += Mathf.RoundToInt(data.effectValue * scale * bonus);
                TeknolojiPath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;

            case GateEffectType.NegativeCP:
                CP = Mathf.Max(50, CP - Mathf.RoundToInt(data.effectValue * scale));
                break;

            case GateEffectType.RiskReward:
                int pen = Mathf.RoundToInt(CP * 0.30f);
                CP = Mathf.Max(100, CP - pen);
                _riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
                Debug.Log("[Risk] -" + pen + " CP, +%50 bonus 3 kapı.");
                break;
        }

        // Risk bonus sayacı
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0) GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        CP = Mathf.Clamp(CP, 50, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
        if (BulletCount != oldBullet) GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    void HandleMerge()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;

        // DÜZELTME: Merge x1.5 (x1.8 değil)
        CP = Mathf.RoundToInt(CP * 1.5f);

        if (total > 0f)
        {
            float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
            string role = t >= 0.5f ? "Teknoloji" : p >= 0.5f ? "Piyade" : m >= 0.5f ? "Mekanize" : "none";
            if (role != "none")
            {
                PiyadePath = MekanizePath = TeknolojiPath = 0f;
                Debug.Log("[Merge] " + role + " → CP: " + CP);
            }
        }
        GameEvents.OnMergeTriggered?.Invoke();
    }

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

    // Path skorları sayaç olarak tutuluyor (1=1 kapı geçişi)
    // Normalize edilerek sinerji kontrolü yapılıyor
    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 1f) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;

        string syn = null;
        if (Mathf.Min(p, Mathf.Min(m, t)) > 0.25f) syn = "PERFECT GENETICS";
        else if (p > 0.5f && m > 0.25f)             syn = "Exosuit Komutu";
        else if (p > 0.5f && t > 0.25f)             syn = "Drone Takimi";
        else if (m > 0.4f && t > 0.3f)              syn = "Fuzyon Robotu";

        if (syn != null) GameEvents.OnSynergyFound?.Invoke(syn);
    }

    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}