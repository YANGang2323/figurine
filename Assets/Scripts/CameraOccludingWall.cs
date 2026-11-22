using UnityEngine;
using UnityEngine.Rendering;

// Attach only to wall renderers or wall groups. Floors and props stay opaque.
[DisallowMultipleComponent]
public sealed class CameraOccludingWall : MonoBehaviour
{
    [SerializeField] private Renderer[] wallRenderers;
    [SerializeField, Range(0f, 1f)] private float occludedAlpha = 0.08f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;
    private Material[][] originals;
    private Material[][] transparent;
    private ShadowCastingMode[] originalShadows;
    private float visibility = 1f;
    public float Visibility => visibility;

    private void Awake()
    {
        if (wallRenderers == null || wallRenderers.Length == 0)
            wallRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Reset() => wallRenderers = GetComponentsInChildren<Renderer>(true);

    public bool OccludesPlayer(Camera camera, Transform player)
    {
        if (!isActiveAndEnabled || wallRenderers == null) return false;
        foreach (Renderer renderer in wallRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            // Test the wall's actual oriented mesh bounds, not its expanded world
            // AABB, which sweeps across the foreground while the room rotates.
            for (int sample = 0; sample < 3; sample++)
            {
                Vector3 target = player.position + Vector3.up * (0.6f + sample * 0.8f);
                Vector3 viewport = camera.WorldToViewportPoint(target);
                if (viewport.z <= camera.nearClipPlane) continue;
                Ray ray = camera.ViewportPointToRay(viewport);
                float length = Vector3.Dot(target - ray.origin, ray.direction);
                Vector3 localStart = renderer.transform.InverseTransformPoint(ray.origin);
                Vector3 localEnd = renderer.transform.InverseTransformPoint(ray.origin + ray.direction * length);
                Vector3 localDelta = localEnd - localStart;
                float localLength = localDelta.magnitude;
                if (localLength <= 0.001f) continue;
                Bounds bounds = filter.sharedMesh.bounds;
                bounds.Expand(0.002f);
                if (bounds.IntersectRay(new Ray(localStart, localDelta / localLength), out float hit) &&
                    hit >= 0f && hit < localLength - 0.02f) return true;
            }
        }
        return false;
    }

    public bool Intersects(Plane[] planes)
    {
        if (!isActiveAndEnabled || wallRenderers == null) return false;
        foreach (Renderer renderer in wallRenderers)
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) return true;
        return false;
    }

    public void SetOccluded(bool occluded)
    {
        float next = Mathf.MoveTowards(visibility, occluded ? occludedAlpha : 1f,
            Time.unscaledDeltaTime / fadeDuration);
        if (Mathf.Approximately(next, visibility)) return;
        EnsureMaterials();
        visibility = next;
        for (int i = 0; i < wallRenderers.Length; i++)
        {
            Renderer renderer = wallRenderers[i];
            if (renderer == null) continue;
            if (visibility >= 1f)
            {
                renderer.sharedMaterials = originals[i];
                renderer.shadowCastingMode = originalShadows[i];
                continue;
            }
            renderer.sharedMaterials = transparent[i];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            for (int j = 0; j < transparent[i].Length; j++)
            {
                Material material = transparent[i][j];
                Material original = originals[i][j];
                if (material == null || original == null) continue;
                string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                if (!material.HasProperty(colorProperty)) continue;
                Color color = original.GetColor(colorProperty);
                color.a *= visibility;
                material.SetColor(colorProperty, color);
            }
        }
    }

    private void EnsureMaterials()
    {
        if (originals != null) return;
        if (wallRenderers == null || wallRenderers.Length == 0)
            wallRenderers = GetComponentsInChildren<Renderer>(true);
        originals = new Material[wallRenderers.Length][];
        transparent = new Material[wallRenderers.Length][];
        originalShadows = new ShadowCastingMode[wallRenderers.Length];
        for (int i = 0; i < wallRenderers.Length; i++)
        {
            Renderer renderer = wallRenderers[i];
            if (renderer == null) continue;
            originalShadows[i] = renderer.shadowCastingMode;
            originals[i] = renderer.sharedMaterials;
            transparent[i] = new Material[originals[i].Length];
            for (int j = 0; j < originals[i].Length; j++)
            {
                Material source = originals[i][j];
                if (source == null) continue;
                Material copy = new Material(source) { name = source.name + " (Camera Fade)" };
                // URP Lit/Simple Lit; already transparent wall shaders keep their blend mode.
                if (copy.HasProperty("_Surface"))
                {
                    copy.SetFloat("_Surface", 1f);
                    copy.SetFloat("_Blend", 0f);
                    copy.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    copy.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    if (copy.HasProperty("_SrcBlendAlpha")) copy.SetFloat("_SrcBlendAlpha", 1f);
                    if (copy.HasProperty("_DstBlendAlpha")) copy.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    copy.SetFloat("_ZWrite", 0f);
                    copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    copy.DisableKeyword("_ALPHATEST_ON");
                    copy.SetOverrideTag("RenderType", "Transparent");
                    copy.renderQueue = (int)RenderQueue.Transparent;
                    copy.SetShaderPassEnabled("DepthOnly", false);
                    copy.SetShaderPassEnabled("ShadowCaster", false);
                }
                transparent[i][j] = copy;
            }
        }
    }

    public void Restore()
    {
        visibility = 1f;
        if (originals == null) return;
        for (int i = 0; i < wallRenderers.Length; i++)
            if (wallRenderers[i] != null)
            {
                wallRenderers[i].sharedMaterials = originals[i];
                wallRenderers[i].shadowCastingMode = originalShadows[i];
            }
    }
    private void OnDisable() => Restore();
    private void OnDestroy()
    {
        Restore();
        if (transparent == null) return;
        foreach (Material[] materials in transparent)
            if (materials != null)
                foreach (Material material in materials)
                    if (material != null) Destroy(material);
    }
}
