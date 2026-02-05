using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class EmergencyPurifyAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    GameObject emergencyPurifyRoot;

    [SerializeField]
    Button emergencyPurifyButton;

    bool hasWarnedMissingStateController;

    void OnEnable()
    {
        RefreshUI();
    }

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickEmergencyPurifyAd()
    {
        var controller = ResolveStateController();
        if (controller == null)
            return;

        Debug.Log("[EMERGENCY PURIFY] Triggered from UI button");
        controller.TryDo(YokaiAction.EmergencyPurifyAd, "EmergencyPurify");
    }

    public void RefreshUI()
    {
        var controller = ResolveStateController();
        if (controller == null)
        {
            SetAllDisabled();
            return;
        }

        bool canEmergencyPurify = controller.CanDo(YokaiAction.EmergencyPurifyAd);
        if (emergencyPurifyRoot != null)
            emergencyPurifyRoot.SetActive(canEmergencyPurify);
        if (emergencyPurifyButton != null)
            emergencyPurifyButton.interactable = canEmergencyPurify;
    }

    void Update()
    {
        RefreshUI();
    }

    void SetAllDisabled()
    {
        if (emergencyPurifyRoot != null)
            emergencyPurifyRoot.SetActive(false);
        if (emergencyPurifyButton != null)
            emergencyPurifyButton.interactable = false;
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

        Debug.LogWarning("[EMERGENCY_PURIFY] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }
}
