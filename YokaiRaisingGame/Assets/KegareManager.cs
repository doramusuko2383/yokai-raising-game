using UnityEngine;
using UnityEngine.UI;
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

    [Header("UI")]
    public Slider kegareSlider;

    [Header("演出")]
    public float emergencyPurifyValue = 30f;

    bool isMononoke = false;

    public event System.Action EmergencyPurifyRequested;

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
        UpdateUI();
    }

    void Update()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
    }

    public void AddKegare(float amount)
    {
        if (isMononoke) return;

        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);

        if (kegare >= maxKegare && !isMononoke)
        {
            isMononoke = true;
            if (stateController != null)
                stateController.RefreshState();
        }

        UpdateUI();
        if (stateController != null)
            stateController.RefreshState();
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

        UpdateUI();
        if (stateController != null)
            stateController.RefreshState();
    }

    public void OnClickAdWatch()
    {
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

    void UpdateUI()
    {
        if (kegareSlider != null)
            kegareSlider.value = kegare / maxKegare;
    }

    public void ExecuteEmergencyPurify()
    {
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
            isMononoke = false;

        UpdateUI();
        if (stateController != null)
            stateController.RefreshState();
    }

}
