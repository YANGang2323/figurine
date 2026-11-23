using UnityEngine;

// Non-physical volume; never changes collisions or interaction raycasts.
public class AnalyticsZone : MonoBehaviour
{
    public string zoneID, zoneName;
    public Vector3 size = new Vector3(12, 12, 12);
    public int priority;
    public bool Contains(Vector3 position)
    {
        var p = transform.InverseTransformPoint(position);
        return Mathf.Abs(p.x) <= size.x / 2 && Mathf.Abs(p.y) <= size.y / 2 && Mathf.Abs(p.z) <= size.z / 2;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix; Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
