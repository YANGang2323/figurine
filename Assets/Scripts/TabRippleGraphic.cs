using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TabRippleGraphic : MaskableGraphic
{
    void Update() => SetVerticesDirty();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        for (int ring = 0; ring < 3; ring++)
        {
            float phase = Mathf.Repeat(Time.unscaledTime * 0.65f + ring / 3f, 1);
            float outer = radius * Mathf.Lerp(0.5f, 1f, phase);
            Color tint = color; tint.a *= 1 - phase;
            for (int i = 0; i < 64; i++)
            {
                int start = vh.currentVertCount;
                float a = i * Mathf.PI * 2 / 64, b = (i + 1) * Mathf.PI * 2 / 64;
                vh.AddVert(new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (outer - 2), tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * outer, tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(b), Mathf.Sin(b)) * outer, tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(b), Mathf.Sin(b)) * (outer - 2), tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
