using UnityEngine;
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
        Debug.Log($"[KegareManager][OnEnable][Enter] kegare={kegare:0.##} maxKegare={maxKegare:0.##} stateController={(stateController == null ? \"null\" : \"ok\")}");
        Debug.Log("[KegareManager][OnEnable][ENTER] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " stateController=" + (stateController == null ? "null" : "ok"));
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
        Debug.Log("[KegareManager][OnEnable][Exit]");
        Debug.Log("[KegareManager][OnEnable][EXIT] currentYokai=" + (currentYokai == null ? "null" : "ok"));
    }

    void OnDisable()
    {
        Debug.Log("[KegareManager][OnDisable][Enter]");
        Debug.Log("[KegareManager][OnDisable][ENTER] currentYokai=" + (currentYokai == null ? "null" : "ok"));
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
        Debug.Log("[KegareManager][OnDisable][Exit]");
        Debug.Log("[KegareManager][OnDisable][EXIT]");
    }

    void Awake()
    {
        Debug.Log($"[KegareManager][Awake][Enter] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        Debug.Log("[KegareManager][Awake][ENTER] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " stateController=" + (stateController == null ? "null" : "ok"));
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
        Debug.Log($"[KegareManager][Awake][Exit] kegare={kegare:0.##} maxKegare={maxKegare:0.##} initialized={initialized}");
        Debug.Log("[KegareManager][Awake][EXIT] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " initialized=" + initialized);
    }

    void Start()
    {
        Debug.Log($"[KegareManager][Start][Enter] kegare={kegare:0.##} maxKegare={maxKegare:0.##} initialized={initialized}");
        Debug.Log("[KegareManager][Start][ENTER] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " initialized=" + initialized);
        InitializeIfNeeded("Start");
        Debug.Log($"[KegareManager][Start][Exit] kegare={kegare:0.##} maxKegare={maxKegare:0.##} initialized={initialized}");
        Debug.Log("[KegareManager][Start][EXIT] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " initialized=" + initialized);
    }

    void Update()
    {
        Debug.Log($"[KegareManager][Update][Enter] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
        Debug.Log("[KegareManager][Update][ENTER] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " isKegareMax=" + isKegareMax);
        HandleNaturalIncrease();
        Debug.Log($"[KegareManager][Update][Exit] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
        Debug.Log("[KegareManager][Update][EXIT] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " isKegareMax=" + isKegareMax);
    }

    public void BindCurrentYokai(GameObject yokai)
    {
        Debug.Log($"[KegareManager][BindCurrentYokai][Enter] yokai={(yokai == null ? \"null\" : yokai.name)}");
        Debug.Log("[KegareManager][BindCurrentYokai][ENTER] yokai=" + (yokai == null ? "null" : yokai.name));
        currentYokai = yokai;
        Debug.Log("[KegareManager][BindCurrentYokai][Exit]");
        Debug.Log("[KegareManager][BindCurrentYokai][EXIT] currentYokai=" + (currentYokai == null ? "null" : currentYokai.name));
    }

    public void AddKegare(float amount)
    {
        Debug.Log($"[KegareManager][AddKegare][Enter] amount={amount:0.##} kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("AddKegare");
        Debug.Log($"[KegareManager][AddKegare][Exit] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
    }

    public void SetKegare(float value, string reason = null)
    {
        Debug.Log($"[KegareManager][SetKegare][Enter] value={value:0.##} reason={reason} kegare={kegare:0.##}/{maxKegare:0.##}");
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(value, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("SetKegare");
        Debug.Log($"[KegareManager][SetKegare][Exit] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
    }

    public void ApplyPurify(float purifyRatio = 0.25f)
    {
        Debug.Log($"[KegareManager][ApplyPurify][Enter] purifyRatio={purifyRatio:0.##}");
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: false, logContext: "おきよめ");
        Debug.Log("[KegareManager][ApplyPurify][Exit]");
    }

    public void Purify()
    {
        Debug.Log("[KegareManager][Purify][Enter]");
        ApplyPurify();
        Debug.Log("[KegareManager][Purify][Exit]");
    }

    public void ApplyPurifyFromMagicCircle(float purifyRatio = 0.45f)
    {
        Debug.Log($"[KegareManager][ApplyPurifyFromMagicCircle][Enter] purifyRatio={purifyRatio:0.##}");
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: true, logContext: "magic circle");
        Debug.Log("[KegareManager][ApplyPurifyFromMagicCircle][Exit]");
    }

    void ApplyPurifyInternal(float purifyRatio, bool allowWhenCritical, string logContext)
    {
        Debug.Log($"[KegareManager][ApplyPurifyInternal][Enter] purifyRatio={purifyRatio:0.##} allowWhenCritical={allowWhenCritical} logContext={logContext}");
        if (!allowWhenCritical && TryGetRecoveryBlockReason(out _))
        {
            Debug.Log("[KegareManager][ApplyPurifyInternal][EarlyReturn] reason=blocked");
            Debug.Log("[KegareManager][ApplyPurifyInternal][EARLY_RETURN] reason=blocked");
            return;
        }

        bool wasKegareMax = isKegareMax;
        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        Debug.Log($"[PURIFY] {logContext} amount={purifyAmount:0.##} kegare={kegare:0.##}/{maxKegare:0.##}");
        NotifyKegareChanged("ApplyPurifyInternal");
        Debug.Log($"[KegareManager][ApplyPurifyInternal][Exit] kegare={kegare:0.##}/{maxKegare:0.##}");
    }

    public void OnClickAdWatch()
    {
        Debug.Log($"[KegareManager][OnClickAdWatch][Enter] kegare={kegare:0.##}/{maxKegare:0.##} stateController={(stateController == null ? \"null\" : \"ok\")}");
        Debug.Log("[KegareManager][OnClickAdWatch][ENTER] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##") + " stateController=" + (stateController == null ? "null" : "ok"));
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null && stateController.currentState != YokaiState.KegareMax)
        {
            Debug.Log("[KegareManager][OnClickAdWatch][EarlyReturn] reason=notKegareMaxState");
            Debug.Log("[KegareManager][OnClickAdWatch][EARLY_RETURN] reason=notKegareMaxState currentState=" + (stateController == null ? "null" : stateController.currentState.ToString()));
            return;
        }

        if (worldConfig != null)
        {
            Debug.Log($"[PURIFY] {worldConfig.recoveredMessage}");
        }

        EmergencyPurifyRequested?.Invoke();
        Debug.Log("[KegareManager][OnClickAdWatch][Exit]");
        Debug.Log("[KegareManager][OnClickAdWatch][EXIT] kegare=" + kegare.ToString("0.##") + " maxKegare=" + maxKegare.ToString("0.##"));
    }

    bool TryGetRecoveryBlockReason(out string reason)
    {
        Debug.Log($"[KegareManager][TryGetRecoveryBlockReason][Enter] stateController={(stateController == null ? \"null\" : \"ok\")}");
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        bool isPurifying = stateController != null && stateController.isPurifying;
        bool isEnergyEmpty = stateController != null && stateController.IsEnergyEmpty();
        if (!isPurifying && !isEnergyEmpty)
        {
            reason = string.Empty;
            Debug.Log("[KegareManager][TryGetRecoveryBlockReason][Exit] blocked=false");
            Debug.Log("[KegareManager][TryGetRecoveryBlockReason][EXIT] blocked=false isPurifying=" + isPurifying + " isEnergyEmpty=" + isEnergyEmpty);
            return false;
        }

        if (isPurifying && isEnergyEmpty)
            reason = "魔法陣 / 霊力0";
        else if (isPurifying)
            reason = "魔法陣";
        else
            reason = "霊力0";

        Debug.Log($"[KegareManager][TryGetRecoveryBlockReason][Exit] blocked=true reason={reason}");
        return true;
    }

    public void ExecuteEmergencyPurify()
    {
        Debug.Log($"[KegareManager][ExecuteEmergencyPurify][Enter] emergencyPurifyValue={emergencyPurifyValue:0.##}");
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged("ExecuteEmergencyPurify");
        Debug.Log($"[KegareManager][ExecuteEmergencyPurify][Exit] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
    }

    void HandleNaturalIncrease()
    {
        if (naturalIncreasePerMinute <= 0f)
        {
            Debug.Log($"[KegareManager][HandleNaturalIncrease][EarlyReturn] reason=naturalIncreasePerMinute<=0 value={naturalIncreasePerMinute:0.##}");
            Debug.Log("[KegareManager][HandleNaturalIncrease][EARLY_RETURN] reason=naturalIncreasePerMinute<=0 value=" + naturalIncreasePerMinute.ToString("0.##"));
            return;
        }

        if (isKegareMax)
        {
            Debug.Log("[KegareManager][HandleNaturalIncrease][EarlyReturn] reason=isKegareMax");
            Debug.Log("[KegareManager][HandleNaturalIncrease][EARLY_RETURN] reason=isKegareMax kegare=" + kegare.ToString("0.##"));
            return;
        }

        increaseTimer += Time.deltaTime;
        if (increaseTimer < increaseIntervalSeconds)
        {
            Debug.Log($"[KegareManager][HandleNaturalIncrease][EarlyReturn] reason=intervalNotReached increaseTimer={increaseTimer:0.##} interval={increaseIntervalSeconds:0.##}");
            Debug.Log("[KegareManager][HandleNaturalIncrease][EARLY_RETURN] reason=intervalNotReached increaseTimer=" + increaseTimer.ToString("0.##") + " interval=" + increaseIntervalSeconds.ToString("0.##"));
            return;
        }

        int ticks = Mathf.FloorToInt(increaseTimer / increaseIntervalSeconds);
        increaseTimer -= ticks * increaseIntervalSeconds;
        float increaseAmount = naturalIncreasePerMinute * ticks;
        AddKegare(increaseAmount);
        Debug.Log($"[KegareManager][HandleNaturalIncrease][Exit] ticks={ticks} increaseAmount={increaseAmount:0.##} kegare={kegare:0.##}/{maxKegare:0.##}");
    }

    void SyncKegareMaxState(bool wasKegareMax, bool requestRelease, bool triggerEnter = true)
    {
        Debug.Log($"[KegareManager][SyncKegareMaxState][Enter] wasKegareMax={wasKegareMax} requestRelease={requestRelease} triggerEnter={triggerEnter} kegare={kegare:0.##}/{maxKegare:0.##}");
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
        Debug.Log($"[KegareManager][SyncKegareMaxState][Exit] isKegareMax={isKegareMax}");
    }

    void EnterKegareMax()
    {
        Debug.Log("[KegareManager][EnterKegareMax][Enter]");
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.EnterKegareMax();

        MentorMessageService.ShowHint(OnmyojiHintType.KegareMax);
        Debug.Log("[KegareManager][EnterKegareMax][Exit]");
    }

    void ExitKegareMax()
    {
        Debug.Log("[KegareManager][ExitKegareMax][Enter]");
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.RequestReleaseKegareMax();

        MentorMessageService.ShowHint(OnmyojiHintType.KegareRecovered);
        Debug.Log("[KegareManager][ExitKegareMax][Exit]");
    }

    void NotifyKegareChanged(string reason)
    {
        Debug.Log($"[KegareManager][NotifyKegareChanged][Enter] reason={reason} kegare={kegare:0.##}/{maxKegare:0.##}");
        UpdateDangerState();
        Debug.Log($"[PURIFY] KegareChanged invoked. reason={reason} kegare={kegare:0.##}/{maxKegare:0.##}");
        kegareChanged?.Invoke(kegare, maxKegare);
        Debug.Log("[KegareManager][NotifyKegareChanged][Exit]");
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
        {
            Debug.Log($"[KegareManager][InitializeIfNeeded][EarlyReturn] reason=alreadyInitialized context={reason}");
            Debug.Log("[KegareManager][InitializeIfNeeded][EARLY_RETURN] reason=alreadyInitialized context=" + reason);
            return;
        }

        Debug.Log($"[KegareManager][InitializeIfNeeded][Enter] context={reason} kegare={kegare:0.##}/{maxKegare:0.##}");
        EnsureDefaults();
        LogKegareInitialized(reason);
        BindCurrentYokai(CurrentYokaiContext.Current);
        SyncKegareMaxState(isKegareMax, requestRelease: false);
        CacheDangerState();
        initialized = true;
        NotifyKegareChanged(reason);
        Debug.Log($"[KegareManager][InitializeIfNeeded][Exit] context={reason} initialized={initialized}");
    }

    public bool HasNoKegare()
    {
        Debug.Log($"[KegareManager][HasNoKegare][Enter] kegare={kegare:0.##}");
        return kegare <= 0f;
    }

    public bool HasValidValues()
    {
        Debug.Log($"[KegareManager][HasValidValues][Enter] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        return !float.IsNaN(maxKegare)
            && maxKegare > 0f
            && !float.IsNaN(kegare)
            && kegare >= 0f
            && kegare <= maxKegare;
    }

    void EnsureDefaults()
    {
        Debug.Log($"[KegareManager][EnsureDefaults][Enter] kegare={kegare:0.##} maxKegare={maxKegare:0.##}");
        if (float.IsNaN(maxKegare) || maxKegare <= 0f)
            maxKegare = DefaultMaxKegare;

        if (float.IsNaN(kegare))
            kegare = 0f;

        kegare = Mathf.Clamp(kegare, 0f, maxKegare);
        isKegareMax = kegare >= maxKegare;
        Debug.Log($"[KegareManager][EnsureDefaults][Exit] kegare={kegare:0.##}/{maxKegare:0.##} isKegareMax={isKegareMax}");
    }

    void LogKegareInitialized(string context)
    {
        Debug.Log($"[PURIFY] Initialized ({context}) kegare={kegare:0.##}/{maxKegare:0.##}");
    }

    void CacheDangerState()
    {
        Debug.Log("[KegareManager][CacheDangerState][Enter]");
        isInDanger = IsDangerState();
        Debug.Log($"[KegareManager][CacheDangerState][Exit] isInDanger={isInDanger}");
    }

    void UpdateDangerState()
    {
        Debug.Log("[KegareManager][UpdateDangerState][Enter]");
        bool isDanger = IsDangerState();
        if (isDanger && !isInDanger)
            MentorMessageService.ShowHint(OnmyojiHintType.KegareWarning);

        isInDanger = isDanger;
        Debug.Log($"[KegareManager][UpdateDangerState][Exit] isInDanger={isInDanger}");
    }

    bool IsDangerState()
    {
        if (isKegareMax)
        {
            Debug.Log("[KegareManager][IsDangerState][EarlyReturn] reason=isKegareMax");
            Debug.Log("[KegareManager][IsDangerState][EARLY_RETURN] reason=isKegareMax kegare=" + kegare.ToString("0.##"));
            return false;
        }

        float threshold = maxKegare * Mathf.Clamp01(dangerThresholdRatio);
        Debug.Log($"[KegareManager][IsDangerState][Exit] threshold={threshold:0.##} kegare={kegare:0.##}");
        return kegare >= threshold;
    }

}
