using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class KegareManager : MonoBehaviour
{
    [Header("数値")]
    public float kegare = 0f;
    public float maxKegare = 100f;
    public float warningThreshold = 70f;

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
    public GameObject warningIcon;
    public CanvasGroup dangerOverlay;

    [Header("広告UI")]
    public GameObject adWatchButton;   // 📺広告ボタン
    public GameObject actionPanel;     // 通常操作UI

    [Header("対象")]
    public SpriteRenderer yokaiSprite;

    [Header("演出")]
    public float pulseSpeed = 2f;
    public float maxOverlayAlpha = 0.35f;
    public float emergencyPurifyValue = 30f;

    bool isDanger = false;
    bool isMononoke = false;
    bool wasWarning = false;

    Color originalColor;

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
        if (yokaiSprite != null)
            originalColor = yokaiSprite.color;

        UpdateUI();
    }

    void Update()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        // 危険演出（警告〜モノノケ）
        if (isDanger && dangerOverlay != null)
        {
            dangerOverlay.alpha =
                0.15f + Mathf.Sin(Time.time * pulseSpeed) * maxOverlayAlpha;
        }
        else if (dangerOverlay != null)
        {
            dangerOverlay.alpha = 0f;
        }
    }

    public void AddKegare(float amount)
    {
        if (isMononoke) return;

        kegare = Mathf.Clamp(kegare + amount, 0, maxKegare);

        if (kegare >= maxKegare)
        {
            EnterMononoke();
        }

        UpdateUI();
    }

    public void ApplyPurify(float purifyRatio = 0.7f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: false, logContext: "おきよめ");
    }

    public void ApplyPurifyFromMagicCircle(float purifyRatio = 0.7f)
    {
        ApplyPurifyInternal(purifyRatio, allowWhenCritical: true, logContext: "magic circle");
    }

    void ApplyPurifyInternal(float purifyRatio, bool allowWhenCritical, string logContext)
    {
        if (!allowWhenCritical && ShouldBlockItemRecovery())
        {
            Debug.Log($"[RECOVERY BLOCK] {logContext} purify ignored because kegare or energy is critical.");
            return;
        }

        float purifyAmount = maxKegare * purifyRatio;
        kegare = Mathf.Clamp(kegare - purifyAmount, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
        {
            RecoverFromMononoke();
        }

        UpdateUI();
    }

    void EnterMononoke()
    {
        isMononoke = true;
        isDanger = true;

        if (yokaiSprite != null)
            yokaiSprite.color = Color.red;

        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (adWatchButton != null)
            adWatchButton.SetActive(true);

        if (worldConfig != null)
        {
            Debug.Log(worldConfig.mononokeMessage);
        }
    }

    public void OnClickAdWatch()
    {
        if (stateController != null && stateController.currentState != YokaiState.KegareMax)
            return;

        ShowAd(() =>
        {
            if (worldConfig != null)
            {
                Debug.Log(worldConfig.recoveredMessage);
            }

            ExecuteEmergencyPurify();
        });
    }

    bool ShouldBlockItemRecovery()
    {
        if (energyManager == null)
        {
            energyManager = FindObjectOfType<EnergyManager>();
        }

        bool isKegareMax = kegare >= maxKegare;
        bool isEnergyZero = energyManager != null && energyManager.energy <= 0f;
        return isKegareMax || isEnergyZero;
    }

    void RecoverFromMononoke()
    {
        isMononoke = false;
        isDanger = false;

        if (yokaiSprite != null)
            yokaiSprite.color = originalColor;

        if (actionPanel != null)
            actionPanel.SetActive(true);

        if (adWatchButton != null)
            adWatchButton.SetActive(false);
    }

    void UpdateUI()
    {
        if (kegareSlider != null)
            kegareSlider.value = kegare / maxKegare;

        bool warningNow = kegare >= warningThreshold && !isMononoke;

        if (warningIcon != null)
            warningIcon.SetActive(warningNow);

        if (worldConfig != null && warningNow && !wasWarning)
        {
            Debug.Log(worldConfig.dangerMessage);
        }

        wasWarning = warningNow;
        isDanger = warningNow || isMononoke;
    }

    public void ExecuteEmergencyPurify()
    {
        kegare = Mathf.Clamp(emergencyPurifyValue, 0f, maxKegare);

        if (kegare < maxKegare && isMononoke)
        {
            RecoverFromMononoke();
        }

        UpdateUI();
    }

    void ShowAd(System.Action onCompleted)
    {
        Debug.Log("[AD] Showing rewarded ad before emergency purify.");
        onCompleted?.Invoke();
    }
}
