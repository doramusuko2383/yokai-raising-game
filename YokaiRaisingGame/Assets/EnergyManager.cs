using UnityEngine;
using Yokai;

public class EnergyManager : MonoBehaviour
{
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
    KegareManager kegareManager;

    [SerializeField]
    YokaiStateController stateController;

    bool isWeak;
    float decayTimer;

    public event System.Action<float, float> EnergyChanged;
    public event System.Action<bool> WeakStateChanged;

    void Awake()
    {
        if (worldConfig == null)
        {
            worldConfig = WorldConfig.LoadDefault();

            if (worldConfig == null)
            {
                Debug.LogWarning("[ENERGY] WorldConfig が見つかりません: Resources/WorldConfig_Yokai");
            }
        }
    }

    void Start()
    {
        if (energy <= 0f)
        {
            EnterWeakState();
        }
        else
        {
            RecoverFromWeak();
        }

        NotifyEnergyChanged();
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

        NotifyEnergyChanged();
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

        if (stateController != null && stateController.currentState != YokaiState.Normal)
        {
            return;
        }

        if (!allowWhenCritical && TryGetRecoveryBlockReason(out _))
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
        WeakStateChanged?.Invoke(true);
        if (worldConfig != null)
        {
            Debug.Log($"[ENERGY] {worldConfig.weakMessage}");
        }
    }

    void RecoverFromWeak()
    {
        isWeak = false;
        WeakStateChanged?.Invoke(false);
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

        if (stateController != null && stateController.currentState != YokaiState.EnergyEmpty)
        {
            return;
        }

        float recoveryAmount = Random.Range(30f, 40f);
        ChangeEnergy(recoveryAmount);
        Debug.Log($"[ENERGY] Emergency dango +{recoveryAmount:0.##} energy={energy:0.##}/{maxEnergy:0.##}");
    }

    bool TryGetRecoveryBlockReason(out string reason)
    {
        if (kegareManager == null)
        {
            kegareManager = FindObjectOfType<KegareManager>();
        }

        bool isKegareMax = kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
        bool isEnergyZero = energy <= 0f;
        if (!isKegareMax && !isEnergyZero)
        {
            reason = string.Empty;
            return false;
        }

        if (isKegareMax && isEnergyZero)
            reason = "穢れMAX / 霊力0";
        else if (isKegareMax)
            reason = "穢れMAX";
        else
            reason = "霊力0";

        return true;
    }

    void NotifyEnergyChanged()
    {
        EnergyChanged?.Invoke(energy, maxEnergy);
    }

}
