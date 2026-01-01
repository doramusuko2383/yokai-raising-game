using UnityEngine;
using UnityEngine.UI;

public class KegareManager : MonoBehaviour
{
    [Header("数値")]
    public float kegare = 0f;
    public float maxKegare = 100f;
    public float warningThreshold = 70f;

    [Header("World")]
    [SerializeField]
    WorldConfig worldConfig;

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
        // テスト用
        if (Input.GetKeyDown(KeyCode.K))
            AddKegare(10f);

        if (Input.GetKeyDown(KeyCode.L))
            AddKegare(-10f);

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
        if (worldConfig != null)
        {
            Debug.Log(worldConfig.recoveredMessage);
        }

        kegare = 0f;
        RecoverFromMononoke();

        UpdateUI();
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
}
