﻿using UnityEngine;
using Yokai;

public class KegareManager : MonoBehaviour
{
    const float DefaultMaxPurity = 100f;

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

    public bool isKegareMax
    {
        get => isPurityEmpty;
        private set => isPurityEmpty = value;
    }
    public bool IsPurityEmpty => isPurityEmpty;
    GameObject currentYokai;
    float increaseTimer;
    bool isInDanger;
    bool isPurityEmpty;
    float maxPurity => maxKegare;
    float purity
    {
        get => Mathf.Clamp(maxPurity - kegare, 0f, maxPurity);
        set => kegare = Mathf.Clamp(maxPurity - value, 0f, maxPurity);
    }

    public event System.Action EmergencyPurifyRequested;
    System.Action<float, float> kegareChanged;
    public event System.Action<float, float> KegareChanged
    {
        add
        {
            kegareChanged += value;
            if (initialized)
            {
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
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
    }

    void Awake()
    {
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
        AddPurity(-amount, "AddKegare");
    }

    public void AddPurity(float amount, string reason = "AddPurity")
    {
        purity = Mathf.Clamp(purity + amount, 0f, maxPurity);
        SyncPurityEmptyState();

        NotifyKegareChanged(reason);
    }

    public void SetKegare(float value, string reason = null)
    {
        SetPurity(maxPurity - value, reason ?? "SetKegare");
    }

    public void SetPurity(float value, string reason = null)
    {
        purity = Mathf.Clamp(value, 0f, maxPurity);
        SyncPurityEmptyState();

        NotifyKegareChanged(reason ?? "SetPurity");
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

        float purifyAmount = maxPurity * purifyRatio;
        AddPurity(purifyAmount, "ApplyPurifyInternal");
    }

    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null && stateController.currentState != YokaiState.KegareMax)
            return;

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
        SetPurity(maxPurity - emergencyPurifyValue, "ExecuteEmergencyPurify");
    }

    void HandleNaturalIncrease()
    {
        if (naturalIncreasePerMinute <= 0f)
            return;

        if (isPurityEmpty)
            return;

        increaseTimer += Time.deltaTime;
        if (increaseTimer < increaseIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(increaseTimer / increaseIntervalSeconds);
        increaseTimer -= ticks * increaseIntervalSeconds;
        float increaseAmount = naturalIncreasePerMinute * ticks;
        AddPurity(-increaseAmount);
    }

    void SyncPurityEmptyState()
    {
        bool wasEmpty = isPurityEmpty;
        bool isNowEmpty = purity <= 0f;
        isPurityEmpty = isNowEmpty;

        if (isNowEmpty && !wasEmpty)
        {
            if (stateController == null)
                stateController = CurrentYokaiContext.ResolveStateController();

            if (stateController != null)
                stateController.OnKegareMax();
        }
    }

    void NotifyKegareChanged(string reason)
    {
        UpdateDangerState();
        kegareChanged?.Invoke(kegare, maxKegare);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        BindCurrentYokai(CurrentYokaiContext.Current);
        SyncPurityEmptyState();
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
            maxKegare = DefaultMaxPurity;

        if (float.IsNaN(kegare))
            kegare = 0f;

        kegare = Mathf.Clamp(kegare, 0f, maxKegare);
        isPurityEmpty = purity <= 0f;
    }

    void LogKegareInitialized(string context)
    {
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
        if (isPurityEmpty)
            return false;

        float threshold = maxPurity * Mathf.Clamp01(1f - dangerThresholdRatio);
        return purity <= threshold;
    }

}
