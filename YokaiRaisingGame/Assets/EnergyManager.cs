using UnityEngine;
using UnityEngine.UI;

public class EnergyManager : MonoBehaviour
{
    [Header("数値")]
    public float energy = 100f;
    public float maxEnergy = 100f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

    [Header("UI")]
    public Slider energySlider;

    [Header("対象")]
    public SpriteRenderer yokaiSprite;

    [Header("操作UI")]
    public GameObject actionPanel;     // 浄化・だんごパネル
    public GameObject adWatchButton;   // 📺 広告を見る
    public GameObject weakMessage;     // 弱り文言（任意）

    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;

    [Header("弱り演出")]
    public float weakScale = 0.8f;
    public float weakAlpha = 0.4f;

    Vector3 originalScale;
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
        // テスト用
        if (Input.GetKeyDown(KeyCode.J))
        {
            ChangeEnergy(-10f);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            ChangeEnergy(+10f);
        }
    }

    public void ChangeEnergy(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0, maxEnergy);

        if (energy <= 0 && currentState != YokaiState.Weak)
        {
            EnterWeakState();
        }
        else if (energy > 0 && currentState == YokaiState.Weak)
        {
            RecoverFromWeak();
        }

        UpdateUI();
    }

    public void ApplyHeal(float healRatio = 0.4f)
    {
        float healAmount = maxEnergy * healRatio;
        ChangeEnergy(healAmount);
    }

    void EnterWeakState()
    {
        currentState = YokaiState.Weak;

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
        currentState = YokaiState.Normal;

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
            energySlider.value = energy / maxEnergy;
    }

    // 📺 広告を見る（仮）
    public void OnClickAdWatch()
    {
        if (worldConfig != null)
        {
            Debug.Log(worldConfig.recoveredMessage);
        }

        energy = maxEnergy;
        RecoverFromWeak();
        UpdateUI();
    }
}
