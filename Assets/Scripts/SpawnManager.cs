using UnityEngine;

/// <summary>
/// Top End War — Birleşik Spawn Yöneticisi (Claude)
/// Her slot'ta KAPIDAN VEYA DÜŞMANDAN biri çıkar.
/// Aynı Z noktasına asla ikisi birden gelmez.
/// GateSpawner ve EnemySpawner scriptlerini/objelerini KALDIR.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn Ayarları")]
    public float spawnAheadDistance = 60f;  // Player önünde kaç birim hazırla
    public float slotSize           = 25f;  // Her slot arası mesafe
    public float laneDistance       = 3.5f; // PlayerController ile aynı

    [Header("Kapı Oranı (0-10)")]
    [Range(0, 10)]
    public int gateSlotsOutOf10 = 6; // 10 slottan 6'sı kapı, 4'ü düşman

    [Header("Zorluk")]
    public int   maxEnemiesPerWave  = 2;    // Bir seferde max kaç düşman
    public float difficultyDistance = 200f; // Bu kadar ilerleyince +1 düşman

    float nextSlotZ    = 30f;
    int   slotsSpawned = 0;

    void Update()
    {
        if (playerTransform == null) return;

        while (playerTransform.position.z + spawnAheadDistance >= nextSlotZ)
        {
            SpawnSlot(nextSlotZ);
            nextSlotZ  += slotSize;
            slotsSpawned++;
        }
    }

    void SpawnSlot(float zPos)
    {
        bool spawnGate = Random.Range(0, 10) < gateSlotsOutOf10;

        if (spawnGate)
            SpawnGatePair(zPos);
        else
            SpawnEnemyWave(zPos);
    }

    // ── Kapı çifti ────────────────────────────────────────────────────────
    void SpawnGatePair(float zPos)
    {
        if (gatePrefab == null || gateDataList == null || gateDataList.Length == 0) return;

        GateData left  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData right = gateDataList[Random.Range(0, gateDataList.Length)];

        SpawnGate(left,  new Vector3(-laneDistance, 1.5f, zPos));
        SpawnGate(right, new Vector3( laneDistance, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 pos)
    {
        GameObject obj  = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate       = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 35f); // Geride kalırsa temizle
    }

    // ── Düşman dalgası ────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        // Zorluk: ne kadar ilerlendiyse o kadar düşman
        int difficultyBonus = Mathf.FloorToInt(playerTransform.position.z / difficultyDistance);
        int count           = Mathf.Clamp(1 + difficultyBonus, 1, maxEnemiesPerWave);

        // Şeritleri karıştır, count kadar seç
        int[] lanes = ShuffleLanes();
        for (int i = 0; i < count; i++)
        {
            float x = (lanes[i] - 1) * laneDistance;
            Instantiate(enemyPrefab, new Vector3(x, 1.2f, zPos), Quaternion.identity);
        }
    }

    int[] ShuffleLanes()
    {
        int[] all = { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j  = Random.Range(0, i + 1);
            int tmp = all[i]; all[i] = all[j]; all[j] = tmp;
        }
        return all;
    }
}