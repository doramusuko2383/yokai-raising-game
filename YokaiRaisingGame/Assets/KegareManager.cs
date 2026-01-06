using UnityEngine;
using Yokai;

public class KegareManager : MonoBehaviour
{
    [Header("数値")]
    public float kegare = 0f;
    public float maxKegare = 100f;

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
    EnergyManager energyManager;

    [SerializeField]
    YokaiStateController stateController;

    [Header("演出")]
    public float emergencyPurifyValue = 30f;

    bool isMononoke = false;
    GameObject currentYokai;
    float increaseTimer;

    public event System.Action EmergencyPurifyRequested;
    public event System.Action<float, float> KegareChanged;

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
        if (worldConfig == null)
        {
            worldConfig = WorldConfig.LoadDefault();

            if (worldConfig == null)
            {
                Debug.LogWarning("[PURIFY] WorldConfig が見つかりません: Resources/WorldConfig_Yokai");
            }
        }
    }

    void Start()
    {
        BindCurrentYokai(CurrentYokaiContext.Current);
        NotifyKegareChanged();
    }

    void Update()
    {
        HandleNaturalIncrease();
    }

    public void BindCurrentYokai(GameObject yokai)
    {
        currentYokai = yokai;
    }

    public void AddKegare(float amount)
    {
        if (isMononoke) return;

        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);

        if (kegare >= maxKegare && !isMononoke)
        {
            isMononoke = true;
        }

        NotifyKegareChanged();
    }

    public void SetKegare(float value, string reason = null)
    {
        kegare = Mathf.Clamp(value, 0f, maxKegare);

        if (kegare >= maxKegare && !isMononoke)
            isMononoke = true;
        else if (kegare < maxKegare && isMononoke)
            isMononoke = false;

        NotifyKegareChanged();
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

        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
            isMononoke = false;

        Debug.Log($"[PURIFY] {logContext} amount={purifyAmount:0.##} kegare={kegare:0.##}/{maxKegare:0.##}");
        NotifyKegareChanged();
    }

    public void OnClickAdWatch()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null && stateController.currentState != YokaiState.KegareMax)
            return;

        if (worldConfig != null)
        {
            Debug.Log($"[PURIFY] {worldConfig.recoveredMessage}");
        }

        EmergencyPurifyRequested?.Invoke();
    }

    bool TryGetRecoveryBlockReason(out string reason)
    {
        if (energyManager == null)
        {
            energyManager = FindObjectOfType<EnergyManager>();
        }

        bool isKegareMax = kegare >= maxKegare;
        bool isEnergyZero = energyManager != null && energyManager.energy <= 0f;
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

    public void ExecuteEmergencyPurify()
    {
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
            isMononoke = false;

        NotifyKegareChanged();
    }

    void HandleNaturalIncrease()
    {
        if (naturalIncreasePerMinute <= 0f)
            return;

        if (kegare >= maxKegare)
            return;

        increaseTimer += Time.deltaTime;
        if (increaseTimer < increaseIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(increaseTimer / increaseIntervalSeconds);
        increaseTimer -= ticks * increaseIntervalSeconds;
        float increaseAmount = naturalIncreasePerMinute * ticks;
        AddKegare(increaseAmount);
    }

    void NotifyKegareChanged()
    {
        KegareChanged?.Invoke(kegare, maxKegare);
    }

}
