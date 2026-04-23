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
    bool _stageClearLocked = false;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        GameEvents.OnBossDefeated += HandleBossDefeated;
    }

    void Start() => LoadStage(_currentWorldID, _currentStageID, true);

    void OnDestroy()
    {
        GameEvents.OnBossDefeated -= HandleBossDefeated;
    }

    void Update()
    {
        if (_activeStage == null) return;
        if (_stageClearLocked) return;
        if (_activeStage.IsBossStage) return;
        if (PlayerStats.Instance == null) return;

        if (PlayerStats.Instance.transform.position.z >= GetStageEndZ())
            OnStageComplete();
    }

    // ── Stage Yukle ───────────────────────────────────────────────────────
   
    public void LoadStage(int worldID, int stageID, bool resetRunState = true)
    {
        if (resetRunState)
        {
            RunState.Instance.Reset();
            SaveManager.Instance?.BeginRun();
            PlayerStats.Instance?.ResetRunGateBonuses();
        }

        Time.timeScale = 1f;
        _stageClearLocked = false;
        _currentWorldID = worldID;
        _currentStageID = stageID;

        _activeWorld = FindWorld(worldID);
        _activeStage = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

        PlayerStats.Instance?.SetExpectedCP(_activeStage.targetDps);

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
        if (_stageClearLocked) return;
        if (_activeStage.IsBossStage && BossManager.Instance != null && BossManager.Instance.IsActive())
            return;

        // Altin odulu
        int gold = _activeStage.goldRewardOverride > 0
            ? _activeStage.goldRewardOverride
            : economyConfig != null
                ? economyConfig.GetGoldReward(_activeStage.stageID, _activeStage.targetDps)
                : 150;
        RunState.Instance.AddRunGold(gold);
        EconomyManager.Instance?.AddGold(gold);
        SaveManager.Instance?.RegisterStageComplete();
        
        // Vertical slice'ta offline boost kapali.
        // EconomyManager.Instance?.AddOfflineRate(_activeStage.offlineBoostPerHour);

        Debug.Log($"[StageManager] Stage tamamlandi. Altin: +{gold}");
        _stageClearLocked = true;
        Time.timeScale = 0f;

        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        bool worldCleared = _activeStage.bossType == BossType.FinalBoss || nextStage == null;

        GameEvents.OnStageCleared?.Invoke(new GameEvents.StageClearInfo
        {
            worldID = _currentWorldID,
            stageID = _currentStageID,
            stageName = _activeStage.DisplayStageName,
            goldReward = gold,
            hasNextStage = nextStage != null,
            worldCleared = worldCleared
        });

        if (worldCleared)
            GameEvents.OnVictory?.Invoke();
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
        GameEvents.OnVictory?.Invoke();
    }

    void HandleBossDefeated()
    {
        if (_activeStage != null && _activeStage.IsBossStage)
            OnStageComplete();
    }

    public float GetStageLength()
    {
        float bossDistance = SpawnManager.Instance != null ? SpawnManager.Instance.bossDistance : 1200f;
        return Mathf.Max(60f, bossDistance / 10f);
    }

    public float GetStageStartZ()
    {
        return (_currentStageID - 1) * GetStageLength();
    }

    public float GetStageEndZ()
    {
        return _currentStageID * GetStageLength();
    }

    public float GetStageProgress01()
    {
        if (PlayerStats.Instance == null) return 0f;
        return Mathf.InverseLerp(GetStageStartZ(), GetStageEndZ(), PlayerStats.Instance.transform.position.z);
    }

    public void ContinueAfterStageClear()
    {
        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        if (nextStage == null) return;
        LoadStage(_currentWorldID, _currentStageID + 1, false);
    }

    public void RestartCurrentStage()
    {
        LoadStage(_currentWorldID, _currentStageID, true);
    }

    public bool HasNextStage()
    {
        return FindStage(_currentWorldID, _currentStageID + 1) != null;
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
