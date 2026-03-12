using UnityEngine;

/// <summary>
/// Top End War — Spawn Yoneticisi v4 (Claude)
/// Dushman iç içe gecmez: Physics.OverlapSphere ile doluluk kontrolu yapilir.
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
    public float waveSpacing = 32f;

    [Header("Zorluk")]
    public float bossDistance = 1200f;
    public int   minEnemies   = 2;
    public int   maxEnemies   = 8;

    float nextGateZ   = 40f;
    float nextWaveZ   = 60f;
    bool  bossSpawned = false;

    void Update()
    {
        if (playerTransform == null) return;
        float pz = playerTransform.position.z;

        if (!bossSpawned && pz >= bossDistance)
        {
            bossSpawned = true;
            Debug.Log("[SpawnManager] BOSS ZAMANI! Z=" + pz);
            // TODO: BossManager.Instance.StartBoss();
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

    // ── Kapi Cifti ────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];

        float offset = ROAD_HALF_WIDTH * 0.45f; // ~3.6 birim
        SpawnGate(left,  new Vector3(-offset, 1.5f, zPos));
        SpawnGate(right, new Vector3( offset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate      = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 45f);
    }

    // ── Dushman Dalgasi ───────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        float progress  = Mathf.Clamp01(playerTransform.position.z / bossDistance);
        int   count     = Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, progress));

        // Grid boyutlari
        int   cols      = Mathf.Min(count, 4);
        int   rows      = Mathf.CeilToInt((float)count / cols);

        // Ic ice gecmemesi icin yeterli aralik:
        // Dushman capsule radius ~0.5, min aralik = 2x radius + bosluk = 2.0
        float minGap    = 2.2f;
        float roadWidth = ROAD_HALF_WIDTH * 1.6f; // Kullanılabilir genişlik
        float colGap    = Mathf.Max(roadWidth / Mathf.Max(cols, 1), minGap);
        float rowGap    = 3.0f; // Satirlar arasi — iç içe girmesin

        // Grid'i ortala
        float startX = -(colGap * (cols - 1)) * 0.5f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float x = Mathf.Clamp(
                    startX + c * colGap,
                    -ROAD_HALF_WIDTH + 1f,
                     ROAD_HALF_WIDTH - 1f);
                float z = zPos + r * rowGap;

                Vector3 spawnPos = new Vector3(x, 1.2f, z);

                // OVERLAP KONTROLU: Cok yakinda baska dushman var mi?
                Collider[] nearby = Physics.OverlapSphere(spawnPos, 1.5f);
                bool tooClose = false;
                foreach (Collider col in nearby)
                    if (col.CompareTag("Enemy")) { tooClose = true; break; }

                if (!tooClose)
                {
                    Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                    spawned++;
                }
                else
                {
                    // Cok yakin — X'i kaydirarak tekrar dene
                    Vector3 altPos = new Vector3(
                        Mathf.Clamp(x + colGap * 0.5f, -ROAD_HALF_WIDTH + 1f, ROAD_HALF_WIDTH - 1f),
                        1.2f, z + 1f);
                    Instantiate(enemyPrefab, altPos, Quaternion.identity);
                    spawned++;
                }
            }
        }
    }
}