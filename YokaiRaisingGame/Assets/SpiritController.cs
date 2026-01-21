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

    [Header("状態")]
    [SerializeField]
    bool hasEverHadSpirit;

    [Header("自然減少")]
    [SerializeField]
    float naturalDecayPerMinute = 2.5f;

    [SerializeField]
    float decayIntervalSeconds = 60f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    [Header("Dependencies")]
    [SerializeField]
    Yokai.YokaiStateController stateController;

    float decayTimer;

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
        hasEverHadSpirit = false;
        isSpiritEmpty = false;
        EnsureDefaults();
        if (stateController == null)
            Debug.LogError("[SPIRIT] StateController not set in Inspector");
        if (worldConfig == null)
        {
            worldConfig = WorldConfig.LoadDefault();

            if (worldConfig == null)
            {
            }
        }

        InitializeIfNeeded("Awake");
    }

    void Start()
    {
        SetSpiritRatio(0.8f);
        InitializeIfNeeded("Start");
    }

    void Update()
    {
        if (!hasEverHadSpirit && spirit > 0f)
        {
            hasEverHadSpirit = true;
        }
        HandleNaturalDecay();
    }

    public void ChangeSpirit(float amount)
    {
        float previousSpirit = spirit;
        spiritGauge.Add(amount);
        SyncGaugeToValues();
        if (!hasEverHadSpirit && (previousSpirit > 0f || spirit > 0f))
        {
            hasEverHadSpirit = true;
        }

        NotifySpiritChanged("ChangeSpirit");
        UpdateSpiritEmptyState(previousSpirit);
    }

    public void AddSpirit(float amount)
    {
        ChangeSpirit(amount);
    }

    public void AddSpiritRatio(float ratio)
    {
        ChangeSpirit(maxSpirit * ratio);
    }

    public void SetSpirit(float value, string reason = "SetSpirit")
    {
        float previousSpirit = spirit;
        spiritGauge.SetCurrent(value);
        SyncGaugeToValues();

        if (!hasEverHadSpirit && (previousSpirit > 0f || spirit > 0f))
        {
            hasEverHadSpirit = true;
        }

        NotifySpiritChanged(reason);
        UpdateSpiritEmptyState(previousSpirit);
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

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        bool wasEmpty = isSpiritEmpty;
        float recoveryAmount = Random.Range(30f, 40f);
        ChangeSpirit(recoveryAmount);

        if (wasEmpty && !isSpiritEmpty)
        {
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        }
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
        initialized = true;
        InitializeGauge();
        NotifySpiritChanged(reason);
        UpdateSpiritEmptyState(spirit);
    }

    public bool HasNoSpirit()
    {
        return spirit <= 0f;
    }

    public bool HasEverHadSpirit => hasEverHadSpirit;

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

    void UpdateSpiritEmptyState(float previousSpirit)
    {
        bool hasEnergyNow = spirit > 0f;
        if (!hasEverHadSpirit && (previousSpirit > 0f || hasEnergyNow))
        {
            hasEverHadSpirit = true;
        }

        bool shouldBeEmpty = spirit <= 0f;
        if (shouldBeEmpty == isSpiritEmpty)
            return;

        isSpiritEmpty = shouldBeEmpty;
        if (stateController == null)
        {
            Debug.LogError("[SPIRIT] StateController not set in Inspector");
            return;
        }

        if (isSpiritEmpty)
            stateController.OnSpiritEmpty();
        else
            stateController.OnSpiritRecovered();
    }

    void LogSpiritInitialized(string context)
    {
    }

}
