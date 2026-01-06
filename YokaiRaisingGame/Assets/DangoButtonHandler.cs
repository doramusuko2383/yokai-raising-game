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
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.Normal, "だんご"))
            return;

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
        {
            energyManager.AddEnergy(dangoAmount);
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
