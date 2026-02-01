using System.Collections;
using UnityEngine;
using UnityEngine.UI.Extensions; // UI Extensions

public class UIPentagramDrawer : MonoBehaviour
{
    [Header("UI Lines (5 segments)")]
    public UILineRenderer[] lines = new UILineRenderer[5];

    [Header("Shape")]
    public float radius = 220f;          // 五芒星の外半径（ピクセル）
    public float lineThickness = 12f;    // 見える太さ
    public Color lineColor = Color.white;

    [Header("Animation")]
    public float reverseDuration = 0.25f;
    public float completeFlashDuration = 0.20f;

    float _progress01;
    Coroutine _reverseCo;
    Coroutine _flashCo;

    // 五芒星（外側5点）を作る：1つ飛ばしで結ぶと星になる
    // 頂点順: 0→2→4→1→3→0 の5線分
    static readonly int[] STAR_ORDER = { 0, 2, 4, 1, 3, 0 };

    Vector2[] _outer5; // 半径適用済みの外側5点

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

        // 上を0番にしたいので 90度開始（UI座標：上が+Y）
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

        for (int i = 0; i < lines.Length; i++)
        {
            var lr = lines[i];
            if (lr == null) continue;

            lr.RelativeSize = false; // ピクセルで扱う
            lr.LineList = true;      // 点ペアを線分として描画
            lr.LineThickness = lineThickness;
            lr.color = lineColor;

            // 2点（=1線分）を必ず持たせる
            lr.Points = new Vector2[2] { Vector2.zero, Vector2.zero };
            lr.SetAllDirty();
        }
    }

    // 0..1 の進捗で5線分を順に表示
    public void SetProgress(float progress01)
    {
        _progress01 = Mathf.Clamp01(progress01);

        if (lines == null || lines.Length < 5) return;

        float scaled = _progress01 * 5f;
        int full = Mathf.FloorToInt(scaled);     // 完全に出る線分数(0..5)
        float frac = scaled - full;              // 次の線分の途中(0..1)

        for (int seg = 0; seg < 5; seg++)
        {
            var lr = lines[seg];
            if (lr == null) continue;

            // この線分の始点終点（五芒星の順番で）
            Vector2 a = _outer5[STAR_ORDER[seg]];
            Vector2 b = _outer5[STAR_ORDER[seg + 1]];

            if (seg < full)
            {
                // 全部表示
                lr.enabled = true;
                lr.Points = new Vector2[2] { a, b };
                lr.color = lineColor;
            }
            else if (seg == full && frac > 0f)
            {
                // 途中まで表示
                lr.enabled = true;
                Vector2 mid = Vector2.Lerp(a, b, frac);
                lr.Points = new Vector2[2] { a, mid };
                lr.color = lineColor;
            }
            else
            {
                // 非表示（線分長0にしてもいいが、enabled切るのが確実）
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
        _reverseCo = null;
    }

    public void PlayCompleteFlash()
    {
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        // 一瞬だけ明るく（アルファ上げ）
        Color c1 = lineColor;
        Color c2 = lineColor;
        c2.a = 1f;

        float t = 0f;
        while (t < completeFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = (completeFlashDuration <= 0f) ? 1f : (t / completeFlashDuration);

            // 前半で強く、後半で戻す感じ
            float a = Mathf.Lerp(c2.a, c1.a, k);

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

        // 最終戻し
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null || !lines[i].enabled) continue;
            lines[i].color = lineColor;
            lines[i].SetAllDirty();
        }

        _flashCo = null;
    }
}