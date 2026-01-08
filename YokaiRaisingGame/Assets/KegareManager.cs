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
    YokaiStateController stateController;

    [Header("演出")]
    public float emergencyPurifyValue = 30f;

    public bool isKegareMax { get; private set; }
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
        SyncKegareMaxState(isKegareMax, requestRelease: false);
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
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged();
    }

    public void SetKegare(float value, string reason = null)
    {
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(value, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

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

        bool wasKegareMax = isKegareMax;
        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

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
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        bool isPurifying = stateController != null && stateController.isPurifying;
        bool isSpiritEmpty = stateController != null && stateController.isSpiritEmpty;
        if (!isPurifying && !isSpiritEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (isPurifying && isSpiritEmpty)
            reason = "魔法陣 / 霊力0";
        else if (isPurifying)
            reason = "魔法陣";
        else
            reason = "霊力0";

        return true;
    }

    public void ExecuteEmergencyPurify()
    {
        bool wasKegareMax = isKegareMax;
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);
        SyncKegareMaxState(wasKegareMax, requestRelease: true);

        NotifyKegareChanged();
    }

    void HandleNaturalIncrease()
    {
        if (naturalIncreasePerMinute <= 0f)
            return;

        if (isKegareMax)
            return;

        increaseTimer += Time.deltaTime;
        if (increaseTimer < increaseIntervalSeconds)
            return;

        int ticks = Mathf.FloorToInt(increaseTimer / increaseIntervalSeconds);
        increaseTimer -= ticks * increaseIntervalSeconds;
        float increaseAmount = naturalIncreasePerMinute * ticks;
        AddKegare(increaseAmount);
    }

    void SyncKegareMaxState(bool wasKegareMax, bool requestRelease, bool triggerEnter = true)
    {
        bool isNowMax = kegare >= maxKegare;
        isKegareMax = isNowMax;

        if (isNowMax && !wasKegareMax && triggerEnter)
        {
            EnterKegareMax();
        }
        else if (!isNowMax && wasKegareMax && requestRelease)
        {
            ExitKegareMax();
        }
    }

    void EnterKegareMax()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.EnterKegareMax();
    }

    void ExitKegareMax()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.RequestReleaseKegareMax();
    }

    void NotifyKegareChanged()
    {
        KegareChanged?.Invoke(kegare, maxKegare);
    }

}
