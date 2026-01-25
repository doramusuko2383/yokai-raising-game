using UnityEngine;
using Yokai;

public class PurityRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;
    bool hasWarnedMissingStateController;

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurityRecoverAd()
    {
        YokaiStatePresentationController.Instance?.NotifyUserInteraction();

        if (ResolveStateController() == null)
        {
            WarnMissingStateController();
            return;
        }

        if (stateController.currentState != YokaiState.PurityEmpty)
            return;

        stateController.BeginPurifying();
    }

    YokaiStateController ResolveStateController()
    {
        if (stateController != null)
            return stateController;

        stateController = FindObjectOfType<YokaiStateController>(true);
        return stateController;
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[RECOVERY] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }
}
