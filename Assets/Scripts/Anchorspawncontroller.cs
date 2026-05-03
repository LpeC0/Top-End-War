using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — AnchorSpawnController v3.0
///
/// v2 → v3 Delta:
///   • Lane sistemi eklendi: WaveGroup.laneBias → AnchorLane dönüşümü.
///   • Spread/Random bias: grup içindeki enemy'lere sol/orta/sağ dağıtılır.
///   • Spawn X pozisyonu AnchorCoverage.LaneToSpawnX'ten alınır.
///   • AnchorEnemyMover.Init'e lane parametresi geçilir.
///   • enemyPrefab auto-resolver korundu.
/// </summary>
public class AnchorSpawnController : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Path'ler")]
    public List<AnchorPath> paths = new List<AnchorPath>();

    [Header("Spawn")]
    public float spawnForwardDistance = 32f;
    public float maxVisibleSpawnForwardDistance = 38f;
    [Range(0f, 1.5f)]
    public float spawnJitterX = 0.4f;

    [Header("Anchor Formasyon")]
    public float spawnZStagger = 1.6f;
    public float laneHalfWidth = 5.2f;
    public float approachSpreadRadius = 2.4f;
    public float curveStrength = 3.8f;

    int _pathRoundRobin = 0;

    // ── Prefab Resolver ───────────────────────────────────────────────────

    void Awake()  => ResolveEnemyPrefab();
    void Reset()  => ResolveEnemyPrefab();

    void ResolveEnemyPrefab()
    {
        if (enemyPrefab != null) return;

        if (SpawnManager.Instance != null && SpawnManager.Instance.enemyPrefab != null)
        {
            enemyPrefab = SpawnManager.Instance.enemyPrefab;
            return;
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("EnemyPrefab t:Prefab");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponent<Enemy>() != null)
            {
                enemyPrefab = go;
                Debug.Log($"[AnchorSpawnController] enemyPrefab otomatik bulundu: {path}");
                return;
            }
        }
#endif
        Debug.LogWarning("[AnchorSpawnController] enemyPrefab bulunamadı — Inspector'dan atayın.");
    }

    // ── Ana Giriş ─────────────────────────────────────────────────────────

    public IEnumerator SpawnWave(AnchorWaveEntry wave, float difficultyMult)
    {
        if (wave?.groups == null || wave.groups.Count == 0) yield break;

        ResolveEnemyPrefab();
        if (enemyPrefab == null)
        {
            Debug.LogError("[AnchorSpawnController] enemyPrefab atanmamış.");
            yield break;
        }

        for (int g = 0; g < wave.groups.Count; g++)
        {
            WaveGroup group = wave.groups[g];
            if (group?.archetype == null) continue;

            AnchorPath path = paths != null && paths.Count > 0
                ? paths[_pathRoundRobin % paths.Count]
                : null;
            _pathRoundRobin++;

            float speed = (path != null && path.speedOverride > 0f)
                ? path.speedOverride
                : group.archetype.moveSpeed;

            int count = Mathf.Max(1, group.count);

            // Lane dağılımını hesapla
            AnchorLane[] lanes = ResolveLanes(group.laneBias, count);

            for (int i = 0; i < count; i++)
            {
                AnchorLane lane = lanes[i];
                List<Vector3> waypoints = BuildDynamicWaypoints(lane, wave.waveType, i, count, g);

                if (waypoints == null)
                {
                    Debug.LogWarning("[AnchorSpawnController] Waypoint hesaplanamadı.");
                    yield break;
                }

                SpawnEnemy(group.archetype, waypoints, speed, difficultyMult, lane);

                if (wave.intraDelay > 0f)
                    yield return new WaitForSeconds(wave.intraDelay);
            }

            if (g < wave.groups.Count - 1 && wave.groupDelay > 0f)
                yield return new WaitForSeconds(wave.groupDelay);
        }
    }

    // ── Lane Dağılımı ─────────────────────────────────────────────────────

    /// <summary>
    /// LaneBias'a göre enemy başına lane atar.
    /// Spread → sol/orta/sağ round-robin.
    /// Random → her enemy için ayrı rastgele.
    /// </summary>
    AnchorLane[] ResolveLanes(LaneBias bias, int count)
    {
        var result = new AnchorLane[count];

        if (bias == LaneBias.Spread)
        {
            AnchorLane[] cycle = { AnchorLane.Left, AnchorLane.Center, AnchorLane.Right };
            for (int i = 0; i < count; i++)
                result[i] = cycle[i % 3];
        }
        else if (bias == LaneBias.Random)
        {
            AnchorLane[] all = { AnchorLane.Left, AnchorLane.Center, AnchorLane.Right };
            for (int i = 0; i < count; i++)
                result[i] = all[Random.Range(0, 3)];
        }
        else
        {
            AnchorLane lane = AnchorCoverage.LaneFromBias(bias);
            for (int i = 0; i < count; i++)
                result[i] = lane;
        }

        return result;
    }

    // ── Dinamik Waypoint ──────────────────────────────────────────────────

    List<Vector3> BuildDynamicWaypoints(AnchorLane lane, AnchorWaveType waveType, int memberIndex, int memberCount, int groupIndex)
    {
        if (PlayerStats.Instance == null || AnchorCore.Instance == null)
            return null;

        Vector3 playerPos = PlayerStats.Instance.transform.position;
        Vector3 corePos   = AnchorCore.Instance.transform.position;

        float safeForward = Mathf.Clamp(spawnForwardDistance, 18f, Mathf.Max(18f, maxVisibleSpawnForwardDistance));
        Vector2 shape = GetShapeOffset(waveType, memberIndex, memberCount, groupIndex);

        float laneX = Mathf.Clamp(
            AnchorCoverage.LaneToSpawnX(lane) + shape.x + Random.Range(-spawnJitterX, spawnJitterX),
            -laneHalfWidth,
            laneHalfWidth);

        Vector3 spawnPos = new Vector3(
            laneX,
            playerPos.y,
            playerPos.z + safeForward + memberIndex * spawnZStagger + shape.y);

        Vector3 targetPos = GetApproachPoint(corePos, lane, memberIndex, groupIndex);
        Vector3 midPos = BuildMidPoint(spawnPos, targetPos, memberIndex, groupIndex);

        return new List<Vector3> { spawnPos, midPos, targetPos };
    }

    Vector2 GetShapeOffset(AnchorWaveType waveType, int memberIndex, int memberCount, int groupIndex)
    {
        float centered = memberCount <= 1 ? 0f : (float)memberIndex / (memberCount - 1) - 0.5f;
        float zig = memberIndex % 2 == 0 ? -1f : 1f;

        switch (waveType)
        {
            case AnchorWaveType.Swarm:
                return new Vector2(
                    Mathf.Sin(memberIndex * 1.618f + groupIndex) * 1.2f,
                    Random.Range(-0.9f, 0.9f));

            case AnchorWaveType.ArmorCheck:
                return new Vector2(centered * 0.7f, -1.0f + groupIndex * 0.6f);

            case AnchorWaveType.EliteStrike:
            case AnchorWaveType.BossWave:
                return new Vector2(centered * 1.5f + zig * 0.35f, groupIndex * 1.1f);

            case AnchorWaveType.Relief:
                return new Vector2(centered * 1.6f, memberIndex * 0.55f);

            default:
                return new Vector2(zig * 0.35f, zig * 0.65f);
        }
    }

    Vector3 BuildMidPoint(Vector3 spawnPos, Vector3 targetPos, int memberIndex, int groupIndex)
    {
        float curve = Mathf.Sin((memberIndex + 1) * 1.37f + groupIndex * 0.9f) * curveStrength;
        float midX = Mathf.Clamp(Mathf.Lerp(spawnPos.x, targetPos.x, 0.45f) + curve, -laneHalfWidth, laneHalfWidth);
        float midZ = Mathf.Lerp(spawnPos.z, targetPos.z, 0.55f);
        return new Vector3(midX, spawnPos.y, midZ);
    }

    Vector3 GetApproachPoint(Vector3 corePos, AnchorLane lane, int memberIndex, int groupIndex)
    {
        float laneSide = lane == AnchorLane.Left ? -1f : lane == AnchorLane.Right ? 1f : 0f;
        float angle = ((memberIndex * 137.5f) + groupIndex * 41f) * Mathf.Deg2Rad;
        Vector3 scatter = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * approachSpreadRadius;
        Vector3 laneOffset = new Vector3(laneSide * 1.35f, 0f, 0f);
        return new Vector3(corePos.x, PlayerStats.Instance.transform.position.y, corePos.z) + scatter + laneOffset;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────

    void SpawnEnemy(EnemyArchetypeConfig archetype, List<Vector3> waypoints,
                    float speed, float diffMult, AnchorLane lane)
    {
        GameObject obj = Instantiate(enemyPrefab, waypoints[0], Quaternion.identity);
        ConfigureEnemyPhysics(obj);

        float targetDps = StageManager.Instance?.ActiveStage != null
            ? StageManager.Instance.ActiveStage.targetDps : 100f;

        int hp     = Mathf.Max(1, Mathf.RoundToInt(archetype.GetHP(targetDps) * diffMult));
        int damage = Mathf.Max(1, Mathf.RoundToInt(archetype.contactDamage * diffMult));
        int cp     = archetype.GetCPReward(targetDps);

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(new DifficultyManager.EnemyStats(hp, damage, speed, cp));
            enemy.ConfigureCombat(archetype.armor, archetype.IsEliteLike);
            enemy.ConfigureArchetype(archetype);
        }

        AnchorEnemyMover mover = obj.GetComponent<AnchorEnemyMover>()
                              ?? obj.AddComponent<AnchorEnemyMover>();
        mover.Init(waypoints, speed, damage, lane);

        Debug.Log($"[AnchorSpawnController] Spawn: {archetype.enemyName} | Lane={lane} | from={waypoints[0]:F0} → mid={waypoints[1]:F0} → to={waypoints[waypoints.Count - 1]:F0}");
    }

    public void ResetPathCursor() => _pathRoundRobin = 0;

    void ConfigureEnemyPhysics(GameObject obj)
    {
        int layer = LayerMask.NameToLayer("Enemy");
        if (layer >= 0) SetLayerRecursive(obj, layer);
        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        { rb.isKinematic = true; rb.useGravity = false; }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
