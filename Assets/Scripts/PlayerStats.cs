using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v4
///
/// MERGE DÜZELTMESİ:
///   Path yoksa:      x1.1 (çok küçük bonus — yanlış kapıya girme cezası)
///   Path var ama <%50: x1.2 (biraz bonus)
///   Bir path >= %50: x1.5 (stratejik ödül)
///   Perfect Genetics: x1.7 + sinerji mesajı
///
///   Bu sayede Merge kapısı:
///   → PathBoost kapılarını önceden geçmek anlamlı hale geliyor
///   → "Beynini kullan" mekaniği çalışıyor
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
    public float PiyadePath      { get; private set; } = 0f;
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

    // ── Düşman çarpma ──────────────────────────────────────────────────────
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

    // ── Kill ödülü ─────────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP = Mathf.Min(CP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }

    // ── Kapı etkisi ────────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.MultiplyCP:
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
                PiyadePath += 1f;
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
                break;
        }

        // Risk bonus sayacı (negatif ve risk kapıları saymaz)
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

    // ── Merge: STRATEJİK (path bazlı) ─────────────────────────────────────
    //
    // MANTIK:
    //   Merge kapısı tek başına anlamsız — önceki PathBoost kapıları
    //   onunla birlikte anlam kazanır. Oyuncu farkında olmadan
    //   "path biriktiriyorum → merge = büyük ödül" mantığına giriyor.
    //
    //   Path yoksa    → x1.1  (minimum bonus — yanlış seçim)
    //   Karma path    → x1.2  (biraz topladın ama odaklanmadın)
    //   Dominant path → x1.5  (odaklandın, ödüllendiriliyorsun)
    //   Perfect       → x1.7  (3 path dengeli — çok zor, büyük ödül)
    //
    void HandleMerge()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float multiplier;
        string role = "none";

        if (total < 1f)
        {
            // Path yok — minimum bonus
            multiplier = 1.1f;
            Debug.Log("[Merge] Path yok → x1.1 (PathBoost kapılarına gir!)");
        }
        else
        {
            float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
            float minPath = Mathf.Min(p, Mathf.Min(m, t));

            if (minPath > 0.28f)
            {
                // Perfect Genetics — 3 path dengeli
                multiplier = 1.7f;
                role = "PERFECT";
                Debug.Log("[Merge] PERFECT GENETICS → x1.7!");
            }
            else if (t >= 0.5f) { multiplier = 1.5f; role = "Teknoloji"; }
            else if (p >= 0.5f) { multiplier = 1.5f; role = "Piyade"; }
            else if (m >= 0.5f) { multiplier = 1.5f; role = "Mekanize"; }
            else
            {
                // Karma path ama dominant yok
                multiplier = 1.2f;
                Debug.Log("[Merge] Karma path → x1.2");
            }
        }

        CP = Mathf.RoundToInt(CP * multiplier);

        if (role != "none" && role != "PERFECT")
        {
            Debug.Log("[Merge] " + role + " dominant → x1.5, CP: " + CP);
            PiyadePath = MekanizePath = TeknolojiPath = 0f; // Skor sıfırla
        }
        else if (role == "PERFECT")
        {
            PiyadePath = MekanizePath = TeknolojiPath = 0f;
            GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS!");
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
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 2f) return; // En az 2 PathBoost geçilmeli
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;

        if (p > 0.5f && m > 0.25f)  GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.3f)  GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}