using UnityEngine;
using Yokai;
using TMPro;

public class PurifyButtonHandler : MonoBehaviour
{
    enum PurifyButtonMode
    {
        NormalPurify,
        EmergencyPurify
    }

    [SerializeField]
    UIActionController actionController;

    [SerializeField]
    TMP_Text buttonText;

    PurifyButtonMode currentMode;
    YokaiStateController subscribedStateController;

    void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        RefreshUI();
    }

    void OnEnable()
    {
        TrySubscribeStateController();
        RefreshUI();
    }

    void OnDisable()
    {
        if (subscribedStateController != null)
            subscribedStateController.OnStatusChanged -= RefreshUI;

        subscribedStateController = null;
    }

    void TrySubscribeStateController()
    {
        var controller = YokaiStateController.Instance;
        if (controller == null || subscribedStateController == controller)
            return;

        if (subscribedStateController != null)
            subscribedStateController.OnStatusChanged -= RefreshUI;

        subscribedStateController = controller;
        subscribedStateController.OnStatusChanged += RefreshUI;
    }

    public void RefreshUI()
    {
        TrySubscribeStateController();

        var newMode = DecideMode();
        if (newMode == currentMode)
            return;

        currentMode = newMode;
        ApplyMode(currentMode);
    }

    PurifyButtonMode DecideMode()
    {
        var controller = YokaiStateController.Instance;
        if (controller != null && controller.CurrentState == YokaiState.PurityEmpty)
            return PurifyButtonMode.EmergencyPurify;

        return PurifyButtonMode.NormalPurify;
    }

    void ApplyMode(PurifyButtonMode mode)
    {
        if (buttonText == null)
            return;

        switch (mode)
        {
            case PurifyButtonMode.EmergencyPurify:
                buttonText.text = "緊急浄化";
                buttonText.color = new Color(1f, 0.6f, 0.6f);
                break;

            default:
                buttonText.text = "おきよめ";
                buttonText.color = Color.black;
                break;
        }
    }

    public void OnClickPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        if (currentMode == PurifyButtonMode.EmergencyPurify)
        {
            actionController.Execute(YokaiAction.EmergencyPurifyAd);
            return;
        }

        actionController.Execute(YokaiAction.PurifyStart);
    }

    public void OnClickEmergencyPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.EmergencyPurifyAd);
    }

    public void OnClickStopPurify()
    {
        if (actionController == null)
        {
            Debug.LogWarning("[PurifyButtonHandler] UIActionController not set in Inspector.");
            return;
        }

        actionController.Execute(YokaiAction.PurifyCancel);
    }
}
