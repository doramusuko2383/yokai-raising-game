using UnityEngine;
using Yokai;

public class EnergyRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

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
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null || stateController.currentState != YokaiState.EnergyEmpty)
            return;

        stateController.RecoverFromSpiritEmptyAd();
    }
}
