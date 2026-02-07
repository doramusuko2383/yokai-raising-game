using UnityEngine;
using UnityEngine.EventSystems;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private PurifyChargeController chargeController;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerDown HIT");
        eventData.Use();
        chargeController.StartCharging();
    }
}
