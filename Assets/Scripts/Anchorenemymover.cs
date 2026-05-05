using System.Collections.Generic;
using TMPro;
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
    bool          _isCharger;
    bool          _windupDone;
    float         _windupEndTime;
    float         _windupDuration;
    Vector3       _baseScale = Vector3.one;
    Color         _windupBaseColor = Color.white;
    bool          _hasWindupBaseColor;
    TextMeshPro   _windupLabel;

    Enemy _enemy;
    Renderer _bodyRenderer;

    const float WAYPOINT_REACH_DIST    = 0.4f;
    const float MIN_ATTACK_INTERVAL    = 0.8f;
    const float MAX_ATTACK_INTERVAL    = 1.2f;
    const float FAR_ANCHOR_WARN_DIST   = 8f;
    const float CHARGER_MIN_WINDUP     = 0.25f;
    const float CHARGER_MAX_WINDUP     = 0.45f;
    const float CHARGER_SPEED_MULT     = 1.45f;

    [SerializeField] bool logAttackTicks = false;

    // ── Coverage için lane bilgisi ─────────────────────────────────────────
    public AnchorLane Lane { get; private set; } = AnchorLane.Center;

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _bodyRenderer = GetComponentInChildren<Renderer>();
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        _initialized     = false;
        _state           = AnchorEnemyMoveState.Idle;
        _waypointIndex   = 0;
        _nextAttackTime  = 0f;
        _loggedFarAnchor = false;
        _windupDone      = false;
        _windupEndTime   = 0f;
        _hasWindupBaseColor = false;
        if (_windupLabel != null)
            _windupLabel.gameObject.SetActive(false);
    }

    void OnDisable() => _enemy?.DisableAnchorMovement();

    /// <summary>
    /// AnchorSpawnController her spawn sonrası çağırır.
    /// lane: enemy'nin geldiği savunma hattı — Bullet coverage hesabında kullanılır.
    /// </summary>
    public void Init(List<Vector3> waypoints, float speed, int contactDamage, AnchorLane lane)
    {
        Init(waypoints, speed, contactDamage, lane, false);
    }

    public void Init(List<Vector3> waypoints, float speed, int contactDamage, AnchorLane lane, bool isCharger)
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
        _isCharger      = isCharger;
        _windupDone     = !isCharger;
        _windupDuration = Random.Range(CHARGER_MIN_WINDUP, CHARGER_MAX_WINDUP); // DEĞİŞİKLİK: Charger kısa okunabilir hazırlık süresi alır.

        _enemy?.EnableAnchorMovement();
        transform.position = waypoints[0];
        if (_isCharger)
            EnsureWindupVisual();
    }

    void Update()
    {
        if (!_initialized) return;
        if (_enemy == null || !_enemy.IsAlive) return;

        switch (_state)
        {
            case AnchorEnemyMoveState.MovingToAnchor:   MoveAlongPath();  break;
            case AnchorEnemyMoveState.Windup:           UpdateWindup();   break;
            case AnchorEnemyMoveState.AttackingAnchor:  AttackAnchor();   break;
        }
    }

    void MoveAlongPath()
    {
        if (_waypointIndex >= _waypoints.Count) { ArriveAtAnchor(); return; }
        if (_isCharger && !_windupDone && _waypointIndex == _waypoints.Count - 1)
        {
            BeginWindup();
            return;
        }

        Vector3 target = _waypoints[_waypointIndex];
        float speed = _speed * (_isCharger && _windupDone ? CHARGER_SPEED_MULT : 1f);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= WAYPOINT_REACH_DIST)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Count)
                ArriveAtAnchor();
        }
    }

    void BeginWindup()
    {
        // DEĞİŞİKLİK: Charger breach hamlesinden önce kısa uyarı/wind-up verir.
        _state = AnchorEnemyMoveState.Windup;
        _windupEndTime = Time.time + _windupDuration;
        _baseScale = transform.localScale;
        if (_bodyRenderer != null)
        {
            _windupBaseColor = _bodyRenderer.material.color;
            _hasWindupBaseColor = true;
        }
        if (_windupLabel != null)
            _windupLabel.gameObject.SetActive(true);
    }

    void UpdateWindup()
    {
        // DEĞİŞİKLİK: Wind-up sırasında küçük scale pulse ve "!" uyarısı gösterilir.
        float remaining = Mathf.Max(0f, _windupEndTime - Time.time);
        float t = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, _windupDuration));
        float pulse = 1f + Mathf.Sin(Time.time * 28f) * 0.08f;
        transform.localScale = _baseScale * pulse;

        if (_windupLabel != null && Camera.main != null)
            _windupLabel.transform.rotation = Quaternion.LookRotation(_windupLabel.transform.position - Camera.main.transform.position);

        if (_bodyRenderer != null)
            _bodyRenderer.material.color = Color.Lerp(_windupBaseColor, new Color(1f, 0.25f, 0.1f), 0.35f + t * 0.45f);

        if (Time.time < _windupEndTime) return;

        transform.localScale = _baseScale;
        if (_bodyRenderer != null && _hasWindupBaseColor)
            _bodyRenderer.material.color = _windupBaseColor;
        if (_windupLabel != null)
            _windupLabel.gameObject.SetActive(false);
        _windupDone = true;
        _state = AnchorEnemyMoveState.MovingToAnchor;
    }

    void EnsureWindupVisual()
    {
        // DEĞİŞİKLİK: Charger wind-up final art olmadan TMP "!" placeholder ile okunur.
        if (_windupLabel != null) return;
        GameObject labelObj = new GameObject("ChargerWindupLabel");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        _windupLabel = labelObj.AddComponent<TextMeshPro>();
        _windupLabel.text = "!";
        _windupLabel.alignment = TextAlignmentOptions.Center;
        _windupLabel.fontSize = 2.2f;
        _windupLabel.fontStyle = FontStyles.Bold;
        _windupLabel.color = new Color(1f, 0.2f, 0.08f);
        _windupLabel.outlineWidth = 0.2f;
        _windupLabel.outlineColor = Color.black;
        _windupLabel.gameObject.SetActive(false);
    }

    void ArriveAtAnchor()
    {
        if (_state == AnchorEnemyMoveState.AttackingAnchor) return;

        _state          = AnchorEnemyMoveState.AttackingAnchor;
        _nextAttackTime = Time.time + _attackInterval;
        RunDebugMetrics.Instance.RecordEnemyReachedAnchor(); // DEĞİŞİKLİK: Anchor'a ulaşan düşmanlar consequence metriğine yazılır.
        AnchorCore.Instance?.TakeDamage(_contactDamage); // DEĞİŞİKLİK: Breach line geçildiği anda Core hit hemen görünür.

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

public enum AnchorEnemyMoveState { Idle, MovingToAnchor, Windup, AttackingAnchor }
