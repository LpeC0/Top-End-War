using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v6 (Claude)
///
/// TAMAMEN BAGIMSIZ calisir:
///   - DifficultyManager yoksa mesafe bazli kendi hesabini yapar
///   - GateDataList bossa kendi ScriptableObject'lerini olusturur
///   - EnemyPrefab bossa primitive capsule kullanir
///   - GatePrefab bossa primitive quad kullanir
///
/// ZORLUK (standalone):
///   0-300m:    2-3 dusman, yavaş
///   300-800m:  4-6 dusman, orta
///   800-1200m: 6-8 dusman, hizli
///   1200m+:    Boss
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn Mesafeleri")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;

    float nextGateZ    = 40f;
    float nextWaveZ    = 55f;
    bool  bossSpawned  = false;

    // DDA
    DifficultyManager.EnemyStats _currentStats;
    bool _statsReady = false;

    // Runtime gate data (GateDataList bossa bunlar kullanilir)
    GateData[] _runtimeGates;

    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        BuildRuntimeGates();
        RefreshStats();
        GameEvents.OnDifficultyChanged += (m, r) => RefreshStats();
    }

    void OnDestroy()
    {
        GameEvents.OnDifficultyChanged -= (m, r) => RefreshStats();
    }

    void RefreshStats()
    {
        _currentStats = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetScaledEnemyStats()
            : FallbackStats();
        _statsReady = true;
    }

    /// <summary>DifficultyManager yoksa mesafeye gore hesapla.</summary>
    DifficultyManager.EnemyStats FallbackStats()
    {
        float z    = playerTransform != null ? playerTransform.position.z : 0f;
        float mult = 1f + Mathf.Pow(z / 1000f, 1.3f);
        return new DifficultyManager.EnemyStats(
            Mathf.RoundToInt(100f * mult),
            Mathf.RoundToInt(25f  * mult),
            Mathf.Min(4f + (mult - 1f) * 1.4f, 7.5f),
            Mathf.RoundToInt(15f  * mult));
    }

    // ── Kapi verilerini runtime olustur (elle yapmana gerek yok) ─────────────
    void BuildRuntimeGates()
    {
        // Inspector'dan baglandiysa kullan
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            MakeGate("+60",       GateEffectType.AddCP,              60f,  new Color(0.2f,0.85f,0.2f,0.7f)),
            MakeGate("+100",      GateEffectType.AddCP,              100f, new Color(0.2f,0.85f,0.2f,0.7f)),
            MakeGate("x2",        GateEffectType.MultiplyCP,         2f,   new Color(0.1f,0.4f,1.0f,0.7f)),
            MakeGate("x1.5",      GateEffectType.MultiplyCP,         1.5f, new Color(0.1f,0.4f,1.0f,0.7f)),
            MakeGate("MERGE",     GateEffectType.Merge,              0f,   new Color(0.6f,0.1f,0.9f,0.7f)),
            MakeGate("+Piyade",   GateEffectType.PathBoost_Piyade,   60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("+Mekanize", GateEffectType.PathBoost_Mekanize, 60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("+Teknoloji",GateEffectType.PathBoost_Teknoloji,60f,  new Color(1.0f,0.5f,0.0f,0.7f)),
            MakeGate("RISK",      GateEffectType.RiskReward,         0f,   new Color(1.0f,0.85f,0.0f,0.7f)),
            MakeGate("-80",       GateEffectType.NegativeCP,         80f,  new Color(0.9f,0.1f,0.1f,0.7f)),
        };
        Debug.Log("[SpawnManager] Runtime gate verileri olusturuldu.");
    }

    GateData MakeGate(string text, GateEffectType type, float value, Color color)
    {
        GateData d      = ScriptableObject.CreateInstance<GateData>();
        d.gateText      = text;
        d.effectType    = type;
        d.effectValue   = value;
        d.gateColor     = color;
        return d;
    }

    GateData[] ActiveGates => (gateDataList != null && gateDataList.Length > 0)
        ? gateDataList : _runtimeGates;

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
            Debug.Log("[SpawnManager] BOSS ZAMANI!");
            return;
        }

        while (pz + spawnAhead >= nextGateZ)
        {
            SpawnGatePair(nextGateZ);
            nextGateZ += gateSpacing;
        }
        while (pz + spawnAhead >= nextWaveZ)
        {
            SpawnEnemyWave(nextWaveZ);
            nextWaveZ += waveSpacing;
        }
    }

    void TryFindPlayer()
    {
        if (PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;
    }

    // ── Kapi ─────────────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        bool pity = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;

        GateData left  = PickGate(pity);
        GateData right = PickGate(pity);

        float offset = ROAD_HALF_WIDTH * 0.45f;
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    GateData PickGate(bool pity)
    {
        GateData[] pool = ActiveGates;
        if (!pity) return pool[Random.Range(0, pool.Length)];

        // Pity: sadece pozitif
        var safe = new List<GateData>(pool.Length);
        foreach (var g in pool)
            if (g.effectType != GateEffectType.NegativeCP &&
                g.effectType != GateEffectType.RiskReward)
                safe.Add(g);
        return safe.Count > 0 ? safe[Random.Range(0, safe.Count)] : pool[0];
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj;

        if (gatePrefab != null)
        {
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        }
        else
        {
            // GatePrefab yoksa quad olustur
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(3f, 4f, 1f);
            Destroy(obj.GetComponent<Collider>());

            // Collider ekle
            BoxCollider bc  = obj.AddComponent<BoxCollider>();
            bc.isTrigger    = true;
            bc.size         = new Vector3(1f, 1.1f, 1.2f);

            // Rigidbody
            Rigidbody rb    = obj.AddComponent<Rigidbody>();
            rb.isKinematic  = true;

            // Gate script
            Gate gate       = obj.AddComponent<Gate>();
            gate.panelRenderer = obj.GetComponent<Renderer>();
            gate.gateData   = data;
        }

        // Gate varsa data ata
        Gate g2 = obj.GetComponent<Gate>();
        if (g2 != null)
        {
            g2.gateData = data;
            g2.Refresh();
        }

        Destroy(obj, 40f);
    }

    // ── Dusman Dalgasi ────────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float pz       = playerTransform.position.z;
        float progress = Mathf.Clamp01(pz / bossDistance);

        // Dusman sayisi: 2'den 8'e lineer + oyuncu gucluyse +1
        int count = Mathf.RoundToInt(Mathf.Lerp(2f, 8f, progress));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f)
            count = Mathf.Min(count + 1, 9);

        // Dalga tipi
        int waveType = PickWaveType(progress);
        switch (waveType)
        {
            case 0: NormalWave(zPos, count);  break;
            case 1: HeavyWave(zPos, count);   break;
            case 2: FlankWave(zPos, count);   break;
        }

        // Stats'i her dalga guncelle (DDA icin)
        RefreshStats();
    }

    int PickWaveType(float progress)
    {
        if (progress < 0.25f) return 0;
        float r = Random.value;
        if (r < 0.5f) return 0;
        if (r < 0.75f) return 1;
        return 2;
    }

    void NormalWave(float z, int count)
    {
        int   cols   = Mathf.Min(count, 4);
        int   rows   = Mathf.CeilToInt((float)count / cols);
        float colGap = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float startX = -(colGap * (cols - 1)) * 0.5f;
        int   placed = 0;
        for (int r = 0; r < rows && placed < count; r++)
            for (int c = 0; c < cols && placed < count; c++)
            {
                PlaceEnemy(new Vector3(
                    Mathf.Clamp(startX + c * colGap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                    1.2f, z + r * 3f));
                placed++;
            }
    }

    void HeavyWave(float z, int count)
    {
        for (int i = 0; i < count; i++)
            PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f));
    }

    void FlankWave(float z, int count)
    {
        int half = count / 2;
        for (int i = 0; i < half; i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH * 0.72f + Random.Range(-0.8f, 0.8f), 1.2f, z + i * 2.8f));
        }
        if (count % 2 == 1) PlaceEnemy(new Vector3(0f, 1.2f, z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        // Cakisma onle
        Collider[] nearby = Physics.OverlapSphere(pos, 1.2f);
        foreach (Collider col in nearby)
            if (col.CompareTag("Enemy")) { pos.x += 2.4f; break; }
        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH + 0.8f, ROAD_HALF_WIDTH - 0.8f);

        GameObject obj;
        if (enemyPrefab != null)
        {
            obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
        else
        {
            // EnemyPrefab yoksa capsule olustur
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position = pos;
            Destroy(obj.GetComponent<Collider>());
            CapsuleCollider cc = obj.AddComponent<CapsuleCollider>();
            cc.isTrigger = true;
            Rigidbody rb  = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            obj.tag = "Enemy";
            obj.AddComponent<Enemy>();
            obj.AddComponent<EnemyHealthBar>();
        }

        obj.GetComponent<Enemy>()?.Initialize(_currentStats);
    }
}