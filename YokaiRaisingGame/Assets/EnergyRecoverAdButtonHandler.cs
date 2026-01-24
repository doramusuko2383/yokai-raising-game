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
            stateController.RequestEvaluateState("SpiritRecovered");
        }
    }
}
