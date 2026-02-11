using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PurifyChargeController chargeController;
    [SerializeField] private YokaiStateController stateController;

    void OnEnable()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerDown HIT");

        if (chargeController == null)
            return;

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        if (stateController != null && !stateController.CanDo(YokaiAction.PurifyHold))
            return;

        // Only consume the pointer when we actually intend to start charging.
        eventData.Use();
        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerUp HIT");
        if (chargeController == null || !chargeController.IsCharging)
            return;
        eventData.Use();
        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[INPUT] Pentagram PointerExit HIT");
        if (chargeController == null || !chargeController.IsCharging)
            return;
        eventData.Use();
        chargeController.CancelCharging();
    }
}
