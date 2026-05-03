using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Oyuncu Istatistikleri v9 (Runtime Stabilite Patch)
///
/// v8 → v9 Delta:
///   • _isDead flag eklendi: TakeContactDamage tekrar GameOver tetiklemez.
///   • ResetRunGateBonuses(): _isDead, _lastDmgTime ve HP sifirlanir —
///     StageManager bu metodu zaten cagirir, yeni run temiz baslar.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public struct RuntimeCombatSnapshot
    {
        public float TotalDPS;
        public float FireRate;
        public int ProjectileCount;
        public int BulletDamage;
        public int ArmorPen;
        public int PierceCount;
        public float WeaponRange;
        public float DisplayedDPS;
        public int CurrentHP;
        public int MaxHP;
        public int CombatPower;
    }

    // ── Komutan ───────────────────────────────────────────────────────────
    [Header("Aktif Komutan (CommanderData SO)")]
    public CommanderData activeCommander;

    // ── Ekipman ───────────────────────────────────────────────────────────
    [Header("Ekipman Seti (EquipmentLoadout SO)")]
    public EquipmentLoadout equippedLoadout;

    [Header("Tekil Ekipmanlar")]
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
    private int   _baseCP        = 0;
    private int   _riskBonusLeft = 0;
    private float _expectedCP    = 200f;
    private float _lastDmgTime   = -99f;

    // PATCH: Cift GameOver tetiklenmesini onler.
    private bool _isDead = false;

    // ── RUN-TIME GATE BONUSLARI ───────────────────────────────────────────
    float _runWeaponPowerPercent = 0f;
    float _runFireRatePercent    = 0f;
    float _runEliteDamagePercent = 0f;
    float _runBossDamagePercent  = 0f;
    int   _runArmorPenFlat       = 0;
    int   _runPierceCount        = 0;
    int   _runPelletCount        = 0;

    public float RunWeaponPowerPercent => _runWeaponPowerPercent;
    public float RunFireRatePercent    => _runFireRatePercent;
    public float RunEliteDamagePercent => _runEliteDamagePercent;
    public float RunBossDamagePercent  => _runBossDamagePercent;
    public int   RunArmorPenFlat       => _runArmorPenFlat;
    public int   RunPierceCount        => _runPierceCount;
    public int   RunPelletCount        => _runPelletCount;

    // ── CP Property ───────────────────────────────────────────────────────
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

    public float GetTotalDPS()
    {
        if (activeCommander == null) return 60f;
        float baseDMG    = activeCommander.GetBaseDMG(CurrentTier);
        float weaponMult = equippedWeapon != null ? equippedWeapon.damageMultiplier    : 1f;
        float slotMult   = GetSlotLevelMult(weaponSlotLevel);
        float rarityMult = GetRarityMult(equippedWeapon != null ? equippedWeapon.rarity : 1);
        float globalMult = 1f;
        if (equippedNecklace != null) globalMult *= equippedNecklace.globalDmgMultiplier;
        if (equippedRing     != null) globalMult *= equippedRing.globalDmgMultiplier;
        return baseDMG * weaponMult * slotMult * rarityMult * globalMult;
    }

    public float GetBaseFireRate()
    {
        if (activeCommander == null) return 1.5f;
        float baseRate  = activeCommander.GetBaseFireRate(CurrentTier);
        float equipMult = equippedWeapon != null ? equippedWeapon.fireRateMultiplier : 1f;
        return baseRate * equipMult;
    }

    public RuntimeCombatSnapshot GetRuntimeCombatSnapshot()
    {
        float fireRate = Mathf.Max(0.01f, GetBaseFireRate() * (1f + _runFireRatePercent / 100f));
        float totalDps = Mathf.Max(0f, GetTotalDPS() * (1f + _runWeaponPowerPercent / 100f));
        int projectileCount = Mathf.Max(1, BulletCount);
        int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(totalDps / (fireRate * projectileCount)));

        int armorPen = _runArmorPenFlat;
        int pierceCount = _runPierceCount;
        if (equippedWeapon != null)
        {
            armorPen += equippedWeapon.weaponArchetype != null ? equippedWeapon.weaponArchetype.armorPen : 0;
            armorPen += equippedWeapon.armorPen;
            pierceCount += equippedWeapon.weaponArchetype != null ? equippedWeapon.weaponArchetype.pierceCount : 0;
            pierceCount += equippedWeapon.pierceCount;
        }
        float displayedDps = bulletDamage * fireRate * projectileCount;
        float weaponRange = GetRuntimeWeaponRange();
        
        int combatPower = CalculateCombatPower(displayedDps, CommanderMaxHP, armorPen, pierceCount, weaponRange);

        return new RuntimeCombatSnapshot
        {
            TotalDPS = totalDps,
            FireRate = fireRate,
            ProjectileCount = projectileCount,
            BulletDamage = bulletDamage,
            ArmorPen = armorPen,
            PierceCount = pierceCount,
            WeaponRange = weaponRange,
            DisplayedDPS = displayedDps,
            CurrentHP = CommanderHP,
            MaxHP = CommanderMaxHP,
            CombatPower = combatPower,
        };
    }

    public float GetRuntimeWeaponRange()
    {
        WeaponFamily family = GetRuntimeWeaponFamily();
        float fallback = family switch
        {
            WeaponFamily.SMG => 18f,
            WeaponFamily.Sniper => 36f,
            _ => 24f
        };

        float range = equippedWeapon != null && equippedWeapon.weaponArchetype != null
            ? equippedWeapon.weaponArchetype.attackRange
            : fallback;

        return family == WeaponFamily.SMG
            ? Mathf.Clamp(range, 16f, 20f)
            : Mathf.Max(4f, range);
    }

    public WeaponFamily GetRuntimeWeaponFamily()
    {
        if (equippedWeapon == null)
            return WeaponFamily.Assault;

        if (equippedWeapon.weaponArchetype != null)
            return equippedWeapon.weaponArchetype.family;

        return equippedWeapon.weaponType switch
        {
            WeaponType.Automatic => WeaponFamily.SMG,
            WeaponType.Sniper => WeaponFamily.Sniper,
            WeaponType.Shotgun => WeaponFamily.Shotgun,
            WeaponType.Launcher => WeaponFamily.Launcher,
            WeaponType.Beam => WeaponFamily.Beam,
            _ => WeaponFamily.Assault,
        };
    }

    public static float GetSlotLevelMult(int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        float bonus = 0f;
        int   tier1 = Mathf.Min(level, 10);
        bonus += tier1 * 0.05f;
        if (level > 10) { int tier2 = Mathf.Min(level - 10, 20); bonus += tier2 * 0.03f; }
        if (level > 30) { int tier3 = level - 30; bonus += tier3 * 0.015f; }
        return 1f + bonus;
    }

    public static float GetRarityMult(int rarity)
        => rarity switch { 1 => 1.0f, 2 => 1.3f, 3 => 1.7f, 4 => 2.2f, 5 => 3.0f, _ => 1.0f };

    /// <summary>
    /// Runtime Combat Power Formülü (DPS, HP, ArmorPen, Pierce, Range bileşimi).
    /// 
    /// Temel ilke:
    ///   - DPS, oyuncunun hasar hızı kapasitesi
    ///   - MaxHP, hayatta kalma gücü
    ///   - ArmorPen, düşman zırhına karşı verimlilik
    ///   - PierceCount, ek hasar etkinliği
    ///   - WeaponRange, strateji ve çok yönlülük
    /// 
    /// Formül:
    ///   power = round(displayedDps * 1.5 + maxHp * 0.2 + armorPen * 15 + pierceCount * 50 + range * 2)
    /// 
    /// Amaç: Stage targetDps (~70) ile karşılaştırılabilir power score oluşturmak.
    /// </summary>
    static int CalculateCombatPower(float displayedDps, int maxHp, int armorPen, int pierceCount, float weaponRange)
    {
        float power = 0f;
        power += displayedDps * 1.5f;       // DPS ağırlık
        power += maxHp * 0.2f;              // HP katkı
        power += armorPen * 8f;            // ArmorPen verimliliği
        power += pierceCount * 50f;         // Pierce utility bonus
        power += weaponRange * 2f;          // Range stratejik değer
        
        return Mathf.Max(1, Mathf.RoundToInt(power));
    }

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

    public int TotalEquipmentHPBonus()
    {
        int bonus = 0;
        bonus += equippedArmor    != null ? equippedArmor.commanderHPBonus    : 0;
        bonus += equippedShoulder != null ? equippedShoulder.commanderHPBonus : 0;
        bonus += equippedKnee     != null ? equippedKnee.commanderHPBonus     : 0;
        return bonus;
    }

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

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        equippedLoadout?.ApplyTo(this);
        _baseCP = 0;
        RefreshWeaponDerivedStats();

        CommanderMaxHP = (activeCommander != null ? activeCommander.GetBaseHP(1) : 500)
                       + TotalEquipmentHPBonus();
        CommanderHP    = CommanderMaxHP;
    }

    void Start()
    {
        GameEvents.OnCPUpdated?.Invoke(CP);
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    // DEĞİŞİKLİK: Enemy tarafı artık hasarın gerçekten işlenip işlenmediğini bilmek istiyor.
    // DEĞİŞİKLİK
public bool TryTakeContactDamage(int amount)
{
    if (_isDead)
    {
        Debug.Log("[PlayerStats] ContactDamage BLOCKED -> already dead");
        return false;
    }

    float dt = Time.time - _lastDmgTime;
    if (dt < invincibilityDuration)
    {
        Debug.Log($"[PlayerStats] ContactDamage BLOCKED -> iFrame aktif | dt={dt:F2} / inv={invincibilityDuration:F2}");
        return false;
    }

    _lastDmgTime = Time.time;

    float dr = TotalDamageReduction();
    int finalAmount = Mathf.RoundToInt(amount * (1f - dr));
    int oldHp = CommanderHP;

    CommanderHP = Mathf.Max(0, CommanderHP - finalAmount);

    GameEvents.OnCommanderDamaged?.Invoke(finalAmount, CommanderHP);
    GameEvents.OnPlayerDamaged?.Invoke(amount);
    GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);

    Debug.Log($"[PlayerStats] ContactDamage APPLIED -> raw={amount} final={finalAmount} hp:{oldHp}->{CommanderHP}");

    if (CommanderHP <= 0)
    {
        _isDead = true;
        Debug.Log("[PlayerStats] ContactDamage APPLIED -> player dead, GameOver");
        GameEvents.OnGameOver?.Invoke();
    }

    return true;
}

    public void TakeContactDamage(int amount)
    {
        TryTakeContactDamage(amount);
    }

    // DEĞİŞİKLİK: Revive sonrası ölüm flagi ve hasar zamanlayıcısı temizlenir.
    public void ReviveFromGameOver()
    {
        _isDead = false;
        _lastDmgTime = -99f;
        CommanderHP = CommanderMaxHP;
        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
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
        _baseCP = Mathf.Min(_baseCP + amount, 99999);
        RefreshTier();
        GameEvents.OnCPUpdated?.Invoke(CP);
        if (CurrentTier != oldTier) OnTierChanged();
    }

    // ── Gate Config ───────────────────────────────────────────────────────

public void ResetRunGateBonuses()
{
    _runWeaponPowerPercent = 0f;
    _runFireRatePercent    = 0f;
    _runEliteDamagePercent = 0f;
    _runBossDamagePercent  = 0f;
    _runArmorPenFlat       = 0;
    _runPierceCount        = 0;
    _runPelletCount        = 0;

    // PATCH: yeni run baslarken olum flagini ve hasar zamanlayicisini sifirla.
    _isDead      = false;
    _lastDmgTime = -99f;
        CommanderHP  = CommanderMaxHP;

        GameEvents.OnCommanderHPChanged?.Invoke(CommanderHP, CommanderMaxHP);
    }

    public void ApplyGateConfig(GateConfig gate)
    {
        if (gate == null) return;
        ApplyModifierList(gate.modifiers);
        if (gate.IsRisk && gate.penaltyModifiers != null && gate.penaltyModifiers.Count > 0)
            ApplyModifierList(gate.penaltyModifiers);
        RefreshWeaponDerivedStats();
        Debug.Log($"[PlayerStats] Gate applied: {gate.title}");
    }

    public void ApplyGateConfig(GateRuntimeData gate)
    {
        if (gate == null) return;
        ApplyModifierList(gate.modifiers);
        if (gate.isRisk && gate.penaltyModifiers != null && gate.penaltyModifiers.Count > 0)
            ApplyModifierList(gate.penaltyModifiers);
        RefreshWeaponDerivedStats();
        Debug.Log($"[PlayerStats] Gate applied: {gate.title}");
    }

    void ApplyModifierList(List<GateModifier2> list)
    {
        if (list == null) return;
        foreach (var mod in list) ApplyModifier(mod);
    }

    public void ApplyAnchorBuff(GateModifier2 mod)
    {
        ApplyModifier(mod);
        RefreshWeaponDerivedStats();
        Debug.Log($"[PlayerStats] Anchor buff applied: {mod.statType} {mod.value}");
    }

    void ApplyModifier(GateModifier2 mod)
    {
        if (mod == null) return;
        switch (mod.statType)
        {
            case GateStatType2.WeaponPowerPercent:        _runWeaponPowerPercent += mod.value; break;
            case GateStatType2.FireRatePercent:            _runFireRatePercent    += mod.value; break;
            case GateStatType2.EliteDamagePercent:         _runEliteDamagePercent += mod.value; break;
            case GateStatType2.BossDamagePercent:          _runBossDamagePercent  += mod.value; break;
            case GateStatType2.ArmorPenFlat:               _runArmorPenFlat += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.PierceCount:                _runPierceCount  += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.PelletCount:                _runPelletCount  += Mathf.RoundToInt(mod.value); break;
            case GateStatType2.AddSoldierCount:
            {
                int count = Mathf.RoundToInt(mod.value);
                switch (mod.targetType)
                {
                    case GateTargetType2.PiyadeSoldiers:     ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade,    count: count); break;
                    case GateTargetType2.MekanikSoldiers:    ArmyManager.Instance?.AddSoldier(SoldierPath.Mekanik,   count: count); break;
                    case GateTargetType2.TeknolojiSoldiers:  ArmyManager.Instance?.AddSoldier(SoldierPath.Teknoloji, count: count); break;
                    default:                                  ArmyManager.Instance?.AddSoldier(SoldierPath.Piyade,    count: count); break;
                }
                break;
            }
            case GateStatType2.HealCommanderPercent:
                HealCommander(Mathf.RoundToInt(CommanderMaxHP * (mod.value / 100f)));
                break;
            case GateStatType2.HealSoldiersPercent:
                ArmyManager.Instance?.HealAll(mod.value / 100f);
                break;
            default:
                Debug.Log($"[PlayerStats] Unsupported gate stat for slice now: {mod.statType}");
                break;
        }
    }

    // ── Tier ─────────────────────────────────────────────────────────────
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

    void ShowPopupMessage(string msg) => GameEvents.OnSynergyFound?.Invoke(msg);

    public string GetTierName()
    {
        if (activeCommander != null) return activeCommander.commanderName;
        string[] fallback = { "Gonullu Er", "Elit Komando", "Gatling Timi", "Hava Indirme", "Suru Drone" };
        return fallback[Mathf.Clamp(CurrentTier - 1, 0, 4)];
    }

    public int GetRiskBonus() => _riskBonusLeft;

    public void RefreshWeaponDerivedStats()
    {
        int baseCount = equippedWeapon != null && equippedWeapon.weaponArchetype != null
            ? equippedWeapon.weaponArchetype.projectileCount
            : 1;

        int nextCount = Mathf.Clamp(baseCount + _runPelletCount, 1, MAX_BULLETS);
        if (BulletCount == nextCount) return;

        BulletCount = nextCount;
        GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }

    public void SetBulletCount(int count)
    {
        BulletCount = Mathf.Clamp(count, 1, MAX_BULLETS);
        GameEvents.OnBulletCountChanged?.Invoke(BulletCount);
    }
}
