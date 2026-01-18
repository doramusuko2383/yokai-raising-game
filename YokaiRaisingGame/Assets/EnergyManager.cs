using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    const float DefaultMaxEnergy = 100f;

    [Header("数値")]
    public float energy = 100f;
    public float maxEnergy = 100f;

    [Header("状態")]
    [SerializeField]
    bool hasEverHadEnergy;

    [Header("自然減少")]
    [SerializeField]
    float naturalDecayPerMinute = 2.5f;

    [SerializeField]
    float decayIntervalSeconds = 60f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    float decayTimer;

    System.Action<float, float> energyChanged;
    public event System.Action OnEnergyEmpty;
    public event System.Action OnEnergyRecovered;
    public event System.Action<float, float> EnergyChanged
    {
        add
        {
            energyChanged += value;
            if (initialized)
            {
                value?.Invoke(energy, maxEnergy);
            }
        }
        remove
        {
            energyChanged -= value;
        }
    }

    bool initialized;
    bool isEnergyEmpty;

    void Awake()
    {
        hasEverHadEnergy = false;
        isEnergyEmpty = false;
        EnsureDefaults();
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
        SetEnergyRatio(0.8f);
        InitializeIfNeeded("Start");
    }

    void Update()
    {
        if (!hasEverHadEnergy && energy > 0f)
        {
            hasEverHadEnergy = true;
        }
        HandleNaturalDecay();
    }

    public void ChangeEnergy(float amount)
    {
        float previousEnergy = energy;
        energy = Mathf.Clamp(energy + amount, 0, maxEnergy);
        if (!hasEverHadEnergy && (previousEnergy > 0f || energy > 0f))
        {
            hasEverHadEnergy = true;
        }

        NotifyEnergyChanged("ChangeEnergy");
        UpdateEnergyEmptyState(previousEnergy);
    }

    public void AddEnergy(float amount)
    {
        ChangeEnergy(amount);
    }

    public void AddEnergyRatio(float ratio)
    {
        ChangeEnergy(maxEnergy * ratio);
    }

    public void SetEnergy(float value, string reason = "SetEnergy")
    {
        float previousEnergy = energy;
        energy = Mathf.Clamp(value, 0f, maxEnergy);

        if (!hasEverHadEnergy && (previousEnergy > 0f || energy > 0f))
        {
            hasEverHadEnergy = true;
        }

        NotifyEnergyChanged(reason);
        UpdateEnergyEmptyState(previousEnergy);
    }

    public void SetEnergyRatio(float ratio)
    {
        SetEnergy(maxEnergy * Mathf.Clamp01(ratio), "SetEnergyRatio");
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
        float healAmount = maxEnergy * healRatio;
        ChangeEnergy(healAmount);
    }

    void HandleNaturalDecay()
    {
        if (naturalDecayPerMinute <= 0f)
        {
            return;
        }

        if (energy <= 0f)
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
        ChangeEnergy(-decayAmount);
    }

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        bool wasEmpty = isEnergyEmpty;
        float recoveryAmount = Random.Range(30f, 40f);
        ChangeEnergy(recoveryAmount);

        if (wasEmpty && !isEnergyEmpty)
        {
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        }
    }

    void NotifyEnergyChanged(string reason)
    {
        energyChanged?.Invoke(energy, maxEnergy);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        initialized = true;
        NotifyEnergyChanged(reason);
        UpdateEnergyEmptyState(energy);
    }

    public bool HasNoEnergy()
    {
        return energy <= 0f;
    }

    public bool HasEverHadEnergy => hasEverHadEnergy;

    public bool HasValidValues()
    {
        return !float.IsNaN(maxEnergy)
            && maxEnergy > 0f
            && !float.IsNaN(energy)
            && energy >= 0f
            && energy <= maxEnergy;
    }

    void EnsureDefaults()
    {
        if (float.IsNaN(maxEnergy) || maxEnergy <= 0f)
            maxEnergy = DefaultMaxEnergy;

        if (float.IsNaN(energy))
            energy = maxEnergy;

        energy = Mathf.Clamp(energy, 0f, maxEnergy);
    }

    void UpdateEnergyEmptyState(float previousEnergy)
    {
        bool hasEnergyNow = energy > 0f;
        if (!hasEverHadEnergy && (previousEnergy > 0f || hasEnergyNow))
        {
            hasEverHadEnergy = true;
        }

        bool shouldBeEmpty = energy <= 0f && hasEverHadEnergy;
        if (shouldBeEmpty == isEnergyEmpty)
            return;

        isEnergyEmpty = shouldBeEmpty;
        if (isEnergyEmpty)
            OnEnergyEmpty?.Invoke();
        else
            OnEnergyRecovered?.Invoke();
    }

    void LogEnergyInitialized(string context)
    {
    }

}
