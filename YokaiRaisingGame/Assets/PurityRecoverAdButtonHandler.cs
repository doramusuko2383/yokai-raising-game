using UnityEngine;
using Yokai;

public class PurityRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

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

        if (stateController.currentState != YokaiState.PurityEmpty)
            return;

        stateController.BeginPurifying();
    }
}
