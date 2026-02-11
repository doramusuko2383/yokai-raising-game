using UnityEngine;
using Yokai;

public class PurityRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    UIActionController actionController;

    public void OnClickPurityRecoverAd()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurityRecoverAdButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.EmergencyPurifyAd);
    }
}
