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
        var controller = ResolveStateController();
        if (controller == null)
            return;

        controller.NotifyUserInteraction();
        controller.TryDo(YokaiAction.PurifyStart, "UI:PurifyButton");
    }

    public void OnClickEmergencyPurify()
    {
        var controller = ResolveStateController();
        if (controller != null)
            controller.NotifyUserInteraction();

        ShowAd(() =>
        {
            var resolvedController = ResolveStateController();
            if (resolvedController != null)
            {
                resolvedController.TryDo(YokaiAction.EmergencyPurifyAd, "UI:EmergencyPurify");
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
        var controller = ResolveStateController();
        if (controller == null)
            return;

        controller.TryDo(YokaiAction.PurifyCancel, "UI:StopPurify");
    }

    void ShowAd(System.Action onCompleted)
    {
        onCompleted?.Invoke();
    }

    YokaiStateController ResolveStateController()
    {
        stateController = CurrentYokaiContext.ResolveStateController();
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
