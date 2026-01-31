using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yokai;

public class MagicCircleSwipeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField]
    float chargeDurationSeconds = 1.6f;

    [SerializeField]
    float guideFadeDuration = 0.25f;

    [SerializeField]
    RectTransform circleRect;

    [SerializeField]
    Image magicCircleGuide;

    [Header("Pentagram")]
    [SerializeField]
    PentagramDrawer pentagramDrawer;

    [Header("Trail (Legacy)")]
    [SerializeField]
    LineRenderer swipeTrail;

    [SerializeField]
    Color trailColor = new Color(0.8f, 0.95f, 1f, 0.9f);

    [SerializeField]
    float trailWidth = 8f;

    [SerializeField]
    float trailFadeDuration = 0.25f;

    [SerializeField]
    float trailMinPointDistance = 2f;

    [Header("Success")]
    [SerializeField]
    float successPulseScale = 1.1f;

    [SerializeField]
    float successPulseDuration = 0.2f;

    [SerializeField]
    MagicCircleActivator magicCircleActivator;

    [SerializeField]
    YokaiStateController stateController;

    bool isTracking;
    bool isCompleted;
    bool hasAppliedPurify;
    float pressStartTime;
    bool wasPurifying;
    CanvasGroup guideCanvasGroup;
    Coroutine guideFadeCoroutine;
    Coroutine trailFadeCoroutine;
    Coroutine successPulseCoroutine;
    readonly List<Vector3> trailPoints = new List<Vector3>();
    bool hasWarnedMissingStateController;

    void OnEnable()
    {
        BindStateController(ResolveStateController());
        CurrentYokaiContext.CurrentChanged += HandleCurrentYokaiChanged;
        ResolveMagicCircleActivator();
        SetupGuideCanvas();
        SetupTrailRenderer();
        ResetChargeVisuals(immediate: true);
        ToggleGuide(IsPurifying(), immediate: true);
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= HandleCurrentYokaiChanged;
        BindStateController(null);
    }

    void Update()
    {
        bool isPurifying = IsPurifying();
        if (isPurifying != wasPurifying)
        {
            ToggleGuide(isPurifying, immediate: false);
            wasPurifying = isPurifying;
            if (!isPurifying)
            {
                isTracking = false;
                isCompleted = false;
                ResetChargeVisuals(immediate: true);
            }
        }

        if (isTracking)
            UpdateChargeProgress();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPurifying())
        {
            return;
        }

        isTracking = true;
        isCompleted = false;
        hasAppliedPurify = false;
        pressStartTime = Time.unscaledTime;
        ResetChargeVisuals(immediate: true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking || isCompleted)
            return;

        UpdateChargeProgress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isTracking)
            return;

        if (!isCompleted)
            CancelSwipe("入力中断");
    }

    bool IsPurifying()
    {
        if (stateController == null)
        {
            WarnMissingStateController();
            return false;
        }

        return stateController.currentState == YokaiState.Purifying;
    }

    void BindStateController(YokaiStateController controller)
    {
        stateController = controller;
    }

    void HandleCurrentYokaiChanged(GameObject activeYokai)
    {
        BindStateController(ResolveStateController());
        wasPurifying = stateController != null && stateController.currentState == YokaiState.Purifying;
        ToggleGuide(wasPurifying, immediate: true);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);
    }

    void ResolveMagicCircleActivator()
    {
        if (magicCircleActivator != null)
            return;

        magicCircleActivator = FindObjectOfType<MagicCircleActivator>(true);
    }

    void HandleSwipeSuccess()
    {
        if (hasAppliedPurify)
            return;

        ResetChargeVisuals(immediate: true);
        PulseMagicCircle();

        if (magicCircleActivator != null)
        {
            magicCircleActivator.RequestSuccessEffect();
            magicCircleActivator.RequestSuccess();
        }
        else if (stateController != null)
        {
            stateController.NotifyPurifySucceeded();
        }
        else
        {
            WarnMissingStateController();
            return;
        }

        hasAppliedPurify = true;
        ToggleGuide(false, immediate: false);
    }

    void CancelSwipe(string reason)
    {
        if (!isTracking)
            return;

        isTracking = false;
        isCompleted = false;
        ReverseChargeVisuals();
        if (IsPurifying())
            stateController.CancelPurifying("SwipeCancelled");
        ToggleGuide(false, immediate: false);
    }

    void UpdateChargeProgress()
    {
        float duration = Mathf.Max(0.1f, chargeDurationSeconds);
        float elapsed = Time.unscaledTime - pressStartTime;
        float progress = Mathf.Clamp01(elapsed / duration);

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(progress);

        if (progress >= 1f && !isCompleted)
        {
            isCompleted = true;
            isTracking = false;
            HandleSwipeSuccess();
            if (pentagramDrawer != null)
                pentagramDrawer.PlayCompleteFlash();
        }
    }

    void ToggleGuide(bool isVisible)
    {
        ToggleGuide(isVisible, immediate: false);
    }

    void ToggleGuide(bool isVisible, bool immediate)
    {
        if (magicCircleGuide == null)
        {
            var guideObject = GameObject.Find("MagicCircleGuide");
            if (guideObject != null)
                magicCircleGuide = guideObject.GetComponent<Image>();
        }

        if (magicCircleGuide == null)
            return;

        SetupGuideCanvas();

        if (guideCanvasGroup == null)
        {
            magicCircleGuide.gameObject.SetActive(isVisible);
            return;
        }

        if (guideFadeCoroutine != null)
            StopCoroutine(guideFadeCoroutine);

        if (!CanStartCoroutine())
        {
            guideCanvasGroup.alpha = isVisible ? 1f : 0f;
            magicCircleGuide.gameObject.SetActive(isVisible);
            return;
        }

        guideFadeCoroutine = StartCoroutine(FadeGuide(isVisible, immediate));
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[PURIFY] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }

    void SetupGuideCanvas()
    {
        if (magicCircleGuide == null)
            return;

        if (guideCanvasGroup != null)
            return;

        guideCanvasGroup = magicCircleGuide.GetComponent<CanvasGroup>();
        if (guideCanvasGroup == null)
            guideCanvasGroup = magicCircleGuide.gameObject.AddComponent<CanvasGroup>();
    }

    void SetupTrailRenderer()
    {
        if (swipeTrail == null)
            swipeTrail = GetComponent<LineRenderer>();

        if (swipeTrail == null)
            swipeTrail = gameObject.AddComponent<LineRenderer>();

        swipeTrail.useWorldSpace = false;
        swipeTrail.alignment = LineAlignment.View;
        swipeTrail.textureMode = LineTextureMode.Stretch;
        swipeTrail.widthMultiplier = trailWidth;
        swipeTrail.positionCount = 0;
        swipeTrail.enabled = false;

        if (swipeTrail.material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                swipeTrail.material = new Material(shader);
        }

        ApplyTrailColor(1f);
    }

    void StartTrail(Vector2 screenPosition, Camera eventCamera)
    {
        SetupTrailRenderer();
        if (swipeTrail == null)
            return;

        if (trailFadeCoroutine != null)
            StopCoroutine(trailFadeCoroutine);

        trailPoints.Clear();
        swipeTrail.positionCount = 0;
        swipeTrail.enabled = true;
        ApplyTrailColor(1f);
        AddTrailPoint(screenPosition, eventCamera, force: true);
    }

    void AddTrailPoint(Vector2 screenPosition, Camera eventCamera, bool force = false)
    {
        if (swipeTrail == null || !swipeTrail.enabled)
            return;

        Vector2 localPoint = GetLocalPoint(screenPosition, eventCamera);
        Vector3 point = new Vector3(localPoint.x, localPoint.y, 0f);

        if (!force && trailPoints.Count > 0)
        {
            float distance = Vector3.Distance(trailPoints[trailPoints.Count - 1], point);
            if (distance < trailMinPointDistance)
                return;
        }

        trailPoints.Add(point);
        swipeTrail.positionCount = trailPoints.Count;
        swipeTrail.SetPositions(trailPoints.ToArray());
    }

    void ClearTrail(bool immediate)
    {
        if (swipeTrail == null)
            return;

        if (!swipeTrail.enabled && swipeTrail.positionCount == 0)
            return;

        if (trailFadeCoroutine != null)
            StopCoroutine(trailFadeCoroutine);

        if (immediate)
        {
            swipeTrail.positionCount = 0;
            swipeTrail.enabled = false;
            ApplyTrailColor(1f);
            return;
        }

        if (!CanStartCoroutine())
        {
            swipeTrail.positionCount = 0;
            swipeTrail.enabled = false;
            ApplyTrailColor(1f);
            return;
        }

        trailFadeCoroutine = StartCoroutine(FadeTrail());
    }

    void ResetChargeVisuals(bool immediate)
    {
        if (pentagramDrawer != null)
        {
            if (immediate)
                pentagramDrawer.ResetImmediate();
            else
                pentagramDrawer.SetProgress(0f);
        }

        ClearTrail(immediate: true);
    }

    void ReverseChargeVisuals()
    {
        if (pentagramDrawer != null)
        {
            pentagramDrawer.ReverseAndClear();
            return;
        }

        ClearTrail(immediate: false);
    }

    System.Collections.IEnumerator FadeTrail()
    {
        float duration = Mathf.Clamp(trailFadeDuration, 0.2f, 0.3f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyTrailColor(1f - t);
            yield return null;
        }

        swipeTrail.positionCount = 0;
        swipeTrail.enabled = false;
        ApplyTrailColor(1f);
        trailFadeCoroutine = null;
    }

    void ApplyTrailColor(float alphaMultiplier)
    {
        if (swipeTrail == null)
            return;

        Color color = trailColor;
        color.a *= Mathf.Clamp01(alphaMultiplier);
        swipeTrail.startColor = color;
        swipeTrail.endColor = color;
    }

    void PulseMagicCircle()
    {
        if (successPulseCoroutine != null)
            StopCoroutine(successPulseCoroutine);

        Transform target = magicCircleGuide != null ? magicCircleGuide.transform : circleRect;
        if (target == null)
            return;

        if (!CanStartCoroutine())
            return;

        successPulseCoroutine = StartCoroutine(PulseScale(target));
    }

    bool CanStartCoroutine()
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    System.Collections.IEnumerator PulseScale(Transform target)
    {
        Vector3 startScale = target.localScale;
        Vector3 peakScale = startScale * Mathf.Max(1f, successPulseScale);
        float duration = Mathf.Max(0.05f, successPulseDuration);
        float half = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(startScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(peakScale, startScale, t);
            yield return null;
        }

        target.localScale = startScale;
        successPulseCoroutine = null;
    }

    System.Collections.IEnumerator FadeGuide(bool isVisible, bool immediate)
    {
        magicCircleGuide.gameObject.SetActive(true);

        float startAlpha = guideCanvasGroup.alpha;
        float targetAlpha = isVisible ? 1f : 0f;
        float duration = immediate ? 0f : Mathf.Max(0.01f, guideFadeDuration);
        float elapsed = 0f;

        if (duration <= 0f)
        {
            guideCanvasGroup.alpha = targetAlpha;
            if (!isVisible)
                magicCircleGuide.gameObject.SetActive(false);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            guideCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        guideCanvasGroup.alpha = targetAlpha;
        if (!isVisible)
            magicCircleGuide.gameObject.SetActive(false);
    }
}
