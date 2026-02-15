using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    public TMP_Text buttonText;
    public Image buttonBackground;
    [Tooltip("ボタンのClickable本体（未設定ならこのGameObjectのButtonを探す）")]
    public Button button;
    public Color normalColor = Color.white;
    public Color adColor = new Color(0.4f, 0.7f, 1f);

    [SerializeField]
    UIActionController actionController;

    bool isAdMode;
    bool isBusy; // 広告再生中などの多重クリック防止
    bool subscribed;
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
        if (!subscribed)
            return;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnDangoChanged -= RefreshUI;

        subscribed = false;
    }

    void TrySubscribe()
    {
        Debug.Log($"[DangoButtonHandler] TrySubscribe called. subscribed={subscribed}, hasSaveManager={SaveManager.Instance != null}");

        if (subscribed)
        {
            Debug.Log("[DangoButtonHandler] TrySubscribe skipped: already subscribed.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[DangoButtonHandler] TrySubscribe skipped: SaveManager.Instance is null.");
            return;
        }

        SaveManager.Instance.OnDangoChanged += RefreshUI;
        subscribed = true;
        Debug.Log("[DangoButtonHandler] TrySubscribe succeeded: subscribed to SaveManager.OnDangoChanged.");
    }

    public void RefreshUI()
    {
        bool isTargetGraphicMatched = button != null && buttonBackground != null && ReferenceEquals(button.targetGraphic, buttonBackground);
        Debug.Log($"[DangoButtonHandler] RefreshUI begin. isBusy={isBusy}, isAdMode={isAdMode}, hasButtonText={buttonText != null}, hasButtonBackground={buttonBackground != null}, hasButton={button != null}, targetGraphicMatched={isTargetGraphicMatched}");

        if (button != null && buttonBackground != null && !ReferenceEquals(button.targetGraphic, buttonBackground))
            Debug.LogWarning("[DangoButtonHandler] RefreshUI validation: buttonBackground is not matched with Button.targetGraphic.");

        if (buttonText == null || buttonBackground == null)
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI aborted: buttonText or buttonBackground is null.");
            return;
        }

        var saveManager = SaveManager.Instance;
        if (saveManager == null)
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI aborted: SaveManager.Instance is null.");
            return;
        }

        var save = saveManager.CurrentSave;
        if (save == null)
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI aborted: CurrentSave is null.");
            return;
        }

        if (save.dango == null)
        {
            Debug.LogWarning("[DangoButtonHandler] RefreshUI aborted: save.dango is null.");
            return;
        }

        int rawCount = save.dango.currentCount;
        int count = Mathf.Clamp(rawCount, 0, 3);
        bool hasDango = count > 0;

        Debug.Log($"[DangoButtonHandler] RefreshUI state. rawCount={rawCount}, clampedCount={count}, hasDango={hasDango}, lastGeneratedUnixTime={save.dango.lastGeneratedUnixTime}");

        if (hasDango)
            ApplyEatMode();
        else
            ApplyAdMode();

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

    public void OnClickDango()
    {
        Debug.Log($"[DangoButtonHandler] OnClickDango begin. isBusy={isBusy}");

        if (isBusy)
        {
            Debug.Log("[DangoButtonHandler] OnClickDango ignored: handler is busy.");
            return;
        }

        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
        {
            Debug.LogWarning($"[DangoButtonHandler] OnClickDango aborted: save or dango is null. hasSave={save != null}, hasDango={save?.dango != null}");
            return;
        }

        int countBefore = save.dango.currentCount;
        Debug.Log($"[DangoButtonHandler] OnClickDango countBefore={countBefore}");

        if (countBefore > 0)
        {
            if (actionController == null)
                actionController = FindObjectOfType<UIActionController>(true);

            actionController?.Execute(YokaiAction.EatDango);
            Debug.Log($"[DangoButtonHandler] OnClickDango executed EatDango. actionControllerFound={actionController != null}");

            return;
        }

        ShowRewardAd();
    }

    void ApplyEatMode()
    {
        Debug.Log($"[DangoButtonHandler] ApplyEatMode. previousIsAdMode={isAdMode}");
        buttonText.text = "だんご";
        buttonBackground.color = normalColor;
        StopPulse();
        isAdMode = false;
    }

    void ApplyAdMode()
    {
        Debug.Log($"[DangoButtonHandler] ApplyAdMode. previousIsAdMode={isAdMode}");
        buttonText.text = "広告で回復";
        buttonBackground.color = adColor;
        StartPulse();
        isAdMode = true;
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

    void StartPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(Pulse());
    }

    void StopPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        transform.localScale = Vector3.one;
    }

    System.Collections.IEnumerator Pulse()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.05f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
        }
    }

    #endregion
}
