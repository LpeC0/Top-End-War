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

    void Start()
    {
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = _fixedRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // DEĞİŞİKLİK
        float camX = Mathf.Clamp(target.position.x * xFollowStrength, -xMaxOffset, xMaxOffset);

        Vector3 desired = new Vector3(
            camX,
            target.position.y + heightOffset,
            target.position.z - backOffset
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * followSpeed
        );

        transform.rotation = _fixedRotation;
    }

    public void SetPitch(float angle)
    {
        pitchAngle     = Mathf.Clamp(angle, 10f, 50f);
        _fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}