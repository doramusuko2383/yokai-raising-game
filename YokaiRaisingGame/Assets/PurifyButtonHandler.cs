using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.Normal, "おきよめ"))
            return;

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            stateController.BeginPurifying();
            TutorialManager.NotifyPurifyUsed();
        }
        else
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないためおきよめできません。");
        }
    }

    public void OnClickEmergencyPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.KegareMax, "緊急お祓い"))
            return;

        ShowAd(() =>
        {
            if (stateController == null)
                stateController = CurrentYokaiContext.ResolveStateController();

            if (stateController != null)
            {
                stateController.ExecuteEmergencyPurify();
                TutorialManager.NotifyPurifyUsed();
            }
            else
            {
                Debug.LogWarning("[PURIFY] StateController が見つからないため緊急お祓いできません。");
            }
        });
    }

    public void OnClickStopPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.Purifying, "おきよめ停止"))
            return;

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.StopPurifying();
    }

    bool IsState(YokaiState state, string actionLabel)
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null || stateController.currentState == state)
            return true;

        // DEBUG: 状態不一致で処理が止まった理由を明示する
        Debug.Log($"[ACTION BLOCK] {actionLabel} blocked. state={stateController.currentState}");
        return false;
    }

    void ShowAd(System.Action onCompleted)
    {
        Debug.Log("[AD] Showing rewarded ad before emergency purify.");
        onCompleted?.Invoke();
    }
}
