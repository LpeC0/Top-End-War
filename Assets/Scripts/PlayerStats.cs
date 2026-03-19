using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Veri Merkezi v5 (Claude)
///
/// v5 DEGISIKLIKLER:
///   - CommanderHP sistemi eklendi (CP'den BAGIMSIZ)
///   - TakeContactDamage artik CommanderHP'yi dusuruyor, CP'yi degil
///   - Yeni gate tipleri: AddSoldier_*, HealCommander, HealSoldiers
///   - PathBoost_* hala calisir (geriye donuk uyum)
///   - AddBullet legacy korundu (BulletCount arttirır, AddSoldier_Piyade gibi davranır)
///
/// UNITY NOTU:
///   - [DefaultExecutionOrder(-10)] — Start'ta PlayerStats.Instance hazir olmali
///   - Player GameObject'e ekle, tag "Player" olmali
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ── Inspector Ayarlari ───────────────────────────────────────────────
    [Header("Baslangic CP")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    // ── CP + Tier ─────────────────────────────────────────────────────────
    public int   CP           { get; private set; }
    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;   // Komutan spread

    // ── Path skorlari (PathBoost kapilari icin) ───────────────────────────
    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    // ── Komutan HP (v5 — CP'den bagimsiz) ────────────────────────────────
    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;

    // ── DDA (DifficultyManager icin) ─────────────────────────────────────
    public float SmoothedPowerRatio { get; private set; } = 1f;

    // ── Dahili ────────────────────────────────────────────────────────────
    float _lastDmgTime    = -99f;
    int   _riskBonusLeft  = 0;
    float _expectedCP     = 200f;

    static readonly int[]    TIER_CP    = { 0, 500, 1500, 4000, 9000 };
    static readonly string[] TIER_NAMES =
        { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
    // Komutan max HP tier'e gore (RefreshTier'da guncellenir)
    static readonly int[]    COMMANDER_HP_BY_TIER = { 500, 700, 950, 1200, 1500 };
    const  int MAX_BULLETS = 5;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CP           = startCP;
        CommanderMaxHP = COMMANDER_HP_BY_TIER[0];
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Komutan Hasar Alma (v5) ───────────────────────────────────────────
    /// <summary>
    /// Dusman temasinda veya boss saldirısında cagrilir.
    /// CP ARTIK DUSMEZ — sadece CommanderHP azalir.
    /// </summary>
    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        CommanderHP = Mathf.Max(0, CommanderHP - amount);
        GameEvents.OnCommanderDamaged?.Invoke(amount, CommanderHP);
        GameEvents.OnPlayerDamaged?.Invoke(amount);   // hasar flash icin
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

        if (CommanderHP <= 0) GameEvents.OnGameOver?.Invoke();
    }

    /// <summary>Komutan HP'yi yeniler (HealCommander kapisi).</summary>
    public void HealCommander(int amount)
    {
        CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + amount);
        GameEvents.OnCommanderHealed?.Invoke(CommanderHP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
        Debug.Log($"[Commander] Heal +{amount} → {CommanderHP}/{CommanderMaxHP}");
    }

    // ── Kill Odulu ───────────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        CP = Mathf.Min(CP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── Kapi Etkisi ──────────────────────────────────────────────────────
    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

        switch (data.effectType)
        {
            // ── Var olan kapı tipleri ────────────────────────────────────
            case GateEffectType.AddCP:
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.MultiplyCP:
                CP = Mathf.RoundToInt(CP * 1.2f);
                break;

            // AddBullet: eski sahnelerle uyumluluk — artık piyade asker de ekler
            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                }
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade);
                break;

            case GateEffectType.Merge:
                HandleMerge();
                break;

            case GateEffectType.PathBoost_Piyade:
                CP        += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;

            case GateEffectType.PathBoost_Mekanize:
                CP          += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Mekanik");
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

            // ── v3 Yeni Kapi Tipleri ─────────────────────────────────────
            case GateEffectType.AddSoldier_Piyade:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.AddSoldier_Mekanik:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.AddSoldier_Teknoloji:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: 2);
                CP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;

            case GateEffectType.HealCommander:
                HealCommander(Mathf.RoundToInt(data.effectValue));
                break;

            case GateEffectType.HealSoldiers:
                float healPct = Mathf.Clamp(data.effectValue, 0f, 1f);
                ArmyManager.Instance?.HealAll(healPct);
                ShowPopupMessage($"ASKER +%{Mathf.RoundToInt(healPct * 100)}");
                break;
        }

        // Risk bonus sayaci
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        CP = Mathf.Clamp(CP, 50, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    // ── Merge: path-bazli carpan ─────────────────────────────────────────
    void HandleMerge()
    {
        // Asker merge (yeni sistem)
        bool mergeOccurred = ArmyManager.Instance != null &&
                             ArmyManager.Instance.TryMerge();

        // CP carpani (eski+yeni birlikte calisir)
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float multiplier;
        string role = "none";

        if (total < 1f)
        {
            multiplier = 1.1f;
        }
        else
        {
            float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
            float minPath = Mathf.Min(p, Mathf.Min(m, t));
            if (minPath > 0.28f)
            {
                multiplier = 1.7f; role = "PERFECT";
                GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS!");
            }
            else if (t >= 0.5f) { multiplier = 1.5f; role = "Teknoloji"; }
            else if (p >= 0.5f) { multiplier = 1.5f; role = "Piyade"; }
            else if (m >= 0.5f) { multiplier = 1.5f; role = "Mekanik"; }
            else                { multiplier = 1.2f; }
        }

        CP = Mathf.RoundToInt(CP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();

        Debug.Log($"[Merge] CP x{multiplier} | Asker merge: {mergeOccurred}");
    }

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged()
    {
        // Komutan max HP'yi guncelle (HP eksik dusmesin — fark kadar ekle)
        int oldMax = CommanderMaxHP;
        CommanderMaxHP = COMMANDER_HP_BY_TIER[Mathf.Clamp(CurrentTier - 1, 0, 4)];
        if (CommanderMaxHP > oldMax)
        {
            int bonus = CommanderMaxHP - oldMax;
            CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + bonus);
        }
        GameEvents.OnTierChanged?.Invoke(CurrentTier);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── DDA Yardimcilari ─────────────────────────────────────────────────
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

    // ── Sinerji ──────────────────────────────────────────────────────────
    void CheckSynergy()
    {
        float total = PiyadePath + MekanizePath + TeknolojiPath;
        if (total < 2f) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    // ── Popup yardimci (HUD yok olabilir) ────────────────────────────────
    void ShowPopupMessage(string msg)
        => GameEvents.OnSynergyFound?.Invoke(msg); // HUD popup bunu yakaliyor

    // ── Getterlar ─────────────────────────────────────────────────────────
    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}