using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class MagicCircleSwipeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField]
    float requiredPathLength = 260f;

    [SerializeField]
    float requiredAngle = 300f;

    [SerializeField]
    float minRadiusRatio = 0.55f;

    [SerializeField]
    float maxRadiusRatio = 1.05f;

    [SerializeField]
    int minimumSamples = 12;

    [SerializeField]
    RectTransform circleRect;

    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    KegareManager kegareManager;

    bool isTracking;
    bool isCompleted;
    bool hasInvalidRadius;
    float totalLength;
    float totalAngle;
    float previousAngle;
    Vector2 previousPosition;
    int sampleCount;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPurifying())
        {
            Debug.Log("[PURIFY] おきよめ失敗 reason=状態不一致");
            return;
        }

        isTracking = true;
        isCompleted = false;
        hasInvalidRadius = false;
        totalLength = 0f;
        totalAngle = 0f;
        sampleCount = 0;
        previousPosition = eventData.position;
        previousAngle = GetAngleFromCenter(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking || isCompleted)
            return;

        float distance = Vector2.Distance(previousPosition, eventData.position);
        totalLength += distance;
        previousPosition = eventData.position;

        float angle = GetAngleFromCenter(eventData.position, eventData.pressEventCamera);
        float deltaAngle = Mathf.DeltaAngle(previousAngle, angle);
        totalAngle += deltaAngle;
        previousAngle = angle;

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
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null)
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないためスワイプ判定を停止します。");
            return false;
        }

        return stateController.currentState == YokaiState.Purifying;
    }

    void HandleSwipeSuccess()
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager == null)
        {
            Debug.LogWarning("[PURIFY] KegareManager が見つからないためおきよめできません。");
            return;
        }

        Debug.Log("[PURIFY] おきよめ成功");
        kegareManager.ApplyPurifyFromMagicCircle();
        stateController.StopPurifying();
    }

    bool IsGestureComplete()
    {
        if (sampleCount < minimumSamples)
            return false;

        if (totalLength < requiredPathLength)
            return false;

        if (Mathf.Abs(totalAngle) < requiredAngle)
            return false;

        if (hasInvalidRadius)
            return false;

        return true;
    }

    void LogSwipeFailure()
    {
        string reason;

        if (sampleCount < minimumSamples)
            reason = "入力回数不足";
        else if (totalLength < requiredPathLength)
            reason = "軌跡長不足";
        else if (Mathf.Abs(totalAngle) < requiredAngle)
            reason = "角度不足";
        else if (hasInvalidRadius)
            reason = "円外の軌跡";
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
}
