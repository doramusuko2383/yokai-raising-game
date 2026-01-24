using UnityEngine;
using Yokai;

public class EnergyRecoverAdButtonHandler : MonoBehaviour
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

    public void OnClickEnergyRecoverAd()
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

            spiritController.AddSpiritRatio(recoverRatio);
            AudioHook.RequestPlay(YokaiSE.SE_DANGO);
            stateController.ForceReevaluate("SpiritRecovered");
        }
    }
}
