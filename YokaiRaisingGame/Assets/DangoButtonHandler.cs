using UnityEngine;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    EnergyManager energyManager;

    public void OnClickDango()
    {
        if (!IsState(YokaiState.Normal))
            return;

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
            energyManager.ApplyHeal();
    }

    bool IsState(YokaiState state)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        return stateController == null || stateController.currentState == state;
    }
}
