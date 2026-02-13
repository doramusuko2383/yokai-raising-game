using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PentagramInputCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField]
    UIActionController actionController;

    bool EnsureActionController()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PentagramInputCatcher] UIActionController not set in Inspector.");
            return false;
        }

        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!EnsureActionController())
            return;

        actionController.Execute(YokaiAction.PurifyHoldStart);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!EnsureActionController())
            return;

        actionController.Execute(YokaiAction.PurifyHoldCancel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!EnsureActionController())
            return;

        actionController.Execute(YokaiAction.PurifyHoldCancel);
    }
}
