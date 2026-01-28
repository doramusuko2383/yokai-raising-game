using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;
    bool hasWarnedMissingStateController;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool hasLoggedAudioResolution;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogAudioResolutionOnce();
#endif
    }

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
            stateController.NotifyUserInteraction();
            stateController.BeginPurifying();
            TutorialManager.NotifyPurifyUsed();
        }
        else
        {
            WarnMissingStateController();
        }
    }

    public void OnClickEmergencyPurify()
    {
        if (ResolveStateController() != null)
            stateController.NotifyUserInteraction();

        if (!IsState(YokaiState.PurityEmpty))
            return;

        ShowAd(() =>
        {
            if (ResolveStateController() != null)
            {
                stateController.BeginPurifying();
                TutorialManager.NotifyPurifyUsed();
            }
            else
            {
                WarnMissingStateController();
            }
        });
    }

    public void OnClickStopPurify()
    {
        if (!IsState(YokaiState.Purifying))
            return;

        if (ResolveStateController() != null)
            stateController.CancelPurifying("StopPurify");
    }

    bool IsActionBlocked()
    {
        ResolveStateController();
        // 不具合③: 状態未同期時はブロックせず、浄化中のみを弾く。
        return stateController != null
            && stateController.currentState == YokaiState.Purifying;
    }

    bool IsState(YokaiState state)
    {
        ResolveStateController();
        if (stateController == null || stateController.currentState == state)
            return true;

        return false;
    }

    void ShowAd(System.Action onCompleted)
    {
        onCompleted?.Invoke();
    }

    YokaiStateController ResolveStateController()
    {
        if (stateController != null)
            return stateController;

        stateController = FindObjectOfType<YokaiStateController>(true);
        if (stateController == null)
            WarnMissingStateController();

        return stateController;
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[PURIFY] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogAudioResolutionOnce()
    {
        if (hasLoggedAudioResolution)
            return;

        bool hasResolver = AudioHook.ClipResolver != null;
        Debug.Log($"[SE] Purify AudioHook resolver ready: {hasResolver}");
        hasLoggedAudioResolution = true;
    }
#endif
}
