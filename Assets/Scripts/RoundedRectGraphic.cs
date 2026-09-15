using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private Color borderColor = Color.clear;
    [SerializeField, Range(0f, 64f)] private float cornerRadius = 16f;
    [SerializeField, Range(0f, 12f)] private float borderWidth = 0f;
    [SerializeField, Range(2, 16)] private int cornerSegments = 8;

    public Color FillColor
    {
        get => fillColor;
        set { fillColor = value; SetVerticesDirty(); }
    }

    public Color BorderColor
    {
        get => borderColor;
        set { borderColor = value; SetVerticesDirty(); }
    }

    public float CornerRadius
    {
        get => cornerRadius;
        set { cornerRadius = Mathf.Max(0f, value); SetVerticesDirty(); }
    }

    public float BorderWidth
    {
        get => borderWidth;
        set { borderWidth = Mathf.Max(0f, value); SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float outerRadius = Mathf.Clamp(cornerRadius, 0f, maxRadius);
        float actualBorder = Mathf.Clamp(borderWidth, 0f, Mathf.Min(rect.width, rect.height) * 0.25f);
        float innerRadius = Mathf.Max(0f, outerRadius - actualBorder);
        int segments = Mathf.Clamp(cornerSegments, 2, 16);

        Vector2[] outer = BuildRoundedRect(rect, outerRadius, segments);
        Vector2[] inner = BuildRoundedRect(Inset(rect, actualBorder), innerRadius, segments);

        UIVertex centerVertex = UIVertex.simpleVert;
        centerVertex.position = rect.center;
        centerVertex.color = fillColor;
        vh.AddVert(centerVertex);

        int innerStart = vh.currentVertCount;
        for (int i = 0; i < inner.Length; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = inner[i];
            v.color = fillColor;
            vh.AddVert(v);
        }

        int outerStart = vh.currentVertCount;
        for (int i = 0; i < outer.Length; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = outer[i];
            v.color = borderColor;
            vh.AddVert(v);
        }

        for (int i = 0; i < inner.Length; i++)
        {
            int next = (i + 1) % inner.Length;
            vh.AddTriangle(0, innerStart + i, innerStart + next);
        }

        if (actualBorder > 0.01f && borderColor.a > 0.001f)
        {
            for (int i = 0; i < outer.Length; i++)
            {
                int next = (i + 1) % outer.Length;
                vh.AddTriangle(outerStart + i, outerStart + next, innerStart + next);
                vh.AddTriangle(outerStart + i, innerStart + next, innerStart + i);
            }
        }
    }

    private Vector2[] BuildRoundedRect(Rect rect, float radius, int segments)
    {
        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;
        float r = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);

        var points = new List<Vector2>(segments * 4 + 4);

        AddArc(points, new Vector2(right - r, top - r), r, 0f, 90f, segments);
        AddArc(points, new Vector2(left + r, top - r), r, 90f, 180f, segments);
        AddArc(points, new Vector2(left + r, bottom + r), r, 180f, 270f, segments);
        AddArc(points, new Vector2(right - r, bottom + r), r, 270f, 360f, segments);

        return points.ToArray();
    }

    private static void AddArc(
        List<Vector2> points,
        Vector2 center,
        float radius,
        float startDegrees,
        float endDegrees,
        int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private static Rect Inset(Rect rect, float amount)
    {
        return new Rect(
            rect.xMin + amount,
            rect.yMin + amount,
            Mathf.Max(0f, rect.width - amount * 2f),
            Mathf.Max(0f, rect.height - amount * 2f)
        );
    }
}
