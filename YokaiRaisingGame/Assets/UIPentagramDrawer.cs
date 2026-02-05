using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIPentagramDrawer : MonoBehaviour
{
    [Header("UI Lines (5 segments)")]
    [SerializeField] UILineRenderer[] lines = new UILineRenderer[5];

    [Header("Shape")]
    [SerializeField] float radius = 220f;

    static readonly int[] StarOrder = { 0, 2, 4, 1, 3, 0 };

    public void SetProgress(float progress)
    {
        if (lines == null || lines.Length < 5)
            return;

        float clamped = Mathf.Clamp01(progress);
        float scaled = clamped * 5f;
        int fullSegments = Mathf.FloorToInt(scaled);
        float partial = scaled - fullSegments;

        Vector2[] points = BuildOuterPoints();

        for (int segment = 0; segment < 5; segment++)
        {
            var line = lines[segment];
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
            points[i] = origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        return points;
    }

    Vector2 GetOrigin()
    {
        RectTransform rectTransform = transform.parent as RectTransform;
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
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
}
