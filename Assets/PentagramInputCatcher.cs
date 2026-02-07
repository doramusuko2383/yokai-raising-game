using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[INPUT] PointerDown on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[INPUT] PointerUp on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.OnPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShouldIgnoreInput())
            return;

        Debug.Log("[INPUT] PointerExit on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.OnPointerUp(eventData);
    }

    bool ShouldIgnoreInput()
    {
        var stateController = CurrentYokaiContext.ResolveStateController();
        return stateController != null && stateController.currentState == YokaiState.PurityEmpty;
    }
}
