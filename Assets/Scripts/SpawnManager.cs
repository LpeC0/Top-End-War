using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v16 (Spawn Rhythm v1)
///
/// v15 → v16 Delta:
///   • [Spawn Rhythm] header + rhythmTable alani eklendi.
///   • _lastPacket: ayni packet ust uste gelmesin diye izlenir.
///   • ResetForStage(): _lastPacket sifirlanir.
///   • SpawnEnemyWave(): TrySpawnPacket() cagrilir —
///     TrySpawnConfiguredWave ve prosedural fallback'ten once.
///   • TrySpawnPacket(): rhythmTable'dan stage'e gore agirlikli secim.
///   • SpawnFromPacket(): packet icindeki WaveGroup'lari Z uzayina yayar,
///     jitterZ / jitterX / intraZStep / hasLeadGap uygulanir.
///
/// Secim onceligi (degismedi):
///   1. StageConfig.waveSequence dolu → TrySpawnConfiguredWave kullanilir.
///   2. rhythmTable atanmis          → TrySpawnPacket kullanilir.   ← YENİ
///   3. Ikisi de yoksa               → prosedural fallback.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;

    [Header("Gate Havuzlari")]
    public GatePoolConfig poolStage1To5;
    public GatePoolConfig poolStage6To10;

    [Header("Spawn")]
    public float spawnAhead  = 85f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Runner Tuning")]
    [Range(0.5f, 1.2f)] public float runnerEnemyHpMultiplier = 0.88f; // DEĞİŞİKLİK: Runner fazla kolaylaşmasın, ama önceki geçilebilirlik korunur.

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    // ── Spawn Rhythm ─────────────────────────────────────────────────────
    [Header("Spawn Rhythm  (Stage 1-10 otomatik packet secimi)")]
    [Tooltip("Atanmazsa prosedural fallback devrede kalir. " +
             "StageConfig.waveSequence dolu stage'lerde bu tablo devreye GIRMEZ.")]
    public SpawnRhythmTable rhythmTable;

    // ── Runtime Durum ─────────────────────────────────────────────────────
    float _nextGateZ  = 40f;
    float _nextWaveZ  = 55f;
    bool  _bossSpawned = false;
    int   _waveCursor  = 0;
    int   _spawnedGateGroups = 0;
    int   _spawnedCombatPackets = 0;
    int   _nextGateChoiceGroupId = 1;
    float _lastCombatSpawnTime = -999f;
    float _lastCombatSpawnPlayerZ = -999f;
    bool  _lastCombatSpawnWasFollowup = false;
    float _lastGateSpawnZ = -999f;
    bool  _followupUsedForLastGate = true;
    float _lastSkipLogTime = -999f;

    const float FIRST_WAVE_Z = 62f;
    const float FIRST_GATE_Z = 52f;
    const float GATE_FOLLOWUP_MIN = 18f;
    const float GATE_FOLLOWUP_MAX = 30f;
    const float MIN_GATE_SPAWN_AHEAD = 38f; // DEĞİŞİKLİK: Gate cursor geride kalırsa kapı oyuncunun dibinde doğmaz.
    const float RUNNER_END_GATE_BUFFER = 18f; // DEĞİŞİKLİK: Runner->Anchor geçiş sınırının ötesine gate planlanmaz.
    const float RUNNER_END_COMBAT_BUFFER = 28f; // DEĞİŞİKLİK: Anchor girişine yakın runner enemy packet pop-in'i engellenir.
    const float MAX_EMPTY_GAP = 65f;
    const float COMBAT_SPAWN_COOLDOWN = 1.2f;
    const float MIN_COMBAT_PLAYER_ADVANCE = 20f;
    const float MIN_COMBAT_SPAWN_AHEAD = 60f;
    const float SKIP_LOG_INTERVAL = 0.75f;

    // RHYTHM: son secilen packet — tekrar azaltmak icin izlenir
    SpawnPacketConfig _lastPacket = null;
    SpawnPacketConfig _plannedFollowupPacket = null; // DEĞİŞİKLİK: Gate sonrası gelecek threat önceden seçilir ve preview edilir.

    public void ResetForStage()
    {
        spawnAhead = Mathf.Max(spawnAhead, 85f); // DEĞİŞİKLİK: Runner pop-in hissini azaltmak için combat daha uzaktan görünür.
        _nextGateZ   = FIRST_GATE_Z;
        _nextWaveZ   = FIRST_WAVE_Z;
        _bossSpawned = false;
        _waveCursor  = 0;
        _spawnedGateGroups = 0;
        _spawnedCombatPackets = 0;
        _nextGateChoiceGroupId = 1;
        _lastCombatSpawnTime = -999f;
        _lastCombatSpawnPlayerZ = -999f;
        _lastCombatSpawnWasFollowup = false;
        _lastGateSpawnZ = -999f;
        _followupUsedForLastGate = true;
        _lastSkipLogTime = -999f;
        _lastPacket  = null;   // RHYTHM: yeni stage'de cesitlilik yeniden baslar
        _plannedFollowupPacket = null; // DEĞİŞİKLİK: Yeni stage'de threat preview planı temizlenir.
        Gate.ResetChoiceState();
    }

    DifficultyManager.EnemyStats _stats;
    bool _statsReady = false;

    float _overrideNormalHP = 0f;
    float _overrideEliteHP  = 0f;
    float _densityMult      = 1f;
    bool  _hpOverrideActive = false;

    // ── Yasam Dongusu ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        RefreshStats();
        GameEvents.OnDifficultyChanged += (m, r) => RefreshStats();
    }

    void RefreshStats()
    {
        _stats = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetScaledEnemyStats()
            : FallbackStats();
        _statsReady = true;
    }

    DifficultyManager.EnemyStats FallbackStats()
    {
        float z = playerTransform != null ? playerTransform.position.z : 0f;
        float m = 1f + Mathf.Pow(z / 1000f, 1.3f);
        return new DifficultyManager.EnemyStats(
            Mathf.RoundToInt(100f * m), Mathf.RoundToInt(25f * m),
            Mathf.Min(4f + (m - 1f) * 1.4f, 7.5f), Mathf.RoundToInt(15f * m));
    }

    // PATCH v15: Boss trigger'i yalnizca boss stage'lerde cal
    bool StageHasBoss()
{
    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    return stage != null && stage.IsBossStage;
}

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();
        if (AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive) return; // DEĞİŞİKLİK: Anchor aktifken runner spawn akışı kesin durur.

        float pz = playerTransform.position.z;

        if (!_bossSpawned && StageHasBoss() && pz >= bossDistance)
        {
            _bossSpawned = true;

            StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;

            if (BossManager.Instance != null && stage != null && stage.bossConfig != null)
                BossManager.Instance.StartBoss(stage.bossConfig, stage.targetDps);
            else if (BossManager.Instance != null && stage != null)
                BossManager.Instance.StartBoss(stage.GetBossHP());
            else if (BossManager.Instance != null)
                BossManager.Instance.StartBoss();

            return;
        }

        GuardSpawnCursors(pz);

        if (pz + spawnAhead >= _nextGateZ)
        {
            if (!(_spawnedCombatPackets == 0 && _nextGateZ > _nextWaveZ))
            {
                float gateZ = Mathf.Max(_nextGateZ, pz + MIN_GATE_SPAWN_AHEAD); // DEĞİŞİKLİK: Runner gate minimum karar mesafesinde spawn edilir.
                if (!ShouldSuppressRunnerSpawn(gateZ, RUNNER_END_GATE_BUFFER))
                {
                    ScheduleGateFollowupOnce(gateZ); // DEĞİŞİKLİK: Gate seçenekleri spawn olmadan önce yaklaşan threat planlanır.
                    SpawnNextGateGroup(gateZ);
                    _followupUsedForLastGate = true; // DEĞİŞİKLİK: Follow-up bu gate için zaten planlandı.
                }
                _nextGateZ = gateZ + gateSpacing;
            }
        }

        if (pz + spawnAhead >= _nextWaveZ)
        {
            TrySpawnCombatAtCursor(pz);
        }
    }

    void GuardSpawnCursors(float playerZ)
    {
        float nearestUpcoming = Mathf.Min(_nextGateZ, _nextWaveZ);
        bool canFillGap = nearestUpcoming - playerZ > MAX_EMPTY_GAP
                       && !_lastCombatSpawnWasFollowup
                       && CanSpawnCombatNow(playerZ, out _)
                       && CountActiveEnemies() <= Mathf.Max(4, GetActiveEnemyCap() / 2);

        if (canFillGap)
        {
            _nextWaveZ = playerZ + 48f;
            Debug.Log($"[Spawn] gap fallback nextWaveZ={_nextWaveZ:F1} activeEnemies={CountActiveEnemies()}");
        }

        if (_spawnedGateGroups > _spawnedCombatPackets + 1)
            _nextWaveZ = Mathf.Min(_nextWaveZ, _nextGateZ - GATE_FOLLOWUP_MIN);
    }

    void ScheduleGateFollowupOnce(float gateZ)
    {
        if (_followupUsedForLastGate && Mathf.Approximately(_lastGateSpawnZ, gateZ))
            return;

        float latestFollowup = gateZ + GATE_FOLLOWUP_MAX;
        float earliestFollowup = gateZ + GATE_FOLLOWUP_MIN;
        if (_nextWaveZ > latestFollowup)
            _nextWaveZ = Random.Range(earliestFollowup, latestFollowup);
        else if (_nextWaveZ > gateZ && _nextWaveZ < earliestFollowup)
            _nextWaveZ = earliestFollowup;

        _followupUsedForLastGate = true;
        PlanGateFollowupThreat(); // DEĞİŞİKLİK: Gate seçimini yaklaşan combat packet'ına bağlayan preview planı.
        Debug.Log($"[Spawn] gate followup used gateZ={gateZ:F1} nextWaveZ={_nextWaveZ:F1}");
    }

    void TrySpawnCombatAtCursor(float playerZ)
    {
        if (!CanSpawnCombatNow(playerZ, out string reason))
        {
            LogSpawnSkip(reason);
            return;
        }

        float spawnZ = Mathf.Max(_nextWaveZ, playerZ + MIN_COMBAT_SPAWN_AHEAD);
        if (ShouldSuppressRunnerSpawn(spawnZ, RUNNER_END_COMBAT_BUFFER))
        {
            // DEĞİŞİKLİK: Anchor'a birkaç metre kala yeni runner enemy packet'i üretilmez.
            _nextWaveZ = spawnZ + GetCombatTempoSpacing();
            return;
        }

        bool spawned = SpawnEnemyWave(spawnZ);
        float spacing = GetCombatTempoSpacing();
        _nextWaveZ = spawnZ + spacing;

        if (!spawned)
            return;

        int activeEnemies = CountActiveEnemies();
        _spawnedCombatPackets++;
        _lastCombatSpawnTime = Time.time;
        _lastCombatSpawnPlayerZ = playerZ;
        _lastCombatSpawnWasFollowup = _lastGateSpawnZ > 0f
                                   && spawnZ >= _lastGateSpawnZ + GATE_FOLLOWUP_MIN - 0.1f
                                   && spawnZ <= _lastGateSpawnZ + GATE_FOLLOWUP_MAX + 0.1f;

        Debug.Log($"[Spawn] packet={_lastPacket?.packetType.ToString() ?? "Fallback"} z={spawnZ:F1} activeEnemies={activeEnemies} nextWaveZ={_nextWaveZ:F1}");
    }

    bool CanSpawnCombatNow(float playerZ, out string reason)
    {
        int activeEnemies = CountActiveEnemies();
        if (activeEnemies >= GetActiveEnemyCap())
        {
            reason = "active cap";
            return false;
        }

        if (Time.time - _lastCombatSpawnTime < COMBAT_SPAWN_COOLDOWN)
        {
            reason = "cooldown";
            return false;
        }

        if (playerZ - _lastCombatSpawnPlayerZ < MIN_COMBAT_PLAYER_ADVANCE)
        {
            reason = "distance";
            return false;
        }

        reason = null;
        return true;
    }

    void LogSpawnSkip(string reason)
    {
        if (Time.time - _lastSkipLogTime < SKIP_LOG_INTERVAL)
            return;

        _lastSkipLogTime = Time.time;
        Debug.Log($"[Spawn] skipped {reason}");
    }

    bool ShouldSuppressRunnerSpawn(float spawnZ, float endBuffer)
    {
        // DEĞİŞİKLİK: Runner->Anchor transition sırasında gate/enemy objeleri stage bitişinin arkasına taşmaz.
        StageManager sm = StageManager.Instance;
        StageConfig stage = sm != null ? sm.ActiveStage : null;
        if (stage == null || stage.playMode != StagePlayMode.RunnerToAnchor) return false;
        return spawnZ >= sm.GetStageEndZ() - Mathf.Max(0f, endBuffer);
    }

    public string GetTransitionDebugState()
    {
        // DEĞİŞİKLİK: Runner-anchor transition debug için cursor state tek satır raporlanır.
        return $"spawnEnabled={enabled} nextGateZ={_nextGateZ:F1} nextEnemyZ={_nextWaveZ:F1} gates={_spawnedGateGroups} packets={_spawnedCombatPackets}";
    }

    float GetCombatTempoSpacing()
    {
        return Mathf.Max(24f, waveSpacing + Random.Range(-4f, 6f));
    }

    int GetActiveEnemyCap()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        if (stage <= 5) return 14;
        if (stage <= 10) return 18;
        if (stage <= 20) return 24;
        return 30;
    }

    int CountActiveEnemies()
    {
        int count = 0;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                count++;
        return count;
    }

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform;
    }

    // ── Wave Dispatch ─────────────────────────────────────────────────────

    bool SpawnEnemyWave(float zPos)
    {
        // 1) Manuel waveSequence (StageConfig'e elle baglanmis dalgalar)
        if (TrySpawnConfiguredWave(zPos)) return true;

        // 2) RHYTHM: rhythmTable atanmissa otomatik packet secimi  ← YENİ
        if (TrySpawnPacket(zPos)) return true;

        // 3) Prosedural fallback (eski davranis)
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int cnt    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }

        return true;
    }

    // ── RHYTHM: Packet Secim ve Spawn ─────────────────────────────────────

    /// <summary>
    /// rhythmTable'dan aktif stage'e gore bir packet secer ve spawn eder.
    /// StageConfig.waveSequence dolu stage'lerde bu metot hic cagrilmaz.
    /// </summary>
    bool TrySpawnPacket(float zPos)
    {
        if (rhythmTable == null) return false;

        SpawnPacketConfig packet = _plannedFollowupPacket != null
            ? _plannedFollowupPacket
            : PickRhythmPacketForCurrentStage(); // DEĞİŞİKLİK: Preview edilen packet varsa spawn onu kullanır.
        _plannedFollowupPacket = null;

        if (packet == null) return false;

        SpawnFromPacket(packet, zPos);
        _lastPacket = packet;

        Debug.Log($"[SpawnManager] Packet: {packet.packetId} ({packet.packetType}) @ z={zPos:F0}");
        return true;
    }

    SpawnPacketConfig PickRhythmPacketForCurrentStage()
    {
        // DEĞİŞİKLİK: Threat preview ve normal rhythm aynı seçim metodunu kullanır.
        if (rhythmTable == null) return null;
        int currentWorld = StageManager.Instance != null ? StageManager.Instance.CurrentWorldID : 1;
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        float stageProgress = StageManager.Instance != null ? StageManager.Instance.GetStageProgress01() : -1f;
        return rhythmTable.Pick(currentWorld, currentStage, _lastPacket, stageProgress);
    }

    void PlanGateFollowupThreat()
    {
        // DEĞİŞİKLİK: Oyuncu gate'e yaklaşırken hemen sonraki tehdidi görür.
        StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        if (stage != null && stage.waveSequence != null && stage.waveSequence.Count > 0) return;

        _plannedFollowupPacket = PickRhythmPacketForCurrentStage();
        if (_plannedFollowupPacket == null) return;

        string preview = BuildThreatPreviewText(_plannedFollowupPacket);
        if (!string.IsNullOrEmpty(preview))
        {
            RunDebugMetrics.Instance.RecordThreatPreview(preview); // DEĞİŞİKLİK: Prep özeti son runner tehdidini bilir.
            GameEvents.OnThreatPreview?.Invoke(preview);
        }
    }

    string BuildThreatPreviewText(SpawnPacketConfig packet)
    {
        // DEĞİŞİKLİK: Packet tipi oyuncuya kısa, aksiyon alınabilir threat diliyle gösterilir.
        if (packet == null) return "";

        float progress = StageManager.Instance != null ? StageManager.Instance.GetStageProgress01() : 0f;
        if (StageManager.Instance?.ActiveStage != null
            && StageManager.Instance.ActiveStage.playMode == StagePlayMode.RunnerToAnchor
            && progress > 0.72f)
            return "ANCHOR ASSAULT SOON";

        return packet.packetType switch
        {
            PacketType.DenseSwarm => "SWARM INCOMING",
            PacketType.ArmorCheck => "ARMOR PRESSURE APPROACHING",
            PacketType.DelayedCharger => "CHARGER PRESSURE AHEAD",
            PacketType.Relief => "SHORT RELIEF AHEAD",
            PacketType.EliteSpike => "ELITE UNIT DETECTED",
            _ => "LANE PRESSURE AHEAD",
        };
    }

    /// <summary>
    /// Packet'teki WaveGroup'lari Z uzayina serer.
    /// hasLeadGap: ilk gruptan SONRA leadGapZ metre bosluk eklenir (DelayedCharger icin).
    /// jitterZ / jitterX: dogal gorunum icin per-enemy rastgele kayma.
    /// </summary>
    void SpawnFromPacket(SpawnPacketConfig packet, float baseZ)
    {
        if (packet == null || packet.groups == null || packet.groups.Count == 0) return;

        StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        float density = stage != null ? stage.spawnDensity : 1f;
        // Rhythm tarafinda density etkisini yumuşak tut: count bazli hafif olcekleme.
        float safeDensity = Mathf.Clamp(density, 0.75f, 1.35f);

        for (int g = 0; g < packet.groups.Count; g++)
        {
            WaveGroup group = packet.groups[g];
            if (group == null || group.archetype == null) continue;
            int spawnCount = Mathf.Clamp(Mathf.RoundToInt(group.count * safeDensity), 1, 15);
            float groupBaseZ = baseZ + (g * packet.groupZStep);
            // DelayedCharger: ilk grup normal gelsin, sonraki gruplardan once bosluk eklensin.
            if (packet.hasLeadGap && g > 0)
                groupBaseZ += packet.leadGapZ;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 shape = GetPacketShapeOffset(packet.packetType, g, i, spawnCount, packet.intraZStep);
                float z = groupBaseZ
                        + shape.y
                        + (i * packet.intraZStep * 0.35f)
                        + Random.Range(-packet.jitterZ * 0.5f, packet.jitterZ * 0.5f);

                Vector3 pos = GetLaneBiasedSpawnPos(group.laneBias, i, spawnCount, z);
                pos.x += shape.x;

                // X jitter (saf Spread dis diger lane'ler icin dogallik)
                if (packet.jitterX > 0f)
                    pos.x = Mathf.Clamp(
                        pos.x + Random.Range(-packet.jitterX * 0.5f, packet.jitterX * 0.5f),
                        -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);

                SpawnArchetypeEnemy(group.archetype, pos);
            }
        }
    }

    // Packet bazli hafif formation offset'leri.
    // Amaç: line-string hissini kırmak; mevcut lane ve rhythm akışını bozmamak.
    Vector2 GetPacketShapeOffset(PacketType type, int groupIndex, int memberIndex, int groupCount, float intraZStep)
    {
        float t = groupCount <= 1 ? 0f : (float)memberIndex / (groupCount - 1); // 0..1
        float centered = t - 0.5f;                                              // -0.5..0.5
        float zig = (memberIndex % 2 == 0) ? -1f : 1f;

        switch (type)
        {
            case PacketType.Baseline:
                // Hafif staggered line
                return new Vector2(centered * 0.55f + zig * 0.15f, zig * intraZStep * 0.35f);

            case PacketType.DenseSwarm:
            {
                // Cluster/blob: merkez etrafında küçük bulut
                float angle = memberIndex * 1.618f;
                float ring = 0.25f + (memberIndex % 3) * 0.22f;
                float x = Mathf.Cos(angle) * ring * 1.4f;
                float z = Mathf.Sin(angle) * ring * 1.0f + zig * intraZStep * 0.18f;
                return new Vector2(x, z);
            }

            case PacketType.DelayedCharger:
                // Support -> daha yaygın; Charger -> center biased ama üst üste değil
                if (groupIndex == 0)
                    return new Vector2(centered * 0.85f + zig * 0.12f, zig * intraZStep * 0.28f);
                return new Vector2(centered * 0.35f + zig * 0.18f, zig * intraZStep * 0.22f);

            case PacketType.ArmorCheck:
                // Front anchor (group0) + arka/yan destek (group1+)
                if (groupIndex == 0)
                    return new Vector2(centered * 0.28f, -0.45f * intraZStep);
                return new Vector2(centered * 0.75f + zig * 0.15f, 0.55f * intraZStep + zig * 0.12f);

            case PacketType.Relief:
                // Seyrek, rahat, ama tek çizgi değil
                return new Vector2(centered * 0.95f + zig * 0.20f, zig * intraZStep * 0.55f);

            default:
                return Vector2.zero;
        }
    }

    // ── Gate ──────────────────────────────────────────────────────────────

    void SpawnNextGateGroup(float zPos) => SpawnNormalPair(zPos, pity: false);

    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;

        GateConfig leftGate  = PickGateForPlannedThreat(); // DEĞİŞİKLİK: Threat preview'a en az bir gate cevabı vermeyi dener.
        GateConfig rightGate = PickGateFromPoolDistinct(leftGate);
        int choiceGroupId = _nextGateChoiceGroupId++;

        SpawnGate(leftGate,  new Vector3(-offset, 1.5f, zPos), scale: 1f, choiceGroupId: choiceGroupId);
        SpawnGate(rightGate, new Vector3( offset, 1.5f, zPos), scale: 1f, choiceGroupId: choiceGroupId);
        _spawnedGateGroups++;
        _lastGateSpawnZ = zPos;
    }

    GatePoolConfig GetActiveGatePool()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return stage <= 5 ? poolStage1To5 : poolStage6To10;
    }

    GateConfig PickGateFromPoolDistinct(GateConfig exclude)
    {
        for (int i = 0; i < 8; i++)
        {
            GateConfig picked = PickGateFromPool();
            if (picked != null && picked != exclude)
                return picked;
        }
        return PickGateFromPool();
    }

    GateConfig PickGateFromPool()
    {
        GatePoolConfig pool  = GetActiveGatePool();
        int            stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return pool != null ? pool.PickRandom(stage) : null;
    }

    GateConfig PickGateForPlannedThreat()
    {
        // DEĞİŞİKLİK: Swarm/Armor/Charger preview varsa ilk gate o probleme cevap olmaya çalışır.
        GatePoolConfig pool = GetActiveGatePool();
        if (pool == null || _plannedFollowupPacket == null)
            return PickGateFromPool();

        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        GateFamily family = _plannedFollowupPacket.packetType switch
        {
            PacketType.DenseSwarm => GateFamily.Tempo,
            PacketType.ArmorCheck => GateFamily.Solve,
            PacketType.DelayedCharger => GateFamily.Power,
            PacketType.EliteSpike => GateFamily.Solve,
            PacketType.Relief => GateFamily.Sustain,
            _ => GateFamily.Power,
        };

        GateConfig picked = pool.PickByFamily(stage, family);
        return picked != null ? picked : PickGateFromPool();
    }

    void SpawnGate(GateConfig data, Vector3 pos, float scale = 1f, int choiceGroupId = 0)
    {
        if (data == null) return;

        GameObject obj;
        if (gatePrefab != null)
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(0.95f, 1f, 1.5f);
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }

        Gate gate = obj.GetComponent<Gate>();
        if (gate != null)
        {
            gate.SetChoiceGroup(choiceGroupId);
            gate.BindGateConfig(data);
        }

        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);

        Destroy(obj, 45f);
    }

    // ── Prosedural Fallback ───────────────────────────────────────────────

    int PickWaveType(float p)
    {
        if (p < 0.25f) return 0;
        float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2;
    }

    void NormalWave(float z, int n)
    {
        int   cols = Mathf.Min(n, 4);
        int   rows = Mathf.CeilToInt((float)n / cols);
        int   pl   = 0;
        float gap  = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx   = -(gap * (cols - 1)) * 0.5f;

        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            {
                PlaceEnemy(new Vector3(
                    Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                    1.2f, z + r * 3f));
                pl++;
            }
    }

    void HeavyWave(float z, int n)
    {
        for (int i = 0; i < n; i++)
            PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
    }

    void FlankWave(float z, int n)
    {
        int h = n / 2;
        for (int i = 0; i < h; i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
        }
        if (n % 2 == 1) PlaceEnemy(new Vector3(0f, 1.2f, z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        foreach (Collider c in Physics.OverlapSphere(pos, 1.2f))
            if (c.CompareTag("Enemy")) { pos.x += 2.4f; break; }

        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);

        GameObject obj;
        if (enemyPrefab != null)
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy"; obj.AddComponent<Enemy>();
        }

        ConfigureEnemyPhysics(obj);
        obj.GetComponent<Enemy>()?.Initialize(GetEnemyStatsForSpawn());
    }

    public void SetMobHP(int normalHP, int eliteHP, float density = 1f)
    {
        _overrideNormalHP = normalHP;
        _overrideEliteHP  = eliteHP;
        _densityMult      = density;
        _hpOverrideActive = true;
        Debug.Log($"[SpawnManager] Mob HP override: Normal={normalHP}, Elite={eliteHP}, Density={density}");
    }

    DifficultyManager.EnemyStats GetEnemyStatsForSpawn()
    {
        if (_hpOverrideActive)
        {
            return new DifficultyManager.EnemyStats(
                health:   Mathf.RoundToInt(_overrideNormalHP),
                damage:   _stats.Damage,
                speed:    _stats.Speed,
                cpReward: _stats.CPReward);
        }
        return _stats;
    }

    // ── Manuel WaveSequence (StageConfig'e el ile baglanmis dalgalar) ─────

    bool TrySpawnConfiguredWave(float zPos)
    {
        StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        if (stage == null || stage.waveSequence == null || stage.waveSequence.Count == 0)
            return false;

        int safeIndex = Mathf.Clamp(_waveCursor, 0, stage.waveSequence.Count - 1);
        WaveConfig wave = stage.waveSequence[safeIndex];
        if (wave == null) return false;

        if (wave.waveRole == WaveRole.Boss) return false;

        SpawnWaveFromConfig(wave, zPos);
        _waveCursor++;
        return true;
    }

    void SpawnWaveFromConfig(WaveConfig wave, float baseZ)
    {
        if (wave == null || wave.groups == null || wave.groups.Count == 0) return;

        float groupZStep = Mathf.Max(4f, wave.spawnGroupDelay * 3f);
        float intraZStep = Mathf.Max(1.5f, wave.intraGroupDelay * 3f);

        for (int g = 0; g < wave.groups.Count; g++)
        {
            WaveGroup group = wave.groups[g];
            if (group == null || group.archetype == null) continue;

            for (int i = 0; i < group.count; i++)
            {
                float z = baseZ + (g * groupZStep) + (i * intraZStep);
                Vector3 pos = GetLaneBiasedSpawnPos(group.laneBias, i, group.count, z);
                SpawnArchetypeEnemy(group.archetype, pos);
            }
        }
    }

    // ── Archetype Spawn ───────────────────────────────────────────────────

    void SpawnArchetypeEnemy(EnemyArchetypeConfig archetype, Vector3 pos)
    {
        if (archetype == null) return;

        StageConfig stage    = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
        float       targetDps = stage != null ? stage.targetDps : 100f;

        int hp       = Mathf.Max(1, Mathf.RoundToInt(archetype.GetHP(targetDps) * GetRunnerEnemyHpMultiplier()));
        int cpReward = archetype.GetCPReward(targetDps);

        var stats = new DifficultyManager.EnemyStats(
            health:   hp,
            damage:   archetype.contactDamage,
            speed:    archetype.moveSpeed,
            cpReward: cpReward);

        GameObject obj;
        if (enemyPrefab != null)
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy";
            obj.AddComponent<Enemy>();
        }

        ConfigureEnemyPhysics(obj);
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(stats);
            enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
            enemy.ConfigureArchetype(archetype);
        }
    }

    float GetRunnerEnemyHpMultiplier()
    {
        // DEĞİŞİKLİK: Runner hazırlık alanı biraz daha geçilebilir, Anchor testi ayrı blueprint zorluğuyla kalır.
        bool anchorActive = AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive;
        return anchorActive ? 1f : runnerEnemyHpMultiplier;
    }

    // ── Lane Yardimcisi ───────────────────────────────────────────────────

    void ConfigureEnemyPhysics(GameObject obj)
    {
        if (obj == null) return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            SetLayerRecursive(obj, enemyLayer);

        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    Vector3 GetLaneBiasedSpawnPos(LaneBias bias, int index, int total, float z)
    {
        float x     = 0f;
        float left  = -ROAD_HALF_WIDTH * 0.72f;
        float right =  ROAD_HALF_WIDTH * 0.72f;

        switch (bias)
        {
            case LaneBias.Center:
                x = Random.Range(-1.25f, 1.25f);
                break;
            case LaneBias.Left:
                x = Random.Range(left - 0.8f, left + 0.8f);
                break;
            case LaneBias.Right:
                x = Random.Range(right - 0.8f, right + 0.8f);
                break;
            case LaneBias.Random:
                x = Random.Range(-ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f);
                break;
            case LaneBias.Spread:
            default:
                x = (total <= 1) ? 0f : Mathf.Lerp(left, right, (float)index / (total - 1));
                break;
        }

        x = Mathf.Clamp(x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);
        return new Vector3(x, 1.2f, z);
    }
}
