﻿using UnityEngine;
using Yokai;

public class PurityManager : MonoBehaviour
{
    const float DefaultMaxPurity = 100f;

    [Header("数値")]
    public float purityValue = 0f;
    public float maxPurity = 100f;

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

    public bool isPurityEmpty
    {
        get => isPurityEmptyValue;
        private set => isPurityEmptyValue = value;
    }
    public bool IsPurityEmpty => isPurityEmptyValue;
    GameObject currentYokai;
    float increaseTimer;
    bool isInDanger;
    bool isPurityEmptyValue;
    float currentPurity
    {
        get => Mathf.Clamp(maxPurity - purityValue, 0f, maxPurity);
        set => purityValue = Mathf.Clamp(maxPurity - value, 0f, maxPurity);
    }

    public event System.Action EmergencyPurifyRequested;
    System.Action<float, float> purityChanged;
    public event System.Action<float, float> PurityChanged
    {
        add
        {
            purityChanged += value;
            if (initialized)
            {
                value?.Invoke(purityValue, maxPurity);
            }
        }
        remove
        {
            purityChanged -= value;
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
        SetPurityRatio(0.8f);
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

    public void AddPurity(float amount)
    {
        AddPurity(-amount, "AddPurity");
    }

    public void AddPurityRatio(float ratio)
    {
        AddPurity(maxPurity * ratio);
    }

    public void AddPurity(float amount, string reason = "AddPurity")
    {
        currentPurity = Mathf.Clamp(currentPurity + amount, 0f, maxPurity);
        SyncPurityEmptyState();

        NotifyPurityChanged(reason);
    }

    public void SetPurityValue(float value, string reason = null)
    {
        SetPurity(maxPurity - value, reason ?? "SetPurityValue");
    }

    public void SetPurityRatio(float ratio)
    {
        SetPurityValue(maxPurity * Mathf.Clamp01(ratio), "SetPurityRatio");
    }

    public void SetPurity(float value, string reason = null)
    {
        currentPurity = Mathf.Clamp(value, 0f, maxPurity);
        SyncPurityEmptyState();

        NotifyPurityChanged(reason ?? "SetPurity");
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

        if (stateController != null && stateController.currentState != YokaiState.PurityEmpty)
            return;

        EmergencyPurifyRequested?.Invoke();
    }

    bool TryGetRecoveryBlockReason(out string reason)
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        bool isPurifying = stateController != null && stateController.isPurifying;
        bool isEnergyEmpty = stateController != null && stateController.currentState == YokaiState.EnergyEmpty;
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

        if (isPurityEmptyValue)
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
        bool wasEmpty = isPurityEmptyValue;
        bool isNowEmpty = currentPurity <= 0f;
        isPurityEmptyValue = isNowEmpty;

        if (isNowEmpty && !wasEmpty)
        {
            if (stateController == null)
                stateController = CurrentYokaiContext.ResolveStateController();

            if (stateController != null)
                stateController.OnPurityEmpty();
        }
    }

    void NotifyPurityChanged(string reason)
    {
        UpdateDangerState();
        purityChanged?.Invoke(purityValue, maxPurity);
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
        NotifyPurityChanged(reason);
    }

    public bool HasNoPurity()
    {
        return purityValue <= 0f;
    }

    public bool HasValidValues()
    {
        return !float.IsNaN(maxPurity)
            && maxPurity > 0f
            && !float.IsNaN(purityValue)
            && purityValue >= 0f
            && purityValue <= maxPurity;
    }

    void EnsureDefaults()
    {
        if (float.IsNaN(maxPurity) || maxPurity <= 0f)
            maxPurity = DefaultMaxPurity;

        if (float.IsNaN(purityValue))
            purityValue = 0f;

        purityValue = Mathf.Clamp(purityValue, 0f, maxPurity);
        isPurityEmptyValue = currentPurity <= 0f;
    }

    void LogPurityInitialized(string context)
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
            MentorMessageService.ShowHint(OnmyojiHintType.PurityWarning);

        isInDanger = isDanger;
    }

    bool IsDangerState()
    {
        if (isPurityEmptyValue)
            return false;

        float threshold = maxPurity * Mathf.Clamp01(1f - dangerThresholdRatio);
        return currentPurity <= threshold;
    }

}
