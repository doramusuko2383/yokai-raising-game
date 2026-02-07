using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[PENTAGRAM] PointerDown detected");

        if (chargeController == null)
        {
            Debug.LogWarning("[PENTAGRAM] chargeController is NULL");
            return;
        }

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[PENTAGRAM] PointerUp detected");

        chargeController?.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[PENTAGRAM] PointerExit detected");

        chargeController?.CancelCharging();
    }

    private bool ShouldIgnoreInput()
    {
        var stateController = CurrentYokaiContext.ResolveStateController();
        return stateController == null
            || stateController.CurrentState != YokaiState.Purifying;
    }
}
