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
    public float pickupForwardOffset = 8f;
    public float pickupY = 1.2f;

    [Header("Timing")]
    public float firstSpawnDelay = 2.4f;
    public float spawnInterval = 5.5f;
    public int maxActivePickups = 2;
    public bool spawnOnlyUnderEnemyPressure = true;

    [Header("Pickup Defaults")]
    public float pickupLifetime = 4.8f;
    public float buffDuration = 6f;

    readonly List<AnchorPickup> _activePickups = new List<AnchorPickup>();
    Coroutine _loop;
    int _spawnIndex;

    public void BeginAnchorPickups()
    {
        StopAnchorPickups();
        _spawnIndex = 0;
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
                _activePickups[i].gameObject.SetActive(false);
        }
        _activePickups.Clear();
    }

    IEnumerator PickupLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive)
        {
            CleanupInactive();

            if (_activePickups.Count < maxActivePickups && HasEnemyPressure())
                SpawnChoicePair();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnChoicePair()
    {
        Vector3 playerPos = PlayerStats.Instance != null
            ? PlayerStats.Instance.transform.position
            : Vector3.zero;

        float z = playerPos.z + pickupForwardOffset;
        AnchorPickupType leftType = _spawnIndex % 2 == 0
            ? AnchorPickupType.AddSoldier
            : AnchorPickupType.RepairSquad;

        AnchorPickupType rightType = _spawnIndex % 2 == 0
            ? AnchorPickupType.FireRateBoost
            : AnchorPickupType.RepairAnchor;

        SpawnPickup(leftType, new Vector3(leftLaneX, pickupY, z));
        SpawnPickup(rightType, new Vector3(rightLaneX, pickupY, z));
        _spawnIndex++;
    }

    void SpawnPickup(AnchorPickupType type, Vector3 position)
    {
        GameObject obj = new GameObject($"AnchorPickup_{type}");
        obj.name = $"AnchorPickup_{type}";
        obj.transform.position = position;

        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.25f;

        AnchorPickup pickup = obj.AddComponent<AnchorPickup>();
        pickup.pickupType = type;
        pickup.lifetime = pickupLifetime;
        pickup.duration = buffDuration;
        pickup.pickupColor = GetColor(type);
        pickup.labelColor = GetTextColor(type);
        ConfigurePickupValues(pickup);

        _activePickups.Add(pickup);
        Debug.Log($"[AnchorPickupSpawner] Spawn {type} at {position}");
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

    bool HasEnemyPressure()
    {
        if (!spawnOnlyUnderEnemyPressure) return true;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e != null && e.gameObject.activeInHierarchy && e.IsAlive)
                return true;
        }

        return false;
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
