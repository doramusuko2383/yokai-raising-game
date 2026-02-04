using UnityEngine;
using Yokai;

public class EnergyRecoverAdButtonHandler : MonoBehaviour
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

    public void OnClickEnergyRecoverAd()
    {
        if (ResolveStateController() == null)
        {
            WarnMissingStateController();
            return;
        }

        stateController.TryDo(YokaiAction.EmergencySpiritRecover);
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
