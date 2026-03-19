using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v11 (Claude)
///
/// v11: Kapı isimleri ve renkleri netleştirildi.
///   Her kapı tipi için belirgin emoji prefix + açık isim.
///   Renk = tip kategorisi (yeşil=artı, kırmızı=eksi, mor=özel, sarı=risk)
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
    bool       _statsReady  = false;
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
            Mathf.RoundToInt(100f * m), Mathf.RoundToInt(25f * m),
            Mathf.Min(4f + (m - 1f) * 1.4f, 7.5f), Mathf.RoundToInt(15f * m));
    }

    // ── KAPILARIN GÖRSEL ŞEMASI ───────────────────────────────────────────
    //
    //  YEŞİL  = CP artışı veya asker ekleme (pozitif kaynak)
    //  GRİ    = Mekanik asker (zırhlı, yavaş ateş)
    //  MAVİ   = Teknoloji asker (hızlı ateş, kırılgan)
    //  MOR    = Özel (Merge, buff)
    //  PEMBE  = Heal (komutan veya asker)
    //  SARI   = Risk/ödül
    //  TURUNCU= Çarpan bonus
    //  KIRMIZI= Negatif
    //
    //  İSİM FORMATI: "[etki] [değer]"  →  oyuncu bir bakışta anlasın
    //
    void BuildRuntimeGates()
    {
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            // ── CP Artışı (yeşil) ──────────────────────────────────────────
            MakeGate("CP  +80",   GateEffectType.AddCP,  80f, new Color(0.15f,0.80f,0.15f,0.80f), 0.22f),
            MakeGate("CP  +45",   GateEffectType.AddCP,  45f, new Color(0.15f,0.80f,0.15f,0.80f), 0.18f),

            // ── Asker Ekleme ───────────────────────────────────────────────
            // Piyade: parlak yeşil, tüfek savaşçısı
            MakeGate("PIY x2",    GateEffectType.AddSoldier_Piyade,    30f, new Color(0.1f,0.90f,0.3f,0.85f),  0.08f),
            // Mekanik: koyu gri, minigun taşıyıcı
            MakeGate("MEK x2",    GateEffectType.AddSoldier_Mekanik,   30f, new Color(0.55f,0.55f,0.55f,0.85f),0.08f),
            // Teknoloji: parlak mavi, plazma savaşçısı
            MakeGate("TEK x2",    GateEffectType.AddSoldier_Teknoloji, 30f, new Color(0.1f,0.45f,1.0f,0.85f),  0.07f),

            // ── Merge (mor) ───────────────────────────────────────────────
            // Mor + "MERGE" yazısı → oyuncu zamanla öğrenir (3 aynı asker birleşir)
            MakeGate("MERGE",     GateEffectType.Merge, 0f, new Color(0.65f,0.05f,0.95f,0.85f), 0.08f),

            // ── Heal (pembe/kırmızı) ──────────────────────────────────────
            MakeGate("KMT +HP",   GateEffectType.HealCommander, 300f, new Color(1.0f,0.2f,0.55f,0.80f), 0.05f),
            MakeGate("ASK HP+",   GateEffectType.HealSoldiers,  0.5f, new Color(0.5f,1.0f,0.55f,0.80f), 0.04f),

            // ── PathBoost (turuncu — küçük ağırlık) ──────────────────────
            MakeGate("PIY +25%",  GateEffectType.PathBoost_Piyade,   50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.04f),
            MakeGate("MEK +25%",  GateEffectType.PathBoost_Mekanize, 50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.04f),
            MakeGate("TEK +25%",  GateEffectType.PathBoost_Teknoloji,50f, new Color(1.0f,0.5f,0.1f,0.80f), 0.03f),

            // ── Özel ──────────────────────────────────────────────────────
            MakeGate("CP x1.2",   GateEffectType.MultiplyCP,   1.2f, new Color(1.0f,0.75f,0.0f,0.80f), 0.04f),
            MakeGate("RISK !",    GateEffectType.RiskReward,    0f,  new Color(1.0f,0.85f,0.0f,0.80f), 0.04f),
            MakeGate("+MERMI",    GateEffectType.AddBullet,    40f,  new Color(0.5f,0.0f,0.85f,0.80f), 0.04f),

            // ── Negatif (kırmızı) ─────────────────────────────────────────
            MakeGate("CP -60",    GateEffectType.NegativeCP, 60f, new Color(0.9f,0.1f,0.1f,0.80f), 0.05f),
        };
        // Toplam ağırlık: 0.22+0.18+0.08+0.08+0.07+0.08+0.05+0.04+0.04+0.04+0.03+0.04+0.04+0.04+0.05 = 1.08 → normalize edilecek
        // CacheWeights() bölme yapmıyor, WeightedRandom toplam üzerinden hesaplıyor — sorun yok.
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

    GateData PickGate(bool pity)
    {
        GateData[] pool = ActiveGates;
        if (pity)
        {
            var safe = new List<GateData>(); float st = 0f;
            foreach (var g in pool)
                if (g.effectType != GateEffectType.NegativeCP &&
                    g.effectType != GateEffectType.RiskReward)
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

    void Update()
    {
        if (playerTransform == null) { TryFindPlayer(); return; }
        if (!_statsReady) RefreshStats();

        float pz = playerTransform.position.z;

        if (!_bossSpawned && pz >= bossDistance)
        {
            _bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
            return;
        }

        while (pz + spawnAhead >= _nextGateZ) { SpawnGatePair(_nextGateZ); _nextGateZ += gateSpacing; }
        while (pz + spawnAhead >= _nextWaveZ) { SpawnEnemyWave(_nextWaveZ); _nextWaveZ += waveSpacing; }
    }

    void TryFindPlayer()
    { if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform; }

    void SpawnGatePair(float zPos)
    {
        bool  pity   = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;
        float offset = ROAD_HALF_WIDTH * 0.40f;
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
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(2.2f, 3.2f, 1f);
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.95f,1f,1.5f);
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) { gate.gateData = data; gate.Refresh(); }
        Destroy(obj, 45f);
    }

    // ── Düşman Dalgası ────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        float prog = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   cnt  = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, prog));
        if (DifficultyManager.Instance?.PlayerPowerRatio > 1.3f) cnt = Mathf.Min(cnt+1, 9);

        switch (PickWaveType(prog))
        {
            case 0: NormalWave(zPos, cnt); break;
            case 1: HeavyWave(zPos, cnt);  break;
            case 2: FlankWave(zPos, cnt);  break;
        }
        RefreshStats();
    }

    int PickWaveType(float p)
    { if (p < 0.25f) return 0; float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2; }

    void NormalWave(float z, int n)
    {
        int   cols=Mathf.Min(n,4), rows=Mathf.CeilToInt((float)n/cols), pl=0;
        float gap =Mathf.Max((ROAD_HALF_WIDTH*1.6f)/cols,2.2f), sx=-(gap*(cols-1))*0.5f;
        for(int r=0;r<rows&&pl<n;r++)
            for(int c=0;c<cols&&pl<n;c++)
            { PlaceEnemy(new Vector3(Mathf.Clamp(sx+c*gap,-ROAD_HALF_WIDTH+1f,ROAD_HALF_WIDTH-1f),1.2f,z+r*3f)); pl++; }
    }

    void HeavyWave(float z, int n)
    { for(int i=0;i<n;i++) PlaceEnemy(new Vector3(Random.Range(-3f,3f),1.2f,z+i*2.5f)); }

    void FlankWave(float z, int n)
    {
        int h=n/2;
        for(int i=0;i<h;i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH*0.72f+Random.Range(-0.8f,0.8f),1.2f,z+i*2.8f));
        }
        if(n%2==1) PlaceEnemy(new Vector3(0f,1.2f,z));
    }

    void PlaceEnemy(Vector3 pos)
    {
        foreach(Collider c in Physics.OverlapSphere(pos,1.2f))
            if(c.CompareTag("Enemy")){ pos.x+=2.4f; break; }
        pos.x=Mathf.Clamp(pos.x,-ROAD_HALF_WIDTH+0.8f,ROAD_HALF_WIDTH-0.8f);

        GameObject obj;
        if(enemyPrefab!=null) obj=Instantiate(enemyPrefab,pos,Quaternion.identity);
        else
        {
            obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.position=pos;
            Destroy(obj.GetComponent<Collider>());
            var cc=obj.AddComponent<CapsuleCollider>(); cc.isTrigger=true;
            var rb=obj.AddComponent<Rigidbody>(); rb.isKinematic=true;
            obj.tag="Enemy"; obj.AddComponent<Enemy>();
        }
        obj.GetComponent<Enemy>()?.Initialize(_stats);
    }
}