using UnityEngine;

/// <summary>
/// Top End War — Oyuncu Istatistikleri v7 (Claude)
///
/// v7 degisiklikleri:
///   + activeCommander (CommanderData SO) — tum tier tablolari buradan gelir
///   + GetTotalDPS(): BaseDMG[tier] x WeaponMult x SlotMult x RarityMult x GlobalMult
///   + GetRarityMult(): rarity 1-5 carpan tablosu
///   + GetBaseFireRate(): activeCommander'dan okur
///   - startCP kaldirildi — starter equipment zorunlu
///   - DAMAGE[] dizisi kaldirildi — PlayerController artik GetTotalDPS() kullanir
///   - BASE_FIRE_RATES dizisi kaldirildi — GetBaseFireRate() kullanilir
///
/// DPS FORMULU (Magic Number Yok):
///   CommanderDPS = BaseDMG[tier] * WeaponDmgMult * SlotLevelMult * RarityMult * GlobalMult
///   BulletDamage = CommanderDPS / (FinalFireRate * BulletCount)
///   [PlayerController.AutoShoot() hesaplar]
///
/// CP KURALI:
///   CP = Gear Score (meta-hub UI icin). DPS hesabinda KULLANILMAZ.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ── Komutan ───────────────────────────────────────────────────────────
    [Header("Aktif Komutan (CommanderData SO)")]
    [Tooltip("Assets > Create > TopEndWar > CommanderData. Inspector'a sur.")]
    public CommanderData activeCommander;

    // ── Ekipman ───────────────────────────────────────────────────────────
    [Header("Ekipman Seti (EquipmentLoadout SO)")]
    public EquipmentLoadout equippedLoadout;

    [Header("Tekil Ekipmanlar (Loadout yoksa veya override icin)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public EquipmentData equippedShoulder;
    public EquipmentData equippedKnee;
    public EquipmentData equippedNecklace;
    public EquipmentData equippedRing;
    public PetData       equippedPet;

    // ── Slot Seviyeleri ───────────────────────────────────────────────────
    [Header("Slot Seviyeleri (max 50)")]
    [Range(1, 50)] public int weaponSlotLevel   = 1;
    [Range(1, 50)] public int armorSlotLevel    = 1;
    [Range(1, 50)] public int shoulderSlotLevel = 1;
    [Range(1, 50)] public int kneeSlotLevel     = 1;
    [Range(1, 50)] public int necklaceSlotLevel = 1;
    [Range(1, 50)] public int ringSlotLevel     = 1;

    // ── Diger Ayarlar ─────────────────────────────────────────────────────
    [Header("Baslangic Ayarlari")]
    public float invincibilityDuration = 0.8f;

    // ── Dahili Durum ──────────────────────────────────────────────────────
    private int   _baseCP        = 0;   // startCP kaldirildi — ekipmandan gelir
    private int   _riskBonusLeft = 0;
    private float _expectedCP    = 200f;
    private float _lastDmgTime   = -99f;

    // ── CP Property ───────────────────────────────────────────────────────
    /// <summary>
    /// Gear Score (meta-hub UI). Ekipman bonuslari + kolye/yuzuk carpanlari dahil.
    /// DPS hesabinda KULLANILMAZ.
    /// </summary>
    public int CP
    {
        get
        {
            int total = _baseCP;
            total += equippedWeapon   != null ? equippedWeapon.baseCPBonus   : 0;
            total += equippedArmor    != null ? equippedArmor.baseCPBonus    : 0;
            total += equippedShoulder != null ? equippedShoulder.baseCPBonus : 0;
            total += equippedKnee     != null ? equippedKnee.baseCPBonus     : 0;
            total += equippedNecklace != null ? equippedNecklace.baseCPBonus : 0;
            total += equippedRing     != null ? equippedRing.baseCPBonus     : 0;
            total += equippedPet      != null ? equippedPet.cpBonus          : 0;

            float mult = equippedNecklace != null ? equippedNecklace.cpMultiplier : 1f;
            if (equippedRing != null) mult *= equippedRing.cpMultiplier;
            return Mathf.RoundToInt(total * mult);
        }
    }

    // ── DPS Formulu ───────────────────────────────────────────────────────
    /// <summary>
    /// Komutanin saniye basi hasari.
    /// Formul: BaseDMG[tier] * WeaponDmgMult * SlotLevelMult * RarityMult * GlobalMult
    ///
    /// PlayerController bu degeri fireRate ve BulletCount ile boler:
    ///   BulletDamage = GetTotalDPS() / (GetBaseFireRate() * BulletCount)
    /// </summary>
    public float GetTotalDPS()
    {
        if (activeCommander == null)
        {
            Debug.LogWarning("[PlayerStats] activeCommander atanmamis! Varsayilan degerler kullaniliyor.");
            return 60f;
        }

        float baseDMG    = activeCommander.GetBaseDMG(CurrentTier);
        float weaponMult = equippedWeapon != null ? equippedWeapon.damageMultiplier    : 1f;
        float slotMult   = GetSlotLevelMult(weaponSlotLevel);
        float rarityMult = GetRarityMult(equippedWeapon != null ? equippedWeapon.rarity : 1);
        float globalMult = 1f;

        // Global DPS carpani: once kolye, sonra yuzuk
        if (equippedNecklace != null) globalMult *= equippedNecklace.globalDmgMultiplier;
        if (equippedRing     != null) globalMult *= equippedRing.globalDmgMultiplier;

        return baseDMG * weaponMult * slotMult * rarityMult * globalMult;
    }

    /// <summary>
    /// Tier ve silah carpanina gore nihai atis hizi.
    /// PlayerController bu degeri kullanir.
    /// </summary>
    public float GetBaseFireRate()
    {
        if (activeCommander == null) return 1.5f;
        float baseRate  = activeCommander.GetBaseFireRate(CurrentTier);
        float equipMult = equippedWeapon != null ? equippedWeapon.fireRateMultiplier : 1f;
        return baseRate * equipMult;
    }

    // ── Slot Seviye Carpani ────────────────────────────────────────────────
    /// <summary>
    /// Azalan verimler:
    ///   Level 1-10:  +%5/seviye  (1-10 = +%50)
    ///   Level 11-30: +%3/seviye  (11-30 = +%60)
    ///   Level 31-50: +%1.5/seviye (31-50 = +%30)
    ///   Max (50):    +%140 → carpan 2.40
    /// Rarity carpani her zaman dominant — yeni silah bulmak her zaman daha degerli.
    /// </summary>
    public static float GetSlotLevelMult(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        float bonus = 0f;

        int   tier1 = Mathf.Min(level, 10);
        bonus += tier1 * 0.05f;

        if (level > 10)
        {
            int tier2 = Mathf.Min(level - 10, 20);
            bonus += tier2 * 0.03f;
        }
        if (level > 30)
        {
            int tier3 = level - 30;
            bonus += tier3 * 0.015f;
        }
        return 1f + bonus;
    }

    // ── Rarity Carpani ────────────────────────────────────────────────────
    /// <summary>
    /// Rarity carpani her zaman SlotLevelMult'tan buyuktur.
    /// Mor silah, max slotlu Gri silahi her zaman yener.
    /// Altin (rarity 5) World 5+'ta acilir.
    /// </summary>
    public static float GetRarityMult(int rarity)
    {
        return rarity switch
        {
            1 => 1.0f,  // Gri
            2 => 1.3f,  // Yesil
            3 => 1.7f,  // Mavi
            4 => 2.2f,  // Mor
            5 => 3.0f,  // Altin (World 5+)
            _ => 1.0f,
        };
    }

    // ── Hasar Azaltma ─────────────────────────────────────────────────────
    public float TotalDamageReduction()
    {
        float dr = 0f;
        dr += equippedArmor    != null ? equippedArmor.damageReduction    : 0f;
        dr += equippedShoulder != null ? equippedShoulder.damageReduction : 0f;
        dr += equippedKnee     != null ? equippedKnee.damageReduction     : 0f;
        dr += equippedRing     != null ? equippedRing.damageReduction     : 0f;
        dr += equippedPet      != null ? equippedPet.anchorDamageReduction: 0f;
        return Mathf.Clamp(dr, 0f, 0.60f);
    }

    // ── Ekipman HP Bonusu ─────────────────────────────────────────────────
    public int TotalEquipmentHPBonus()
    {
        int bonus = 0;
        bonus += equippedArmor    != null ? equippedArmor.commanderHPBonus    : 0;
        bonus += equippedShoulder != null ? equippedShoulder.commanderHPBonus : 0;
        bonus += equippedKnee     != null ? equippedKnee.commanderHPBonus     : 0;
        return bonus;
    }

    // ── Tier ve Diger Durum ───────────────────────────────────────────────
    public int   CurrentTier  { get; private set; } = 1;
    public int   BulletCount  { get; private set; } = 1;

    public float PiyadePath    { get; private set; } = 0f;
    public float MekanizePath  { get; private set; } = 0f;
    public float TeknolojiPath { get; private set; } = 0f;

    public int CommanderMaxHP { get; private set; } = 500;
    public int CommanderHP    { get; private set; } = 500;
    public float SmoothedPowerRatio { get; private set; } = 1f;

    static readonly int[] TIER_CP = { 0, 300, 900, 2500, 6000 };
    const int MAX_BULLETS = 5;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        equippedLoadout?.ApplyTo(this);

        // startCP yok — baslangic gucu tamamen ekipmandan gelir
        _baseCP = 0;

        // Komutan HP'sini hesapla
        if (activeCommander == null)
            Debug.LogError("[PlayerStats] activeCommander atanmamis! Inspector'a CommanderData SO suru.");

        CommanderMaxHP = (activeCommander != null ? activeCommander.GetBaseHP(1) : 500)
                       + TotalEquipmentHPBonus();
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Hasar / Iyilesme ─────────────────────────────────────────────────
    public void TakeContactDamage(int amount)
    {
        if (Time.time - _lastDmgTime < invincibilityDuration) return;
        _lastDmgTime = Time.time;

        float dr         = TotalDamageReduction();
        int finalAmount  = Mathf.RoundToInt(amount * (1f - dr));

        CommanderHP = Mathf.Max(0, CommanderHP - finalAmount);
        GameEvents.OnCommanderDamaged?.Invoke(finalAmount, CommanderHP);
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

    // ── CP Guncellemeleri ─────────────────────────────────────────────────
    public void AddCPFromKill(int amount)
    {
        int oldTier = CurrentTier;
        _baseCP = Mathf.Min(_baseCP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── Kapi Etkileri ─────────────────────────────────────────────────────
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
                HandleMerge(_riskBonusLeft > 0);
                break;
            case GateEffectType.PathBoost_Piyade:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                PiyadePath += 1f;
                GameEvents.OnPathBoosted?.Invoke("Piyade");
                break;
            case GateEffectType.PathBoost_Mekanize:
                _baseCP += Mathf.RoundToInt(data.effectValue * scale * bonus);
                MekanizePath += 1f;
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
            {
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Piyade!");
                break;
            }
            case GateEffectType.AddSoldier_Mekanik:
            {
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Mekanik!");
                break;
            }
            case GateEffectType.AddSoldier_Teknoloji:
            {
                int count = _riskBonusLeft > 0 ? 3 : 2;
                ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: count);
                _baseCP += Mathf.RoundToInt(data.effectValue * scale);
                if (_riskBonusLeft > 0) ShowPopupMessage("RISK: +3 Teknoloji!");
                break;
            }
            case GateEffectType.HealCommander:
            {
                HealCommander(Mathf.RoundToInt(data.effectValue));
                if (_riskBonusLeft > 0)
                {
                    CommanderMaxHP += 100;
                    CommanderHP = Mathf.Min(CommanderHP + 50, CommanderMaxHP);
                    GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
                    ShowPopupMessage("RISK: +100 MaxHP!");
                }
                break;
            }
            case GateEffectType.HealSoldiers:
            {
                float pct = _riskBonusLeft > 0 ? 1.0f : Mathf.Clamp(data.effectValue, 0f, 1f);
                ArmyManager.Instance?.HealAll(pct);
                ShowPopupMessage(_riskBonusLeft > 0 ? "RISK: Asker FULL HP!" :
                    $"Asker +%{Mathf.RoundToInt(pct * 100)}");
                break;
            }
        }

        // Risk sayacini dusuR
        if (_riskBonusLeft > 0 &&
            data.effectType != GateEffectType.NegativeCP &&
            data.effectType != GateEffectType.RiskReward)
        {
            _riskBonusLeft--;
            if (_riskBonusLeft > 0)
                GameEvents.OnRiskBonusActivated?.Invoke(_riskBonusLeft);
        }

        _baseCP = Mathf.Clamp(_baseCP, 0, 99999);
        UpdateSmoothedRatio();
        RefreshTier();
        CheckSynergy();
        GameEvents.OnCPUpdated?.Invoke(CP);

        if (CurrentTier != oldTier) OnTierChanged();
        if (BulletCount != oldBullet)
            GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    // ── Merge ────────────────────────────────────────────────────────────
    void HandleMerge(bool riskActive = false)
    {
        ArmyManager.Instance?.TryMerge();

        float total = PiyadePath + MekanizePath + TeknolojiPath;
        float riskBonus = riskActive ? 0.2f : 0f;
        float multiplier;
        string role = "none";

        if (riskActive) ShowPopupMessage("RISK: Merge Guclendi!");

        if (total < 1f)
        {
            multiplier = 1.1f + riskBonus;
        }
        else
        {
            float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;
            float minPath = Mathf.Min(p, Mathf.Min(m, t));
            if (minPath > 0.28f)
            {
                multiplier = 1.7f + riskBonus; role = "PERFECT";
                GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS!");
            }
            else if (t >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Teknoloji"; }
            else if (p >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Piyade"; }
            else if (m >= 0.5f) { multiplier = 1.5f + riskBonus; role = "Mekanik"; }
            else                { multiplier = 1.2f + riskBonus; }
        }

        _baseCP = Mathf.RoundToInt(_baseCP * multiplier);
        if (role != "none") PiyadePath = MekanizePath = TeknolojiPath = 0f;
        GameEvents.OnMergeTriggered?.Invoke();
    }

    // ── Tier Degisimi ─────────────────────────────────────────────────────
    void OnTierChanged()
    {
        if (activeCommander == null) return;

        int oldMax = CommanderMaxHP;
        CommanderMaxHP = activeCommander.GetBaseHP(CurrentTier) + TotalEquipmentHPBonus();

        if (CommanderMaxHP > oldMax)
        {
            int bonusHP = CommanderMaxHP - oldMax;
            CommanderHP = Mathf.Min(CommanderMaxHP, CommanderHP + bonusHP);
        }
        GameEvents.OnTierChanged?.Invoke(CurrentTier);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // ── Yardimci ─────────────────────────────────────────────────────────
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
        float p = PiyadePath / total, m = MekanizePath / total, t = TeknolojiPath / total;
        if      (p > 0.5f && m > 0.25f) GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");
        else if (p > 0.5f && t > 0.25f) GameEvents.OnSynergyFound?.Invoke("Drone Takimi");
        else if (m > 0.4f && t > 0.30f) GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");
    }

    void ShowPopupMessage(string msg) => GameEvents.OnSynergyFound?.Invoke(msg);

    public string GetTierName()
    {
        if (activeCommander != null) return activeCommander.commanderName;
        string[] fallback = { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
        return fallback[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    }

    public int GetRiskBonus() => _riskBonusLeft;
}