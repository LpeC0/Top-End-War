using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v3 (Claude)
/// xLimit=8 ile genis yol. Kapi+dushman birlikte var olabilir.
/// Boss mesafesi: 1200 birim.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static float ROAD_HALF_WIDTH = 8f; // PlayerController.xLimit ile AYNI

    [Header("Baglantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn")]
    public float spawnAhead  = 65f;
    public float gateSpacing = 40f;
    public float waveSpacing = 32f;

    [Header("Zorluk")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    float nextGateZ  = 40f;
    float nextWaveZ  = 60f;
    bool  bossSpawned = false;

    void Update()
    {
        if (playerTransform == null) return;
        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            Debug.Log("BOSS ZAMANI! Z: " + pz);
            GameEvents.OnGameOver?.Invoke(); // TODO: BossManager.StartBoss()
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

    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];

        // Kapılar yolun ortasına yakın — aralarında geçilebilir boşluk var
        float offset = ROAD_HALF_WIDTH * 0.45f; // ~3.6
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

    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        float progress = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   count    = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));

        int   cols     = Mathf.Min(count, 4);
        int   rows     = Mathf.CeilToInt((float)count / cols);
        float colGap   = (ROAD_HALF_WIDTH * 1.4f) / Mathf.Max(cols, 1);
        float startX   = -(colGap * (cols - 1)) / 2f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = Mathf.Clamp(startX + c * colGap,
                                      -ROAD_HALF_WIDTH + 0.5f,
                                       ROAD_HALF_WIDTH - 0.5f);
                float z = zPos + r * 2.8f;
                Instantiate(enemyPrefab, new Vector3(x, 1.2f, z), Quaternion.identity);
                spawned++;
            }
        }
    }
}