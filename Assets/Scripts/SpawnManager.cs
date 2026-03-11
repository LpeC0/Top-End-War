using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Birleşik Spawn Yöneticisi (Claude)
/// GateSpawner + EnemySpawner yerine bu TEK script kullanılır.
/// Kapılar ve düşmanlar hiçbir zaman aynı Z'ye çıkmaz.
/// GateSpawner ve EnemySpawner objelerini/scriptlerini KALDIR, bunu ekle.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Transform  playerTransform;
    public GameObject gatePrefab;
    public GameObject enemyPrefab;
    public GateData[] gateDataList;

    [Header("Spawn Mesafeleri")]
    public float spawnAheadDistance = 50f;   // Player önünde kaç birim hazırla
    public float slotSize           = 30f;   // Her slot arası mesafe (kapı VEYA düşman)

    [Header("Şerit")]
    public float laneDistance = 3.5f;

    [Header("Oran (10 slottan kaçı kapı, kaçı düşman?)")]
    [Range(1,9)]
    public int gateSlotsOutOf10  = 5;  // 10'da 5 kapı, 5 düşman

    float nextSlotZ = 30f;

    void Update()
    {
        if (playerTransform == null) return;

        while (playerTransform.position.z + spawnAheadDistance >= nextSlotZ)
        {
            SpawnSlot(nextSlotZ);
            nextSlotZ += slotSize;
        }
    }

    void SpawnSlot(float zPos)
    {
        // %gateSlotsOutOf10 ihtimalle kapı, geri kalanı düşman
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
        GameObject obj = Instantiate(gatePrefab, pos, Quaternion.identity);
        Gate gate = obj.GetComponent<Gate>();
        if (gate != null) gate.gateData = data;
        Destroy(obj, 30f);
    }

    // ── Düşman dalgası ────────────────────────────────────────────────────
    void SpawnEnemyWave(float zPos)
    {
        if (enemyPrefab == null) return;

        // 1 veya 2 düşman, orta şerit her zaman boş kalsın (kaçış yolu)
        int count = Random.Range(1, 3); // 1 veya 2
        int[] lanes = PickLanes(count);

        foreach (int lane in lanes)
        {
            float x = (lane - 1) * laneDistance;
            Instantiate(enemyPrefab, new Vector3(x, 1.2f, zPos), Quaternion.identity);
        }
    }

    int[] PickLanes(int count)
    {
        int[] all = { 0, 1, 2 };
        // Shuffle
        for (int i = 2; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            int tmp = all[i]; all[i] = all[j]; all[j] = tmp;
        }
        int[] result = new int[count];
        for (int i = 0; i < count; i++) result[i] = all[i];
        return result;
    }
}