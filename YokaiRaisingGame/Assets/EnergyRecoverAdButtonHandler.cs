using UnityEngine;
using Yokai;

public class EnergyRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    float recoverRatio = 0.5f;
    bool hasWarnedMissingStateController;
    bool hasWarnedMissingSpiritController;

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

        stateController.NotifyUserInteraction();

        if (stateController.currentState == YokaiState.EnergyEmpty)
        {
            var spiritController = stateController.SpiritController;
            if (spiritController == null)
            {
                WarnMissingSpiritController();
                return;
            }

            spiritController.AddSpiritRatio(recoverRatio);
            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
            stateController.RequestEvaluateState("SpiritRecovered");
        }
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

    void WarnMissingSpiritController()
    {
        if (hasWarnedMissingSpiritController)
            return;

        Debug.LogWarning("[RECOVERY] SpiritController not set in Inspector");
        hasWarnedMissingSpiritController = true;
    }
}
