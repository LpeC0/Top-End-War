using System.Collections;
using UnityEngine;

/// <summary>
/// Top End War — AnchorModeManager v2.0
///
/// arenaForwardOffset = 0: AnchorCore player'ın yanına gelir.
/// Spawn noktaları AnchorSpawnController'da player'ın önünde dinamik hesaplanır.
/// </summary>
public class AnchorModeManager : MonoBehaviour
{
    public static AnchorModeManager Instance { get; private set; }

    [Header("Bağlantılar")]
    [SerializeField] AnchorSpawnController _spawnController;
    [SerializeField] AnchorPickupSpawner _pickupSpawner;
    [SerializeField] AnchorBuffManager _buffManager;

    [Header("Arena Hizalama")]
    public Transform anchorArenaRoot;
    [Tooltip("0 = core player ile aynı Z. Negatif = player arkasında. POZİTİF VERME.")]
    public float arenaForwardOffset = 0f;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] StageBlueprint _activeBlueprint;
    [SerializeField] int  _currentWaveIndex;
    [SerializeField] bool _isActive;
    [SerializeField] bool _isComplete;
    [SerializeField] AnchorModeState _state;

    public bool IsActive      => _isActive;
    public bool IsComplete    => _isComplete;
    public int  CurrentWave   => _currentWaveIndex;
    public int  TotalWaves    => _activeBlueprint != null ? _activeBlueprint.TotalWaves : 0;
    public AnchorModeState State => _state;

    float _survivalTimer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnAnchorDestroyed += HandleAnchorDestroyed;
        GameEvents.OnGameOver        += HandleGameOver;
    }

    void OnDisable()
    {
        GameEvents.OnAnchorDestroyed -= HandleAnchorDestroyed;
        GameEvents.OnGameOver        -= HandleGameOver;
    }

    void Update()
    {
        if (!_isActive || _isComplete) return;
        if (_activeBlueprint.winCondition == AnchorWinCondition.SurviveSeconds)
        {
            _survivalTimer += Time.deltaTime;
            if (_survivalTimer >= _activeBlueprint.survivalDuration)
                CompleteAnchor();
        }
    }

    public void StartAnchor(StageBlueprint blueprint)
    {
        if (blueprint == null) { Debug.LogError("[AnchorModeManager] Blueprint null."); return; }

        if (blueprint.winCondition == AnchorWinCondition.ClearAllWaves && blueprint.TotalWaves <= 0)
        {
            Debug.LogWarning("[AnchorModeManager] Blueprint dalga içermiyor.");
            ThreatManager.Instance?.SetRunActive(false);
            GameEvents.OnAnchorModeChanged?.Invoke(false);
            return;
        }

        _activeBlueprint  = blueprint;
        _currentWaveIndex = 0;
        _isActive         = true;
        _isComplete       = false;
        _survivalTimer    = 0f;
        _state            = AnchorModeState.WaitingForWave;

        ClearRunnerEnemiesForAnchor();
        AlignArenaToPlayer();
        ResolveSpawnController();
        ResolvePickupSpawner();
        ResolveBuffManager();

        ThreatManager.Instance?.SetRunActive(true);
        AnchorCore.Instance?.InitAnchor(blueprint.anchorBaseHP);
        GameEvents.OnAnchorModeChanged?.Invoke(true);
        _pickupSpawner?.BeginAnchorPickups();

        Debug.Log($"[AnchorModeManager] Başladı | {blueprint.blueprintId} | {blueprint.TotalWaves} dalga");
        StartCoroutine(RunWaveSequence());
    }

    void ClearRunnerEnemiesForAnchor()
    {
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e != null && e.gameObject.activeInHierarchy)
                e.gameObject.SetActive(false);
        }
    }

    void ResolveSpawnController()
    {
        if (_spawnController == null)
            _spawnController = FindFirstObjectByType<AnchorSpawnController>();
    }

    void ResolvePickupSpawner()
    {
        if (_pickupSpawner == null)
            _pickupSpawner = FindFirstObjectByType<AnchorPickupSpawner>();

        if (_pickupSpawner == null)
            _pickupSpawner = gameObject.AddComponent<AnchorPickupSpawner>();
    }

    void ResolveBuffManager()
    {
        if (_buffManager == null)
            _buffManager = AnchorBuffManager.EnsureInstance();
    }

    void AlignArenaToPlayer()
    {
        if (anchorArenaRoot == null)
        {
            var found = GameObject.Find("AnchorArenaRoot");
            if (found != null) anchorArenaRoot = found.transform;
        }

        if (anchorArenaRoot == null || PlayerStats.Instance == null) return;

        Vector3 p = PlayerStats.Instance.transform.position;
        // arenaForwardOffset = 0 → core player konumuna gelir.
        // Spawn noktası SpawnController'da player'ın ÖNÜNDE hesaplanır.
        anchorArenaRoot.position = new Vector3(0f, 0f, p.z + arenaForwardOffset);
        Debug.Log($"[AnchorModeManager] Arena hizalandı | core≈{anchorArenaRoot.position} | player={p}");
    }

    IEnumerator RunWaveSequence()
    {
        while (_currentWaveIndex < _activeBlueprint.TotalWaves)
        {
            if (!_isActive) yield break;

            AnchorWaveEntry wave = _activeBlueprint.GetWave(_currentWaveIndex);
            if (wave == null) { _currentWaveIndex++; continue; }

            if (!string.IsNullOrEmpty(wave.warningText))
            {
                _state = AnchorModeState.ShowingWarning;
                GameEvents.OnWaveWarning?.Invoke(wave.warningText);
                yield return new WaitForSeconds(wave.warningLeadTime);
            }

            _state = AnchorModeState.WaveActive;
            GameEvents.OnAnchorWaveStarted?.Invoke(_currentWaveIndex, _activeBlueprint.TotalWaves);
            Debug.Log($"[AnchorModeManager] Dalga {_currentWaveIndex + 1}/{_activeBlueprint.TotalWaves}");

            ResolveSpawnController();
            if (_spawnController != null)
                yield return StartCoroutine(_spawnController.SpawnWave(wave, _activeBlueprint.difficultyMultiplier));
            else
                Debug.LogWarning("[AnchorModeManager] AnchorSpawnController atanmamış.");

            if (wave.waitForClearBeforeNext)
            {
                // DEĞİŞİKLİK: Eski stage davranışı korunur; sadece işaretlenen W1-01 surge dalgaları overlap eder.
                _state = AnchorModeState.WaitingForClear;
                yield return StartCoroutine(WaitForWaveClear());
                if (!_isActive) yield break;
                GameEvents.OnAnchorWaveCleared?.Invoke(_currentWaveIndex + 1);
            }

            _currentWaveIndex++;

            if (_currentWaveIndex < _activeBlueprint.TotalWaves)
            {
                _state = AnchorModeState.WaveCooldown;
                yield return new WaitForSeconds(_activeBlueprint.waveCooldown);
            }
        }

        if (_isActive && !_isComplete)
        {
            // DEĞİŞİKLİK: Overlap wave kullanılsa bile stage clear için tüm düşmanlar temizlenir.
            if (_activeBlueprint.winCondition == AnchorWinCondition.ClearAllWaves)
                yield return StartCoroutine(WaitForWaveClear());
            if (!_isActive) yield break;
            if (_activeBlueprint.winCondition == AnchorWinCondition.ClearAllWaves)
                CompleteAnchor();
        }
    }

    IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            if (!_isActive) yield break;
            bool anyAlive = false;
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (e.IsAlive) { anyAlive = true; break; }
            if (!anyAlive) yield break;
            yield return new WaitForSeconds(0.5f);
        }
    }

    void CompleteAnchor()
    {
        if (_isComplete) return;
        _isComplete = true;
        _isActive   = false;
        _state      = AnchorModeState.Complete;
        _pickupSpawner?.StopAnchorPickups();
        ThreatManager.Instance?.SetRunActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        bool perfect = AnchorCore.Instance != null
                    && AnchorCore.Instance.MaxHP > 0
                    && AnchorCore.Instance.CurrentHP >= AnchorCore.Instance.MaxHP;
        GameEvents.OnAnchorCompleted?.Invoke(perfect);
        Debug.Log($"[AnchorModeManager] Tamamlandı | Perfect={perfect}");
    }

    void HandleAnchorDestroyed()
    {
        if (!_isActive) return;
        _isActive = false; _isComplete = true; _state = AnchorModeState.Failed;
        _pickupSpawner?.StopAnchorPickups();
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
    }

    void HandleGameOver()
    {
        if (!_isActive) return;
        _isActive = false; _state = AnchorModeState.Failed;
        _pickupSpawner?.StopAnchorPickups();
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
    }

    public void ForceStop()
    {
        _isActive = false;
        _pickupSpawner?.StopAnchorPickups();
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!_isActive) return;
        GUILayout.BeginArea(new Rect(10, 380, 260, 80));
        GUILayout.Label("[AnchorModeManager]");
        GUILayout.Label($"Dalga: {_currentWaveIndex + 1} / {TotalWaves}");
        GUILayout.Label($"Durum: {_state}");
        if (PlayerStats.Instance != null)
        {
            AnchorStance stance = AnchorCoverage.StanceFromX(PlayerStats.Instance.transform.position.x);
            GUILayout.Label($"Stance: {stance}");

            if (Time.time - AnchorCoverage.LastReportTime < 2.0f)
                GUILayout.Label($"Coverage: {AnchorCoverage.LastEnemyLane} x{AnchorCoverage.LastMultiplier:F2} [{AnchorCoverage.GetQualityLabel(AnchorCoverage.LastMultiplier)}]");
        }
        if (_activeBlueprint?.winCondition == AnchorWinCondition.SurviveSeconds)
            GUILayout.Label($"Süre: {_survivalTimer:F1} / {_activeBlueprint.survivalDuration:F0}s");
        GUILayout.EndArea();
    }
#endif
}

public enum AnchorModeState
{
    Idle, ShowingWarning, WaveActive, WaitingForClear,
    WaveCooldown, WaitingForWave, Complete, Failed,
}
