using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    UIActionController actionController;

    public void OnClickPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.PurifyStart);
    }

    public void OnClickEmergencyPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.EmergencyPurifyAd);
    }

    public void OnClickStopPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.PurifyCancel);
    }
}
