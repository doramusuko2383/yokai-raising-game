using UnityEngine;
using UnityEngine.EventSystems;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerDown HIT");
        eventData.Use();
        if (chargeController != null)
            chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerUp HIT");
        eventData.Use();
        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerExit HIT");
        eventData.Use();
        chargeController.CancelCharging();
    }
}
