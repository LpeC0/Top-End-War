using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic Ayarlari")]
    public int   startCP               = 200;
    public float invincibilityDuration = 0.8f;

    // YENİ: Kuşanılmış Eşyalar (ScriptableObjects)
    [Header("Kuşanılmış Eşyalar (ScriptableObjects)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public PetData equippedPet;

    // YENİ MİMARİ: _baseCP oyun içi ham puanı tutar.
    private int _baseCP;

    // CP artık baseCP ve eşyaların toplamıdır. Sadece okunabilir (get).
    public int CP 
    { 
        get 
        {
            int total = _baseCP;
            if (equippedWeapon != null) total += equippedWeapon.baseCPBonus;
            if (equippedArmor != null) total += equippedArmor.baseCPBonus;
            if (equippedPet != null) total += equippedPet.cpBonus;
            return total;
        } 
    }

    // Diğer her şey senin orijinal kodunla aynı
    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;   

    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    float _lastDmgTime    = -99f;
    int   _riskBonusLeft  = 0;
    float _expectedCP     = 200f;

    static readonly int[]    TIER_CP    = { 0, 500, 1500, 4000, 9000 };
    static readonly string[] TIER_NAMES = { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
    static readonly int[]    COMMANDER_HP_BY_TIER = { 500, 700, 950, 1200, 1500 };
    const  int MAX_BULLETS = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        
        _baseCP        = startCP; // startCP'yi baseCP'ye atıyoruz
        CommanderMaxHP = COMMANDER_HP_BY_TIER[0];
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        CommanderHP = Mathf.Max(0, CommanderHP - amount);
        GameEvents.OnCommanderDamaged?.Invoke(amount, CommanderHP);
        GameEvents.OnPlayerDamaged?.Invoke(amount);  
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

        if (CommanderHP <= 0) GameEvents.OnGameOver?.Invoke();
    }

    public void HealCommander(int amount)
    {
        CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + amount);
        GameEvents.OnCommanderHealed?.Invoke(CommanderHP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        _baseCP = Mathf.Min(_baseCP + amount, 99999); // CP yerine _baseCP kullanıyoruz
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP); // Toplam CP'yi UI'a yolluyoruz
        if (CurrentTier != oldTier) OnTierChanged();
    }

    public void ApplyGateEffect(GateData data)
    {
        if (data == null) return;
        int   oldTier   = CurrentTier;
        int   oldBullet = BulletCount;
        float bonus     = _riskBonusLeft > 0 ? 1.5f : 1f;
        float scale     = 1f + transform.position.z / 2400f;

        // BÜTÜN CP İŞLEMLERİ ARTIK _baseCP ÜZERİNDEN YAPILIYOR
        switch (data.effectType)
        {
            case GateEffectType.AddCP:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;
            case GateEffectType.MultiplyCP:
                _baseCP = Mathf.RoundToInt(_baseCP * 1.2f);
                break;
            case GateEffectType.AddBullet:
                if (BulletCount < MAX_BULLETS)
                {
                    BulletCount++;
                    GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
                }
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade);
                break;
            case GateEffectType.Merge:
                HandleMerge();
                break;
            case GateEffectType.PathBoost_Piyade:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;
            case GateEffectType.PathBoost_Mekanize:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath+= 1f;
                GameEvents.OnPathBoosted?.Invoke("Mekanik");
                break;
            case GateEffectType.PathBoost_Teknoloji:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                TeknolojiPath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Teknoloji");
                break;
            case GateEffectType.NegativeCP:
                _baseCP = Mathf.Max(50, _baseCP - Mathf.RoundToInt(data.effectValue * scale));
                break;
            case GateEffectType.RiskReward:
                int pen = Mathf.RoundToInt(_baseCP * 0.30f);
                _baseCP = Mathf.Max(100, _baseCP - pen);
                _riskBonusLeft = 3;
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
                break;
            case GateEffectType.AddSoldier_Piyade:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: 2);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;
            case GateEffectType.AddSoldier_Mekanik:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: 2);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                break;
            case GateEffectType.AddSoldier_Teknoloji:
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: 2);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
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

        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        _baseCP = Mathf.Clamp(_baseCP, 50, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        
        // UI'ı GÜNCELLE
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    void HandleMerge()
    {
        bool mergeOccurred = ArmyManager.Instance != null &&
                             ArmyManager.Instance.TryMerge();

        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float multiplier;
        string role = "none";

        if (total < 1f) multiplier = 1.1f;
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

        _baseCP = Mathf.RoundToInt(_baseCP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();
    }

    void OnTierChanged()
    {
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
        if (total < 2f) return;
        float p = PiyadePath/total, m = MekanizePath/total, t = TeknolojiPath/total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    void ShowPopupMessage(string msg)
        => GameEvents.OnSynergyFound?.Invoke(msg);

    public string GetTierName()  => TIER_NAMES[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    public int    GetRiskBonus() => _riskBonusLeft;
}