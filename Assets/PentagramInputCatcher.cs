using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField]
    YokaiStateController stateController;

    void OnEnable()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);
    }

    bool CanHandleHold()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        return stateController != null && stateController.CanDo(YokaiAction.PurifyHold);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanHandleHold())
            return;

        eventData.Use();
        stateController.TryDo(YokaiAction.PurifyHoldStart, "PentagramStartHold");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanHandleHold())
            return;

        eventData.Use();
        stateController.TryDo(YokaiAction.PurifyHoldCancel, "PentagramCancelHold");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanHandleHold())
            return;

        eventData.Use();
        stateController.TryDo(YokaiAction.PurifyHoldCancel, "PentagramCancelHold");
    }
}
