using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v3 (Claude)
///
/// BOLUM UZUNLUGU (Sivas):
///   0–300:   Kolaylik, az dushman, cok kapi
///   300–800: Orta, 3-5 dushman/dalga
///   800–1200: Zor, 5-8 dushman/dalga, hizli
///   1200+:   Boss giris sekansı (BossManager tetiklenir)
///
/// xLimit = 5.5 (PlayerController ile AYNI olmali)
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 5.5f; // xLimit ile ayni

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn")]
    public float spawnAhead    = 65f;
    public float gateSpacing   = 40f;
    public float waveSpacing   = 32f;

    [Header("Zorluk")]
    public float bossDistance  = 1200f; // Bu mesafede boss cikacak
    public int   minEnemies    = 2;
    public int   maxEnemies    = 8;

    float nextGateZ = 40f;
    float nextWaveZ = 60f;   // Ilk dalga kapıdan bağımsız

    bool bossSpawned = false;

    void Update()
    {
        if (playerTransform == null) return;

        float pz = playerTransform.position.z;

        // Boss mesafesine geldik mi?
        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            GameEvents.OnGameOver?.Invoke(); // TODO: BossManager.Instance.StartBoss()
            Debug.Log("BOSS ZAMANI! Mesafe: " + pz);
            return;
        }

        // Kapi spawn
        while (pz + spawnAhead >= nextGateZ)
        {
            SpawnGatePair(nextGateZ);
            nextGateZ += gateSpacing;
        }

        // Dushman dalgasi
        while (pz + spawnAhead >= nextWaveZ)
        {
            SpawnEnemyWave(nextWaveZ);
            nextWaveZ += waveSpacing;
        }
    }

    // ── Kapi Cifti ────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];

        // Kapılar yol ortasında, arasinda bosluk: oyuncu birinden geçmek zorunda
        float offset = ROAD_HALF_WIDTH * 0.5f; // 2.75
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 40f);
    }

    // ── Dushman Dalgasi ───────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        float pz = playerTransform.position.z;

        // Zorluk: mesafeye gore dushman sayisi artar
        float progress = Mathf.Clamp01(pz / bossDistance);
        int count = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));

        // Grid: maks 4 sutun, N satır
        int cols       = Mathf.Min(count, 4);
        int rows       = Mathf.CeilToInt((float)count / cols);
        float colGap   = (ROAD_HALF_WIDTH * 1.6f) / Mathf.Max(cols, 1); // sutun araligi
        float rowGap   = 2.8f; // satir araligi — cakisma olmasin diye yeterince genis

        float startX   = -(colGap * (cols - 1)) / 2f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = startX + c * colGap;
                float z = zPos + r * rowGap;
                // Sinir kontrolu
                x = Mathf.Clamp(x, -ROAD_HALF_WIDTH + 0.5f, ROAD_HALF_WIDTH - 0.5f);
                Instantiate(enemyPrefab, new Vector3(x, 1.2f, z), Quaternion.identity);
                spawned++;
            }
        }
    }
}