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

    void OnEnable()
    {
        ToggleGuide(true);
    }

    void OnDisable()
    {
        ToggleGuide(false);
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

        if (!IsWithinRadius(eventData.position, eventData.pressEventCamera))
        {
            hasInvalidRadius = true;
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
            hasInvalidRadius = true;

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
            LogSwipeFailure();
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
        SEHub.Play(YokaiSE.Purify_Success);
        kegareManager.ApplyPurifyFromMagicCircle();
        hasAppliedPurify = true;
        stateController.StopPurifying();
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
        if (magicCircleGuide == null)
        {
            var guideObject = GameObject.Find("MagicCircleGuide");
            if (guideObject != null)
                magicCircleGuide = guideObject.GetComponent<Image>();
        }

        if (magicCircleGuide != null)
            magicCircleGuide.gameObject.SetActive(isVisible);
    }
}
