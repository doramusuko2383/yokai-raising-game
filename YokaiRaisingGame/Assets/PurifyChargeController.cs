using UnityEngine;
using UnityEngine.EventSystems;
using Yokai; // © ‚ ‚È‚½‚Ì namespace ‚É‡‚í‚¹‚Ä

public class PurifyChargeController :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private GameObject magicCircleRoot;
    [SerializeField] private UIPentagramBaseCircle baseCircle;
    [SerializeField] private YokaiStateController stateController;

    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 2.0f;
    [SerializeField] private float fadeDuration = 0.25f;

    private bool isCharging;
    private float chargeTimer;
    private Coroutine fadeCoroutine;

    // =========================
    // Pointer
    // =========================

    public void OnPointerDown(PointerEventData eventData)
    {
        BeginCharge();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelCharge();
    }

    // =========================
    // Charge
    // =========================

    public void BeginCharge()
    {
        if (isCharging) return;

        // e‚©‚ç•K‚¸ active
        if (!magicCircleRoot.activeInHierarchy)
        {
            magicCircleRoot.SetActive(true);
        }

        isCharging = true;
        chargeTimer = 0f;

        StartFade(0f, 1f);

        Debug.Log("[PURIFY] BeginCharge");
    }

    public void CancelCharge()
    {
        if (!isCharging) return;

        isCharging = false;
        chargeTimer = 0f;

        StartFade(baseCircleAlpha: 1f, targetAlpha: 0f);

        Debug.Log("[PURIFY] CancelCharge");
    }

    private void CompleteCharge()
    {
        isCharging = false;

        StartFade(1f, 0f);

        stateController.BeginPurifying("ChargeComplete");

        Debug.Log("[PURIFY] Charge Complete");
    }

    // =========================
    // Update
    // =========================

    private void Update()
    {
        if (!isCharging) return;

        chargeTimer += Time.deltaTime;

        if (chargeTimer >= chargeDuration)
        {
            CompleteCharge();
        }
    }

    // =========================
    // Fade (Coroutine host here)
    // =========================

    private void StartFade(float baseCircleAlpha, float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(baseCircleAlpha, targetAlpha));
    }

    private System.Collections.IEnumerator FadeRoutine(float from, float to)
    {
        float t = 0f;

        baseCircle.SetAlpha(from);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            baseCircle.SetAlpha(a);
            yield return null;
        }

        baseCircle.SetAlpha(to);
    }
}