﻿using UnityEngine;
using Yokai;

public class KegareManager : MonoBehaviour
{
    const float DefaultMaxKegare = 100f;

    [Header("数値")]
    public float kegare = 0f;
    public float maxKegare = 100f;

    [Header("自然増加")]
    [SerializeField]
    float naturalIncreasePerMinute = 2f;

    [SerializeField]
    float increaseIntervalSeconds = 60f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    [Header("Dependencies")]
    [SerializeField]
    YokaiStateController stateController;

    [Header("演出")]
    public float emergencyPurifyValue = 30f;

    [Header("Mentor Message")]
    [SerializeField]
    float dangerThresholdRatio = 0.7f;

    public bool isKegareMax { get; private set; }
    GameObject currentYokai;
    float increaseTimer;
    bool isInDanger;

    public event System.Action EmergencyPurifyRequested;
    System.Action<float, float> kegareChanged;
    public event System.Action<float, float> KegareChanged
    {
        add
        {
            kegareChanged += value;
            if (initialized)
            {
                Debug.Log($"[PURIFY] KegareChanged invoked. reason=EventSubscribe kegare={kegare:0.##}/{maxKegare:0.##}");
                value?.Invoke(kegare, maxKegare);
            }
        }
        remove
        {
            kegareChanged -= value;
        }
    }

    bool initialized;

    void OnEnable()
    {
        Debug.Log($"[PURIFY][OnEnable] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
    }

    void Awake()
    {
        Debug.Log($"[PURIFY][Awake] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        EnsureDefaults();
        if (worldConfig == null)
        {
            worldConfig = WorldConfig.LoadDefault();

            if (worldConfig == null)
            {
                Debug.LogWarning("[PURIFY] WorldConfig が見つかりません: Resources/WorldConfig_Yokai");
            }
        }

        InitializeIfNeeded("Awake");
    }

    void Start()
    {
        Debug.Log($"[PURIFY][Start] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        InitializeIfNeeded("Start");
    }

    void Update()
    {
        HandleNaturalIncrease();
    }

    public void BindCurrentYokai(GameObject yokai)
    {
        currentYokai = yokai;
    }

    public void AddKegare(float amount)
    {
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("AddKegare");
    }

    public void SetKegare(float value, string reason = null)
    {
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(value, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("SetKegare");
    }

    public void ApplyPurify(float purifyRatio = 0.25f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: false, logContext: "おきよめ");
    }

    public void Purify()
    {
        ApplyPurify();
    }

    public void ApplyPurifyFromMagicCircle(float purifyRatio = 0.45f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: true, logContext: "magic circle");
    }

    void ApplyPurifyInternal(float purifyRatio, bool allowWhenCritical, string logContext)
    {
        if (!allowWhenCritical && TryGetRecoveryBlockReason(out _))
        {
            return;
        }

        bool wasKegareMax = isKegareMax;
        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        Debug.Log($"[PURIFY] {logContext} amount={purifyAmount:0.##} kegare={kegare:0.##}/{maxKegare:0.##}");
        NotifyKegareChanged("ApplyPurifyInternal");
    }

    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null && stateController.currentState != YokaiState.KegareMax)
            return;

        if (worldConfig != null)
        {
            Debug.Log($"[PURIFY] {worldConfig.recoveredMessage}");
        }

        EmergencyPurifyRequested?.Invoke();
    }

    bool TryGetRecoveryBlockReason(out string reason)
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        bool isPurifying = stateController != null && stateController.isPurifying;
        bool isEnergyEmpty = stateController != null && stateController.IsEnergyEmpty();
        if (!isPurifying && !isEnergyEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (isPurifying && isEnergyEmpty)
            reason = "魔法陣 / 霊力0";
        else if (isPurifying)
            reason = "魔法陣";
        else
            reason = "霊力0";

        return true;
    }

    public void ExecuteEmergencyPurify()
    {
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("ExecuteEmergencyPurify");
    }

    void HandleNaturalIncrease()
    {
        if (naturalIncreasePerMinute <= 0f)
            return;

        if (isKegareMax)
            return;

        increaseTimer += Time.deltaTime;
        if (increaseTimer < increaseIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(increaseTimer / increaseIntervalSeconds);
        increaseTimer -= ticks * increaseIntervalSeconds;
        float increaseAmount = naturalIncreasePerMinute * ticks;
        AddKegare(increaseAmount);
    }

    void SyncKegareMaxState(bool wasKegareMax, bool requestRelease, bool triggerEnter = true)
    {
        bool isNowMax = kegare >= maxKegare;
        isKegareMax = isNowMax;

        if (isNowMax && !wasKegareMax && triggerEnter)
        {
            EnterKegareMax();
        }
        else if (!isNowMax && wasKegareMax && requestRelease)
        {
            ExitKegareMax();
        }
    }

    void EnterKegareMax()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.EnterKegareMax();

        MentorMessageService.ShowHint(OnmyojiHintType.KegareMax);
    }

    void ExitKegareMax()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.RequestReleaseKegareMax();

        MentorMessageService.ShowHint(OnmyojiHintType.KegareRecovered);
    }

    void NotifyKegareChanged(string reason)
    {
        UpdateDangerState();
        Debug.Log($"[PURIFY] KegareChanged invoked. reason={reason} kegare={kegare:0.##}/{maxKegare:0.##}");
        kegareChanged?.Invoke(kegare, maxKegare);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        LogKegareInitialized(reason);
        BindCurrentYokai(CurrentYokaiContext.Current);
        SyncKegareMaxState(isKegareMax, requestRelease: false);
        CacheDangerState();
        initialized = true;
        NotifyKegareChanged(reason);
    }

    public bool HasNoKegare()
    {
        return kegare <= 0f;
    }

    public bool HasValidValues()
    {
        return !float.IsNaN(maxKegare)
            && maxKegare > 0f
            && !float.IsNaN(kegare)
            && kegare >= 0f
            && kegare <= maxKegare;
    }

    void EnsureDefaults()
    {
        if (float.IsNaN(maxKegare) || maxKegare <= 0f)
            maxKegare = DefaultMaxKegare;

        if (float.IsNaN(kegare))
            kegare = 0f;

        kegare = Mathf.Clamp(kegare, 0f, maxKegare);
        isKegareMax = kegare >= maxKegare;
    }

    void LogKegareInitialized(string context)
    {
        Debug.Log($"[PURIFY] Initialized ({context}) kegare={kegare:0.##}/{maxKegare:0.##}");
    }

    void CacheDangerState()
    {
        isInDanger = IsDangerState();
    }

    void UpdateDangerState()
    {
        bool isDanger = IsDangerState();
        if (isDanger && !isInDanger)
            MentorMessageService.ShowHint(OnmyojiHintType.KegareWarning);

        isInDanger = isDanger;
    }

    bool IsDangerState()
    {
        if (isKegareMax)
            return false;

        float threshold = maxKegare * Mathf.Clamp01(dangerThresholdRatio);
        return kegare >= threshold;
    }

}