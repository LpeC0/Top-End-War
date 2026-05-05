using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Pozisyon")]
    public float heightOffset = 10.5f;
    public float backOffset   = 14f;
    public float followSpeed  = 8f;

    [Header("Açı")]
    [Range(10f, 50f)]
    public float pitchAngle = 28f;

    // DEĞİŞİKLİK
    [Header("X Takip")]
    [Range(0f, 1f)] public float xFollowStrength = 0.42f;
    public float xMaxOffset = 2.6f;

    Quaternion _fixedRotation;
    float _shakeUntil;
    float _shakeMagnitude;
    Vector3 _lastTargetPos;
    Vector3 _lastCameraPos;
    float _lastSnapTraceTime = -999f;
    bool _hasSnapTraceBaseline;

    void Start()
    {
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = _fixedRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        TraceSnapIfNeeded("pre-follow"); // DEĞİŞİKLİK: Camera/player snap kaynağını bulmak için transition log.

        // DEĞİŞİKLİK
        float camX = Mathf.Clamp(target.position.x * xFollowStrength, -xMaxOffset, xMaxOffset);

        Vector3 desired = new Vector3(
            camX,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        if (Time.time < _shakeUntil)
        {
            // DEĞİŞİKLİK: Camera shake follow hedefinin üstüne eklenir; eski localPosition'a snap-back yapılmaz.
            Vector2 shake = Random.insideUnitCircle * _shakeMagnitude;
            desired += new Vector3(shake.x, shake.y, 0f);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * followSpeed
        );

        transform.rotation = _fixedRotation;

        _lastTargetPos = target.position;
        _lastCameraPos = transform.position;
        _hasSnapTraceBaseline = true;
    }

    public void SetPitch(float angle)
    {
        pitchAngle     = Mathf.Clamp(angle, 10f, 50f);
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }

    public void AddShake(float magnitude, float duration)
    {
        // DEĞİŞİKLİK: Anchor core hit feedback kamera reset hissi yaratmadan uygulanır.
        _shakeMagnitude = Mathf.Clamp(Mathf.Max(_shakeMagnitude, Mathf.Max(0f, magnitude)), 0f, 0.2f);
        _shakeUntil = Mathf.Max(_shakeUntil, Time.time + Mathf.Max(0f, duration));
    }

    void TraceSnapIfNeeded(string reason)
    {
        // DEĞİŞİKLİK: Camera/player snap kaynağını bulmak için transition log.
        if (!_hasSnapTraceBaseline) return;
        bool anchorActive = AnchorModeManager.Instance != null && AnchorModeManager.Instance.IsActive;
        if (!anchorActive) return;

        float targetDelta = Vector3.Distance(target.position, _lastTargetPos);
        float cameraDelta = Vector3.Distance(transform.position, _lastCameraPos);
        bool suspicious = targetDelta > 1.2f || cameraDelta > 6f;
        if (!suspicious || Time.time - _lastSnapTraceTime < 0.5f) return;

        _lastSnapTraceTime = Time.time;
        bool runnerActive = SpawnManager.Instance != null && SpawnManager.Instance.enabled;
        Debug.Log($"[SnapTrace] t={Time.time:F1} state={AnchorModeManager.Instance.State} playerPos={target.position:F1} cameraPos={transform.position:F1} target={target.name} anchorActive={anchorActive} runnerActive={runnerActive} reason={reason} targetDelta={targetDelta:F1} cameraDelta={cameraDelta:F1}");
    }
}
