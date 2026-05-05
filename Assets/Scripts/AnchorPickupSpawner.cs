using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal anchor pickup lane prototype.
/// Spawns left/right pickup choices during anchor mode so moving lanes has a reward/risk.
/// </summary>
public class AnchorPickupSpawner : MonoBehaviour
{
    [Header("Lane")]
    public float leftLaneX = -5.2f;
    public float rightLaneX = 5.2f;
    public float pickupForwardOffset = 5.5f; // DEĞİŞİKLİK: Pickup artık uzak obje değil, lane capture zone gibi savunma alanına yakın çıkar.
    public float pickupY = 0.08f;

    [Header("Timing")]
    public float firstSpawnDelay = 2.4f;
    public float spawnInterval = 6.2f; // DEĞİŞİKLİK: Pickup ödülü baskı testini çok sık kesmesin.
    public int maxActivePickups = 2;
    public bool spawnOnlyUnderEnemyPressure = true;
    public int minAliveEnemiesForPickup = 3; // DEĞİŞİKLİK: Pickup seçimi daha net enemy baskısı varken çıkar.
    public float minThreatScoreForPickup = 0.35f; // DEĞİŞİKLİK: Enemy sayısı düşükse threat score pickup için ikinci koşuldur.
    public float maxEmptyThreatTimeForPickup = 0.75f; // DEĞİŞİKLİK: Boş pencerede pickup bedava ödül gibi kalmaz.

    [Header("Pickup Defaults")]
    public float pickupLifetime = 4.2f; // DEĞİŞİKLİK: Capture kararı kısa baskı penceresine bağlı kalır.
    public float buffDuration = 6f;

    [Header("Debug")]
    public bool traceSpawns = false; // DEĞİŞİKLİK: Gerekirse pickup spawn kaynağı tek satır trace ile doğrulanır.

    readonly List<AnchorPickup> _activePickups = new List<AnchorPickup>();
    Coroutine _loop;
    int _spawnIndex;
    int _nextChoiceGroupId = 1;

    public void BeginAnchorPickups()
    {
        StopAnchorPickups();
        _spawnIndex = 0;
        _nextChoiceGroupId = 1;
        AnchorPickup.ResetChoiceState(); // DEĞİŞİKLİK: Anchor pickup pair seçimleri her anchor başlangıcında sıfırlanır.
        _loop = StartCoroutine(PickupLoop());
    }

    public void StopAnchorPickups()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }

        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            if (_activePickups[i] != null)
                Destroy(_activePickups[i].gameObject); // DEĞİŞİKLİK: Anchor bitince pickup mock objeleri inactive birikmez.
        }
        _activePickups.Clear();
    }

    IEnumerator PickupLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive)
        {
            CleanupInactive();
            CancelPickupsIfPressureGone(); // DEĞİŞİKLİK: Spawn sonrası baskı yoksa pickup boş ödül gibi kalmaz.

            if (_activePickups.Count < maxActivePickups && HasPickupPressure())
                SpawnChoicePair();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnChoicePair()
    {
        Vector3 playerPos = PlayerStats.Instance != null
            ? PlayerStats.Instance.transform.position
            : Vector3.zero;

        float z = playerPos.z + Mathf.Clamp(pickupForwardOffset, 3.5f, 7.5f); // DEĞİŞİKLİK: Lane capture zone player'ın önünde yakın ama bedava olmayan noktada kalır.
        AnchorPickupType leftType = _spawnIndex % 2 == 0
            ? AnchorPickupType.AddSoldier
            : AnchorPickupType.RepairSquad;

        AnchorPickupType rightType = _spawnIndex % 2 == 0
            ? AnchorPickupType.FireRateBoost
            : AnchorPickupType.RepairAnchor;

        int choiceGroupId = _nextChoiceGroupId++;
        SpawnPickup(leftType, new Vector3(leftLaneX, pickupY, z), choiceGroupId);
        SpawnPickup(rightType, new Vector3(rightLaneX, pickupY, z), choiceGroupId);
        _spawnIndex++;
    }

    void SpawnPickup(AnchorPickupType type, Vector3 position, int choiceGroupId)
    {
        GameObject obj = new GameObject($"AnchorPickup_{type}");
        obj.name = $"AnchorPickup_{type}";
        obj.transform.position = position;

        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.85f; // DEĞİŞİKLİK: Swipe ile bedelsiz yakalanmasın, lane capture kararı istesin.

        AnchorPickup pickup = obj.AddComponent<AnchorPickup>();
        pickup.pickupType = type;
        pickup.choiceGroupId = choiceGroupId; // DEĞİŞİKLİK: Anchor pickup pair gate gibi tek seçim haline gelir.
        pickup.lifetime = pickupLifetime;
        pickup.duration = buffDuration;
        pickup.pickupColor = GetColor(type);
        pickup.labelColor = GetTextColor(type);
        ConfigurePickupValues(pickup);

        _activePickups.Add(pickup);
        if (traceSpawns)
        {
            // DEĞİŞİKLİK: Spawn kaynağını tespit etmek için geçici debug log.
            Vector3 playerPos = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : Vector3.zero;
            Debug.Log($"[SpawnTrace] name={obj.name} source=AnchorPickupSpawner mode=Anchor pos={position:F1} distToPlayer={Vector3.Distance(position, playerPos):F1} prefab=runtime-new-gameobject");
        }
    }

    void ConfigurePickupValues(AnchorPickup pickup)
    {
        switch (pickup.pickupType)
        {
            case AnchorPickupType.AddSoldier:
                pickup.value = 1f;
                pickup.iconText = "+";
                pickup.labelText = "+1 SOLDIER";
                pickup.feedbackText = "+1 SOLDIER!";
                break;
            case AnchorPickupType.RepairSquad:
                pickup.healPercent = 0.25f;
                pickup.iconText = "HP";
                pickup.labelText = "SQUAD HEAL";
                pickup.feedbackText = "SQUAD REPAIRED!";
                break;
            case AnchorPickupType.FireRateBoost:
                pickup.value = 35f;
                pickup.duration = buffDuration;
                pickup.iconText = ">>";
                pickup.labelText = "FIRE RATE";
                pickup.feedbackText = "FIRE RATE BOOST!";
                break;
            case AnchorPickupType.ArmorPenBoost:
                pickup.value = 8f;
                pickup.duration = buffDuration;
                pickup.iconText = "AP";
                pickup.labelText = "ARMOR PEN";
                pickup.feedbackText = "ARMOR PEN BOOST!";
                break;
            case AnchorPickupType.RepairAnchor:
                pickup.healPercent = 0.18f;
                pickup.iconText = "+";
                pickup.labelText = "REPAIR";
                pickup.feedbackText = "ANCHOR REPAIRED!";
                break;
        }
    }

    Color GetColor(AnchorPickupType type)
    {
        switch (type)
        {
            case AnchorPickupType.AddSoldier: return new Color(0.25f, 0.95f, 0.35f);
            case AnchorPickupType.RepairSquad: return new Color(0.35f, 0.8f, 1f);
            case AnchorPickupType.FireRateBoost: return new Color(1f, 0.85f, 0.15f);
            case AnchorPickupType.ArmorPenBoost: return new Color(1f, 0.45f, 0.15f);
            case AnchorPickupType.RepairAnchor: return new Color(0.2f, 1f, 0.8f);
            default: return Color.white;
        }
    }

    Color GetTextColor(AnchorPickupType type)
    {
        switch (type)
        {
            case AnchorPickupType.AddSoldier: return new Color(0.45f, 1f, 0.55f);
            case AnchorPickupType.RepairSquad: return new Color(0.45f, 0.9f, 1f);
            case AnchorPickupType.FireRateBoost: return new Color(1f, 0.9f, 0.2f);
            case AnchorPickupType.ArmorPenBoost: return new Color(0.45f, 0.75f, 1f);
            case AnchorPickupType.RepairAnchor: return new Color(0.25f, 1f, 0.75f);
            default: return Color.white;
        }
    }

    bool HasPickupPressure()
    {
        // DEĞİŞİKLİK: Pickup sadece aktif baskı penceresinde çıkar.
        if (!spawnOnlyUnderEnemyPressure) return true;

        int alive = CountAliveEnemies();
        float threat = ThreatManager.Instance != null ? ThreatManager.Instance.ThreatScore : 0f;
        float empty = RunDebugMetrics.Instance.TimeWithoutThreat;
        if (empty > maxEmptyThreatTimeForPickup) return false;
        return alive >= minAliveEnemiesForPickup || threat >= minThreatScoreForPickup;
    }

    int CountAliveEnemies()
    {
        // DEĞİŞİKLİK: Pickup pressure kararı için aktif enemy sayısı doğrudan okunur.
        int alive = 0;
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e != null && e.gameObject.activeInHierarchy && e.IsAlive)
                alive++;
        }

        return alive;
    }

    void CancelPickupsIfPressureGone()
    {
        // DEĞİŞİKLİK: Pickup spawn olduktan sonra baskı tamamen bittiyse seçim iptal edilir.
        if (_activePickups.Count == 0) return;
        if (HasPickupPressure()) return;

        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            if (_activePickups[i] != null)
                _activePickups[i].DismissFromChoiceGroup();
        }
        _activePickups.Clear();
    }

    void CleanupInactive()
    {
        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            if (_activePickups[i] == null || !_activePickups[i].gameObject.activeInHierarchy)
                _activePickups.RemoveAt(i);
        }
    }
}
