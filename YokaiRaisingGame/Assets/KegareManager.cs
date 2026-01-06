using UnityEngine;
using Yokai;

public class KegareManager : MonoBehaviour
{
    [Header("数値")]
    public float kegare = 0f;
    public float maxKegare = 100f;

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
                Debug.LogWarning("WorldConfig が見つかりません: Resources/WorldConfig_Yokai");
            }
        }
    }

    void Start()
    {
        BindCurrentYokai(CurrentYokaiContext.Current);
        NotifyKegareChanged();
    }

    public void BindCurrentYokai(GameObject yokai)
    {
        currentYokai = yokai;
        Debug.Log($"[KEGARE][Bind] currentYokai={(currentYokai != null ? currentYokai.name : "null")}");
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

    public void ApplyPurify(float purifyRatio = 0.7f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: false, logContext: "おきよめ");
    }

    public void Purify()
    {
        ApplyPurify();
    }

    public void ApplyPurifyFromMagicCircle(float purifyRatio = 0.7f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: true, logContext: "magic circle");
    }

    void ApplyPurifyInternal(float purifyRatio, bool allowWhenCritical, string logContext)
    {
        if (!allowWhenCritical && TryGetRecoveryBlockReason(out string blockReason))
        {
            // DEBUG: ブロック理由を明確にログ出力する
            Debug.Log($"[RECOVERY BLOCK] {logContext} purify blocked. reason={blockReason}");
            return;
        }

        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
            isMononoke = false;

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
            Debug.Log(worldConfig.recoveredMessage);
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

    void NotifyKegareChanged()
    {
        LogKegareStatus("Update");
        KegareChanged?.Invoke(kegare, maxKegare);
    }

    void LogKegareStatus(string label)
    {
        string yokaiName = currentYokai != null ? currentYokai.name : CurrentYokaiContext.CurrentName();
        string stateName = stateController != null ? stateController.currentState.ToString() : "Unknown";
        Debug.Log($"[KEGARE][{label}] yokai={yokaiName} state={stateName} value={kegare:0.##}/{maxKegare:0.##}");
    }

}
