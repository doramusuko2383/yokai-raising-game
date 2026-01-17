using UnityEngine;
using Yokai;

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

    [Header("Dependencies")]
    [SerializeField]
    YokaiStateController stateController;

    bool isWeak;
    float decayTimer;

    System.Action<float, float> energyChanged;
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

    public event System.Action<bool> WeakStateChanged;

    bool initialized;

    void Awake()
    {
        hasEverHadEnergy = false;
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

        if (energy <= 0 && !isWeak && hasEverHadEnergy)
        {
            EnterWeakState();
        }
        else if (energy > 0 && isWeak)
        {
            RecoverFromWeak();
        }

        NotifyEnergyChanged("ChangeEnergy");
    }

    public void AddEnergy(float amount)
    {
        ChangeEnergy(amount);
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
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && (stateController.isPurifying || stateController.IsEnergyEmpty()))
        {
            return;
        }

        if (!allowWhenCritical && stateController != null && stateController.IsEnergyEmpty())
        {
            return;
        }

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

    void EnterWeakState()
    {
        if (isWeak)
            return; // すでに弱体なら何もしない

        isWeak = true;

        WeakStateChanged?.Invoke(true);

        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyZero);

        Debug.Log("[STATE] StateChange: Normal -> EnergyEmpty");
    }




    void RecoverFromWeak()
    {
        if (!isWeak)
            return; // すでに回復済みなら何もしない

        isWeak = false;

        WeakStateChanged?.Invoke(false);

        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        MentorMessageService.NotifyRecovered();

        Debug.Log("[STATE] Energy recovered from weak");
    }


    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && !stateController.IsEnergyEmpty())
        {
            return;
        }

        bool wasEmpty = energy <= 0f;
        float recoveryAmount = Random.Range(30f, 40f);
        ChangeEnergy(recoveryAmount);

        if (wasEmpty && energy > 0f)
        {
            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
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
        {
            return;
        }

        EnsureDefaults();
        if (energy <= 0f && hasEverHadEnergy)
        {
            EnterWeakState();
        }
        else
        {
            RecoverFromWeak();
        }

        initialized = true;
        NotifyEnergyChanged(reason);
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

    void LogEnergyInitialized(string context)
    {
    }

}
