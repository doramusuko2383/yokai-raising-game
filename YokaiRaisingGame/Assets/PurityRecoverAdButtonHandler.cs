using UnityEngine;
using Yokai;

public class PurityRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    float recoverRatio = 0.5f;

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurityRecoverAd()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
        {
            Debug.LogError("[RECOVERY] StateController not set in Inspector");
            return;
        }

        if (stateController.currentState == YokaiState.EnergyEmpty)
        {
            var spiritController = stateController.SpiritController;
            if (spiritController == null)
            {
                Debug.LogError("[RECOVERY] SpiritController not set in Inspector");
                return;
            }

            spiritController.SetSpiritRatio(recoverRatio);
        }
        else if (stateController.currentState == YokaiState.PurityEmpty)
        {
            var purityController = stateController.PurityController;
            if (purityController == null)
            {
                Debug.LogError("[RECOVERY] PurityController not set in Inspector");
                return;
            }

            purityController.SetPurityRatio(recoverRatio);
        }
    }
}
