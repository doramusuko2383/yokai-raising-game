using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    enum PurifyButtonMode
    {
        NormalPurify,
        EmergencyPurify
    }

    [SerializeField] UIActionController actionController;
    [SerializeField] Image buttonImage;
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite emergencySprite;

    PurifyButtonMode currentMode;
    YokaiStateController subscribedStateController;

    void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

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
        UpdateButtonState(mode == PurifyButtonMode.EmergencyPurify);
    }

    public void UpdateButtonState(bool isEmergency)
    {
        if (buttonImage == null)
            return;

        var targetSprite = isEmergency ? emergencySprite : normalSprite;
        if (targetSprite != null)
            buttonImage.sprite = targetSprite;
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
