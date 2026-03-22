using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FOVMesh : MonoBehaviour
{
    public float viewAngle = 60f;
    public float viewDistance = 8f;
    public float height = 3f;
    public int rayCount = 50;

    public LayerMask obstacleMask;

    Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        DrawFOV();
    }

    void DrawFOV()
    {
        float angleStep = viewAngle / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = -viewAngle / 2 + angleStep * i;
            Vector3 dir = DirFromAngle(angle);

            RaycastHit hit;

            Vector3 point;

            if (Physics.Raycast(transform.position, dir, out hit, viewDistance, obstacleMask))
            {
                point = hit.point;
            }
            else
            {
                point = transform.position + dir * viewDistance;
            }

            // 위/아래 두 점 생성 (3D 형태)
            vertices[i + 1] = transform.InverseTransformPoint(point);
        }

        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    Vector3 DirFromAngle(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return transform.forward * Mathf.Cos(rad) + transform.right * Mathf.Sin(rad);
    }
}