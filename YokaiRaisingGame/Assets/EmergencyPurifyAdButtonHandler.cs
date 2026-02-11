using UnityEngine;
using Yokai;

public class EmergencyPurifyAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    public void OnClickEmergencyPurifyAd()
    {
        var controller = stateController ?? FindObjectOfType<YokaiStateController>(true);
        if (controller != null)
            controller.TryDo(YokaiAction.EmergencyPurifyAd, "UI_EmergencyAd");
    }
}
