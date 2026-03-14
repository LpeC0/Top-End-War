using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v5 (Claude)
/// - DifficultyManager entegrasyonu: dusman stat'lari dinamik
/// - Pity Timer: Boss oncesi 200 birimde kotu kapi yok (Gemini onerisi)
/// - Dalga Tipleri: Normal / Agir / Kanat (Claude onerisi)
/// - Ic ice gecme kontrolu: OverlapSphere
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f;

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;     // Inspector'dan GateData asset'lerini bagla

    [Header("Spawn Mesafeleri")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 32f;

    [Header("Zorluk")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    float nextGateZ   = 40f;
    float nextWaveZ   = 60f;
    bool  bossSpawned = false;

    // DifficultyManager verileri
    DifficultyManager.EnemyStats currentStats;

    void Start()
    {
        // Ilk stat hesapla
        currentStats = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetScaledEnemyStats()
            : new DifficultyManager.EnemyStats(100, 25, 4.5f, 15);

        GameEvents.OnDifficultyChanged += OnDifficultyChanged;
    }

    void OnDestroy() => GameEvents.OnDifficultyChanged -= OnDifficultyChanged;

    void OnDifficultyChanged(float mult, float ratio)
    {
        if (DifficultyManager.Instance != null)
            currentStats = DifficultyManager.Instance.GetScaledEnemyStats();
    }

    void Update()
    {
        if (playerTransform == null) return;
        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            GameEvents.OnBossEncountered?.Invoke();
            Debug.Log("[SpawnManager] BOSS! Z=" + pz);
            return;
        }

        while (pz + spawnAhead >= nextGateZ) { SpawnGatePair(nextGateZ); nextGateZ += gateSpacing; }
        while (pz + spawnAhead >= nextWaveZ) { SpawnEnemyWave(nextWaveZ); nextWaveZ += waveSpacing; }
    }

    // ── Kapi Cifti ────────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        // PITY TIMER: Boss oncesinde kotu kapi cikmasin (Gemini + Claude)
        bool inPityZone = DifficultyManager.Instance != null
            && DifficultyManager.Instance.IsInPityZone(bossDistance);

        GateData left  = PickGateData(inPityZone);
        GateData right = PickGateData(inPityZone);

        float offset = ROAD_HALF_WIDTH * 0.45f;
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    /// <summary>
    /// Pity zone'da NegativeCP ve RiskReward kapilari listeden cikar,
    /// kalan listeden rastgele sec.
    /// </summary>
    GateData PickGateData(bool pityZone)
    {
        if (!pityZone)
            return gateDataList[Random.Range(0, gateDataList.Length)];

        // Pity zone: sadece pozitif kapılar
        var safe = new System.Collections.Generic.List<GateData>(gateDataList.Length);
        foreach (var g in gateDataList)
            if (g.effectType != GateEffectType.NegativeCP &&
                g.effectType != GateEffectType.RiskReward)
                safe.Add(g);

        if (safe.Count == 0) return gateDataList[0];
        return safe[Random.Range(0, safe.Count)];
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 45f);
    }

    // ── Dusman Dalgasi ────────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        // Dalga tipini belirle (Claude): 0=Normal, 1=Agir, 2=Kanat
        int   waveType = PickWaveType();
        int   count    = CalculateEnemyCount(waveType);

        switch (waveType)
        {
            case 0: SpawnNormalWave(zPos, count);   break; // Grid
            case 1: SpawnHeavyWave(zPos, count);    break; // Merkez yogunlasma
            case 2: SpawnFlankingWave(zPos, count); break; // Iki yandan sarma
        }
    }

    int PickWaveType()
    {
        float progress = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        // Erken oyunda sadece normal, ilerledikce cesitlenir
        float r = Random.value;
        if (progress < 0.3f) return 0;           // Hep normal
        if (r < 0.5f)        return 0;           // %50 normal
        if (r < 0.75f)       return 1;           // %25 agir
        return 2;                                 // %25 kanat
    }

    int CalculateEnemyCount(int waveType)
    {
        float progress = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   base_    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));

        // Oyuncu cok gucluyse daha fazla (DDA)
        if (DifficultyManager.Instance != null && DifficultyManager.Instance.PlayerPowerRatio > 1.3f)
            base_ = Mathf.Min(base_ + 1, maxEnemies);

        // Agir dalga: daha az ama guclu
        if (waveType == 1) base_ = Mathf.Max(2, base_ - 2);

        return base_;
    }

    // 0: Normal — standart grid
    void SpawnNormalWave(float zPos, int count)
    {
        int   cols    = Mathf.Min(count, 4);
        int   rows    = Mathf.CeilToInt((float)count / cols);
        float colGap  = Mathf.Max((ROAD_HALF_WIDTH * 1.6f) / Mathf.Max(cols, 1), 2.2f);
        float startX  = -(colGap * (cols - 1)) * 0.5f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = Mathf.Clamp(startX + c * colGap, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f);
                PlaceEnemy(new Vector3(x, 1.2f, zPos + r * 3f));
                spawned++;
            }
    }

    // 1: Agir — merkezde yogunlasma
    void SpawnHeavyWave(float zPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-ROAD_HALF_WIDTH * 0.35f, ROAD_HALF_WIDTH * 0.35f);
            float z = zPos + i * 2.5f;
            PlaceEnemy(new Vector3(x, 1.2f, z));
        }
    }

    // 2: Kanat — iki yandan gelir
    void SpawnFlankingWave(float zPos, int count)
    {
        int half = count / 2;
        for (int i = 0; i < half; i++)
        {
            PlaceEnemy(new Vector3(-ROAD_HALF_WIDTH * 0.7f + Random.Range(-1f, 1f), 1.2f, zPos + i * 2.8f));
            PlaceEnemy(new Vector3( ROAD_HALF_WIDTH * 0.7f + Random.Range(-1f, 1f), 1.2f, zPos + i * 2.8f));
        }
        if (count % 2 == 1) PlaceEnemy(new Vector3(0f, 1.2f, zPos));
    }

    void PlaceEnemy(Vector3 pos)
    {
        // Ic ice gecme kontrolu
        Collider[] nearby = Physics.OverlapSphere(pos, 1.4f);
        foreach (Collider col in nearby)
            if (col.CompareTag("Enemy")) { pos.x += 2.2f; break; }

        pos.x = Mathf.Clamp(pos.x, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f);
        GameObject obj = Instantiate(enemyPrefab, pos, Quaternion.identity);

        // DDA stat uygula
        Enemy e = obj.GetComponent<Enemy>();
        if (e != null) e.Initialize(currentStats);
    }
}