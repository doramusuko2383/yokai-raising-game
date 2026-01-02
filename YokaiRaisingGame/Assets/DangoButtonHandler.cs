using UnityEngine;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    EnergyManager energyManager;

    [SerializeField]
    float dangoAmount = 40f;

    public void OnClickDango()
    {
        if (!IsState(YokaiState.Normal))
            return;

        if (energyManager != null)
            energyManager.AddEnergy(dangoAmount);
    }

    bool IsState(YokaiState state)
    {
        return stateController == null || stateController.currentState == state;
    }
}
