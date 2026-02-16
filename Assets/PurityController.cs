using UnityEngine;
using UnityEngine.Serialization;

public class PurityController : MonoBehaviour
{
    const float DefaultMaxPurity = 100f;

    [Header("数値")]
    [FormerlySerializedAs("kegare")]
    public float purity = 100f;
    [FormerlySerializedAs("maxKegare")]
    public float maxPurity = 100f;

    [Header("自然減少")]
    [SerializeField]
    [FormerlySerializedAs("naturalIncreasePerMinute")]
    float naturalDecayPerMinute = 2f;

    [SerializeField]
    [FormerlySerializedAs("increaseIntervalSeconds")]
    float decayIntervalSeconds = 60f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    float decayTimer;
    bool naturalDecayEnabled = true;

    System.Action<float, float> purityChanged;
    public event System.Action OnPurityEmpty;
    public event System.Action<string> OnPurityRecovered;
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

    bool initialized;
    bool isPurityEmpty;
    StatGauge purityGauge;

    public float PurityNormalized => purityGauge != null ? purityGauge.Normalized : (maxPurity > 0f ? Mathf.Clamp01(purity / maxPurity) : 0f);
    public bool IsPurityEmpty => isPurityEmpty;

    void Awake()
    {
        EnsureDefaults();
        ResolveWorldConfigIfNeeded();

        InitializeIfNeeded("Awake");
    }

    void Start()
    {
        SetPurityRatio(0.8f);
        InitializeIfNeeded("Start");
    }

    void Update()
    {
        HandleNaturalDecay();
    }

    public bool SetNaturalDecayEnabled(bool enabled)
    {
        if (naturalDecayEnabled == enabled)
        {
            return false;
        }

        naturalDecayEnabled = enabled;
        if (!naturalDecayEnabled)
        {
            decayTimer = 0f;
        }

        return true;
    }

    public void ChangePurity(float amount)
    {
        purityGauge.Add(amount);
        SyncGaugeToValues();

        NotifyPurityChanged("ChangePurity");
        UpdatePurityEmptyState("ChangePurity");
    }

    public void AddPurity(float amount, string reason = "AddPurity")
    {
        ChangePurity(amount);
    }

    public void AddPurityRatio(float ratio)
    {
        ChangePurity(maxPurity * ratio);
    }

    public void RecoverPurityByRatio(float ratio)
    {
        AddPurityRatio(Mathf.Clamp01(ratio));
    }

    public void SetPurity(float value, string reason = "SetPurity")
    {
        purityGauge.SetCurrent(value);
        SyncGaugeToValues();

        NotifyPurityChanged(reason);
        UpdatePurityEmptyState(reason);
    }

    public void SetPurityRatio(float ratio)
    {
        SetPurity(maxPurity * Mathf.Clamp01(ratio), "SetPurityRatio");
    }

    void ApplyPurityDecay(float decayAmount)
    {
        if (decayAmount <= 0f)
            return;

        ChangePurity(-decayAmount);

        var stateController = CurrentYokaiContext.StateController;
        if (stateController != null)
        {
            stateController.NotifyStatusChanged();
        }
    }

    void HandleNaturalDecay()
    {
        if (!naturalDecayEnabled)
            return;

        if (naturalDecayPerMinute <= 0f)
            return;

        if (purity <= 0f)
            return;

        decayTimer += Time.deltaTime;
        if (decayTimer < decayIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(decayTimer / decayIntervalSeconds);
        decayTimer -= ticks * decayIntervalSeconds;
        float decayAmount = naturalDecayPerMinute * ticks;
        ApplyPurityDecay(decayAmount);
    }

    void NotifyPurityChanged(string reason)
    {
        purityChanged?.Invoke(purity, maxPurity);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        ResolveWorldConfigIfNeeded();
        InitializeGauge();
        UpdatePurityEmptyState(reason);
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
    }

    void ResolveWorldConfigIfNeeded()
    {
        if (worldConfig != null)
            return;

        worldConfig = WorldConfig.LoadDefault();
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

    void UpdatePurityEmptyState(string reason)
    {
        if (purity <= 0f)
        {
            if (purity < 0f)
            {
                purity = 0f;
                if (purityGauge != null)
                {
                    purityGauge.SetCurrent(0f);
                }
            }

            if (!isPurityEmpty)
            {
                isPurityEmpty = true;
                OnPurityEmpty?.Invoke();
            }
        }
        else
        {
            var stateController = CurrentYokaiContext.StateController;
            if (stateController != null && stateController.currentState != YokaiState.PurityEmpty)
            {
                return;
            }

            if (isPurityEmpty)
            {
                isPurityEmpty = false;
                OnPurityRecovered?.Invoke(reason);
            }
        }
    }
}
