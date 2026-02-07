using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanHandleInput())
            return;

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanHandleInput())
            return;

        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanHandleInput())
            return;

        chargeController.CancelCharging();
    }

    private bool CanHandleInput()
    {
        if (chargeController == null)
            return false;

        var stateController = CurrentYokaiContext.ResolveStateController();
        return stateController != null
            && stateController.CurrentState == YokaiState.Purifying;
    }
}
