using UnityEngine;
using UnityEngine.Serialization;
using Yokai;

public class PurityController : MonoBehaviour
{
    const float DefaultMaxPurity = 100f;

    [Header("数値")]
    [FormerlySerializedAs("kegare")]
    public float purity = 100f;
    [FormerlySerializedAs("maxKegare")]
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

    public float MaxPurity => maxPurity;
    public bool IsPurityEmpty => isPurityEmpty;
    public bool IsPurityEmptyPublic => IsPurityEmpty;

    GameObject currentYokai;
    float increaseTimer;
    bool isInDanger;
    bool isPurityEmpty;
    bool initialized;
    StatGauge purityGauge;

    public float PurityNormalized => purityGauge != null ? purityGauge.Normalized : (maxPurity > 0f ? Mathf.Clamp01(purity / maxPurity) : 0f);

    public event System.Action EmergencyPurifyRequested;
    System.Action<float, float> purityChanged;
    public event System.Action<float, float> PurityChanged
    {
        add
        {
            purityChanged += value;
            if (initialized)
            {
                value?.Invoke(purity, maxPurity);
            }
        }
        remove
        {
            purityChanged -= value;
        }
    }

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

    public void AddPurity(float amount, string reason = "AddPurity")
    {
        purityGauge.Add(amount);
        SyncGaugeToValues();
        SyncPurityEmptyState();

        NotifyPurityChanged(reason);
    }

    public void AddPurityRatio(float ratio)
    {
        AddPurity(maxPurity * ratio, "AddPurityRatio");
    }

    public void SetPurity(float value, string reason = null)
    {
        purityGauge.SetCurrent(value);
        SyncGaugeToValues();
        SyncPurityEmptyState();

        NotifyPurityChanged(reason ?? "SetPurity");
    }

    public void SetPurityRatio(float ratio)
    {
        SetPurity(maxPurity * Mathf.Clamp01(ratio), "SetPurityRatio");
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
        bool isSpiritEmpty = stateController != null && stateController.currentState == YokaiState.EnergyEmpty;
        if (!isPurifying && !isSpiritEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (isPurifying && isSpiritEmpty)
            reason = "魔法陣 / 霊力0";
        else if (isPurifying)
            reason = "魔法陣";
        else
            reason = "霊力0";

        return true;
    }

    public void ExecuteEmergencyPurify()
    {
        AddPurity(emergencyPurifyValue, "ExecuteEmergencyPurify");
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

        if (wasEmpty == isNowEmpty)
            return;

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null)
            return;

        if (isNowEmpty)
            stateController.OnPurityEmpty();
        else
            stateController.OnPurityRecovered();
    }

    void NotifyPurityChanged(string reason)
    {
        UpdateDangerState();
        purityChanged?.Invoke(purity, maxPurity);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        BindCurrentYokai(CurrentYokaiContext.Current);
        InitializeGauge();
        SyncPurityEmptyState();
        CacheDangerState();
        initialized = true;
        NotifyPurityChanged(reason);
    }

    public bool HasNoPurity()
    {
        return purity <= 0f;
    }

    public bool HasValidValues()
    {
        return !float.IsNaN(maxPurity)
            && maxPurity > 0f
            && !float.IsNaN(purity)
            && purity >= 0f
            && purity <= maxPurity;
    }

    void EnsureDefaults()
    {
        if (float.IsNaN(maxPurity) || maxPurity <= 0f)
            maxPurity = DefaultMaxPurity;

        if (float.IsNaN(purity))
            purity = maxPurity;

        purity = Mathf.Clamp(purity, 0f, maxPurity);
        isPurityEmpty = purity <= 0f;
    }

    void InitializeGauge()
    {
        if (purityGauge == null)
            purityGauge = new StatGauge(maxPurity, purity);
        else
            purityGauge.Reset(purity, maxPurity);
    }

    void SyncGaugeToValues()
    {
        purity = purityGauge.Current;
        maxPurity = purityGauge.Max;
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
        if (isPurityEmpty)
            return false;

        float threshold = maxPurity * Mathf.Clamp01(1f - dangerThresholdRatio);
        return purity <= threshold;
    }
}
