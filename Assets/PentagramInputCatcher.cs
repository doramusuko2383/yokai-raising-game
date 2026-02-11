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

    private bool CanStartHold()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        // PurifyChargeController 側でも state==Purifying を見ているが、
        // ここで先に弾いて、UIクリックを奪わないようにする。
        return stateController != null && stateController.CanDo(YokaiAction.PurifyHold);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanStartHold())
            return;

        Debug.Log("[INPUT] Pentagram PointerDown HIT");
        eventData.Use();

        if (chargeController == null)
            return;

        chargeController.StartCharging();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // チャージ中だけ扱う（そうでない時は UI を邪魔しない）
        if (!CanStartHold())
            return;

        Debug.Log("[INPUT] Pentagram PointerUp HIT");
        eventData.Use();

        if (chargeController == null)
            return;

        chargeController.CancelCharging();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanStartHold())
            return;

        Debug.Log("[INPUT] Pentagram PointerExit HIT");
        eventData.Use();

        if (chargeController == null)
            return;

        chargeController.CancelCharging();
    }
}
