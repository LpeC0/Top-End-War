using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Sonsuz Yol Yöneticisi
/// Zemin parçalarını (Chunk) Player'ın önüne dizer, arkada kalanları siler.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject chunkPrefab;      // Hazırladığımız RoadChunk prefabı
    public Transform playerTransform;   // Karakterimiz
    public int initialChunks = 5;       // Ekranda aynı anda kaç zemin olacak?
    public float chunkLength = 50f;     // Plane Z scale 5 ise uzunluk 50'dir.

    private float spawnZ = 0f;          // Bir sonraki zeminin çıkacağı Z konumu
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    void Start()
    {
        // Oyun başlarken ilk zeminleri döşe
        for (int i = 0; i < initialChunks; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Player yeterince ilerlediyse, yeni chunk üret ve en eskisini sil
        // player Z konumu, arkada kalan chunk'ı geçtiğinde tetiklenir
        if (playerTransform.position.z - (chunkLength * 1.5f) > (spawnZ - (initialChunks * chunkLength)))
        {
            SpawnChunk();
            DeleteOldChunk();
        }
    }

    void SpawnChunk()
    {
        // Yeni zemini spawnla
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        
        // Zeminleri gruplamak için bu objenin (ChunkManager) altına koyalım
        chunk.transform.SetParent(this.transform);
        
        activeChunks.Enqueue(chunk);
        
        // Bir sonraki spawn noktasını ileri taşı
        spawnZ += chunkLength;
    }

    void DeleteOldChunk()
    {
        // Kuyruktan en baştakini (en eskisini) al ve yok et
        GameObject oldChunk = activeChunks.Dequeue();
        Destroy(oldChunk);
        // İleride performans için bunu da Object Pool'a çevirebiliriz ama zeminler için Destroy şu an mobilde bile çok dert değil.
    }
}