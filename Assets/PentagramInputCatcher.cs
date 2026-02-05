using UnityEngine;
using UnityEngine.EventSystems;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (chargeController == null)
            return;

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (chargeController == null)
            return;

        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (chargeController == null)
            return;

        chargeController.CancelCharging();
    }
}
