using UnityEngine;

/// <summary>
/// Top End War — Stage Yoneticisi v2 (Claude) + Vertical Slice Patch
///
/// v2: StageConfig.targetDps formullerine gore HP degerlerini
///     SpawnManager ve BossManager'a iletir.
///     EconomyManager'a altin ekler (Offline boost slice'ta kapali).
///     Dunya bitisinde WorldConfig'deki komutani acar.
///
/// KURULUM:
///   Hierarchy > Create Empty > "StageManager" > bu scripti ekle.
///   worlds[]      : WorldConfig SO'lari sur (World 1, 2, 3...).
///   stageConfigs[]: StageConfig SO'lari sur (veya Resources/Stages/'a koy).
///   economyConfig : EconomyConfig SO'sunu sur.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Dunya Listesi (sirali — World 1, 2, 3...)")]
    public WorldConfig[] worlds;

    [Header("Stage Verileri")]
    [Tooltip("Bos birakılırsa Resources/Stages/Stage_W{w}_{s:D2} yolundan yuklenir")]
    public StageConfig[] stageConfigs;

    [Header("Ekonomi Formulü")]
    public EconomyConfig economyConfig;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] int _currentWorldID = 1;
    [SerializeField] int _currentStageID = 1;

    StageConfig _activeStage;
    WorldConfig _activeWorld;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => LoadStage(_currentWorldID, _currentStageID);

    // ── Stage Yukle ───────────────────────────────────────────────────────
   
    public void LoadStage(int worldID, int stageID)
    {
        RunState.Instance.Reset();
        SaveManager.Instance?.BeginRun();
        _currentWorldID = worldID;
        _currentStageID = stageID;

        _activeWorld = FindWorld(worldID);
        _activeStage = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

        // Stage başında gate bonuslarını sıfırla
        PlayerStats.Instance?.ResetRunGateBonuses();

        // Biyomu guncelle
        if (_activeWorld != null)
            BiomeManager.Instance?.SetBiome(_activeWorld.biome);

        // DEĞİŞİKLİK: stage runtime spawn state reset
        SpawnManager.Instance?.ResetForStage();

        // SpawnManager'a mob HP'yi ilet
        ApplyMobHP();

        // Boss stage ise BossManager'a HP'yi ilet
        if (_activeStage.IsBossStage)
            ApplyBossHP();

        GameEvents.OnStageChanged?.Invoke(worldID, stageID);
        Debug.Log($"[StageManager] W{worldID}-{stageID} | targetDps={_activeStage.targetDps} " +
                  $"| mobHP={_activeStage.GetNormalMobHP()} | bossHP={_activeStage.GetBossHP()}");
    }

    // ── HP Dagitimi ───────────────────────────────────────────────────────
    void ApplyMobHP()
    {
        if (SpawnManager.Instance == null || _activeStage == null) return;

        SpawnManager.Instance.SetMobHP(
            normalHP: _activeStage.GetNormalMobHP(),
            eliteHP:  _activeStage.GetEliteHP(),
            density:  _activeStage.spawnDensity);
    }

    void ApplyBossHP()
    {
        if (BossManager.Instance == null || _activeStage == null) return;
        BossManager.Instance.bossMaxHP = _activeStage.GetBossHP();
        Debug.Log($"[StageManager] Boss HP set: {_activeStage.GetBossHP()} ({_activeStage.bossType})");
    }

    // ── Stage Tamamlandi ─────────────────────────────────────────────────
    public void OnStageComplete()
    {
        

        if (_activeStage == null) return;

        // Altin odulu
        int gold = _activeStage.goldRewardOverride > 0
            ? _activeStage.goldRewardOverride
            : economyConfig != null
                ? economyConfig.GetGoldReward(_activeStage.stageID, _activeStage.targetDps)
                : 150;
        RunState.Instance.AddRunGold(gold);
        EconomyManager.Instance?.AddGold(gold);
        
        // Vertical slice'ta offline boost kapali.
        // EconomyManager.Instance?.AddOfflineRate(_activeStage.offlineBoostPerHour);

        Debug.Log($"[StageManager] Stage tamamlandi. Altin: +{gold}");

        // Sadece FinalBoss world complete sayilsin.
        if (_activeStage.bossType == BossType.FinalBoss)
        {
            OnWorldComplete();
            return;
        }

        // MiniBoss ise normal stage gibi ilerlesin; sonraki stage yoksa slice biter.
        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        
        if (nextStage != null)
        {
            LoadStage(_currentWorldID, _currentStageID + 1);
            return;
        }

        Debug.Log("[StageManager] Vertical slice tamamlandi.");
        GameEvents.OnWorldChanged?.Invoke(_currentWorldID); // mevcut event sistemi bozulmasin
    }

    // ── Stage Ortasi Micro-Loot ───────────────────────────────────────────
    public void OnMidStageReached()
    {
        if (_activeStage == null || !_activeStage.hasMidStageLoot) return;

        int midGold = economyConfig != null
            ? economyConfig.GetMidLootGold(_activeStage.stageID, _activeStage.targetDps)
            : 50;

        EconomyManager.Instance?.AddGold(midGold);
        Debug.Log($"[StageManager] Micro-loot: +{midGold} Altin");

        // Vertical slice economy: TechCore kapali.
        // if (Random.value < _activeStage.techCoreDropChance)
        // {
        //     EconomyManager.Instance?.AddTechCore(1);
        // }
    }

    // ── Dunya Tamamlandi ─────────────────────────────────────────────────
    void OnWorldComplete()
    {
        if (_activeWorld != null)
        {
            EconomyManager.Instance?.AddOfflineRate(_activeWorld.offlineIncomeBoost);
            if (_activeWorld.unlockedCommander != null)
                Debug.Log($"[StageManager] Komutan acildi: {_activeWorld.unlockedCommander.commanderName}");
            
            // TODO: Komutan unlock UI
        }

        GameEvents.OnWorldChanged?.Invoke(_currentWorldID);
        LoadStage(_currentWorldID + 1, stageID: 1);
    }

    // ── Yardimcilar ───────────────────────────────────────────────────────
    WorldConfig FindWorld(int id)
    {
        if (worlds != null)
            foreach (var w in worlds)
                if (w != null && w.worldID == id) return w;
        return null;
    }

    StageConfig FindStage(int worldID, int stageID)
    {
        if (stageConfigs != null)
            foreach (var s in stageConfigs)
                if (s != null && s.worldID == worldID && s.stageID == stageID) return s;
                
        return Resources.Load<StageConfig>($"Stages/Stage_W{worldID}_{stageID:D2}");
    }

    // ── Getter'lar ────────────────────────────────────────────────────────
    public StageConfig ActiveStage => _activeStage;
    public WorldConfig ActiveWorld => _activeWorld;
    public int CurrentWorldID => _currentWorldID;
    public int CurrentStageID => _currentStageID;
}