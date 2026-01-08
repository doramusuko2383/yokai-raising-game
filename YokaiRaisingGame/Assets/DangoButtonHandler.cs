using UnityEngine;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    EnergyManager energyManager;

    [SerializeField]
    float dangoAmount = 30f;

    public void OnClickDango()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (IsActionBlocked())
            return;

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
        {
            energyManager.AddEnergy(dangoAmount);
            Debug.Log($"[ENERGY] Dango +{dangoAmount:0.##} energy={energyManager.energy:0.##}/{energyManager.maxEnergy:0.##}");
            TutorialManager.NotifyDangoUsed();
            MentorMessageService.NotifyRecovered();
        }
        else
        {
            Debug.LogWarning("[ENERGY] EnergyManager が見つからないためだんごが使えません。");
        }
    }

    bool IsActionBlocked()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        return stateController != null && (stateController.isPurifying || stateController.isSpiritEmpty);
    }
}
