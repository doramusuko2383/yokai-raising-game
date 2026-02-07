using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerDown HIT");
        eventData.Use(); // ÅöÇ±ÇÍ

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerUp HIT");

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
