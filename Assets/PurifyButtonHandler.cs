using System;
using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    public event Action PurifyRequested;
    public event Action EmergencyPurifyRequested;
    public event Action StopPurifyRequested;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool hasLoggedAudioResolution;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogAudioResolutionOnce();
#endif
    }

    public void OnClickPurify()
    {
        if (ResolveStateController() == null)
            return;

        Debug.Log("[PURIFY] OnClickPurify");
        PurifyRequested?.Invoke();
    }

    public void OnClickPurify_DebugOnly()
    {
        Debug.Log("[PURIFY][DEBUG] Btn_Purify OnClick received");
    }

    public void OnClickEmergencyPurify()
    {
        if (ResolveStateController() == null)
            return;

        Debug.Log("[EMERGENCY PURIFY] Button clicked");
        EmergencyPurifyRequested?.Invoke();
        TutorialManager.NotifyPurifyUsed();
    }

    public void OnClickStopPurify()
    {
        if (ResolveStateController() == null)
            return;

        StopPurifyRequested?.Invoke();
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
