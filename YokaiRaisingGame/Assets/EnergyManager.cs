using UnityEngine;
using Yokai;

public class EnergyManager : MonoBehaviour
{
    const float DefaultMaxEnergy = 100f;

    [Header("数値")]
    public float energy = 100f;
    public float maxEnergy = 100f;

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
        Debug.Log($"[ENERGY][Awake] energy={energy:0.##} maxEnergy={maxEnergy:0.##}");
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
    }

    void Start()
    {
        Debug.Log($"[ENERGY][Start] energy={energy:0.##} maxEnergy={maxEnergy:0.##}");
        InitializeIfNeeded("Start");
    }

    void Update()
    {
        HandleNaturalDecay();
    }

    public void ChangeEnergy(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0, maxEnergy);

        if (energy <= 0 && !isWeak)
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

        if (stateController != null && (stateController.isPurifying || stateController.isSpiritEmpty))
        {
            return;
        }

        if (!allowWhenCritical && stateController != null && stateController.isSpiritEmpty)
        {
            return;
        }

        float healAmount = maxEnergy * healRatio;
        ChangeEnergy(healAmount);
    }

    void HandleNaturalDecay()
    {
        if (naturalDecayPerMinute <= 0f)
            return;

        if (energy <= 0f)
            return;

        decayTimer += Time.deltaTime;
        if (decayTimer < decayIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(decayTimer / decayIntervalSeconds);
        decayTimer -= ticks * decayIntervalSeconds;
        float decayAmount = naturalDecayPerMinute * ticks;
        ChangeEnergy(-decayAmount);
    }

    void EnterWeakState()
    {
        isWeak = true;
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
        if (stateController != null)
            stateController.EnterSpiritEmptyState();
        WeakStateChanged?.Invoke(true);
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyZero);
        if (worldConfig != null)
        {
            Debug.Log($"[ENERGY] {worldConfig.weakMessage}");
        }
    }

    void RecoverFromWeak()
    {
        bool wasWeak = isWeak;
        isWeak = false;
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
        if (stateController != null)
            stateController.ExitSpiritEmptyState();
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
    }

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && !stateController.isSpiritEmpty)
        {
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
    }

    void NotifyEnergyChanged(string reason)
    {
        Debug.Log($"[ENERGY] EnergyChanged invoked. reason={reason} energy={energy:0.##}/{maxEnergy:0.##}");
        energyChanged?.Invoke(energy, maxEnergy);
    }

    void InitializeIfNeeded(string reason)
    {
        if (initialized)
            return;

        EnsureDefaults();
        LogEnergyInitialized(reason);
        if (energy <= 0f)
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
        Debug.Log($"[ENERGY] Initialized ({context}) energy={energy:0.##}/{maxEnergy:0.##}");
    }

}
