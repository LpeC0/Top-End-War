using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — AnchorPath v1.0
///
/// Anchor modda düşmanların takip ettiği yolu tanımlar.
/// Waypoint'ler bu objenin child transform'ları olarak tutulur —
/// Scene'de görsel olarak düzenlenebilir, Gizmo ile çizilir.
///
/// Kullanım:
///   - Sahneye boş bir GameObject ekle, adını "Path_Main" gibi yap.
///   - Bu scripti koy.
///   - Child objeler ekle: WP_00, WP_01, WP_02... sıralı isimle.
///   - AnchorSpawnController bu path'e referans alır.
///
/// Level Editor bağlantısı:
///   Path şablonları (PathTemplate SO) ileride bu MonoBehaviour'dan
///   Vector3 listesi serialize edilerek data-driven hale getirilebilir.
///   Şimdilik scene objesi olarak çalışır — değiştirmeye gerek yok.
///
/// Çoklu Path:
///   AnchorSpawnController birden fazla path tutabilir.
///   Her dalga farklı path'e atanabilir → ileride farklı yol tipleri buradan gelir.
/// </summary>
public class AnchorPath : MonoBehaviour
{
    [Header("Yol Ayarları")]
    [Tooltip("Düşman bu yolun başına spawn olur, sona doğru ilerler.")]
    public string pathId = "path_main";

    [Tooltip("Düşman bu yolda ne kadar hızlı ilerler. " +
             "0 = EnemyArchetypeConfig.moveSpeed kullanılır.")]
    public float speedOverride = 0f;

    // ── Waypoint Listesi ─────────────────────────────────────────────────

    /// <summary>
    /// Child transform'lardan waypoint listesi üretir.
    /// Sıralama: child index sırasına göre (Unity hiyerarşi sırası).
    /// </summary>
    public List<Vector3> GetWaypoints()
    {
        var points = new List<Vector3>();
        foreach (Transform child in transform)
            points.Add(child.position);
        return points;
    }

    /// <summary>
    /// İlk waypoint — spawn noktası.
    /// </summary>
    public Vector3 SpawnPoint => transform.childCount > 0
        ? transform.GetChild(0).position
        : transform.position;

    /// <summary>
    /// Son waypoint — anchor hedefi.
    /// </summary>
    public Vector3 EndPoint => transform.childCount > 0
        ? transform.GetChild(transform.childCount - 1).position
        : transform.position;

    /// <summary>
    /// Waypoint sayısı.
    /// </summary>
    public int WaypointCount => transform.childCount;

    public bool IsValid => transform.childCount >= 2;

    public float PathDistance
    {
        get
        {
            if (transform.childCount < 2) return 0f;

            float distance = 0f;
            Vector3 previous = transform.GetChild(0).position;
            for (int i = 1; i < transform.childCount; i++)
            {
                Vector3 current = transform.GetChild(i).position;
                distance += Vector3.Distance(previous, current);
                previous = current;
            }

            return distance;
        }
    }

    public void ValidateForAnchorSpawn()
    {
        if (!IsValid)
        {
            Debug.LogWarning($"[AnchorPath] Path '{pathId}' invalid, waypoint count={WaypointCount}.");
            return;
        }

        float distance = PathDistance;
        if (distance < 15f)
            Debug.LogWarning($"[AnchorPath] Path too short, enemies may instantly hit anchor. pathId={pathId} distance={distance:F1}");
        else
            Debug.Log($"[AnchorPath] Path valid. pathId={pathId} waypointCount={WaypointCount} distance={distance:F1}");
    }

    // ── Editor Gizmo ─────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (transform.childCount < 2) return;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.85f);

        Vector3 prev = transform.GetChild(0).position;
        for (int i = 1; i < transform.childCount; i++)
        {
            Vector3 curr = transform.GetChild(i).position;
            Gizmos.DrawLine(prev, curr);

            // Ok ucu — yön göstergesi
            Vector3 dir = (curr - prev).normalized;
            Vector3 mid = Vector3.Lerp(prev, curr, 0.55f);
            Gizmos.DrawSphere(mid, 0.18f);

            prev = curr;
        }

        // Spawn noktası: yeşil
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.GetChild(0).position, 0.4f);

        // Bitiş noktası: kırmızı
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.GetChild(transform.childCount - 1).position, 0.4f);

        // Path adı
        UnityEditor.Handles.Label(transform.GetChild(0).position + Vector3.up * 0.6f,
            $"[{pathId}] start");
    }
#endif
}
