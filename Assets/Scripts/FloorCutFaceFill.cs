using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Fills the exposed floor cut face without moving the lens or changing physics.
[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
public sealed class FloorCutFaceFill : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Collider floor;
    [SerializeField] private Color faceColor = new Color(147f / 255f, 117f / 255f, 132f / 255f, 1f);
    [SerializeField] private Light sceneLight;
    [SerializeField] private bool followSceneLighting;
    private float initialLightIntensity;
    private Mesh mesh;
    private Material material;
    private MeshRenderer face;
    private readonly List<Vector3> polygon = new List<Vector3>(6);
    private readonly List<Vector3> clipped = new List<Vector3>(6);
    private readonly List<int> triangles = new List<int>(12);

    private void Awake()
    {
        initialLightIntensity = sceneLight != null ? sceneLight.intensity : 1f;
        mesh = new Mesh { name = "Floor cut face fill" };
        mesh.MarkDynamic();
        var filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        face = gameObject.AddComponent<MeshRenderer>();
        face.shadowCastingMode = ShadowCastingMode.Off;
        face.receiveShadows = false;
        face.allowOcclusionWhenDynamic = false;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", faceColor);
        material.SetFloat("_Cull", 0f);
        face.sharedMaterial = material;
    }
    private void LateUpdate()
    {
        if (viewCamera == null || floor == null || !floor.enabled) return;
        mesh.Clear(); polygon.Clear(); clipped.Clear(); triangles.Clear();
        float depth = viewCamera.nearClipPlane + 0.02f;
        polygon.Add(viewCamera.ViewportToWorldPoint(new Vector3(0,0,depth)));
        polygon.Add(viewCamera.ViewportToWorldPoint(new Vector3(0,1,depth)));
        polygon.Add(viewCamera.ViewportToWorldPoint(new Vector3(1,1,depth)));
        polygon.Add(viewCamera.ViewportToWorldPoint(new Vector3(1,0,depth)));
        float surface = floor.bounds.max.y - 0.005f;
        Vector3 previous = polygon[polygon.Count - 1];
        bool previousInside = previous.y <= surface;
        foreach(Vector3 current in polygon)
        {
            bool inside = current.y <= surface;
            if (inside != previousInside)
                clipped.Add(Vector3.Lerp(previous, current, (surface - previous.y) / (current.y - previous.y)));
            if (inside) clipped.Add(current);
            previous = current; previousInside = inside;
        }
        for(int i=0;i<clipped.Count;i++)clipped[i]=transform.InverseTransformPoint(clipped[i]);
        for(int i=1;i<clipped.Count-1;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}
        mesh.SetVertices(clipped);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        float brightness = followSceneLighting && sceneLight != null
            ? Mathf.Clamp(sceneLight.intensity / Mathf.Max(0.001f, initialLightIntensity), 0.08f, 1f)
            : 1f;
        Color color = faceColor * brightness;
        color.a = 1f;
        material.SetColor("_BaseColor",color);
    }
    private void OnDisable(){if(face!=null)face.enabled=false;}
    private void OnEnable(){if(face!=null)face.enabled=true;}
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
