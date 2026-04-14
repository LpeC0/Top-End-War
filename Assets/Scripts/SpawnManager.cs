using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v14 (BossConfig Patch)
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;

    [Header("Gate Havuzlari")]
    public GatePoolConfig poolStage1To5;
    public GatePoolConfig poolStage6To10;

    [Header("Spawn")]
    public float spawnAhead = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int minEnemies = 2;
    public int maxEnemies = 8;

    float _nextGateZ = 40f;
    float _nextWaveZ = 55f;
    bool _bossSpawned = false;

    int _waveCursor = 0; // Dalga sırasını takip eder

public void ResetForStage()
{
    _nextGateZ = 40f;
    _nextWaveZ = 55f;
    _bossSpawned = false;
    _waveCursor = 0; // Yeni stage başladığında sıfırlanmalı
}

    DifficultyManager.EnemyStats _stats;
    bool _statsReady = false;

    float _overrideNormalHP = 0f;
    float _overrideEliteHP = 0f;
    float _densityMult = 1f;
    bool _hpOverrideActive = false;

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

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        // YENİ: Boss Spawn Mantığı
        if (!_bossSpawned && pz >= bossDistance)
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
        GatePoolConfig pool = GetActiveGatePool();
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageID : 1;
        return pool != null ? pool.PickRandom(stage) : null;
    }

void SpawnEnemyWave(float zPos)
{
    if (TrySpawnConfiguredWave(zPos))
        return;

    // Fallback: eski procedural davranış
    float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
    int cnt = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));

    switch (PickWaveType(prog))
    {
        case 0: NormalWave(zPos, cnt); break;
        case 1: HeavyWave(zPos, cnt);  break;
        case 2: FlankWave(zPos, cnt);  break;
    }
}

    void SpawnNextGateGroup(float zPos)
    {
        SpawnNormalPair(zPos, pity: false);
    }

    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;

    GateConfig leftGate = PickGateFromPool();
    GateConfig rightGate = PickGateFromPoolDistinct(leftGate);

    SpawnGate(leftGate,  new Vector3(-offset, 1.5f, zPos), scale: 1f);
    SpawnGate(rightGate, new Vector3( offset, 1.5f, zPos), scale: 1f);
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
            gate.gateConfig = data;
            gate.Refresh();
        }

        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);
            
        Destroy(obj, 45f);
    }


    int PickWaveType(float p)
    { 
        if (p < 0.25f) return 0;
        float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2;
    }

    void NormalWave(float z, int n)
    {
        int cols = Mathf.Min(n, 4), rows = Mathf.CeilToInt((float)n / cols), pl = 0;
        float gap = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx = -(gap * (cols - 1)) * 0.5f;
        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            { 
                PlaceEnemy(new Vector3(Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f), 1.2f, z + r * 3f));
                pl++; 
            }
    }

    void HeavyWave(float z, int n)
    { 
        for (int i = 0; i < n; i++) PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
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
        if (enemyPrefab != null) obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            var cc = obj.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.tag = "Enemy"; obj.AddComponent<Enemy>();
        }

        var stats = GetEnemyStatsForSpawn();
        obj.GetComponent<Enemy>()?.Initialize(stats);
    }

    public void SetMobHP(int normalHP, int eliteHP, float density = 1f)
    {
        _overrideNormalHP = normalHP;
        _overrideEliteHP = eliteHP;
        _densityMult = density;
        _hpOverrideActive = true;
        Debug.Log($"[SpawnManager] Mob HP override: Normal={normalHP}, Elite={eliteHP}, Density={density}");
    }

    DifficultyManager.EnemyStats GetEnemyStatsForSpawn()
    {
        if (_hpOverrideActive)
        {
            float speed = _stats.Speed;
            int reward = _stats.CPReward;
            return new DifficultyManager.EnemyStats(
                health:   Mathf.RoundToInt(_overrideNormalHP),
                damage:   _stats.Damage,
                speed:    speed,
                cpReward: reward);
        }
        return _stats;
    }

    // --- SPAWN MANAGER YARDIMCI METOTLARI (PATCH 3 - C) ---

bool TrySpawnConfiguredWave(float zPos)
{
    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    if (stage == null || stage.waveSequence == null || stage.waveSequence.Count == 0)
        return false;

    int safeIndex = Mathf.Clamp(_waveCursor, 0, stage.waveSequence.Count - 1);
    WaveConfig wave = stage.waveSequence[safeIndex];
    if (wave == null)
        return false;

    // Boss wave'leri simdilik normal enemy spawn akisina sokma
    if (wave.waveRole == WaveRole.Boss)
        return false;

    SpawnWaveFromConfig(wave, zPos);
    _waveCursor++;
    return true;
}

void SpawnWaveFromConfig(WaveConfig wave, float baseZ)
{
    if (wave == null || wave.groups == null || wave.groups.Count == 0)
        return;

    // Runner mesafesi ile zaman arasındaki köprü:
    // Saniye değerlerini (time) yaklaşık z-offset'e çeviriyoruz.
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

void SpawnArchetypeEnemy(EnemyArchetypeConfig archetype, Vector3 pos)
{
    if (archetype == null) return;

    StageConfig stage = StageManager.Instance != null ? StageManager.Instance.ActiveStage : null;
    float targetDps = stage != null ? stage.targetDps : 100f;

    // HP ve Ödül, Archetype içindeki formüle göre targetDps üzerinden hesaplanır
    int hp = archetype.GetHP(targetDps);
    int cpReward = archetype.GetCPReward(targetDps);

    var stats = new DifficultyManager.EnemyStats(
        health: hp,
        damage: archetype.contactDamage,
        speed: archetype.moveSpeed,
        cpReward: cpReward
    );

    GameObject obj;
    if (enemyPrefab != null)
    {
        obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
    else
    {
        // Prefab yoksa görsel bir placeholder yaratır
        obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.transform.position = pos;
        Destroy(obj.GetComponent<Collider>());
        var cc = obj.AddComponent<CapsuleCollider>();
        cc.isTrigger = true;
        var rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        obj.tag = "Enemy";
        obj.AddComponent<Enemy>();
    }

    Enemy enemy = obj.GetComponent<Enemy>();
    if (enemy != null)
    {
        enemy.Initialize(stats);
        // Zırh ve elitlik bilgisini buraya işler
        enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
    }
}

Vector3 GetLaneBiasedSpawnPos(LaneBias bias, int index, int total, float z)
{
    float x = 0f;
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
            if (total <= 1) x = 0f;
            else
            {
                float t = (float)index / (total - 1);
                x = Mathf.Lerp(left, right, t);
            }
            break;
    }

    x = Mathf.Clamp(x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);
    return new Vector3(x, 1.2f, z);
}
}