using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — AnchorEnemyMover v2.0
///
/// v1 → v2 Delta:
///   • AnchorLane field eklendi — Bullet coverage hesabı için.
///   • Init'e lane parametresi eklendi.
///   • Spawn X pozisyonu lane'e göre AnchorCoverage.LaneToSpawnX'ten alınır.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class AnchorEnemyMover : MonoBehaviour
{
    List<Vector3> _waypoints;
    int           _waypointIndex;
    float         _speed;
    int           _contactDamage;
    bool          _initialized;
    AnchorEnemyMoveState _state = AnchorEnemyMoveState.Idle;
    float         _nextAttackTime;
    float         _attackInterval;
    bool          _loggedFarAnchor;

    Enemy _enemy;

    const float WAYPOINT_REACH_DIST    = 0.4f;
    const float MIN_ATTACK_INTERVAL    = 0.8f;
    const float MAX_ATTACK_INTERVAL    = 1.2f;
    const float FAR_ANCHOR_WARN_DIST   = 8f;

    [SerializeField] bool logAttackTicks = false;

    // ── Coverage için lane bilgisi ─────────────────────────────────────────
    public AnchorLane Lane { get; private set; } = AnchorLane.Center;

    void Awake()  => _enemy = GetComponent<Enemy>();

    void OnEnable()
    {
        _initialized     = false;
        _state           = AnchorEnemyMoveState.Idle;
        _waypointIndex   = 0;
        _nextAttackTime  = 0f;
        _loggedFarAnchor = false;
    }

    void OnDisable() => _enemy?.DisableAnchorMovement();

    /// <summary>
    /// AnchorSpawnController her spawn sonrası çağırır.
    /// lane: enemy'nin geldiği savunma hattı — Bullet coverage hesabında kullanılır.
    /// </summary>
    public void Init(List<Vector3> waypoints, float speed, int contactDamage, AnchorLane lane)
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            Debug.LogWarning($"[AnchorEnemyMover] Geçersiz waypoint — {gameObject.name}");
            return;
        }

        _waypoints      = waypoints;
        _speed          = Mathf.Max(0.5f, speed);
        _contactDamage  = Mathf.Max(1, contactDamage);
        _waypointIndex  = 1;
        _initialized    = true;
        _state          = AnchorEnemyMoveState.MovingToAnchor;
        _attackInterval = Random.Range(MIN_ATTACK_INTERVAL, MAX_ATTACK_INTERVAL);
        _nextAttackTime = 0f;
        _loggedFarAnchor = false;
        Lane            = lane;

        _enemy?.EnableAnchorMovement();
        transform.position = waypoints[0];
    }

    void Update()
    {
        if (!_initialized) return;
        if (_enemy == null || !_enemy.IsAlive) return;

        switch (_state)
        {
            case AnchorEnemyMoveState.MovingToAnchor:   MoveAlongPath();  break;
            case AnchorEnemyMoveState.AttackingAnchor:  AttackAnchor();   break;
        }
    }

    void MoveAlongPath()
    {
        if (_waypointIndex >= _waypoints.Count) { ArriveAtAnchor(); return; }

        Vector3 target = _waypoints[_waypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= WAYPOINT_REACH_DIST)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Count)
                ArriveAtAnchor();
        }
    }

    void ArriveAtAnchor()
    {
        if (_state == AnchorEnemyMoveState.AttackingAnchor) return;

        _state          = AnchorEnemyMoveState.AttackingAnchor;
        _nextAttackTime = Time.time + Random.Range(0.05f, 0.2f);
        RunDebugMetrics.Instance.RecordEnemyReachedAnchor(); // DEĞİŞİKLİK: Anchor'a ulaşan düşmanlar consequence metriğine yazılır.

        Vector3 anchorPos = AnchorCore.Instance != null
            ? AnchorCore.Instance.transform.position
            : _waypoints[_waypoints.Count - 1];

        float dist = AnchorCore.Instance != null
            ? Vector3.Distance(_waypoints[_waypoints.Count - 1], anchorPos)
            : 0f;

        if (dist > FAR_ANCHOR_WARN_DIST && !_loggedFarAnchor)
        {
            Debug.LogWarning($"[AnchorEnemyMover] Son waypoint core'dan uzak. dist={dist:F1}");
            _loggedFarAnchor = true;
        }

        transform.position = GetAttackPosition(anchorPos);
        Debug.Log($"[AnchorEnemyMover] Anchor'a ulaştı, saldırıyor. Lane={Lane}");
    }

    void AttackAnchor()
    {
        if (AnchorCore.Instance == null || AnchorCore.Instance.IsDestroyed) return;
        if (Time.time < _nextAttackTime) return;

        AnchorCore.Instance.TakeDamage(_contactDamage);
        _nextAttackTime = Time.time + _attackInterval;

        if (logAttackTicks)
            Debug.Log($"[AnchorEnemyMover] Attack tick dmg={_contactDamage}");
    }

    Vector3 GetAttackPosition(Vector3 anchorPos)
    {
        int hash    = Mathf.Abs(GetInstanceID());
        float angle = (hash % 360) * Mathf.Deg2Rad;
        float r     = 1.2f + (hash % 5) * 0.25f;
        return anchorPos + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_waypoints == null || _waypoints.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = _waypointIndex; i < _waypoints.Count - 1; i++)
            Gizmos.DrawLine(_waypoints[i], _waypoints[i + 1]);
    }
#endif
}

public enum AnchorEnemyMoveState { Idle, MovingToAnchor, AttackingAnchor }
