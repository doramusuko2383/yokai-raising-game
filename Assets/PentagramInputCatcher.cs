using UnityEngine;
using UnityEngine.EventSystems;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[INPUT] PointerDown on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[INPUT] PointerUp on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[INPUT] PointerExit on PentagramInputCatcher");

        if (chargeController == null)
        {
            Debug.LogWarning("[INPUT] chargeController is NULL");
            return;
        }

        chargeController.CancelCharging();
    }
}
