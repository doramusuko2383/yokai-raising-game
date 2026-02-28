using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    enum ActionButtonMode
    {
        EatDango,
        SpecialRecover
    }

    [SerializeField] Image buttonImage;
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite specialSprite;
    [Tooltip("ボタンのClickable本体（未設定ならこのGameObjectのButtonを探す）")]
    [SerializeField] Button button;

    [SerializeField] UIActionController actionController;

    ActionButtonMode currentMode;
    bool isAdMode;
    bool isBusy; // 広告再生中などの多重クリック防止
    bool subscribedSave;
    YokaiStateController subscribedStateController;
    Coroutine pulseRoutine;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        RefreshUI();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void Start()
    {
        TrySubscribe();
        RefreshUI();
    }

    void OnDisable()
    {
        if (subscribedSave && SaveManager.Instance != null)
            SaveManager.Instance.OnDangoChanged -= RefreshUI;

        if (subscribedStateController != null)
            subscribedStateController.OnStatusChanged -= RefreshUI;

        subscribedSave = false;
        subscribedStateController = null;
    }

    void TrySubscribe()
    {
        TrySubscribeStateController();

        if (subscribedSave || SaveManager.Instance == null)
            return;

        SaveManager.Instance.OnDangoChanged += RefreshUI;
        subscribedSave = true;
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
        var newMode = DecideMode();
        if (newMode != currentMode)
        {
            currentMode = newMode;
            ApplyMode(currentMode);
        }

        if (button != null)
            button.interactable = !isBusy;
    }

    ActionButtonMode DecideMode()
    {
        var controller = YokaiStateController.Instance;
        if (controller == null)
            return ActionButtonMode.EatDango;

        if (controller.CurrentState == YokaiState.EnergyEmpty)
            return ActionButtonMode.SpecialRecover;

        return ActionButtonMode.EatDango;
    }

    void ApplyMode(ActionButtonMode mode)
    {
        switch (mode)
        {
            case ActionButtonMode.EatDango:
                ApplyEatMode();
                break;
            case ActionButtonMode.SpecialRecover:
                ApplySpecialRecoverMode();
                break;
        }
    }

    public void UpdateButtonState(bool isSpecial)
    {
        if (buttonImage == null)
            return;

        var targetSprite = isSpecial ? specialSprite : normalSprite;
        if (targetSprite != null)
            buttonImage.sprite = targetSprite;
    }

    public void OnClickDango()
    {
        if (isBusy)
            return;

        switch (currentMode)
        {
            case ActionButtonMode.EatDango:
                ExecuteEat();
                break;
            case ActionButtonMode.SpecialRecover:
                ExecuteSpecialRecover();
                break;
        }
    }

    void ExecuteEat()
    {
        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
            return;

        int countBefore = save.dango.currentCount;
        if (countBefore <= 0)
            return;

        if (actionController == null)
            actionController = FindObjectOfType<UIActionController>(true);

        actionController?.Execute(YokaiAction.EatDango);
    }

    void ApplyEatMode()
    {
        StopPulse();
        UpdateButtonState(false);
        isAdMode = false;
    }

    void ApplySpecialRecoverMode()
    {
        StartPulse(1.02f);
        UpdateButtonState(true);
        isAdMode = true;
    }

    void ExecuteSpecialRecover()
    {
        ShowRewardAd();
    }

    void ShowRewardAd()
    {
        isBusy = true;
        if (button != null) button.interactable = false;

        var controller = YokaiStateController.Instance;
        controller?.RecoverFromAd();

        isBusy = false;
        SaveManager.Instance?.NotifyDangoChanged();

        if (button != null)
            button.interactable = true;
    }

    #region Pulse Animation

    void StartPulse(float scale)
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(Pulse(scale));
    }

    void StopPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        transform.localScale = Vector3.one;
    }

    System.Collections.IEnumerator Pulse(float scale)
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * scale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
        }
    }

    #endregion
}
