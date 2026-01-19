using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Yokai;

public class MagicCircleSwipeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField]
    float requiredAngle = 270f;

    [SerializeField]
    float minRadiusRatio = 0.55f;

    [SerializeField]
    float maxRadiusRatio = 1.05f;

    [SerializeField]
    int minimumSamples = 12;

    [SerializeField]
    float maxAngleJump = 70f;

    [SerializeField]
    float minAngleDelta = 0.5f;

    [SerializeField]
    float minTravelDistanceRatio = 0.75f;

    [SerializeField]
    float timeLimitSeconds = 2.5f;

    [SerializeField]
    float guideFadeDuration = 0.25f;

    [SerializeField]
    RectTransform circleRect;

    [SerializeField]
    Image magicCircleGuide;

    [Header("Trail")]
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
    YokaiStateController stateController;

    [FormerlySerializedAs("kegareManager")]
    [SerializeField]
    PurityController purityController;

    bool isTracking;
    bool isCompleted;
    bool hasAppliedPurify;
    bool hasInvalidRadius;
    bool hasInvalidPath;
    float totalAngle;
    float totalTravelDistance;
    float previousAngle;
    Vector2 previousPoint;
    float directionSign;
    bool hasDirection;
    int sampleCount;
    float swipeStartTime;
    bool wasPurifying;
    CanvasGroup guideCanvasGroup;
    Coroutine guideFadeCoroutine;
    Coroutine trailFadeCoroutine;
    Coroutine successPulseCoroutine;
    readonly List<Vector3> trailPoints = new List<Vector3>();

    void OnEnable()
    {
        SetupGuideCanvas();
        SetupTrailRenderer();
        ClearTrail(immediate: true);
        ToggleGuide(IsPurifying(), immediate: true);
    }

    void OnDisable()
    {
        //ToggleGuide(false, immediate: true);
        //ClearTrail(immediate: true);
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
                ClearTrail(immediate: true);
            }
        }

        if (isTracking && timeLimitSeconds > 0f)
        {
            float elapsed = Time.unscaledTime - swipeStartTime;
            if (elapsed > timeLimitSeconds)
            {
                CancelSwipe("時間切れ");
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPurifying())
        {
            Debug.Log("[PURIFY] おきよめ失敗 reason=状態不一致");
            return;
        }

        isTracking = true;
        isCompleted = false;
        hasAppliedPurify = false;
        hasInvalidRadius = false;
        hasInvalidPath = false;
        totalAngle = 0f;
        totalTravelDistance = 0f;
        sampleCount = 0;
        previousAngle = GetAngleFromCenter(eventData.position, eventData.pressEventCamera);
        previousPoint = GetLocalPoint(eventData.position, eventData.pressEventCamera);
        directionSign = 0f;
        hasDirection = false;
        swipeStartTime = Time.unscaledTime;

        Debug.Log("[PURIFY] Swipe start");
        StartTrail(eventData.position, eventData.pressEventCamera);

        if (!IsWithinRadius(eventData.position, eventData.pressEventCamera))
        {
            CancelSwipe("円外開始");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking || isCompleted)
            return;

        AddTrailPoint(eventData.position, eventData.pressEventCamera);

        float angle = GetAngleFromCenter(eventData.position, eventData.pressEventCamera);
        Vector2 localPoint = GetLocalPoint(eventData.position, eventData.pressEventCamera);
        float deltaAngle = Mathf.DeltaAngle(previousAngle, angle);
        float absDelta = Mathf.Abs(deltaAngle);
        previousAngle = angle;

        if (absDelta < minAngleDelta)
            return;

        if (!hasDirection)
        {
            directionSign = Mathf.Sign(deltaAngle);
            hasDirection = true;
        }
        else if (Mathf.Sign(deltaAngle) != directionSign)
        {
            hasInvalidPath = true;
        }

        if (absDelta > maxAngleJump)
        {
            hasInvalidPath = true;
        }

        totalAngle += deltaAngle;
        totalTravelDistance += Vector2.Distance(previousPoint, localPoint);
        previousPoint = localPoint;
        sampleCount++;
        if (!IsWithinRadius(eventData.position, eventData.pressEventCamera))
        {
            CancelSwipe("円外逸脱");
            return;
        }

        if (IsGestureComplete())
        {
            isCompleted = true;
            isTracking = false;
            HandleSwipeSuccess();
        }
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
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null)
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないためスワイプ判定を停止します。");
            return false;
        }

        return stateController.currentState == YokaiState.Purifying;
    }

    void HandleSwipeSuccess()
    {
        if (hasAppliedPurify)
            return;

        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (purityController == null)
        {
            Debug.LogWarning("[PURIFY] PurityController が見つからないためおきよめできません。");
            return;
        }

        Debug.Log("[PURIFY] おきよめ成功");
        Debug.Log("[PURIFY] Trail success");
        ClearTrail(immediate: true);
        PulseMagicCircle();
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        purityController.ApplyPurifyFromMagicCircle();
        hasAppliedPurify = true;
        stateController.StopPurifyingForSuccess();
        ToggleGuide(false, immediate: false);
    }

    bool IsGestureComplete()
    {
        if (sampleCount < minimumSamples)
            return false;

        if (Mathf.Abs(totalAngle) < requiredAngle)
            return false;

        if (totalTravelDistance < GetRequiredTravelDistance())
            return false;

        if (hasInvalidRadius)
            return false;

        if (hasInvalidPath)
            return false;

        return true;
    }

    void LogSwipeFailure()
    {
        string reason;

        if (sampleCount < minimumSamples)
            reason = "入力回数不足";
        else if (Mathf.Abs(totalAngle) < requiredAngle)
            reason = "角度不足";
        else if (totalTravelDistance < GetRequiredTravelDistance())
            reason = "移動距離不足";
        else if (hasInvalidRadius)
            reason = "円外の軌跡";
        else if (hasInvalidPath)
            reason = "連続スワイプ失敗";
        else
            reason = "条件未達";

        Debug.Log($"[PURIFY] おきよめ失敗 reason={reason}");
    }

    void CancelSwipe(string reason)
    {
        if (!isTracking)
            return;

        isTracking = false;
        isCompleted = false;
        Debug.Log($"[PURIFY] Swipe cancel reason={reason}");
        LogSwipeFailure();
        ClearTrail(immediate: false);
        if (IsPurifying())
            stateController.StopPurifying();
        ToggleGuide(false, immediate: false);
    }

    float GetAngleFromCenter(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform targetRect = circleRect != null ? circleRect : transform as RectTransform;
        if (targetRect == null)
            return 0f;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
            return 0f;

        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    Vector2 GetLocalPoint(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform targetRect = circleRect != null ? circleRect : transform as RectTransform;
        if (targetRect == null)
            return Vector2.zero;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
            return Vector2.zero;

        return localPoint;
    }

    bool IsWithinRadius(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform targetRect = circleRect != null ? circleRect : transform as RectTransform;
        if (targetRect == null)
            return true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
            return false;

        float radius = targetRect.rect.width * 0.5f;
        float minRadius = radius * minRadiusRatio;
        float maxRadius = radius * maxRadiusRatio;
        float distance = localPoint.magnitude;

        return distance >= minRadius && distance <= maxRadius;
    }

    float GetRequiredTravelDistance()
    {
        RectTransform targetRect = circleRect != null ? circleRect : transform as RectTransform;
        if (targetRect == null)
            return 0f;

        float radius = targetRect.rect.width * 0.5f;
        float requiredArc = Mathf.Deg2Rad * requiredAngle * radius;
        return Mathf.Abs(requiredArc) * Mathf.Clamp01(minTravelDistanceRatio);
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
        Debug.Log("[PURIFY] Trail start");
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
            Debug.Log("[PURIFY] Trail cleared");
            return;
        }

        if (!CanStartCoroutine())
        {
            swipeTrail.positionCount = 0;
            swipeTrail.enabled = false;
            ApplyTrailColor(1f);
            Debug.Log("[PURIFY] Trail cleared");
            return;
        }

        trailFadeCoroutine = StartCoroutine(FadeTrail());
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
        Debug.Log("[PURIFY] Trail cleared");
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
