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
                Debug.Log($"[ENERGY] EnergyChanged invoked. reason=EventSubscribe energy={energy:0.##}/{maxEnergy:0.##}");
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
        Debug.Log($"[EnergyManager][Awake][Enter] energy={energy:0.##} maxEnergy={maxEnergy:0.##} hasEverHadEnergy={hasEverHadEnergy}");
        Debug.Log("[EnergyManager][Awake][ENTER] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " hasEverHadEnergy=" + hasEverHadEnergy + " stateController=" + (stateController == null ? "null" : "ok"));
        hasEverHadEnergy = false;
        EnsureDefaults();
        if (worldConfig == null)
        {
            worldConfig = WorldConfig.LoadDefault();

            if (worldConfig == null)
            {
                Debug.LogWarning("[ENERGY] WorldConfig が見つかりません: Resources/WorldConfig_Yokai");
            }
        }

        InitializeIfNeeded("Awake");
        Debug.Log($"[EnergyManager][Awake][Exit] energy={energy:0.##} maxEnergy={maxEnergy:0.##} initialized={initialized}");
        Debug.Log("[EnergyManager][Awake][EXIT] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " initialized=" + initialized);
    }

    void Start()
    {
        Debug.Log($"[EnergyManager][Start][Enter] energy={energy:0.##} maxEnergy={maxEnergy:0.##} initialized={initialized}");
        Debug.Log("[EnergyManager][Start][ENTER] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " initialized=" + initialized);
        InitializeIfNeeded("Start");
        Debug.Log($"[EnergyManager][Start][Exit] energy={energy:0.##} maxEnergy={maxEnergy:0.##} initialized={initialized}");
        Debug.Log("[EnergyManager][Start][EXIT] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " initialized=" + initialized);
    }

    void Update()
    {
        Debug.Log($"[EnergyManager][Update][Enter] energy={energy:0.##} maxEnergy={maxEnergy:0.##} hasEverHadEnergy={hasEverHadEnergy}");
        Debug.Log("[EnergyManager][Update][ENTER] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " hasEverHadEnergy=" + hasEverHadEnergy + " isWeak=" + isWeak);
        if (!hasEverHadEnergy && energy > 0f)
        {
            hasEverHadEnergy = true;
        }
        HandleNaturalDecay();
        Debug.Log($"[EnergyManager][Update][Exit] energy={energy:0.##} maxEnergy={maxEnergy:0.##} hasEverHadEnergy={hasEverHadEnergy}");
        Debug.Log("[EnergyManager][Update][EXIT] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " hasEverHadEnergy=" + hasEverHadEnergy);
    }

    public void ChangeEnergy(float amount)
    {
        Debug.Log($"[EnergyManager][ChangeEnergy][Enter] amount={amount:0.##} energy={energy:0.##}/{maxEnergy:0.##} isWeak={isWeak} hasEverHadEnergy={hasEverHadEnergy}");
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
        Debug.Log($"[EnergyManager][ChangeEnergy][Exit] energy={energy:0.##}/{maxEnergy:0.##} isWeak={isWeak} hasEverHadEnergy={hasEverHadEnergy}");
    }

    public void AddEnergy(float amount)
    {
        ChangeEnergy(amount);
    }

    public void ApplyHeal(float healRatio = 0.4f)
    {
        Debug.Log($"[EnergyManager][ApplyHeal][Enter] healRatio={healRatio:0.##}");
        ApplyHealInternal(healRatio, allowWhenCritical: false, logContext: "だんご");
        Debug.Log("[EnergyManager][ApplyHeal][Exit]");
    }

    public void ApplyHealFromMagicCircle(float healRatio = 0.4f)
    {
        Debug.Log($"[EnergyManager][ApplyHealFromMagicCircle][Enter] healRatio={healRatio:0.##}");
        ApplyHealInternal(healRatio, allowWhenCritical: true, logContext: "magic circle");
        Debug.Log("[EnergyManager][ApplyHealFromMagicCircle][Exit]");
    }

    void ApplyHealInternal(float healRatio, bool allowWhenCritical, string logContext)
    {
        Debug.Log($"[EnergyManager][ApplyHealInternal][Enter] healRatio={healRatio:0.##} allowWhenCritical={allowWhenCritical} logContext={logContext}");
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && (stateController.isPurifying || stateController.IsEnergyEmpty()))
        {
            Debug.Log($"[EnergyManager][ApplyHealInternal][EarlyReturn] reason=stateBlocked isPurifying={(stateController != null && stateController.isPurifying)} isEnergyEmpty={(stateController != null && stateController.IsEnergyEmpty())}");
            Debug.Log("[EnergyManager][ApplyHealInternal][EARLY_RETURN] reason=stateBlocked isPurifying=" + (stateController != null && stateController.isPurifying) + " isEnergyEmpty=" + (stateController != null && stateController.IsEnergyEmpty()));
            return;
        }

        if (!allowWhenCritical && stateController != null && stateController.IsEnergyEmpty())
        {
            Debug.Log("[EnergyManager][ApplyHealInternal][EarlyReturn] reason=criticalBlock");
            Debug.Log("[EnergyManager][ApplyHealInternal][EARLY_RETURN] reason=criticalBlock isEnergyEmpty=" + (stateController != null && stateController.IsEnergyEmpty()));
            return;
        }

        float healAmount = maxEnergy * healRatio;
        ChangeEnergy(healAmount);
        Debug.Log($"[EnergyManager][ApplyHealInternal][Exit] healAmount={healAmount:0.##} energy={energy:0.##}/{maxEnergy:0.##}");
    }

    void HandleNaturalDecay()
    {
        if (naturalDecayPerMinute <= 0f)
        {
            Debug.Log($"[EnergyManager][HandleNaturalDecay][EarlyReturn] reason=naturalDecayPerMinute<=0 value={naturalDecayPerMinute:0.##}");
            Debug.Log("[EnergyManager][HandleNaturalDecay][EARLY_RETURN] reason=naturalDecayPerMinute<=0 value=" + naturalDecayPerMinute.ToString("0.##"));
            return;
        }

        if (energy <= 0f)
        {
            Debug.Log($"[EnergyManager][HandleNaturalDecay][EarlyReturn] reason=energy<=0 energy={energy:0.##}");
            Debug.Log("[EnergyManager][HandleNaturalDecay][EARLY_RETURN] reason=energy<=0 energy=" + energy.ToString("0.##"));
            return;
        }

        decayTimer += Time.deltaTime;
        if (decayTimer < decayIntervalSeconds)
        {
            Debug.Log($"[EnergyManager][HandleNaturalDecay][EarlyReturn] reason=intervalNotReached decayTimer={decayTimer:0.##} interval={decayIntervalSeconds:0.##}");
            Debug.Log("[EnergyManager][HandleNaturalDecay][EARLY_RETURN] reason=intervalNotReached decayTimer=" + decayTimer.ToString("0.##") + " interval=" + decayIntervalSeconds.ToString("0.##"));
            return;
        }

        int ticks = Mathf.FloorToInt(decayTimer / decayIntervalSeconds);
        decayTimer -= ticks * decayIntervalSeconds;
        float decayAmount = naturalDecayPerMinute * ticks;
        ChangeEnergy(-decayAmount);
        Debug.Log($"[EnergyManager][HandleNaturalDecay][Exit] ticks={ticks} decayAmount={decayAmount:0.##} energy={energy:0.##}/{maxEnergy:0.##}");
    }

    void EnterWeakState()
    {
        Debug.Log($"[EnergyManager][EnterWeakState][Enter] energy={energy:0.##}/{maxEnergy:0.##} isWeak={isWeak}");
        isWeak = true;
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
        WeakStateChanged?.Invoke(true);
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyZero);
        if (worldConfig != null)
        {
            Debug.Log($"[ENERGY] {worldConfig.weakMessage}");
        }
        Debug.Log($"[EnergyManager][EnterWeakState][Exit] isWeak={isWeak}");
    }

    void RecoverFromWeak()
    {
        Debug.Log($"[EnergyManager][RecoverFromWeak][Enter] energy={energy:0.##}/{maxEnergy:0.##} isWeak={isWeak}");
        bool wasWeak = isWeak;
        isWeak = false;
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
        WeakStateChanged?.Invoke(false);
        if (wasWeak)
        {
            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
            MentorMessageService.NotifyRecovered();
        }
        if (worldConfig != null)
        {
            Debug.Log($"[ENERGY] {worldConfig.normalMessage}");
        }
        Debug.Log($"[EnergyManager][RecoverFromWeak][Exit] isWeak={isWeak} wasWeak={wasWeak}");
    }

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        Debug.Log($"[EnergyManager][OnClickAdWatch][Enter] energy={energy:0.##}/{maxEnergy:0.##} stateController={(stateController == null ? "null" : "ok")}");
        Debug.Log("[EnergyManager][OnClickAdWatch][ENTER] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " stateController=" + (stateController == null ? "null" : "ok"));
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && !stateController.IsEnergyEmpty())
        {
            Debug.Log("[EnergyManager][OnClickAdWatch][EarlyReturn] reason=notEnergyEmpty");
            Debug.Log("[EnergyManager][OnClickAdWatch][EARLY_RETURN] reason=notEnergyEmpty isEnergyEmpty=" + (stateController != null && stateController.IsEnergyEmpty()));
            return;
        }

        bool wasEmpty = energy <= 0f;
        float recoveryAmount = Random.Range(30f, 40f);
        ChangeEnergy(recoveryAmount);
        Debug.Log($"[ENERGY] Emergency dango +{recoveryAmount:0.##} energy={energy:0.##}/{maxEnergy:0.##}");

        if (wasEmpty && energy > 0f)
        {
            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        }
        Debug.Log($"[EnergyManager][OnClickAdWatch][Exit] energy={energy:0.##}/{maxEnergy:0.##} wasEmpty={wasEmpty}");
        Debug.Log("[EnergyManager][OnClickAdWatch][EXIT] energy=" + energy.ToString("0.##") + " maxEnergy=" + maxEnergy.ToString("0.##") + " wasEmpty=" + wasEmpty);
    }

    void NotifyEnergyChanged(string reason)
    {
        Debug.Log($"[EnergyManager][NotifyEnergyChanged][Enter] reason={reason} energy={energy:0.##}/{maxEnergy:0.##}");
        Debug.Log($"[ENERGY] EnergyChanged invoked. reason={reason} energy={energy:0.##}/{maxEnergy:0.##}");
        energyChanged?.Invoke(energy, maxEnergy);
        Debug.Log("[EnergyManager][NotifyEnergyChanged][Exit]");
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
        {
            Debug.Log($"[EnergyManager][InitializeIfNeeded][EarlyReturn] reason=alreadyInitialized context={reason}");
            Debug.Log("[EnergyManager][InitializeIfNeeded][EARLY_RETURN] reason=alreadyInitialized context=" + reason + " energy=" + energy.ToString("0.##"));
            return;
        }

        Debug.Log($"[EnergyManager][InitializeIfNeeded][Enter] context={reason} energy={energy:0.##}/{maxEnergy:0.##} hasEverHadEnergy={hasEverHadEnergy}");
        EnsureDefaults();
        LogEnergyInitialized(reason);
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
        Debug.Log($"[EnergyManager][InitializeIfNeeded][Exit] context={reason} initialized={initialized}");
    }

    public bool HasNoEnergy()
    {
        Debug.Log($"[EnergyManager][HasNoEnergy][Enter] energy={energy:0.##}");
        return energy <= 0f;
    }

    public bool HasEverHadEnergy => hasEverHadEnergy;

    public bool HasValidValues()
    {
        Debug.Log($"[EnergyManager][HasValidValues][Enter] energy={energy:0.##} maxEnergy={maxEnergy:0.##}");
        return !float.IsNaN(maxEnergy)
            && maxEnergy > 0f
            && !float.IsNaN(energy)
            && energy >= 0f
            && energy <= maxEnergy;
    }

    void EnsureDefaults()
    {
        Debug.Log($"[EnergyManager][EnsureDefaults][Enter] energy={energy:0.##} maxEnergy={maxEnergy:0.##}");
        if (float.IsNaN(maxEnergy) || maxEnergy <= 0f)
            maxEnergy = DefaultMaxEnergy;

        if (float.IsNaN(energy))
            energy = maxEnergy;

        energy = Mathf.Clamp(energy, 0f, maxEnergy);
        Debug.Log($"[EnergyManager][EnsureDefaults][Exit] energy={energy:0.##} maxEnergy={maxEnergy:0.##}");
    }

    void LogEnergyInitialized(string context)
    {
        Debug.Log($"[ENERGY] Initialized ({context}) energy={energy:0.##}/{maxEnergy:0.##}");
    }

}
