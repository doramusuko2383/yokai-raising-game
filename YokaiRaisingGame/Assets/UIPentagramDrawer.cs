using System.Collections;
using UnityEngine;
using UnityEngine.UI.Extensions; // UI Extensions

public class UIPentagramDrawer : MonoBehaviour
{
    [Header("UI Lines (5 segments)")]
    public UILineRenderer[] lines = new UILineRenderer[5];

    [Header("Shape")]
    public float radius = 220f;
    public float lineThickness = 12f;
    public float scale = 1f;
    public Color lineColor = new Color(1.0f, 0.84f, 0.25f, 1f);

    [Header("Animation")]
    public float reverseDuration = 0.25f;
    public float completeFlashDuration = 0.20f;
    public float completeFlashAlpha = 1.2f;
    public float completeFlashThicknessBoost = 3f;

    float _progress01;
    Coroutine _reverseCo;
    Coroutine _flashCo;
    Coroutine _fadeCo;
    bool _suppressRendering;

    // Outer points for pentagram
    static readonly int[] STAR_ORDER = { 0, 2, 4, 1, 3, 0 };

    Vector2[] _outer5;
    float _baseThickness;

    void Awake()
    {
        BuildOuterPoints();
        ApplyStyle();
        SetProgress(0f);
    }

    void OnValidate()
    {
        BuildOuterPoints();
        ApplyStyle();
        SetProgress(_progress01);
    }

    void BuildOuterPoints()
    {
        _outer5 = new Vector2[5];

        for (int i = 0; i < 5; i++)
        {
            float deg = 90f + i * 72f;
            float rad = deg * Mathf.Deg2Rad;
            _outer5[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }
    }

    void ApplyStyle()
    {
        if (lines == null) return;
        _baseThickness = lineThickness;

        for (int i = 0; i < lines.Length; i++)
        {
            var lr = lines[i];
            if (lr == null) continue;

            lr.RelativeSize = false;
            lr.LineList = true;
            lr.LineThickness = lineThickness;
            lr.color = lineColor;
            lr.raycastTarget = false;

            lr.Points = new Vector2[2] { Vector2.zero, Vector2.zero };
            lr.SetAllDirty();
        }
    }

    public void SetProgress(float progress01)
    {
        _progress01 = Mathf.Clamp01(progress01);

        if (lines == null || lines.Length < 5) return;

        float scaled = _progress01 * 5f;
        int full = Mathf.FloorToInt(scaled);
        float frac = scaled - full;

        for (int seg = 0; seg < 5; seg++)
        {
            var lr = lines[seg];
            if (lr == null) continue;

            Vector2 a = _outer5[STAR_ORDER[seg]] * scale;
            Vector2 b = _outer5[STAR_ORDER[seg + 1]] * scale;

            if (seg < full)
            {
                lr.enabled = !_suppressRendering;
                lr.Points = new Vector2[2] { a, b };
                lr.color = lineColor;
            }
            else if (seg == full && frac > 0f)
            {
                lr.enabled = !_suppressRendering;
                Vector2 mid = Vector2.Lerp(a, b, frac);
                lr.Points = new Vector2[2] { a, mid };
                lr.color = lineColor;
            }
            else
            {
                lr.enabled = false;
            }

            lr.SetAllDirty();
        }
    }

    public void ReverseAndClear()
    {
        if (_reverseCo != null) StopCoroutine(_reverseCo);
        _reverseCo = StartCoroutine(CoReverse());
    }

    IEnumerator CoReverse()
    {
        float start = _progress01;
        float t = 0f;

        while (t < reverseDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = (reverseDuration <= 0f) ? 1f : (t / reverseDuration);
            SetProgress(Mathf.Lerp(start, 0f, k));
            yield return null;
        }

        SetProgress(0f);
        _suppressRendering = false;
        _reverseCo = null;
    }

    public void PlayCompleteFlash()
    {
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        Color c1 = lineColor;
        Color c2 = lineColor;
        c2.a = completeFlashAlpha;
        float targetThickness = lineThickness + completeFlashThicknessBoost;

        float t = 0f;
        while (t < completeFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = (completeFlashDuration <= 0f) ? 1f : (t / completeFlashDuration);

            float a = Mathf.Lerp(c2.a, c1.a, k);
            float thickness = Mathf.Lerp(targetThickness, lineThickness, k);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null || !lines[i].enabled) continue;
                var cc = lines[i].color;
                cc.a = a;
                lines[i].color = cc;
                lines[i].LineThickness = thickness;
                lines[i].SetAllDirty();
            }

            yield return null;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null || !lines[i].enabled) continue;
            lines[i].color = lineColor;
            lines[i].LineThickness = _baseThickness;
            lines[i].SetAllDirty();
        }

        _flashCo = null;
    }

    public void FadeOutLines(float duration)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFadeOutLines(duration));
    }

    IEnumerator CoFadeOutLines(float duration)
    {
        _suppressRendering = false;
        float startAlpha = lineColor.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = (duration <= 0f) ? 1f : (t / duration);
            float a = Mathf.Lerp(startAlpha, 0f, k);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null || !lines[i].enabled) continue;
                var cc = lines[i].color;
                cc.a = a;
                lines[i].color = cc;
                lines[i].SetAllDirty();
            }

            yield return null;
        }

        _suppressRendering = true;
        _fadeCo = null;
    }

    public void ClearSuppressRendering()
    {
        _suppressRendering = false;
    }
}
