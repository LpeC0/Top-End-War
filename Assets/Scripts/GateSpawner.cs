using UnityEngine;

/// <summary>
/// Top End War — Kapı Üretici
/// Player'ın önüne çift kapı spawn eder. Geride kalanları temizler.
/// </summary>
public class GateSpawner : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GateData[] gateDataList;

    [Header("Spawn Ayarları")]
    public float spawnAheadDistance = 35f;  // Player'ın kaç birim önünde çıksın
    public float spacingBetweenGates = 18f; // Kapı çiftleri arası mesafe (daha seyrek)
    public float laneOffset = 3.5f;         // Sol/sağ mesafe (PlayerController ile aynı)
    public float cleanupDistance = 15f;     // Player geçtikten kaç birim sonra silinsin

    float nextSpawnZ = 30f;

    void Update()
    {
        if (playerTransform == null || gatePrefab == null) return;

        // Önüne yeni kapı üret
        while (playerTransform.position.z + spawnAheadDistance >= nextSpawnZ)
        {
            SpawnGatePair(nextSpawnZ);
            nextSpawnZ += spacingBetweenGates;
        }
    }

    void SpawnGatePair(float zPos)
    {
        if (gateDataList == null || gateDataList.Length == 0) return;

        GateData leftData  = gateDataList[Random.Range(0, gateDataList.Length)];
        GateData rightData = gateDataList[Random.Range(0, gateDataList.Length)];

        SpawnGate(leftData,  new Vector3(-laneOffset, 1.5f, zPos));
        SpawnGate(rightData, new Vector3( laneOffset, 1.5f, zPos));
    }

    void SpawnGate(GateData data, Vector3 position)
    {
        GameObject obj = Instantiate(gatePrefab, position, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;

        // Geride kalan kapıyı otomatik temizle
        Destroy(obj, 30f);
    }
}