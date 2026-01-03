using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class EnergyManager : MonoBehaviour
{
    [Header("数値")]
    public float energy = 100f;
    public float maxEnergy = 100f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    [Header("Dependencies")]
    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    YokaiStateController stateController;

    [Header("UI")]
    public Slider energySlider;

    [Header("対象")]
    public SpriteRenderer yokaiSprite;

    [Header("操作UI")]
    public GameObject actionPanel;     // 浄化・だんごパネル
    public GameObject adWatchButton;   // 📺 広告を見る
    public GameObject weakMessage;     // 弱り文言（任意）

    [Header("弱り演出")]
    public float weakScale = 0.8f;
    public float weakAlpha = 0.4f;

    Vector3 originalScale;
    Color originalColor;
    bool isWeak;

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
        if (yokaiSprite == null)
        {
            Debug.LogError("❌ Yokai SpriteRenderer が設定されていません");
            enabled = false;
            return;
        }

        originalScale = yokaiSprite.transform.localScale;
        originalColor = yokaiSprite.color;

        // 初期は通常状態
        SetWeakUI(false);
        UpdateUI();
    }

    void Update()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
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

        UpdateUI();
    }

    public void AddEnergy(float amount)
    {
        ChangeEnergy(amount);
        UpdateUI();
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
        if (stateController != null && stateController.currentState != YokaiState.Normal)
        {
            // DEBUG: 状態不一致で処理が止まった理由を明示する
            Debug.Log($"[RECOVERY BLOCK] {logContext} heal blocked. state={stateController.currentState}");
            return;
        }

        if (!allowWhenCritical && TryGetRecoveryBlockReason(out string blockReason))
        {
            // DEBUG: ブロック理由を明確にログ出力する
            Debug.Log($"[RECOVERY BLOCK] {logContext} heal blocked. reason={blockReason}");
            return;
        }

        float healAmount = maxEnergy * healRatio;
        ChangeEnergy(healAmount);
    }

    void EnterWeakState()
    {
        isWeak = true;

        yokaiSprite.transform.localScale = originalScale * weakScale;
        yokaiSprite.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            weakAlpha
        );

        SetWeakUI(true);
        if (worldConfig != null)
        {
            Debug.Log(worldConfig.weakMessage);
        }
    }

    void RecoverFromWeak()
    {
        isWeak = false;

        yokaiSprite.transform.localScale = originalScale;
        yokaiSprite.color = originalColor;

        SetWeakUI(false);
        if (worldConfig != null)
        {
            Debug.Log(worldConfig.normalMessage);
        }
    }

    void SetWeakUI(bool isWeak)
    {
        if (actionPanel != null)
            actionPanel.SetActive(!isWeak);

        if (adWatchButton != null)
            adWatchButton.SetActive(isWeak);

        if (weakMessage != null)
            weakMessage.SetActive(isWeak);
    }

    void UpdateUI()
    {
        if (energySlider != null)
            energySlider.value = (float)energy / (float)maxEnergy;
        Debug.Log($"[ENERGY UI] raw={energy}/{maxEnergy} => value={(float)energy / (float)maxEnergy}");

    }

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        if (stateController != null && stateController.currentState != YokaiState.Normal)
        {
            return;
        }

        if (worldConfig != null)
        {
            Debug.Log(worldConfig.recoveredMessage);
        }

        energy = maxEnergy;
        RecoverFromWeak();
        UpdateUI();
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
}
