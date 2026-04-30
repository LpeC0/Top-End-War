using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Nesne Havuzu v1.1 (Gameplay Fix Patch)
///
/// v1 → v1.1 Fix Delta:
///   • SpawnFromPool(): transform.position/rotation artık SetActive(true) ÖNCE atanıyor.
///     Eski sıra: SetActive → OnEnable → _lastPos = YANLIŞ pozisyon → pos ata
///     Yeni sıra: pos ata → SetActive → OnEnable → _lastPos = DOĞRU pozisyon
///     Bu değişiklik Bullet'ın ilk frame OverlapCapsule sweep'ini düzeltiyor.
///
///   • Aktif (hâlâ uçan) bullet'lar artık yeniden kullanılmıyor.
///     Eski kod: her spawn'da sıranın başındaki objeyi alıyordu — aktif olup olmadığına
///     bakmıyordu. Hızlı ateşte uçmakta olan bullet'lar teleport ediyordu.
///     Yeni kod: foreach ile inaktif ilk obje bulunuyor. Hepsi aktifse havuz büyüyor.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string     tag;
        public GameObject prefab;
        public int        size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (Pool pool in pools)
        {
            Queue<GameObject> q = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                ConfigureSpawnedObject(pool.tag, obj);
                obj.SetActive(false);
                obj.transform.parent = this.transform;
                q.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, q);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        // FIX: Aktif (hâlâ uçmakta olan) objeleri atla; inaktif ilk objeyi seç.
        // Queue<T> foreach sırayı bozmadan iterate eder.
        foreach (GameObject obj in poolDictionary[tag])
        {
            if (obj.activeSelf) continue;

            // FIX: Pozisyon ve rotasyonu SetActive'den ÖNCE ata.
            // Böylece Bullet.OnEnable() → _lastPos = transform.position doğru pozisyonu yakalar.
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            ConfigureSpawnedObject(tag, obj);
            obj.SetActive(true);
            return obj;
        }

        // Havuzda inaktif obje kalmadı — havuzu büyüt.
        Pool poolDef = pools.Find(p => p.tag == tag);
        if (poolDef != null && poolDef.prefab != null)
        {
            GameObject newObj = Instantiate(poolDef.prefab);
            ConfigureSpawnedObject(tag, newObj);
            newObj.SetActive(false);
            newObj.transform.parent = transform;
            poolDictionary[tag].Enqueue(newObj);

            // FIX: Yeni obje için de aynı sıra: pozisyon → SetActive.
            newObj.transform.position = position;
            newObj.transform.rotation = rotation;
            ConfigureSpawnedObject(tag, newObj);
            newObj.SetActive(true);

            Debug.Log($"[ObjectPooler] Pool '{tag}' büyütüldü (aktif obje kalmamıştı).");
            return newObj;
        }

        Debug.LogWarning($"[ObjectPooler] '{tag}' havuzunda inaktif obje yok ve prefab bulunamadı.");
        return null;
    }

    void ConfigureSpawnedObject(string tag, GameObject obj)
    {
        if (tag != "Enemy" || obj == null) return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            SetLayerRecursive(obj, enemyLayer);

        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
