using UnityEngine;
using Yokai;

public class EnergyRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    UIActionController actionController;

    public void OnClickEnergyRecoverAd()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[EnergyRecoverAdButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.EmergencySpiritRecover);
    }
}
