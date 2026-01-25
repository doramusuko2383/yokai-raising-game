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
        if (IsActionBlocked())
            return;

        if (stateController != null)
        {
            stateController.BeginPurifying();
            TutorialManager.NotifyPurifyUsed();
        }
        else
        {
            Debug.LogError("[PURIFY] StateController not set in Inspector");
        }
    }

    public void OnClickEmergencyPurify()
    {
        if (!IsState(YokaiState.PurityEmpty))
            return;

        ShowAd(() =>
        {
            if (stateController != null)
            {
                stateController.BeginPurifying();
                TutorialManager.NotifyPurifyUsed();
            }
            else
            {
                Debug.LogError("[PURIFY] StateController not set in Inspector");
            }
        });
    }

    public void OnClickStopPurify()
    {
        if (!IsState(YokaiState.Purifying))
            return;

        if (stateController != null)
            stateController.CancelPurifying("StopPurify");
    }

    bool IsActionBlocked()
    {
        // 不具合③: 状態未同期時はブロックせず、浄化中のみを弾く。
        return stateController != null
            && stateController.currentState == YokaiState.Purifying;
    }

    bool IsState(YokaiState state)
    {
        if (stateController == null || stateController.currentState == state)
            return true;

        return false;
    }

    void ShowAd(System.Action onCompleted)
    {
        onCompleted?.Invoke();
    }

}
