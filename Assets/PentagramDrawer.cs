using System.Collections;
using UnityEngine;

public class PentagramDrawer : MonoBehaviour
{
    [SerializeField]
    LineRenderer[] lines;

    [SerializeField]
    float reverseDuration = 0.25f;

    [SerializeField]
    float completeFlashDuration = 0.2f;

    [SerializeField]
    float flashIntensity = 2f;

    static readonly Color GlowGold = new Color(1.0f, 0.84f, 0.0f, 1.0f);
    const float SizeMultiplier = 2.0f;
    const float ThicknessMultiplier = 3.0f;

    Vector3[] cachedStarts;
    Vector3[] cachedEnds;
    float[] cachedWidths;
    float currentProgress;
    bool isCached;

    void Awake()
    {
        CacheLinePositions();
        ResetImmediate();
    }

    void OnEnable()
    {
        if (!isCached)
            CacheLinePositions();
    }

    void CacheLinePositions()
    {
        if (lines == null || lines.Length == 0)
            return;

        cachedStarts = new Vector3[lines.Length];
        cachedEnds = new Vector3[lines.Length];
        cachedWidths = new float[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null)
                continue;

            if (line.positionCount < 2)
                line.positionCount = 2;

            cachedStarts[i] = line.GetPosition(0);
            cachedEnds[i] = line.GetPosition(1);
            cachedWidths[i] = line.widthMultiplier;
        }

        isCached = true;
    }

    public void SetProgress(float progress)
    {
        if (lines == null || lines.Length == 0)
            return;

        if (!isCached)
            CacheLinePositions();

        EnsureLineSettings();

        currentProgress = Mathf.Clamp01(progress);
        float segmentProgress = currentProgress * lines.Length;

        for (int i = 0; i < lines.Length; i++)
        {
            float t = Mathf.Clamp01(segmentProgress - i);
            SetLineProgress(i, t);
        }
    }

    void SetLineProgress(int index, float t)
    {
        if (lines == null || index < 0 || index >= lines.Length)
            return;

        var line = lines[index];
        if (line == null)
            return;

        Vector3 start = cachedStarts != null && cachedStarts.Length > index
            ? cachedStarts[index]
            : line.GetPosition(0);
        Vector3 end = cachedEnds != null && cachedEnds.Length > index
            ? cachedEnds[index]
            : line.GetPosition(1);
        Vector3 scaledStart = start * SizeMultiplier;
        Vector3 scaledEnd = end * SizeMultiplier;
        Vector3 current = Vector3.Lerp(scaledStart, scaledEnd, t);

        line.positionCount = 2;
        line.SetPosition(0, scaledStart);
        line.SetPosition(1, current);
        line.enabled = t > 0f;
    }

    public void PlayCompleteFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        float timer = 0f;

        while (timer < completeFlashDuration)
        {
            float t = timer / completeFlashDuration;
            float intensity = Mathf.Lerp(flashIntensity, 1f, t);

            foreach (var line in lines)
            {
                if (line == null || line.material == null)
                    continue;

                if (line.material.HasProperty("_EmissionColor"))
                {
                    Color baseColor = line.startColor;
                    line.material.SetColor("_EmissionColor", baseColor * intensity);
                }
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (var line in lines)
        {
            if (line == null || line.material == null)
                continue;

            if (line.material.HasProperty("_EmissionColor"))
            {
                Color baseColor = line.startColor;
                line.material.SetColor("_EmissionColor", baseColor);
            }
        }
    }

    public void ReverseAndClear()
    {
        StopAllCoroutines();
        StartCoroutine(ReverseCoroutine());
    }

    IEnumerator ReverseCoroutine()
    {
        float startProgress = currentProgress;
        float timer = 0f;
        float duration = Mathf.Max(0.05f, reverseDuration);

        while (timer < duration)
        {
            float t = timer / duration;
            float progress = Mathf.Lerp(startProgress, 0f, t);
            SetProgress(progress);

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetImmediate();
    }

    public void ResetImmediate()
    {
        currentProgress = 0f;

        if (lines == null || lines.Length == 0)
            return;

        if (!isCached)
            CacheLinePositions();

        EnsureLineSettings();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null)
                continue;

            Vector3 start = cachedStarts != null && cachedStarts.Length > i
                ? cachedStarts[i]
                : line.GetPosition(0);
            Vector3 scaledStart = start * SizeMultiplier;
            line.positionCount = 2;
            line.SetPosition(0, scaledStart);
            line.SetPosition(1, scaledStart);
            line.enabled = false;
        }
    }

    void EnsureLineSettings()
    {
        if (lines == null)
            return;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null)
                continue;

            float baseWidth = cachedWidths != null && cachedWidths.Length > i && cachedWidths[i] > 0f
                ? cachedWidths[i]
                : line.widthMultiplier;
            line.widthMultiplier = baseWidth * ThicknessMultiplier;
            line.startColor = GlowGold;
            line.endColor = GlowGold;

            if (line.material != null && line.material.HasProperty("_EmissionColor"))
            {
                line.material.EnableKeyword("_EMISSION");
                line.material.SetColor("_EmissionColor", GlowGold);
            }
        }
    }
}
