using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class MagicCircleSwipeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField]
    float requiredSwipeDistance = 120f;

    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    KegareManager kegareManager;

    bool isTracking;
    Vector2 startPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPurifying())
            return;

        isTracking = true;
        startPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking)
            return;

        float distance = Vector2.Distance(startPosition, eventData.position);
        if (distance < requiredSwipeDistance)
            return;

        isTracking = false;
        HandleSwipeSuccess();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isTracking = false;
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

        Debug.Log("[PURIFY] MagicCircle swipe success.");
        kegareManager.ApplyPurifyFromMagicCircle();
        stateController.StopPurifying();
    }
}
