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

    System.Action<float, float> purityChanged;
    public event System.Action OnPurityEmpty;
    public event System.Action OnPurityRecovered;
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
        if (worldConfig == null)
            Debug.LogError("[PURITY] WorldConfig not set in Inspector");

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

    public void ChangePurity(float amount)
    {
        purityGauge.Add(amount);
        SyncGaugeToValues();

        NotifyPurityChanged("ChangePurity");
        UpdatePurityEmptyState();
    }

    public void AddPurity(float amount, string reason = "AddPurity")
    {
        ChangePurity(amount);
    }

    public void AddPurityRatio(float ratio)
    {
        ChangePurity(maxPurity * ratio);
    }

    public void SetPurity(float value, string reason = "SetPurity")
    {
        purityGauge.SetCurrent(value);
        SyncGaugeToValues();

        NotifyPurityChanged(reason);
        UpdatePurityEmptyState();
    }

    public void SetPurityRatio(float ratio)
    {
        SetPurity(maxPurity * Mathf.Clamp01(ratio), "SetPurityRatio");
    }

    void HandleNaturalDecay()
    {
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
        ChangePurity(-decayAmount);
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
        InitializeGauge();
        UpdatePurityEmptyState();
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

    void UpdatePurityEmptyState()
    {
        bool shouldBeEmpty = purity <= 0f;
        if (shouldBeEmpty == isPurityEmpty)
            return;

        isPurityEmpty = shouldBeEmpty;
        if (isPurityEmpty)
            OnPurityEmpty?.Invoke();
        else
            OnPurityRecovered?.Invoke();
    }
}
