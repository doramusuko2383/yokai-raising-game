using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    void OnEnable()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);
    }

    public void OnClickPurify()
    {
        var controller = stateController ?? FindObjectOfType<YokaiStateController>(true);
        if (controller != null)
            controller.TryDo(YokaiAction.PurifyStart, "UI_PurifyButton");
    }

    public void OnClickEmergencyPurify()
    {
        var controller = stateController ?? FindObjectOfType<YokaiStateController>(true);
        if (controller != null)
            controller.TryDo(YokaiAction.EmergencyPurifyAd, "UI_EmergencyButton");
    }

    public void OnClickStopPurify()
    {
        var controller = stateController ?? FindObjectOfType<YokaiStateController>(true);
        if (controller != null)
            controller.TryDo(YokaiAction.PurifyStop, "UI_StopPurify");
    }
}
