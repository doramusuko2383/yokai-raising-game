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
        Debug.Log("[RECOVERY] PurityRecoverAd button clicked");


        var controller = ResolveStateController();
        if (controller == null)
        {
            WarnMissingStateController();
            return;
        }

        Debug.Log("[RECOVERY] CurrentState=" + controller.currentState);


        controller.NotifyUserInteraction();

        if (controller.currentState != YokaiState.PurityEmpty)
        {
            Debug.LogWarning("[RECOVERY] Click ignored: not in PurityEmpty");
            return;
        }

        Debug.Log("[RECOVERY] Execute emergency purify via Ad");


        controller.ExecuteEmergencyPurify("PurityRecoverAd");
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
