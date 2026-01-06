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
        if (!IsState(YokaiState.Normal, "だんご"))
            return;

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
        {
            energyManager.AddEnergy(dangoAmount);
            Debug.Log($"[DANGO] +{dangoAmount:0.##} energy={energyManager.energy:0.##}/{energyManager.maxEnergy:0.##}");
            TutorialManager.NotifyDangoUsed();
        }
        else
        {
            Debug.LogWarning("[DANGO] EnergyManager が見つからないためだんごが使えません。");
        }
    }

    bool IsState(YokaiState state, string actionLabel)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null || stateController.currentState == state)
            return true;

        // DEBUG: 状態不一致で処理が止まった理由を明示する
        Debug.Log($"[ACTION BLOCK] {actionLabel} blocked. state={stateController.currentState}");
        return false;
    }
}
