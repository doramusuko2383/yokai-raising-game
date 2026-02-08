using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    YokaiStateController stateController;
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
        stateController = controller;
    }

    public void OnClickPurify()
    {
        Debug.Log("[LEGACY] PurifyButtonHandler disabled");
        return;
    }

    public void OnClickEmergencyPurify()
    {
        ShowAd(() =>
        {
            var resolvedController = ResolveStateController();
            if (resolvedController == null)
                return;

            resolvedController.TryDo(YokaiAction.EmergencyPurifyAd, "UI:EmergencyPurify");
            TutorialManager.NotifyPurifyUsed();
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
        stateController =
            CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);

        if (stateController == null)
            Debug.LogError("[PURIFY] StateController could not be resolved.");

        return stateController;
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
