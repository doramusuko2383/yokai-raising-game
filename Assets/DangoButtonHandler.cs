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
        if (subscribed)
            return;

        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.OnDangoChanged += RefreshUI;
        subscribed = true;
    }

    public void RefreshUI()
    {
        // 参照が刺さってないと即落ちするので安全化
        if (buttonText == null || buttonBackground == null)
            return;

        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
            return;

        int count = Mathf.Clamp(save.dango.currentCount, 0, 3);
        bool hasDango = count > 0;

        if (hasDango)
            ApplyEatMode();
        else
            ApplyAdMode();

        // Busy中は押せない（広告SDK差し替え時の事故防止）
        if (button != null)
            button.interactable = !isBusy;
    }

    public void OnClickDango()
    {
        if (isBusy)
            return;

        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
            return;

        int countBefore = save.dango.currentCount;

        if (countBefore > 0)
        {
            if (actionController == null)
                actionController = FindObjectOfType<UIActionController>(true);

            actionController?.Execute(YokaiAction.EatDango);

            return;
        }

        ShowRewardAd();
    }

    void ApplyEatMode()
    {
        buttonText.text = "だんご";
        buttonBackground.color = normalColor;
        StopPulse();
        isAdMode = false;
    }

    void ApplyAdMode()
    {
        buttonText.text = "広告で回復";
        buttonBackground.color = adColor;
        StartPulse();
        isAdMode = true;
    }

    void ShowRewardAd()
    {
        // 本番は広告SDK呼び出しで非同期になる。今の段階でも多重クリック防止のためBusyにする。
        isBusy = true;
        if (button != null) button.interactable = false;

        Debug.Log("[AD] Rewarded Ad Simulated");

        var save = SaveManager.Instance.CurrentSave;
        save.dango.currentCount = Mathf.Clamp(save.dango.currentCount + 1, 0, 3);
        save.dango.lastGeneratedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveManager.Instance.MarkDirty();

        // シミュレーションなので即解除。広告SDK導入後は「成功コールバック」で解除する。
        isBusy = false;
        SaveManager.Instance.NotifyDangoChanged();

        if (button != null)
            button.interactable = true;

        RefreshUI();
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
