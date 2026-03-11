using UnityEngine;
using System.Collections;

/// <summary>
/// Top End War — Tier Morph Sistemi (Claude)
/// Tier atladığında Player modeli değişir + VFX oynar.
/// Player objesine ekle. Tier prefabları Inspector'dan bağla.
/// </summary>
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefabları (1'den 5'e sıralı)")]
    public GameObject[] tierPrefabs; // 0=Tier1, 1=Tier2 ... 4=Tier5

    [Header("VFX")]
    public GameObject morphParticlePrefab; // Patlama efekti (isteğe bağlı)
    public float      flashDuration = 0.3f;

    GameObject currentModel;
    int        currentTierIndex = 0;

    void Start()
    {
        // Oyun başlarken Tier 1 modelini göster
        GameEvents.OnTierChanged += OnTierChanged;
        SpawnModel(0);
    }

    void OnDestroy()
    {
        GameEvents.OnTierChanged -= OnTierChanged;
    }

    void OnTierChanged(int newTier)
    {
        int index = Mathf.Clamp(newTier - 1, 0, tierPrefabs.Length - 1);
        if (index == currentTierIndex) return;

        StartCoroutine(MorphSequence(index));
    }

    IEnumerator MorphSequence(int newIndex)
    {
        // 1) Ekran flaşı — Player'ı gizle
        if (currentModel != null)
            currentModel.SetActive(false);

        // 2) Parçacık efekti
        if (morphParticlePrefab != null)
            Destroy(Instantiate(morphParticlePrefab, transform.position, Quaternion.identity), 2f);

        yield return new WaitForSeconds(flashDuration);

        // 3) Yeni modeli göster
        SpawnModel(newIndex);
        currentTierIndex = newIndex;

        Debug.Log($"Morph: Tier {newIndex + 1} modeli aktif");
    }

    void SpawnModel(int index)
    {
        // Eskiyi sil
        if (currentModel != null)
            Destroy(currentModel);

        // Prefab yoksa küp göster (placeholder)
        if (tierPrefabs == null || index >= tierPrefabs.Length || tierPrefabs[index] == null)
        {
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentModel.transform.SetParent(transform);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localScale    = Vector3.one;
            // Collider çakışmasın
            Destroy(currentModel.GetComponent<Collider>());
            return;
        }

        currentModel = Instantiate(tierPrefabs[index], transform.position, transform.rotation);
        currentModel.transform.SetParent(transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localScale    = Vector3.one;

        // Morph modelinin kendi collider'ı varsa kaldır (Player'ın collider'ı yeterli)
        foreach (Collider c in currentModel.GetComponentsInChildren<Collider>())
            Destroy(c);
    }
}