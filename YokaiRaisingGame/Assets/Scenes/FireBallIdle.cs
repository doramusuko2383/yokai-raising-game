using System.Collections;
using TMPro;
using UnityEngine;

public class FireBallIdle : MonoBehaviour
{
    [Header("成長設定")]
    [Range(0f, 1f)] public float currentGrowth = 0f;
    public float growthSpeed = 0.05f;
    [Range(0f, 1f)] public float completeGrowthThreshold = 1f;
    public float maxScaleMultiplier = 2.0f;

    [Header("お世話")]
    public float careGrowthAmount = 0.05f;

    [Header("参照")]
    [SerializeField] private YokaiIdleStateController idleStateController;
    [SerializeField] private YokaiStateDisplay stateDisplay;
    [SerializeField] private TMP_Text growthMessageText;

    [Header("メッセージ")]
    [SerializeField] private string growthCompleteMessage = "育成を終え、記録が残りました";

    bool isGrowthComplete = false;
    Vector3 baseScale;
    Vector3 maxScale;

    GameManager gameManager;
    SpriteRenderer sr;

    void Start()
    {
        baseScale = transform.localScale;
        maxScale = baseScale * maxScaleMultiplier;

        gameManager = FindObjectOfType<GameManager>();
        if (idleStateController == null)
        {
            idleStateController = FindObjectOfType<YokaiIdleStateController>();
        }
        if (stateDisplay == null)
        {
            stateDisplay = FindObjectOfType<YokaiStateDisplay>();
        }
        sr = GetComponent<SpriteRenderer>();

        if (growthMessageText != null)
        {
            growthMessageText.text = string.Empty;
        }

        ApplyGrowthToScale();
        UpdateGrowthGauge();

        StartCoroutine(GrowthLoop());
    }

    IEnumerator GrowthLoop()
    {
        var wait = new WaitForSeconds(0.2f);

        while (!isGrowthComplete)
        {
            yield return wait;

            if (!CanGrow())
            {
                continue;
            }

            currentGrowth = Mathf.Clamp01(currentGrowth + growthSpeed * 0.2f);
            ApplyGrowthToScale();
            UpdateGrowthGauge();

            if (currentGrowth >= completeGrowthThreshold)
            {
                HandleGrowthComplete();
            }
        }
    }

    void OnMouseDown()
    {
        if (!CanGrow())
        {
            return;
        }

        currentGrowth = Mathf.Clamp01(currentGrowth + careGrowthAmount);
        ApplyGrowthToScale();
        UpdateGrowthGauge();

        if (idleStateController != null)
        {
            idleStateController.RecordAction();
        }

        if (currentGrowth >= completeGrowthThreshold)
        {
            HandleGrowthComplete();
        }
    }

    bool CanGrow()
    {
        if (isGrowthComplete)
        {
            return false;
        }

        if (stateDisplay != null && stateDisplay.CurrentState != YokaiState.Normal)
        {
            return false;
        }

        if (idleStateController != null && idleStateController.IsIdleNow())
        {
            return false;
        }

        return true;
    }

    void ApplyGrowthToScale()
    {
        transform.localScale = Vector3.Lerp(baseScale, maxScale, currentGrowth);
    }

    void UpdateGrowthGauge()
    {
        if (gameManager != null)
        {
            gameManager.UpdateGauge(currentGrowth);
        }
    }

    void HandleGrowthComplete()
    {
        isGrowthComplete = true;
        currentGrowth = Mathf.Clamp01(currentGrowth);
        ApplyGrowthToScale();
        UpdateGrowthGauge();

        if (sr != null)
        {
            sr.enabled = false;
        }

        if (growthMessageText != null)
        {
            growthMessageText.text = growthCompleteMessage;
        }

        if (gameManager != null)
        {
            gameManager.RegisterToDex("FireBall");
            gameManager.SpawnFireBall();
        }
    }
}
