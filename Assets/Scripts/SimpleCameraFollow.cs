using UnityEngine;

/// <summary>
/// Top End War — Runner Kamera (Claude)
/// X sabit — serit degistirince kamera sallanmaz.
/// Cinemachine GEREKMIYOR. Main Camera'ya attach et.
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;
    public float heightOffset = 9f;
    public float backOffset   = 11f;
    public float followSpeed  = 12f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(0f, target.position.y + heightOffset, target.position.z - backOffset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
