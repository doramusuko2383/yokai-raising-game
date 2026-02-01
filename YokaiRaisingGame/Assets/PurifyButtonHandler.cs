using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;
    [SerializeField]
    PurifyChargeController chargeController;
    bool hasWarnedMissingStateController;
    bool hasWarnedMissingChargeController;
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

        var controller = ResolveStateController();
        if (controller != null)
            controller.NotifyUserInteraction();

        var charge = ResolveChargeController();
        if (charge != null)
        {
            charge.BeginCharge();
        }
        else
        {
            WarnMissingChargeController();
        }
    }

    public void OnClickEmergencyPurify()
    {
        var controller = ResolveStateController();
        if (controller != null)
            controller.NotifyUserInteraction();

        if (!IsState(YokaiState.PurityEmpty))
            return;

        ShowAd(() =>
        {
            var resolvedController = ResolveStateController();
            if (resolvedController != null)
            {
                resolvedController.BeginPurifying();
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
        stateController = CurrentYokaiContext.ResolveStateController();
        if (stateController == null)
            WarnMissingStateController();

        return stateController;
    }

    PurifyChargeController ResolveChargeController()
    {
        if (chargeController == null)
        {
            chargeController = FindObjectOfType<PurifyChargeController>(true);
        }

        if (chargeController == null)
        {
            WarnMissingChargeController();
        }

        return chargeController;
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[PURIFY] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }

    void WarnMissingChargeController()
    {
        if (hasWarnedMissingChargeController)
            return;

        Debug.LogWarning("[PURIFY] ChargeController not set in Inspector");
        hasWarnedMissingChargeController = true;
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
