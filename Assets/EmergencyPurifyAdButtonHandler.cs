using UnityEngine;
using Yokai;

public class EmergencyPurifyAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    UIActionController actionController;

    public void OnClickEmergencyPurifyAd()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[EmergencyPurifyAdButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.EmergencyPurifyAd);
    }
}
