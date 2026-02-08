using System;
using UnityEngine;

public class PurifyButtonHandler : MonoBehaviour
{
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
        Debug.Log("[PURIFY] OnClickPurify");
        PurifyRequested?.Invoke();
    }

    public void OnClickPurify_DebugOnly()
    {
        Debug.Log("[PURIFY][DEBUG] Btn_Purify OnClick received");
    }

    public void OnClickEmergencyPurify()
    {
        Debug.Log("[EMERGENCY PURIFY] Button clicked");
        EmergencyPurifyRequested?.Invoke();
        TutorialManager.NotifyPurifyUsed();
    }

    public void OnClickStopPurify()
    {
        StopPurifyRequested?.Invoke();
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
