using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    KegareManager kegareManager;

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

    void OnEnable()
    {
        SetupGuideCanvas();
        ToggleGuide(IsPurifying(), immediate: true);
    }

    void OnDisable()
    {
        ToggleGuide(false, immediate: true);
    }

    void Update()
    {
        bool isPurifying = IsPurifying();
        if (isPurifying != wasPurifying)
        {
            ToggleGuide(isPurifying, immediate: false);
            wasPurifying = isPurifying;
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

        if (!IsWithinRadius(eventData.position, eventData.pressEventCamera))
        {
            CancelSwipe("円外開始");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking || isCompleted)
            return;

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

        Debug.Log($"[PURIFY] angle progress total={Mathf.Abs(totalAngle):F1} sample={sampleCount}");

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

        isTracking = false;

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

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager == null)
        {
            Debug.LogWarning("[PURIFY] KegareManager が見つからないためおきよめできません。");
            return;
        }

        Debug.Log("[PURIFY] おきよめ成功");
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        kegareManager.ApplyPurifyFromMagicCircle();
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
