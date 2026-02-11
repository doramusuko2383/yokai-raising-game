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

        return stateController != null && stateController.CanDo(YokaiAction.PurifyHoldStart);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanHandleHold())
            return;

        stateController.TryDo(YokaiAction.PurifyHoldStart, "HoldStart");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        stateController?.TryDo(YokaiAction.PurifyHoldCancel, "HoldCancel");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        stateController?.TryDo(YokaiAction.PurifyHoldCancel, "HoldCancel");
    }
}
