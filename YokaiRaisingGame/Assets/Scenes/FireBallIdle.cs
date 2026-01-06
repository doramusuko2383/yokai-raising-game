using UnityEngine;
using Yokai;

public class FireBallIdle : MonoBehaviour
{
    [Header("進化設定")]
    public float evolveScale = 2.0f;
    public float evolveTime = 10f;

    [Header("タップ加速")]
    public float tapBoostAmount = 0.2f;

    [Header("進化演出")]
    public float popScale = 1.4f;
    public float popDuration = 0.3f;

    float baseScale;
    float growRate;
    float tapBoost = 0f;
    bool evolved = false;

    int tapCount = 0;
    float lifeTime = 0f;
    int bornHour; // ★ 生まれた時間（時）

    GameManager gameManager;
    SpriteRenderer sr;
    Yokai.YokaiStateController stateController;
    YokaiEvolutionController evolutionController;
    bool evolutionReady;

    void Start()
    {
        baseScale = transform.localScale.x;
        growRate = (evolveScale - baseScale) / evolveTime;

        bornHour = System.DateTime.Now.Hour; // ★ ここ重要

        gameManager = FindObjectOfType<GameManager>();
        sr = GetComponent<SpriteRenderer>();
        stateController = FindObjectOfType<Yokai.YokaiStateController>();
        evolutionController = FindObjectOfType<YokaiEvolutionController>();

        YokaiEncyclopedia.RegisterDiscovery(YokaiId.FireBall);
    }

    void Update()
    {
        if (evolved) return;

        lifeTime += Time.deltaTime;

        baseScale += (growRate + tapBoost) * Time.deltaTime;
        tapBoost = Mathf.Lerp(tapBoost, 0f, Time.deltaTime * 4f);

        transform.localScale = Vector3.one * baseScale;

        if (gameManager != null)
        {
            gameManager.UpdateGauge(baseScale / evolveScale);
        }

        if (!evolutionReady && baseScale >= evolveScale)
            SetEvolutionReady();
    }

    void OnMouseDown()
    {
        if (evolutionReady)
        {
            if (evolutionController != null)
            {
                evolutionController.OnClickEvolve();
            }
            else
            {
                Debug.LogWarning("[EVOLUTION] YokaiEvolutionController not found. Evolution request ignored.");
            }
            return;
        }

        tapCount++;
        tapBoost += tapBoostAmount;
    }

    void SetEvolutionReady()
    {
        evolutionReady = true;
        evolved = true;
        baseScale = evolveScale;
        transform.localScale = Vector3.one * baseScale;
        if (sr != null)
            sr.color = Color.white;

        if (stateController != null)
            stateController.SetEvolutionReady();

        Debug.Log("[EVOLUTION READY] FireBall is ready to evolve.");
    }
}
