using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Spawn Yoneticisi v12 (Claude)
///
/// v12: "Olay Kapisi" sistemi eklendi.
///
/// OLAY KAPILARI (her 5 normal kapidan sonra 1 tane cıkar):
///
///   DUEL  — 2 kapı, biri iyi biri kotu. Oyuncu rengi gorur ama ne oldugunu
///            tam bilmez. Yuksek risk/odul. Renk ipucu: parlak = iyi,
///            karanlık = kotu (ama her zaman degil — bazen aldatici).
///
///   UCLU  — 3 kapı aynı anda farklı tiplerde. Ortadan gecmek imkansız.
///            Oyuncu hizli karar vermek zorunda.
///
///   TEKLI — 1 kapı, ekstra buyuk odul. "Bu kapiya girersen x2 bonus."
///            Girmezsen de sorun yok ama cazip.
///
/// v11'den geri kalan: kapı isimleri/renkleri netleştirildi.
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

    [Header("Olay Kapilari")]
    [Tooltip("Kac normal kapidan sonra bir olay kapisi cikar")]
    public int eventGateEvery = 5;

    float _nextGateZ   = 40f;
    float _nextWaveZ   = 55f;
    bool  _bossSpawned = false;
    int   _gateCount   = 0;  // kac normal kapı cıktı

    DifficultyManager.EnemyStats _stats;
    bool       _statsReady  = false;
    GateData[] _runtimeGates;
    float      _totalWeight = 0f;

    // Olay tipi
    enum EventType { Duel, Triple, Single }

    // ─────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────
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
    { if (PlayerStats.Instance != null) playerTransform = PlayerStats.Instance.transform; }

    // ── Sıradaki kapı grubunu belirle ────────────────────────────────────
    void SpawnNextGateGroup(float zPos)
    {
        _gateCount++;

        // Her N kapıda bir olay kapısı
        bool isEvent = (_gateCount % eventGateEvery == 0);

        // Boss yakınında sadece iyi kapılar
        bool pity = DifficultyManager.Instance?.IsInPityZone(bossDistance) ?? false;

        if (isEvent && !pity)
            SpawnEventGate(zPos);
        else
            SpawnNormalPair(zPos, pity);
    }

    // ── Normal çift kapı ─────────────────────────────────────────────────
    void SpawnNormalPair(float zPos, bool pity)
    {
        float offset = ROAD_HALF_WIDTH * 0.40f;
        SpawnGate(PickGate(pity), new Vector3(-offset, 1.5f, zPos), scale: 1f);
        SpawnGate(PickGate(pity), new Vector3( offset, 1.5f, zPos), scale: 1f);
    }

    // ── Olay kapısı ───────────────────────────────────────────────────────
    void SpawnEventGate(float zPos)
    {
        // Olayları eşit şansla seç (boss'a yaklaştıkça Duel azalır)
        float progress = playerTransform.position.z / bossDistance;

        // 0-30%: Tekli ağırlıklı (tanıtım)
        // 30-70%: Duel ağırlıklı (gerilim)
        // 70-100%: Üçlü ağırlıklı (kaos)
        EventType ev;
        float r = Random.value;
        if (progress < 0.30f)
            ev = r < 0.50f ? EventType.Single : r < 0.80f ? EventType.Triple : EventType.Duel;
        else if (progress < 0.70f)
            ev = r < 0.45f ? EventType.Duel : r < 0.75f ? EventType.Triple : EventType.Single;
        else
            ev = r < 0.45f ? EventType.Triple : r < 0.75f ? EventType.Duel : EventType.Single;

        switch (ev)
        {
            case EventType.Single: SpawnSingleEvent(zPos);  break;
            case EventType.Duel:   SpawnDuelEvent(zPos);    break;
            case EventType.Triple: SpawnTripleEvent(zPos);  break;
        }
    }

    // ── TEKLİ olay: Büyük ödüllü 1 kapı ─────────────────────────────────
    void SpawnSingleEvent(float zPos)
    {
        // Büyük bir pozitif kapı, ortada, büyük ölçekte
        GateData big = MakeBigGate();
        SpawnGate(big, new Vector3(0f, 1.8f, zPos), scale: 1.6f);

        // Label için GateFeedback label'ı override et
        // (Gate.cs'de ApplyVisuals çalışır, metni kapı gösterir)
        Debug.Log($"[Spawn] OLAY: TEKLİ — {big.gateText}");
    }

    // ── DUEL olay: 1 iyi 1 kötü, hangisi hangisi belli değil ─────────────
    void SpawnDuelEvent(float zPos)
    {
        GateData good = PickPositiveGate();
        GateData bad  = PickNegativeGate();

        // %50 şansla yer değiştir — oyuncu rengi görür ama pozisyon yanıltıcı
        bool swap = Random.value > 0.5f;
        float leftX  = -(ROAD_HALF_WIDTH * 0.42f);
        float rightX =   ROAD_HALF_WIDTH * 0.42f;

        SpawnGate(swap ? bad : good,  new Vector3(leftX,  1.5f, zPos), scale: 1.2f);
        SpawnGate(swap ? good : bad,  new Vector3(rightX, 1.5f, zPos), scale: 1.2f);

        Debug.Log($"[Spawn] OLAY: DUEL — {good.gateText} vs {bad.gateText}");
    }

    // ── ÜÇLÜ olay: 3 farklı kapı — Z ofsetli, iç içe geçmez ─────────────
    void SpawnTripleEvent(float zPos)
    {
        // Yatay aralık: sol/sag biraz disarida, ortadaki öne çıkmış
        // Her kapı 0.7 scale → daha ince, aralara sığar
        // Z offset: orta öne (oyuncu önce ortayı görsün), yanlar biraz geride
        float spacing = ROAD_HALF_WIDTH * 0.60f; // 4.8 birim aralik

        GateData left   = PickGate(false);
        GateData center = PickGate(false);
        GateData right  = PickGate(false);

        SpawnGate(left,   new Vector3(-spacing, 1.5f, zPos + 4f), scale: 0.75f); // geride
        SpawnGate(center, new Vector3(0f,        1.8f, zPos),      scale: 1.0f);  // önde, büyük
        SpawnGate(right,  new Vector3( spacing,  1.5f, zPos + 4f), scale: 0.75f); // geride

        Debug.Log($"[Spawn] OLAY: ÜÇLÜ — {left.gateText} | {center.gateText} | {right.gateText}");
    }

    // ── Gate data seçiciler ───────────────────────────────────────────────
    GateData PickPositiveGate()
    {
        // Sadece pozitif tipler
        var positives = new List<GateData>();
        float w = 0f;
        foreach (var g in ActiveGates)
        {
            if (g.effectType == GateEffectType.AddCP          ||
                g.effectType == GateEffectType.AddSoldier_Piyade   ||
                g.effectType == GateEffectType.AddSoldier_Mekanik  ||
                g.effectType == GateEffectType.AddSoldier_Teknoloji||
                g.effectType == GateEffectType.HealCommander  ||
                g.effectType == GateEffectType.MultiplyCP)
            { positives.Add(g); w += g.spawnWeight; }
        }
        return positives.Count > 0 ? WeightedRandom(positives.ToArray(), w) : ActiveGates[0];
    }

    GateData PickNegativeGate()
    {
        var negatives = new List<GateData>();
        float w = 0f;
        foreach (var g in ActiveGates)
        {
            if (g.effectType == GateEffectType.NegativeCP ||
                g.effectType == GateEffectType.RiskReward)
            { negatives.Add(g); w += g.spawnWeight; }
        }
        if (negatives.Count > 0) return WeightedRandom(negatives.ToArray(), w);
        // Fallback: negatif yoksa düz NegativeCP üret
        return MakeGate("CP -80", GateEffectType.NegativeCP, 80f,
            new Color(0.9f,0.1f,0.1f,0.85f), 0.1f);
    }

    GateData MakeBigGate()
    {
        // Mevcut oyunun CP'sine göre büyük bonus
        float z     = playerTransform.position.z;
        float scale = 1f + z / 2000f;
        int   bonus = Mathf.RoundToInt(150f * scale);

        GateData d = ScriptableObject.CreateInstance<GateData>();
        d.gateText    = $"CP +{bonus}";
        d.effectType  = GateEffectType.AddCP;
        d.effectValue = bonus;
        d.gateColor   = new Color(1f, 0.85f, 0f, 0.9f); // altın sarısı
        d.spawnWeight = 0.01f; // nadiren normal listede olsun
        return d;
    }

    // ── Kapı spawn ────────────────────────────────────────────────────────
    void SpawnGate(GateData data, Vector3 pos, float scale = 1f)
    {
        if (data == null) return;
        GameObject obj;

        if (gatePrefab != null)
            obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.position   = pos;
            Destroy(obj.GetComponent<MeshCollider>());
            var bc = obj.AddComponent<BoxCollider>(); bc.isTrigger = true;
            bc.size = new Vector3(0.95f, 1f, 1.5f);
            var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
            obj.AddComponent<Gate>();
        }

        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) { gate.gateData = data; gate.Refresh(); }

        // Scale'i Refresh'ten sonra uygula — collider doğru hesaplansın
        if (scale != 1f)
            obj.transform.localScale = new Vector3(scale, scale, 1f);

        Destroy(obj, 45f);
    }

    // ── Gate data havuzu ─────────────────────────────────────────────────
    void BuildRuntimeGates()
    {
        if (gateDataList != null && gateDataList.Length > 0) return;

        _runtimeGates = new GateData[]
        {
            MakeGate("CP  +80",   GateEffectType.AddCP,               80f,  new Color(0.15f,0.80f,0.15f,0.80f), 0.22f),
            MakeGate("CP  +45",   GateEffectType.AddCP,               45f,  new Color(0.15f,0.80f,0.15f,0.80f), 0.18f),
            MakeGate("PIY x2",    GateEffectType.AddSoldier_Piyade,   30f,  new Color(0.1f, 0.90f,0.3f, 0.85f), 0.08f),
            MakeGate("MEK x2",    GateEffectType.AddSoldier_Mekanik,  30f,  new Color(0.55f,0.55f,0.55f,0.85f), 0.08f),
            MakeGate("TEK x2",    GateEffectType.AddSoldier_Teknoloji,30f,  new Color(0.1f, 0.45f,1.0f, 0.85f), 0.07f),
            MakeGate("MERGE",     GateEffectType.Merge,               0f,   new Color(0.65f,0.05f,0.95f,0.85f), 0.08f),
            MakeGate("KMT +HP",   GateEffectType.HealCommander,       300f, new Color(1.0f, 0.2f, 0.55f,0.80f), 0.05f),
            MakeGate("ASK HP+",   GateEffectType.HealSoldiers,        0.5f, new Color(0.5f, 1.0f, 0.55f,0.80f), 0.04f),
            MakeGate("PIY +25%",  GateEffectType.PathBoost_Piyade,    50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.04f),
            MakeGate("MEK +25%",  GateEffectType.PathBoost_Mekanize,  50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.04f),
            MakeGate("TEK +25%",  GateEffectType.PathBoost_Teknoloji, 50f,  new Color(1.0f, 0.5f, 0.1f, 0.80f), 0.03f),
            MakeGate("CP x1.2",   GateEffectType.MultiplyCP,          1.2f, new Color(1.0f, 0.75f,0.0f, 0.80f), 0.04f),
            MakeGate("RISK !",    GateEffectType.RiskReward,          0f,   new Color(1.0f, 0.85f,0.0f, 0.80f), 0.04f),
            MakeGate("+MERMI",    GateEffectType.AddBullet,           40f,  new Color(0.5f, 0.0f, 0.85f,0.80f), 0.04f),
            MakeGate("CP -60",    GateEffectType.NegativeCP,          60f,  new Color(0.9f, 0.1f, 0.1f, 0.80f), 0.05f),
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

    // ── Düşman dalgası ───────────────────────────────────────────────────
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
    { if (p < 0.25f) return 0; float r = Random.value; return r < 0.5f ? 0 : r < 0.75f ? 1 : 2; }

    void NormalWave(float z, int n)
    {
        int   cols = Mathf.Min(n, 4), rows = Mathf.CeilToInt((float)n / cols), pl = 0;
        float gap  = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / cols, 2.2f);
        float sx   = -(gap * (cols - 1)) * 0.5f;
        for (int r = 0; r < rows && pl < n; r++)
            for (int c = 0; c < cols && pl < n; c++)
            { PlaceEnemy(new Vector3(Mathf.Clamp(sx + c * gap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f), 1.2f, z + r * 3f)); pl++; }
    }

    void HeavyWave(float z, int n)
    { for (int i = 0; i < n; i++) PlaceEnemy(new Vector3(Random.Range(-3f, 3f), 1.2f, z + i * 2.5f)); }

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
        obj.GetComponent<Enemy>()?.Initialize(_stats);
    }
}