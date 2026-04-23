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
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

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

    // RHYTHM: son secilen packet — tekrar azaltmak icin izlenir
    SpawnPacketConfig _lastPacket = null;

    public void ResetForStage()
    {
        _nextGateZ   = 40f;
        _nextWaveZ   = 55f;
        _bossSpawned = false;
        _waveCursor  = 0;
        _lastPacket  = null;   // RHYTHM: yeni stage'de cesitlilik yeniden baslar
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

        while (pz + spawnAhead >= _nextGateZ)
        {
            SpawnNextGateGroup(_nextGateZ);
            _nextGateZ += gateSpacing;
        }

        while (pz + spawnAhead >= _nextWaveZ)
        {
            SpawnEnemyWave(_nextWaveZ);
            _nextWaveZ += waveSpacing;
        }
    }

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform;
    }

    // ── Wave Dispatch ─────────────────────────────────────────────────────

    void SpawnEnemyWave(float zPos)
    {
        // 1) Manuel waveSequence (StageConfig'e elle baglanmis dalgalar)
        if (TrySpawnConfiguredWave(zPos)) return;

        // 2) RHYTHM: rhythmTable atanmissa otomatik packet secimi  ← YENİ
        if (TrySpawnPacket(zPos)) return;

        // 3) Prosedural fallback (eski davranis)
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int cnt    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }
    }

    // ── RHYTHM: Packet Secim ve Spawn ─────────────────────────────────────

    /// <summary>
    /// rhythmTable'dan aktif stage'e gore bir packet secer ve spawn eder.
    /// StageConfig.waveSequence dolu stage'lerde bu metot hic cagrilmaz.
    /// </summary>
    bool TrySpawnPacket(float zPos)
    {
        if (rhythmTable == null) return false;

        int currentWorld = StageManager.Instance != null ? StageManager.Instance.CurrentWorldID : 1;
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        float stageProgress = StageManager.Instance != null ? StageManager.Instance.GetStageProgress01() : -1f;
        SpawnPacketConfig packet = rhythmTable.Pick(currentWorld, currentStage, _lastPacket, stageProgress);

        if (packet == null) return false;

        SpawnFromPacket(packet, zPos);
        _lastPacket = packet;

        Debug.Log($"[SpawnManager] Packet: {packet.packetId} ({packet.packetType}) @ z={zPos:F0}");
        return true;
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

        GateConfig leftGate  = PickGateFromPool();
        GateConfig rightGate = PickGateFromPoolDistinct(leftGate);

        SpawnGate(leftGate,  new Vector3(-offset, 1.5f, zPos), scale: 1f);
        SpawnGate(rightGate, new Vector3( offset, 1.5f, zPos), scale: 1f);
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

    void SpawnGate(GateConfig data, Vector3 pos, float scale = 1f)
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

        int hp       = archetype.GetHP(targetDps);
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

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(stats);
            enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
        }
    }

    // ── Lane Yardimcisi ───────────────────────────────────────────────────

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
