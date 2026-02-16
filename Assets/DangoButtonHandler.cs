using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    enum ActionButtonMode
    {
        EatDango,
        AdRecover,
        SpecialDango
    }

    public TMP_Text buttonText;
    public Image buttonBackground;
    [Tooltip("ボタンのClickable本体（未設定ならこのGameObjectのButtonを探す）")]
    public Button button;
    public Color normalColor = Color.white;
    public Color adColor = new Color(0.4f, 0.7f, 1f);

    [SerializeField]
    UIActionController actionController;

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
        Debug.Log($"[DangoButtonHandler] TrySubscribe called. subscribedSave={subscribedSave}, hasSaveManager={SaveManager.Instance != null}");

        TrySubscribeStateController();

        if (subscribedSave)
        {
            Debug.Log("[DangoButtonHandler] TrySubscribe skipped: already subscribed to SaveManager.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[DangoButtonHandler] TrySubscribe skipped: SaveManager.Instance is null.");
            return;
        }

        SaveManager.Instance.OnDangoChanged += RefreshUI;
        subscribedSave = true;
        Debug.Log("[DangoButtonHandler] TrySubscribe succeeded: subscribed to SaveManager.OnDangoChanged.");
    }

    void TrySubscribeStateController()
    {
        var controller = YokaiStateController.Instance;
        if (controller == null)
            return;

        if (subscribedStateController == controller)
            return;

        if (subscribedStateController != null)
            subscribedStateController.OnStatusChanged -= RefreshUI;

        subscribedStateController = controller;
        subscribedStateController.OnStatusChanged += RefreshUI;
        Debug.Log("[DangoButtonHandler] TrySubscribeStateController succeeded: subscribed to YokaiStateController.OnStatusChanged.");
    }

    public void RefreshUI()
    {
        bool isTargetGraphicMatched = button != null && buttonBackground != null && ReferenceEquals(button.targetGraphic, buttonBackground);
        Debug.Log($"[DangoButtonHandler] RefreshUI begin. isBusy={isBusy}, currentMode={currentMode}, hasButtonText={buttonText != null}, hasButtonBackground={buttonBackground != null}, hasButton={button != null}, targetGraphicMatched={isTargetGraphicMatched}");

        if (button != null && buttonBackground != null && !ReferenceEquals(button.targetGraphic, buttonBackground))
            Debug.LogWarning("[DangoButtonHandler] RefreshUI validation: buttonBackground is not matched with Button.targetGraphic.");

        if (buttonText == null || buttonBackground == null)
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI aborted: buttonText or buttonBackground is null.");
            return;
        }

        var newMode = DecideMode();
        if (newMode != currentMode)
        {
            currentMode = newMode;
            ApplyMode(currentMode);
        }

        if (button != null)
        {
            button.interactable = !isBusy;
            Debug.Log($"[DangoButtonHandler] RefreshUI applied interactable={button.interactable}");
        }
        else
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI: button is null, interactable was not updated.");
        }
    }

    ActionButtonMode DecideMode()
    {
        var controller = YokaiStateController.Instance;
        var save = SaveManager.Instance?.CurrentSave;
        if (controller == null)
            return ActionButtonMode.EatDango;

        if (controller.CurrentState == YokaiState.EnergyEmpty)
        {
            if (controller.CanUseSpecialDango)
                return ActionButtonMode.SpecialDango;

            return ActionButtonMode.AdRecover;
        }

        if (save?.dango?.currentCount > 0)
            return ActionButtonMode.EatDango;

        return ActionButtonMode.AdRecover;
    }

    void ApplyMode(ActionButtonMode mode)
    {
        Debug.Log($"[DangoButtonHandler] ApplyMode mode={mode}");

        switch (mode)
        {
            case ActionButtonMode.EatDango:
                ApplyEatMode();
                break;
            case ActionButtonMode.AdRecover:
                ApplyAdMode();
                break;
            case ActionButtonMode.SpecialDango:
                ApplySpecialDangoMode();
                break;
        }
    }

    public void OnClickDango()
    {
        Debug.Log($"[DangoButtonHandler] OnClickDango begin. isBusy={isBusy}, currentMode={currentMode}");

        if (isBusy)
        {
            Debug.Log("[DangoButtonHandler] OnClickDango ignored: handler is busy.");
            return;
        }

        switch (currentMode)
        {
            case ActionButtonMode.EatDango:
                ExecuteEat();
                break;
            case ActionButtonMode.AdRecover:
                ShowRewardAd();
                break;
            case ActionButtonMode.SpecialDango:
                ExecuteSpecialDango();
                break;
        }
    }

    void ExecuteEat()
    {
        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
        {
            Debug.LogWarning($"[DangoButtonHandler] ExecuteEat aborted: save or dango is null. hasSave={save != null}, hasDango={save?.dango != null}");
            return;
        }

        int countBefore = save.dango.currentCount;
        Debug.Log($"[DangoButtonHandler] ExecuteEat countBefore={countBefore}");

        if (countBefore <= 0)
        {
            Debug.Log("[DangoButtonHandler] ExecuteEat skipped: no dango available.");
            return;
        }

        if (actionController == null)
            actionController = FindObjectOfType<UIActionController>(true);

        actionController?.Execute(YokaiAction.EatDango);
        Debug.Log($"[DangoButtonHandler] ExecuteEat executed EatDango. actionControllerFound={actionController != null}");
    }

    void ApplyEatMode()
    {
        Debug.Log($"[DangoButtonHandler] ApplyEatMode. previousIsAdMode={isAdMode}");
        buttonText.text = "だんご";
        buttonText.color = Color.black;
        StopPulse();
        isAdMode = false;
    }

    void ApplyAdMode()
    {
        Debug.Log($"[DangoButtonHandler] ApplyAdMode. previousIsAdMode={isAdMode}");
        buttonText.text = "広告で回復";
        buttonText.color = new Color(0.65f, 0.8f, 1f);
        StartPulse(1.02f);
        isAdMode = true;
    }

    void ApplySpecialDangoMode()
    {
        Debug.Log($"[DangoButtonHandler] ApplySpecialDangoMode. previousIsAdMode={isAdMode}");
        buttonText.text = "特別だんご";
        buttonText.color = Color.yellow;
        StopPulse();
        isAdMode = false;
    }


    void ExecuteSpecialDango()
    {
        if (actionController == null)
            actionController = FindObjectOfType<UIActionController>(true);

        actionController?.Execute(YokaiAction.EmergencySpiritRecover);
        Debug.Log($"[DangoButtonHandler] ExecuteSpecialDango executed EmergencySpiritRecover. actionControllerFound={actionController != null}");
    }


    void ShowRewardAd()
    {
        isBusy = true;
        if (button != null) button.interactable = false;

        Debug.Log("[AD] Rewarded Ad Simulated");

        var save = SaveManager.Instance.CurrentSave;
        save.dango.currentCount = Mathf.Clamp(save.dango.currentCount + 1, 0, 3);
        save.dango.lastGeneratedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveManager.Instance.MarkDirty();

        isBusy = false;
        SaveManager.Instance.NotifyDangoChanged();

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
