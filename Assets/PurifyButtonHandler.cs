using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [Header("UI References")]
    [SerializeField]
    GameObject purifyRoot;

    [SerializeField]
    Button purifyButton;

    [SerializeField]
    GameObject emergencyPurifyRoot;

    [SerializeField]
    Button emergencyPurifyButton;

    [SerializeField]
    GameObject stopPurifyRoot;

    [SerializeField]
    Button stopPurifyButton;

    bool hasWarnedMissingStateController;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool hasLoggedAudioResolution;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogAudioResolutionOnce();
#endif
        RefreshUI();
    }

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurify()
    {
        Debug.Log("[PURIFY][DEBUG] Btn_Purify OnClick received");
    }

    public void OnClickPurify_DebugOnly()
    {
        Debug.Log("[PURIFY][DEBUG] Btn_Purify OnClick received");
    }

    public void OnClickEmergencyPurify()
    {
        var controller = ResolveStateController();
        if (controller == null)
            return;

        Debug.Log("[EMERGENCY PURIFY] Button clicked");
        controller.TryDo(YokaiAction.EmergencyPurifyAd, "EmergencyPurify");
        TutorialManager.NotifyPurifyUsed();
    }

    public void OnClickStopPurify()
    {
        var controller = ResolveStateController();
        if (controller == null)
            return;

        controller.TryDo(YokaiAction.PurifyCancel, "UI:StopPurify");
    }

    public void RefreshUI()
    {
        var controller = ResolveStateController();
        if (controller == null)
        {
            SetAllDisabled();
            return;
        }

        bool canPurify = controller.CanDo(YokaiAction.PurifyStart);
        bool canEmergency = controller.CanDo(YokaiAction.EmergencyPurifyAd);
        bool canStop = controller.CanDo(YokaiAction.PurifyCancel);

        if (purifyRoot != null)
            purifyRoot.SetActive(canPurify);
        if (purifyButton != null)
            purifyButton.interactable = canPurify;

        if (emergencyPurifyRoot != null)
            emergencyPurifyRoot.SetActive(canEmergency);
        if (emergencyPurifyButton != null)
            emergencyPurifyButton.interactable = canEmergency;

        if (stopPurifyRoot != null)
            stopPurifyRoot.SetActive(canStop);
        if (stopPurifyButton != null)
            stopPurifyButton.interactable = canStop;
    }

    void Update()
    {
        RefreshUI();
    }

    void SetAllDisabled()
    {
        if (purifyRoot != null)
            purifyRoot.SetActive(false);
        if (purifyButton != null)
            purifyButton.interactable = false;

        if (emergencyPurifyRoot != null)
            emergencyPurifyRoot.SetActive(false);
        if (emergencyPurifyButton != null)
            emergencyPurifyButton.interactable = false;

        if (stopPurifyRoot != null)
            stopPurifyRoot.SetActive(false);
        if (stopPurifyButton != null)
            stopPurifyButton.interactable = false;
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
