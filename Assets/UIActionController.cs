using UnityEngine;
using Yokai;

public class UIActionController : MonoBehaviour
{
    [SerializeField]
    private YokaiStateController stateController;

    public void Execute(YokaiAction action)
    {
        Debug.Log($"[UIAction] Execute {action}");

        if (stateController == null)
        {
            Debug.LogError("[UIActionController] StateController not assigned.");
            return;
        }

        stateController.TryDo(action, "UIActionController");
    }

    public void Execute(YokaiAction action, string reason)
    {
        Debug.Log($"[UIAction] Execute {action} ({reason})");

        if (stateController == null)
        {
            Debug.LogError("[UIActionController] StateController not assigned.");
            return;
        }

        stateController.TryDo(action, reason);
    }
}
