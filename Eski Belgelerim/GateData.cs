using System.Collections.Generic;
using UnityEngine;

public enum GateEffectType
{
    Power,
    Tempo,
    Geometry,
    Army,
    Sustain,
    Tactical
}

public enum GateDeliveryType
{
    Single,
    Duel,
    Risk,
    Recovery,
    BossPrep
}

public enum GateTargetTag
{
    AllWeapons,
    Assault,
    SMG,
    Sniper,
    Shotgun,
    Launcher,
    ArmyAll,
    Piyade,
    Mekanik,
    Teknoloji,
    Commander,
    Soldiers
}

public enum GateStatType
{
    WeaponPowerPercent,
    FireRatePercent,
    EliteDamagePercent,
    BossDamagePercent,
    ArmorPenFlat,
    PierceCount,
    BounceCount,
    PelletCount,
    SplashRadiusPercent,
    ArmyDamagePercent,
    AddSoldierCount,
    PromoteWeakestSoldier,
    HealCommanderPercent,
    HealSoldiersPercent,
    CommanderDamageReductionPercent,
    CloseRangeDamagePercent,
    ArmoredTargetDamagePercent
}

[System.Serializable]
public class GateModifier
{
    public GateTargetTag targetTag;
    public GateStatType statType;
    public float value;
}

[CreateAssetMenu(fileName = "NewGateData", menuName = "TopEndWar/Gate Data v2")]
public class GateData : ScriptableObject
{
    [Header("Presentation")]
    public string title;
    public string subtitle;
    public Color gateColor;
    public Sprite icon;
    public GateEffectType family;
    public GateDeliveryType deliveryType;

    [Header("Gameplay Tags")]
    public List<GateTargetTag> bestForTags = new();
    public bool runnerAllowed = true;
    public bool anchorAllowed = true;
    public bool tutorialAllowed = true;
    public bool bossPrepAllowed = false;

    [Header("Balance")]
    public float gateValueBudget = 1.0f;
    public int minStage = 1;
    public int maxStage = 999;
    public float spawnWeight = 0.1f;

    [Header("Modifiers")]
    public List<GateModifier> modifiers = new();

    [Header("Risk")]
    public bool hasPenalty = false;
    public List<GateModifier> penaltyModifiers = new();
}