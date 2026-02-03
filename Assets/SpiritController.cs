using UnityEngine;
using UnityEngine.Serialization;

public class SpiritController : MonoBehaviour
{
    const float DefaultMaxSpirit = 100f;

    [Header("数値")]
    [FormerlySerializedAs("energy")]
    public float spirit = 100f;
    [FormerlySerializedAs("maxEnergy")]
    public float maxSpirit = 100f;

    [Header("自然減少")]
    [SerializeField]
    float naturalDecayPerMinute = 2.5f;

    [SerializeField]
    float decayIntervalSeconds = 60f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    float decayTimer;
    bool naturalDecayEnabled = true;

    System.Action<float, float> spiritChanged;
    public event System.Action OnSpiritEmpty;
    public event System.Action OnSpiritRecovered;
    public event System.Action<float, float> SpiritChanged
    {
        add
        {
            spiritChanged += value;
            if (initialized)
            {
                value?.Invoke(spirit, maxSpirit);
            }
        }
        remove
        {
            spiritChanged -= value;
        }
    }

    bool initialized;
    bool isSpiritEmpty;
    StatGauge spiritGauge;

    public float SpiritNormalized => spiritGauge != null ? spiritGauge.Normalized : (maxSpirit > 0f ? Mathf.Clamp01(spirit / maxSpirit) : 0f);

    void Awake()
    {
        isSpiritEmpty = false;
        EnsureDefaults();
        ResolveWorldConfigIfNeeded();

        InitializeIfNeeded("Awake");
    }

    void Start()
    {
        SetSpiritRatio(0.8f);
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

    public void ChangeSpirit(float amount)
    {
        spiritGauge.Add(amount);
        SyncGaugeToValues();

        NotifySpiritChanged("ChangeSpirit");
        UpdateSpiritEmptyState();
    }

    public void AddSpirit(float amount)
    {
        ChangeSpirit(amount);
    }

    public void AddSpiritRatio(float ratio)
    {
        ChangeSpirit(maxSpirit * ratio);
    }

    public void Recover(float amount)
    {
        ChangeSpirit(amount);
    }

    public void SetSpirit(float value, string reason = "SetSpirit")
    {
        spiritGauge.SetCurrent(value);
        SyncGaugeToValues();

        NotifySpiritChanged(reason);
        UpdateSpiritEmptyState();
    }

    public void SetSpiritRatio(float ratio)
    {
        SetSpirit(maxSpirit * Mathf.Clamp01(ratio), "SetSpiritRatio");
    }

    public void ApplyHeal(float healRatio = 0.4f)
    {
        ApplyHealInternal(healRatio, allowWhenCritical: false, logContext: "だんご");
    }

    public void ApplyHealFromMagicCircle(float healRatio = 0.4f)
    {
        ApplyHealInternal(healRatio, allowWhenCritical: true, logContext: "magic circle");
    }

    void ApplyHealInternal(float healRatio, bool allowWhenCritical, string logContext)
    {
        float healAmount = maxSpirit * healRatio;
        ChangeSpirit(healAmount);
    }

    void HandleNaturalDecay()
    {
        if (!naturalDecayEnabled)
        {
            return;
        }

        if (naturalDecayPerMinute <= 0f)
        {
            return;
        }

        if (spirit <= 0f)
        {
            return;
        }

        decayTimer += Time.deltaTime;
        if (decayTimer < decayIntervalSeconds)
        {
            return;
        }

        int ticks = Mathf.FloorToInt(decayTimer / decayIntervalSeconds);
        decayTimer -= ticks * decayIntervalSeconds;
        float decayAmount = naturalDecayPerMinute * ticks;
        ChangeSpirit(-decayAmount);
    }

    void NotifySpiritChanged(string reason)
    {
        spiritChanged?.Invoke(spirit, maxSpirit);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        ResolveWorldConfigIfNeeded();
        initialized = true;
        InitializeGauge();
        NotifySpiritChanged(reason);
        UpdateSpiritEmptyState();
    }

    public bool HasNoSpirit()
    {
        return spirit <= 0f;
    }

    public bool HasValidValues()
    {
        return !float.IsNaN(maxSpirit)
            && maxSpirit > 0f
            && !float.IsNaN(spirit)
            && spirit >= 0f
            && spirit <= maxSpirit;
    }

    void EnsureDefaults()
    {
        if (float.IsNaN(maxSpirit) || maxSpirit <= 0f)
            maxSpirit = DefaultMaxSpirit;

        if (float.IsNaN(spirit))
            spirit = maxSpirit;

        spirit = Mathf.Clamp(spirit, 0f, maxSpirit);
    }

    void ResolveWorldConfigIfNeeded()
    {
        if (worldConfig != null)
            return;

        worldConfig = WorldConfig.LoadDefault();
    }

    void InitializeGauge()
    {
        if (spiritGauge == null)
            spiritGauge = new StatGauge(maxSpirit, spirit);
        else
            spiritGauge.Reset(spirit, maxSpirit);
    }

    void SyncGaugeToValues()
    {
        spirit = spiritGauge.Current;
        maxSpirit = spiritGauge.Max;
    }

    void UpdateSpiritEmptyState()
    {
        if (spirit <= 0f)
        {
            if (spirit < 0f)
            {
                spirit = 0f;
                if (spiritGauge != null)
                {
                    spiritGauge.SetCurrent(0f);
                }
            }

            if (!isSpiritEmpty)
            {
                isSpiritEmpty = true;
                OnSpiritEmpty?.Invoke();
            }
        }
        else
        {
            if (isSpiritEmpty)
            {
                isSpiritEmpty = false;
                OnSpiritRecovered?.Invoke();
            }
        }
    }

    void LogSpiritInitialized(string context)
    {
    }

}
