using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v8 (Claude)
///
/// DEGİSİKLİKLER:
///   Gate boyutu kucultuldu: 3x4 → 2x3 (hareket alani icinde kalir)
///   Gate offset duzeltildi: 0.45 → 0.40 (cakisma yok)
///   Gate degerleri mesafeye gore scale edilir (0m=1x, 1200m=2.5x)
///   AddBullet kapisi eklendi (agirlik 0.12)
///   MultiplyCP kaldirildi (x2 denge bozuyordu) — sadece x1.3 nadir
///
/// Yeni agirliklar (toplam = 1.00):
///   AddCP_large  0.20 | AddCP_small  0.23
///   AddCP_huge   0.12 | PathBoost    0.12
///   AddBullet    0.12 | Merge        0.07
///   RiskReward   0.05 | NegativeCP   0.04
///   MultiplyCP   0.05 (x1.3 sürpriz)
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 30f;

    [Header("Boss")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    float _nextGateZ   = 40f;
    float _nextWaveZ   = 55f;
    bool  _bossSpawned = false;

    DifficultyManager.EnemyStats _stats;
    bool      _statsReady = false;
    GateData[] _runtimeGates;
    float      _totalWeight = 0f;

    void Start()
    {
        if (playerTransform == null && PlayerStats.Instance != null)
            playerTransform = PlayerStats.Instance.transform;

        BuildRuntimeGates();
        CacheWeights();
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
            Mathf.RoundToInt(100f * m),
            Mathf.RoundToInt(25f  * m),
            Mathf.Min(4f + (m-1f) * 1.4f, 7.5f),
            Mathf.RoundToInt(15f  * m));
    }

    // ── Runtime Gate Olusturma ─────────────────────────────────────────────
    // AGIRLIKLAR NEDEN DEGISTI:
    //   MultiplyCP x2: 5 kapıda T5'e ulaşılıyordu → kaldırıldı
    //   AddBullet: "ordu hissi" + denge için eklendi
    //   Toplam: 0.20+0.23+0.12+0.12+0.12+0.07+0.05+0.05+0.04 = 1.00
    void BuildRuntimeGates()
    {
        if (gateDataList != null && gateDataList.Length > 0) return;

        // effectValue = base deger (mesafeye gore scale edilir)
        _runtimeGates = new GateData[]
        {
            // AddCP - %40'ı karşılar: large+small+huge
            MakeGate("+80",        GateEffectType.AddCP,             80f,  new Color(0.2f, 0.85f, 0.2f, 0.7f), 0.20f),
            MakeGate("+50",        GateEffectType.AddCP,             50f,  new Color(0.2f, 0.85f, 0.2f, 0.7f), 0.23f),
            MakeGate("+200",       GateEffectType.AddCP,            200f,  new Color(0.1f, 0.95f, 0.1f, 0.7f), 0.12f), // Nadir büyük
            // PathBoost - %12
            MakeGate("+Piyade",    GateEffectType.PathBoost_Piyade,  70f,  new Color(1.0f, 0.5f,  0.0f, 0.7f), 0.04f),
            MakeGate("+Mekanize",  GateEffectType.PathBoost_Mekanize,70f,  new Color(1.0f, 0.5f,  0.0f, 0.7f), 0.04f),
            MakeGate("+Teknoloji", GateEffectType.PathBoost_Teknoloji,70f, new Color(1.0f, 0.5f,  0.0f, 0.7f), 0.04f),
            // AddBullet - %12 (YENİ)
            MakeGate("+MERMI",     GateEffectType.AddBullet,         70f,  new Color(0.5f, 0.0f,  0.8f, 0.7f), 0.12f),
            // Merge - %7
            MakeGate("MERGE",      GateEffectType.Merge,              0f,  new Color(0.6f, 0.1f,  0.9f, 0.7f), 0.07f),
            // RiskReward - %5
            MakeGate("RISK",       GateEffectType.RiskReward,         0f,  new Color(1.0f, 0.85f, 0.0f, 0.7f), 0.05f),
            // MultiplyCP - %5 (nadir, sadece x1.3)
            MakeGate("x1.3",       GateEffectType.MultiplyCP,        1.3f, new Color(0.1f, 0.5f,  1.0f, 0.7f), 0.05f),
            // NegativeCP - %4
            MakeGate("-CP",        GateEffectType.NegativeCP,        80f,  new Color(0.9f, 0.1f,  0.1f, 0.7f), 0.04f),
        };
    }

    GateData MakeGate(string text, GateEffectType type, float val, Color color, float weight)
    {
        GateData d = ScriptableObject.CreateInstance<GateData>();
        d.gateText = text; d.effectType = type; d.effectValue = val;
        d.gateColor = color; d.spawnWeight = weight;
        return d;
    }

    GateData[] ActiveGates =>
        (gateDataList != null && gateDataList.Length > 0) ? gateDataList : _runtimeGates;

    void CacheWeights()
    {
        _totalWeight = 0f;
        foreach (var g in ActiveGates) _totalWeight += g.spawnWeight;
    }

    // ── AGIRLIKLI SECIM ────────────────────────────────────────────────────
    GateData PickGate(bool pity)
    {
        GateData[] pool = ActiveGates;

        if (pity)
        {
            var safe = new List<GateData>(pool.Length); float st = 0f;
            foreach (var g in pool)
                if (g.effectType != GateEffectType.NegativeCP && g.effectType != GateEffectType.RiskReward)
                { safe.Add(g); st += g.spawnWeight; }
            if (safe.Count > 0) return WeightedRandom(safe.ToArray(), st);
        }

        return WeightedRandom(pool, _totalWeight);
    }

    GateData WeightedRandom(GateData[] pool, float total)
    {
        if (pool.Length == 0 || total <= 0f) return pool[0];
        float r = Random.value * total, cum = 0f;
        for (int i = 0; i < pool.Length; i++)
        { cum += pool[i].spawnWeight; if (r <= cum) return pool[i]; }
        return pool[pool.Length - 1];
    }

    // ── Update ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!_bossSpawned && pz >= bossDistance)
        {
            _bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
            Debug.Log("[SpawnManager] BOSS! Z=" + pz);
            return;
        }

        while (pz + spawnAhead >= _nextGateZ) { SpawnGatePair(_nextGateZ); _nextGateZ += gateSpacing; }
        while (pz + spawnAhead >= _nextWaveZ) { SpawnEnemyWave(_nextWaveZ); _nextWaveZ += waveSpacing; }
    }

    void TryFindPlayer()
    { if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform; }

    // ── Gate Spawn ─────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        bool pity = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;

        // DUZELTME: offset 0.45 → 0.40 (yol iyi kaplar, kucuk skale)
        float offset = ROAD_HALF_WIDTH * 0.40f; // = 3.2f
        SpawnGate(PickGate(pity), new Vector3(-offset, 1.5f, zPos));
        SpawnGate(PickGate(pity), new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        if (data == null) return;

        GameObject obj;
        if (gatePrefab != null)
        {
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        }
        else
        {
            // DUZELTME: 3x4 → 2x3 (hareket alani icinde kalir)
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(2f, 3f, 1f); // KUCULTULDU
            Destroy(obj.GetComponent<MeshCollider>());

            BoxCollider bc = obj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size      = new Vector3(0.95f, 1.0f, 1.5f); // Z derinligi arttirildi

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            Gate g = obj.AddComponent<Gate>();
            g.panelRenderer = obj.GetComponent<Renderer>();
        }

        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) { gate.gateData = data; gate.Refresh(); }
        Destroy(obj, 45f);
    }

    // ── Düşman Dalgası ─────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   cnt  = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f) cnt = Mathf.Min(cnt + 1, 9);

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }
        RefreshStats();
    }

    int PickWaveType(float p)
    {
        if (p < 0.25f) return 0;
        float r = Random.value;
        return r < 0.5f ? 0 : r < 0.75f ? 1 : 2;
    }

    void NormalWave(float z, int n)
    {
        int   cols = Mathf.Min(n, 4), rows = Mathf.CeilToInt((float)n/cols), pl = 0;
        float gap  = Mathf.Max((ROAD_HALF_WIDTH*1.6f)/cols, 2.2f);
        float sx   = -(gap*(cols-1))*0.5f;
        for (int r=0; r<rows&&pl<n; r++)
            for (int c=0; c<cols&&pl<n; c++)
            { PlaceEnemy(new Vector3(Mathf.Clamp(sx+c*gap,-ROAD_HALF_WIDTH+1f,ROAD_HALF_WIDTH-1f),1.2f,z+r*3f)); pl++; }
    }

    void HeavyWave(float z, int n)
    { for(int i=0;i<n;i++) PlaceEnemy(new Vector3(Random.Range(-3f,3f),1.2f,z+i*2.5f)); }

    void FlankWave(float z, int n)
    {
        int h = n/2;
        for(int i=0;i<h;i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
        }
        if(n%2==1) PlaceEnemy(new Vector3(0f,1.2f,z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        foreach (Collider col in Physics.OverlapSphere(pos, 1.2f))
            if (col.CompareTag("Enemy")) { pos.x += 2.4f; break; }
        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH+0.8f, ROAD_HALF_WIDTH-0.8f);

        GameObject obj;
        if (enemyPrefab != null) obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
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
        obj.GetComponent<Enemy>()?.Initialize(_stats);
    }
}