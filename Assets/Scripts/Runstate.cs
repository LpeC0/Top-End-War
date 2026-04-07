using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Run Durumu v1 (Claude)
///
/// Bir run sirasindaki gecici state. MonoBehaviour degil — servis sinifi.
/// Run bittikten sonra sifirlanir; PlayerPrefs'e yazilmaz.
///
/// NEDEN AYRI?
///   PlayerStats  → hesaplama motorlari + baslangic degerleri
///   RunState     → "su an ne durumda?" sorusunun cevabi
///   SaveData     → oyuncunun kalici ilerlemesi
///
/// KULLANIM:
///   RunState.Instance.AddGateEffect(modifier);
///   RunState.Instance.CommanderCurrentHp;
///   RunState.Instance.Reset();
/// </summary>
public class RunState
{
    // ── Singleton ─────────────────────────────────────────────────────────
    private static RunState _instance;
    public static RunState Instance => _instance ??= new RunState();

    // ── Komutan ───────────────────────────────────────────────────────────
    public int CommanderCurrentHp  { get; set; }
    public int CommanderMaxHp      { get; set; }

    // ── Para ──────────────────────────────────────────────────────────────
    public int CurrentRunGold      { get; private set; }
    public int CurrentRunTechCore  { get; private set; }

    public void AddRunGold(int amount)    => CurrentRunGold     += amount;
    public void AddRunTechCore(int amount) => CurrentRunTechCore += amount;

    // ── Ordu ──────────────────────────────────────────────────────────────
    public int PiyadeCount         { get; set; }
    public int MekanikCount        { get; set; }
    public int TeknolojiCount      { get; set; }

    // ── Gate Efektleri ────────────────────────────────────────────────────
    // Run boyunca biriken aktif gate efektlerinin listesi.
    // GateEffectApplier bu listeyi okuyarak stat carpanlarini hesaplar.
    public List<ActiveGateEffect> ActiveGateEffects { get; } = new List<ActiveGateEffect>();

    public void AddGateEffect(GateConfig source, GateModifier2 mod)
    {
        ActiveGateEffects.Add(new ActiveGateEffect { SourceGateId = source.gateId, Modifier = mod });
    }

    // ── Stat Toplama Yardimcilari ─────────────────────────────────────────
    /// <summary>Silah gücü toplam % bonus (ornegin 16 = +%16).</summary>
    public float GetWeaponPowerBonus()     => SumPercent(GateStatType2.WeaponPowerPercent);
    public float GetFireRateBonus()        => SumPercent(GateStatType2.FireRatePercent);
    public int   GetArmorPenBonus()        => SumFlat(GateStatType2.ArmorPenFlat);
    public float GetEliteDamageBonus()     => SumPercent(GateStatType2.EliteDamagePercent);
    public float GetBossDamageBonus()      => SumPercent(GateStatType2.BossDamagePercent);
    public float GetArmoredDamageBonus()   => SumPercent(GateStatType2.ArmoredTargetDamagePercent);
    public int   GetPierceBonus()          => SumFlat(GateStatType2.PierceCount);
    public int   GetBounceBonus()          => SumFlat(GateStatType2.BounceCount);

    float SumPercent(GateStatType2 stat)
    {
        float total = 0f;
        foreach (var e in ActiveGateEffects)
            if (e.Modifier.statType == stat && e.Modifier.operation == GateOperation2.AddPercent)
                total += e.Modifier.value;
        return total;
    }

    int SumFlat(GateStatType2 stat)
    {
        int total = 0;
        foreach (var e in ActiveGateEffects)
            if (e.Modifier.statType == stat && e.Modifier.operation == GateOperation2.AddFlat)
                total += Mathf.RoundToInt(e.Modifier.value);
        return total;
    }

    // ── Boss ──────────────────────────────────────────────────────────────
    public int  BossPhase          { get; set; }
    public bool BossActive         { get; set; }

    // ── Istatistik ────────────────────────────────────────────────────────
    public int  KillCount          { get; set; }
    public float DistanceTravelled { get; set; }

    // ── Sifirla ───────────────────────────────────────────────────────────
    public void Reset()
    {
        CommanderCurrentHp  = 0;
        CommanderMaxHp      = 0;
        CurrentRunGold      = 0;
        CurrentRunTechCore  = 0;
        PiyadeCount         = 0;
        MekanikCount        = 0;
        TeknolojiCount      = 0;
        ActiveGateEffects.Clear();
        BossPhase           = 0;
        BossActive          = false;
        KillCount           = 0;
        DistanceTravelled   = 0f;
    }
}

public class ActiveGateEffect
{
    public string        SourceGateId;
    public GateModifier2 Modifier;
}