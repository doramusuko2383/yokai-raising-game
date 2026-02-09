using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIPentagramDrawer : MonoBehaviour
{
    [SerializeField] UILineRenderer[] lines = new UILineRenderer[5];
    [SerializeField] float radius = 220f;

    static readonly int[] StarOrder = { 0, 2, 4, 1, 3, 0 };
    static readonly Color GlowGold = new Color(1.0f, 0.84f, 0.0f, 1.0f);
    const float SizeMultiplier = 2.0f;
    const float ThicknessMultiplier = 3.0f;
    float[] baseThickness;

    public void SetProgress(float progress)
    {
        if (lines == null || lines.Length < 5)
            return;

        EnsureLineSettings();

        float clamped = Mathf.Clamp01(progress);
        float scaled = clamped * 5f;
        int fullSegments = Mathf.FloorToInt(scaled);
        float partial = scaled - fullSegments;

        Vector2[] points = BuildOuterPoints();

        for (int segment = 0; segment < 5; segment++)
        {
            UILineRenderer line = lines[segment];
            if (line == null)
                continue;

            Vector2 start = points[StarOrder[segment]];
            Vector2 end = points[StarOrder[segment + 1]];

            if (segment < fullSegments)
            {
                SetLine(line, start, end, true);
            }
            else if (segment == fullSegments && partial > 0f)
            {
                Vector2 current = Vector2.Lerp(start, end, partial);
                SetLine(line, start, current, true);
            }
            else
            {
                SetLine(line, start, start, false);
            }
        }
    }

    Vector2[] BuildOuterPoints()
    {
        Vector2 origin = GetOrigin();
        var points = new Vector2[5];

        for (int i = 0; i < 5; i++)
        {
            float deg = 90f + i * 72f;
            float rad = deg * Mathf.Deg2Rad;
            points[i] = origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius * SizeMultiplier;
        }

        return points;
    }

    Vector2 GetOrigin()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return Vector2.zero;

        return rectTransform.rect.center;
    }

    static void SetLine(UILineRenderer line, Vector2 start, Vector2 end, bool enabled)
    {
        line.enabled = enabled;
        line.Points = new[] { start, end };
        line.SetAllDirty();
    }

    void EnsureLineSettings()
    {
        if (baseThickness == null || baseThickness.Length != lines.Length)
        {
            baseThickness = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == null)
                    continue;

                baseThickness[i] = line.LineThickness;
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null)
                continue;

            float thickness = baseThickness.Length > i && baseThickness[i] > 0f
                ? baseThickness[i]
                : line.LineThickness;
            line.LineThickness = thickness * ThicknessMultiplier;
            line.color = GlowGold;

            if (line.material != null && line.material.HasProperty("_EmissionColor"))
            {
                line.material.EnableKeyword("_EMISSION");
                line.material.SetColor("_EmissionColor", GlowGold);
            }
        }
    }
}
