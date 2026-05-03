using UnityEngine;

/// <summary>
/// Top End War — Stage Yoneticisi v3 (Stage Flow Integration)
///
/// v2 → v3 Delta:
///   • StageRuntimePhase eklendi: Runner / Anchor / Complete
///   • HandleRunnerSegmentComplete(): playMode'a göre Anchor veya Clear
///   • StartAnchorForCurrentStage(): SpawnManager'ı durdurur, Anchor başlatır
///   • OnAnchorCompleted dinleyicisi: Anchor sonucunu Stage clear'a bağlar
///   • LoadStage(): her stage başında SpawnManager yeniden aktive edilir
///   • AnchorOnly playMode: LoadStage'den direkt anchor başlatır
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
    [SerializeField] StageRuntimePhase _phase = StageRuntimePhase.Runner;

    StageConfig _activeStage;
    WorldConfig _activeWorld;
    bool _stageClearLocked = false;

    enum StageRuntimePhase { Runner, Anchor, Complete }

    public StageConfig GetActiveStageConfig() => _activeStage;

    // ── Yaşam Döngüsü ──────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        GameEvents.OnBossDefeated     += HandleBossDefeated;
        GameEvents.OnAnchorCompleted  += HandleAnchorCompleted;
    }

    void Start() => LoadStage(_currentWorldID, _currentStageID, true);

    void OnDestroy()
    {
        GameEvents.OnBossDefeated    -= HandleBossDefeated;
        GameEvents.OnAnchorCompleted -= HandleAnchorCompleted;
    }

    void Update()
    {
        if (_activeStage == null)    return;
        if (_stageClearLocked)       return;
        if (PlayerStats.Instance == null) return;
        if (_phase != StageRuntimePhase.Runner) return;
        if (_activeStage.IsBossStage) return;

        if (PlayerStats.Instance.transform.position.z >= GetStageEndZ())
            HandleRunnerSegmentComplete();
    }

    // ── Runner → Anchor / Clear ───────────────────────────────────────────

    void HandleRunnerSegmentComplete()
    {
        if (_activeStage == null) return;

        if (_activeStage.playMode == StagePlayMode.RunnerToAnchor)
        {
            StartAnchorForCurrentStage();
            return;
        }

        OnStageComplete();
    }

    void StartAnchorForCurrentStage()
    {
        if (_activeStage == null) return;

        if (_activeStage.anchorBlueprint == null)
        {
            Debug.LogWarning("[StageManager] anchorBlueprint bos — OnStageComplete'e düşülüyor.");
            OnStageComplete();
            return;
        }

        if (!ValidateAnchorBlueprint(_activeStage.anchorBlueprint))
        {
            Debug.LogWarning("[StageManager] Anchor blueprint geçersiz — güvenli fallback ile stage tamamlanıyor.");
            OnStageComplete();
            return;
        }

        if (AnchorModeManager.Instance == null)
        {
            Debug.LogWarning("[StageManager] AnchorModeManager sahnede yok — OnStageComplete'e düşülüyor.");
            OnStageComplete();
            return;
        }

        _phase = StageRuntimePhase.Anchor;

        // Runner spawn sistemi anchor sırasında çalışmasın
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.enabled = false;

        // Runner'dan kalan objeler wave clear kontrolünü bozmasın
        ClearRuntimeObjects();

        AnchorModeManager.Instance.StartAnchor(_activeStage.anchorBlueprint);
        Debug.Log("[StageManager] Runner bitti → Anchor başladı.");
    }

    // ── Anchor Tamamlandı (AnchorModeManager'dan gelir) ──────────

    void HandleAnchorCompleted(bool perfect)
    {
        if (_phase != StageRuntimePhase.Anchor)
        {
            Debug.LogWarning("[StageManager] OnAnchorCompleted geldi ama phase Anchor değil.");
            return;
        }

        Debug.Log($"[StageManager] Anchor tamamlandı. Perfect={perfect}");
        OnStageComplete();
    }

    // ── Stage Yükle ───────────────────────────────────────────────────────

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
        _phase = StageRuntimePhase.Runner;

        // Her yeni stage'de SpawnManager'ı yeniden aktive et
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.enabled = true;

        _currentWorldID = worldID;
        _currentStageID = stageID;
        _activeWorld    = FindWorld(worldID);
        _activeStage    = FindStage(worldID, stageID);

        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] W{worldID}-{stageID} bulunamadi!");
            return;
        }

        ClearRuntimeObjects();

        Playercontroller pc = FindFirstObjectByType<Playercontroller>();
        pc?.ResetForStage(0f);

        PlayerStats.Instance?.ReviveFromGameOver();
        PlayerStats.Instance?.SetExpectedCP(_activeStage.targetDps);

        if (_activeWorld != null)
            BiomeManager.Instance?.SetBiome(_activeWorld.biome);

        SpawnManager.Instance?.ResetForStage();
        ApplyMobHP();

        if (_activeStage.IsBossStage)
            ApplyBossHP();

        GameEvents.OnStageChanged?.Invoke(worldID, stageID);
        Debug.Log($"[StageManager] W{worldID}-{stageID} | playMode={_activeStage.playMode} " +
                  $"| targetDps={_activeStage.targetDps}");

        // AnchorOnly: runner yok, direkt anchor başlar
        if (_activeStage.playMode == StagePlayMode.AnchorOnly)
            StartAnchorForCurrentStage();
    }

    // ── Runtime Temizliği ─────────────────────────────────────────────────

    void ClearRuntimeObjects()
    {
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e != null) Destroy(e.gameObject);

        foreach (var g in FindObjectsByType<Gate>(FindObjectsSortMode.None))
            if (g != null) Destroy(g.gameObject);

        foreach (var b in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            if (b != null && b.gameObject.activeSelf) b.gameObject.SetActive(false);

        foreach (var boss in FindObjectsByType<BossHitReceiver>(FindObjectsSortMode.None))
            if (boss != null) boss.gameObject.SetActive(false);
    }

    // ── HP Dağıtımı ───────────────────────────────────────────────────────

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
        Debug.Log($"[StageManager] Boss HP: {_activeStage.GetBossHP()}");
    }

    // ── Stage Tamamlandı ─────────────────────────────────────────────────

    /// <summary>
    /// Normal akış. Stage sonucunun, ödülün ve result event'in tek sahibidir.
    /// </summary>
    public void OnStageComplete()
    {
        if (_activeStage == null) return;
        if (_stageClearLocked) return;
        if (_activeStage.IsBossStage && BossManager.Instance != null && BossManager.Instance.IsActive()) return;

        _phase = StageRuntimePhase.Complete;
        _stageClearLocked = true;

        int gold = _activeStage.goldRewardOverride > 0
            ? _activeStage.goldRewardOverride
            : economyConfig != null
                ? economyConfig.GetGoldReward(_activeStage.stageID, _activeStage.targetDps)
                : 150;

        RunState.Instance.AddRunGold(gold);
        EconomyManager.Instance?.AddGold(gold);
        SaveManager.Instance?.RegisterStageComplete();

        Time.timeScale = 0f;

        StageConfig nextStage = FindStage(_currentWorldID, _currentStageID + 1);
        bool worldCleared = _activeStage.bossType == BossType.FinalBoss || nextStage == null;

        GameEvents.OnStageCleared?.Invoke(new GameEvents.StageClearInfo
        {
            worldID      = _currentWorldID,
            stageID      = _currentStageID,
            stageName    = _activeStage.DisplayStageName,
            goldReward   = gold,
            hasNextStage = nextStage != null,
            worldCleared = worldCleared
        });

        if (worldCleared) OnWorldComplete();

        Debug.Log($"[StageManager] Stage clear | Gold: {gold} | WorldCleared: {worldCleared}");
    }

    public void OnMidStageReached()
    {
        if (_activeStage == null || !_activeStage.hasMidStageLoot) return;
        int midGold = economyConfig != null
            ? economyConfig.GetMidLootGold(_activeStage.stageID, _activeStage.targetDps)
            : 50;
        EconomyManager.Instance?.AddGold(midGold);
        Debug.Log($"[StageManager] Micro-loot: +{midGold}");
    }

    void OnWorldComplete()
    {
        if (_activeWorld != null)
        {
            EconomyManager.Instance?.AddOfflineRate(_activeWorld.offlineIncomeBoost);
            if (_activeWorld.unlockedCommander != null)
                Debug.Log($"[StageManager] Komutan açıldı: {_activeWorld.unlockedCommander.commanderName}");
        }
        GameEvents.OnWorldChanged?.Invoke(_currentWorldID);
        GameEvents.OnVictory?.Invoke();
    }

    void HandleBossDefeated()
    {
        if (_activeStage != null && _activeStage.IsBossStage)
            OnStageComplete();
    }

    bool ValidateAnchorBlueprint(StageBlueprint blueprint)
    {
        if (blueprint == null)
        {
            Debug.LogWarning("[StageManager] Anchor blueprint null.");
            return false;
        }

        if (blueprint.waves == null || blueprint.waves.Count == 0)
        {
            Debug.LogWarning($"[StageManager] Anchor blueprint '{blueprint.blueprintId}' wave içermiyor.");
            return false;
        }

        bool hasSpawnableGroup = false;
        for (int i = 0; i < blueprint.waves.Count; i++)
        {
            AnchorWaveEntry wave = blueprint.waves[i];
            if (wave == null)
            {
                Debug.LogWarning($"[StageManager] Anchor blueprint '{blueprint.blueprintId}' wave {i + 1} null.");
                continue;
            }

            if (wave.groups == null || wave.groups.Count == 0)
            {
                Debug.LogWarning($"[StageManager] Anchor blueprint '{blueprint.blueprintId}' wave {i + 1} group içermiyor.");
                continue;
            }

            for (int g = 0; g < wave.groups.Count; g++)
            {
                WaveGroup group = wave.groups[g];
                if (group == null || group.archetype == null || group.count <= 0)
                {
                    Debug.LogWarning($"[StageManager] Anchor blueprint '{blueprint.blueprintId}' wave {i + 1} group {g + 1} geçersiz.");
                    continue;
                }

                hasSpawnableGroup = true;
            }
        }

        if (!hasSpawnableGroup)
            Debug.LogWarning($"[StageManager] Anchor blueprint '{blueprint.blueprintId}' spawn edilebilir group içermiyor.");

        return hasSpawnableGroup;
    }

    // ── Mesafe ───────────────────────────────────────────────────────────

    public float GetStageLength()
    {
        float bossDist = SpawnManager.Instance != null ? SpawnManager.Instance.bossDistance : 1200f;
        return Mathf.Max(300f, bossDist);
    }

    public float GetStageStartZ() => 0f;
    public float GetStageEndZ()   => GetStageLength();

    public float GetStageProgress01()
    {
        if (PlayerStats.Instance == null) return 0f;
        return Mathf.InverseLerp(GetStageStartZ(), GetStageEndZ(),
            PlayerStats.Instance.transform.position.z);
    }

    // ── Kontroller ────────────────────────────────────────────────────────

    public void ContinueAfterStageClear()
    {
        StageConfig next = FindStage(_currentWorldID, _currentStageID + 1);
        if (next == null) return;
        LoadStage(_currentWorldID, _currentStageID + 1, false);
    }

    public void RestartCurrentStage() => LoadStage(_currentWorldID, _currentStageID, true);
    public bool HasNextStage() => FindStage(_currentWorldID, _currentStageID + 1) != null;

    // ── Yardımcılar ───────────────────────────────────────────────────────

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

    public StageConfig ActiveStage   => _activeStage;
    public WorldConfig ActiveWorld   => _activeWorld;
    public int CurrentWorldID        => _currentWorldID;
    public int CurrentStageID        => _currentStageID;
}
