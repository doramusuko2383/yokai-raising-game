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
        eventData.Use();

        if (chargeController == null)
            return;

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        if (stateController != null && !stateController.CanDo(YokaiAction.PurifyHold))
            return;

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
