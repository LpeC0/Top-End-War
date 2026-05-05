using System.Collections;
using UnityEngine;
using TMPro;

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
    Transform _tempVisualRoot;
    const int CONTINUOUS_PRESSURE_ALIVE_THRESHOLD = 2; // DEĞİŞİKLİK: Kısa cooldown anchor blueprint'lerinde son 1-2 düşman kalınca sonraki baskı hazırlanır.
    const float CONTINUOUS_PRESSURE_COOLDOWN_LIMIT = 1.0f; // DEĞİŞİKLİK: Eski uzun cooldown stage'leri bu overlap davranışından etkilenmez.

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
        if (_isActive && !_isComplete)
        {
            // DEĞİŞİKLİK: Anchor start aynı stage içinde ikinci kez tetiklenirse kamera/origin yeniden hizalanmaz.
            LogAnchorState("DuplicateStartBlocked");
            return;
        }

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
        EnsureTempAnchorVisuals(); // DEĞİŞİKLİK: Anchor savunma alanı mock lane/breach çizgileriyle okunur.
        ResolveSpawnController();
        ResolvePickupSpawner();
        ResolveBuffManager();

        ThreatManager.Instance?.SetRunActive(true);
        AnchorCore.Instance?.InitAnchor(blueprint.anchorBaseHP);
        GameEvents.OnAnchorModeChanged?.Invoke(true);
        _pickupSpawner?.BeginAnchorPickups();

        Debug.Log($"[AnchorModeManager] Başladı | {blueprint.blueprintId} | {blueprint.TotalWaves} dalga");
        LogAnchorState("Start"); // DEĞİŞİKLİK: Anchor start sonrası player/camera/origin tek satır izlenir.
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
            LogAnchorState($"Wave{_currentWaveIndex + 1}"); // DEĞİŞİKLİK: Kamera/reset şüphesi için sadece wave state değişiminde loglanır.

            ResolveSpawnController();
            if (_spawnController != null)
                yield return StartCoroutine(_spawnController.SpawnWave(wave, _activeBlueprint.difficultyMultiplier));
            else
                Debug.LogWarning("[AnchorModeManager] AnchorSpawnController atanmamış.");

            if (wave.waitForClearBeforeNext)
            {
                _state = AnchorModeState.WaitingForClear;
                if (ShouldContinueBeforeFullClear())
                    yield return StartCoroutine(WaitForPressureLow(CONTINUOUS_PRESSURE_ALIVE_THRESHOLD)); // DEĞİŞİKLİK: Flood-surge akışı boş ekran beklemeden sıradaki wave'i hazırlar.
                else
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

    IEnumerator WaitForPressureLow(int aliveThreshold)
    {
        // DEĞİŞİKLİK: Continuous pressure için tamamen clear değil, düşük baskı eşiği beklenir.
        while (true)
        {
            if (!_isActive) yield break;
            if (CountAliveEnemies() <= Mathf.Max(0, aliveThreshold)) yield break;
            yield return new WaitForSeconds(0.25f);
        }
    }

    bool ShouldContinueBeforeFullClear()
    {
        // DEĞİŞİKLİK: Sadece W1-01 gibi kısa cooldown flood-surge blueprint'leri overlap eder.
        return _activeBlueprint != null
            && _currentWaveIndex < _activeBlueprint.TotalWaves - 1
            && _activeBlueprint.waveCooldown <= CONTINUOUS_PRESSURE_COOLDOWN_LIMIT;
    }

    int CountAliveEnemies()
    {
        // DEĞİŞİKLİK: Anchor wave overlap kararı için aktif düşman sayısı tek yerde hesaplanır.
        int alive = 0;
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e != null && e.IsAlive)
                alive++;
        return alive;
    }

    void CompleteAnchor()
    {
        if (_isComplete) return;
        _isComplete = true;
        _isActive   = false;
        _state      = AnchorModeState.Complete;
        _pickupSpawner?.StopAnchorPickups();
        ClearTempAnchorVisuals(); // DEĞİŞİKLİK: Anchor bitince mock savunma görselleri temizlenir.
        AnchorCore.Instance?.SetTempVisualsVisible(false); // DEĞİŞİKLİK: Core placeholder anchor sonrası runner'da kalmaz.
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
        ClearTempAnchorVisuals(); // DEĞİŞİKLİK: Fail durumunda temp anchor görselleri sahnede kalmaz.
        AnchorCore.Instance?.SetTempVisualsVisible(false); // DEĞİŞİKLİK: Fail sonrası Core mock görünürlüğü temizlenir.
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
    }

    void HandleGameOver()
    {
        if (!_isActive) return;
        _isActive = false; _state = AnchorModeState.Failed;
        _pickupSpawner?.StopAnchorPickups();
        ClearTempAnchorVisuals(); // DEĞİŞİKLİK: GameOver sonrası temp anchor görselleri temizlenir.
        AnchorCore.Instance?.SetTempVisualsVisible(false); // DEĞİŞİKLİK: GameOver sonrası Core mock runner/sahne üstünde kalmaz.
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
    }

    public void ForceStop()
    {
        _isActive = false;
        _pickupSpawner?.StopAnchorPickups();
        ClearTempAnchorVisuals(); // DEĞİŞİKLİK: Force stop temp görselleri de kaldırır.
        AnchorCore.Instance?.SetTempVisualsVisible(false); // DEĞİŞİKLİK: Force stop Core mock cleanup yapar.
        StopAllCoroutines();
        ThreatManager.Instance?.SetRunActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
    }

    void EnsureTempAnchorVisuals()
    {
        // DEĞİŞİKLİK: Final art yokken breach line ve lane strip'leri primitive mock görsellerle gösterilir.
        ClearTempAnchorVisuals();
        if (PlayerStats.Instance == null || AnchorCore.Instance == null) return;

        GameObject root = new GameObject("Temp_AnchorDefenseVisuals");
        _tempVisualRoot = root.transform;

        Vector3 playerPos = PlayerStats.Instance.transform.position;
        Vector3 corePos = AnchorCore.Instance.transform.position;
        float laneLength = 30f;
        float laneZ = playerPos.z + laneLength * 0.5f + 2f;

        CreateStrip("Lane_Left", new Vector3(-4.8f, 0.035f, laneZ), new Vector3(1.45f, 0.035f, laneLength), new Color(0.15f, 0.45f, 1f, 0.45f));
        CreateStrip("Lane_Center", new Vector3(0f, 0.04f, laneZ), new Vector3(1.45f, 0.035f, laneLength), new Color(0.2f, 1f, 0.55f, 0.45f));
        CreateStrip("Lane_Right", new Vector3(4.8f, 0.035f, laneZ), new Vector3(1.45f, 0.035f, laneLength), new Color(1f, 0.55f, 0.15f, 0.45f));

        float breachZ = corePos.z + 2.1f;
        CreateStrip("BreachLine", new Vector3(0f, 0.08f, breachZ), new Vector3(11.2f, 0.08f, 0.42f), new Color(1f, 0.15f, 0.08f, 0.85f));
        CreateLaneLabel("LEFT", new Vector3(-4.8f, 0.16f, breachZ + 1.1f), new Color(0.35f, 0.65f, 1f));
        CreateLaneLabel("CENTER", new Vector3(0f, 0.16f, breachZ + 1.1f), new Color(0.35f, 1f, 0.6f));
        CreateLaneLabel("RIGHT", new Vector3(4.8f, 0.16f, breachZ + 1.1f), new Color(1f, 0.7f, 0.3f));
        CreateLaneLabel("BREACH LINE", new Vector3(0f, 0.25f, breachZ - 0.9f), new Color(1f, 0.25f, 0.18f));
        Debug.Log("[AnchorVisual] create core=1 line=1 lanes=3"); // DEĞİŞİKLİK: Temp visual duplicate/cleanup takibi için tek satır log.
    }

    void CreateStrip(string name, Vector3 position, Vector3 scale, Color color)
    {
        // DEĞİŞİKLİK: Lane/breach çizgileri basit cube strip olarak oluşturulur.
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.SetParent(_tempVisualRoot, true);
        strip.transform.position = position;
        strip.transform.localScale = scale;
        Destroy(strip.GetComponent<Collider>());
        Renderer renderer = strip.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }
    }

    void CreateLaneLabel(string text, Vector3 position, Color color)
    {
        // DEĞİŞİKLİK: Anchor lane kimliği TMP placeholder label ile okunur.
        GameObject obj = new GameObject("LaneLabel_" + text);
        obj.transform.SetParent(_tempVisualRoot, true);
        obj.transform.position = position;
        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 0.75f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.outlineWidth = 0.16f;
        tmp.outlineColor = Color.black;
        if (Camera.main != null)
            obj.transform.rotation = Quaternion.LookRotation(obj.transform.position - Camera.main.transform.position);
    }

    void ClearTempAnchorVisuals()
    {
        // DEĞİŞİKLİK: Anchor mock görselleri runtime-only tutulur.
        if (_tempVisualRoot == null) return;
        Destroy(_tempVisualRoot.gameObject);
        _tempVisualRoot = null;
        Debug.Log("[AnchorVisual] cleanup count=1"); // DEĞİŞİKLİK: Anchor temp visual cleanup doğrulanır.
    }

    void LogAnchorState(string label)
    {
        // DEĞİŞİKLİK: Kamera/origin reset problemini log spam olmadan izler.
        Vector3 playerPos = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : Vector3.zero;
        Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        Vector3 anchorPos = anchorArenaRoot != null ? anchorArenaRoot.position : Vector3.zero;
        bool runnerActive = SpawnManager.Instance != null && SpawnManager.Instance.enabled;
        Debug.Log($"[SnapTrace] t={Time.time:F1} state={_state} playerPos={playerPos:F1} cameraPos={cameraPos:F1} target=Player anchorActive={_isActive} runnerActive={runnerActive} reason={label} anchorOrigin={anchorPos:F1}");
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
