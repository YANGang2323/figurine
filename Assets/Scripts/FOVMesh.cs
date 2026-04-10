using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class FOVMesh : MonoBehaviour
{
    public float viewAngle = 60f;
    public float viewDistance = 8f;

    public int horizontalRayCount = 50;
    public int verticalRayCount = 25;

    public float verticalAngle = 45f;

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
        Vector3 origin = transform.position;

        int hCount = horizontalRayCount;
        int vCount = verticalRayCount;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // 🔥 꼭짓점 (CCTV 위치)
        vertices.Add(transform.InverseTransformPoint(origin));

        // 🔥 Ray로 점 생성
        for (int y = 0; y <= vCount; y++)
        {
            float vAngle = Mathf.Lerp(-verticalAngle / 2, verticalAngle / 2, (float)y / vCount);

            for (int x = 0; x <= hCount; x++)
            {
                float hAngle = Mathf.Lerp(-viewAngle / 2, viewAngle / 2, (float)x / hCount);

                Vector3 dir = Quaternion.Euler(vAngle, hAngle, 0) * transform.forward;

                RaycastHit hit;
                Vector3 point;

                if (Physics.Raycast(origin, dir, out hit, viewDistance, obstacleMask))
                {
                    point = hit.point; // 🔥 벽에서 멈춤
                }
                else
                {
                    point = origin + dir * viewDistance;
                }

                vertices.Add(transform.InverseTransformPoint(point));

                Debug.DrawRay(origin, dir * viewDistance, Color.red);
            }
        }

        int row = hCount + 1;

        // 🔥 삼각형 연결 (핵심)
        for (int y = 0; y < vCount; y++)
        {
            for (int x = 0; x < hCount; x++)
            {
                int current = y * row + x + 1;
                int next = current + row;

                triangles.Add(0);
                triangles.Add(current);
                triangles.Add(current + 1);

                triangles.Add(0);
                triangles.Add(current);
                triangles.Add(next);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}