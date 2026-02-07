using System.Collections;
using UnityEngine;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Settings")]
    public float chargeDuration = 2.0f;

    [Header("Refs")]
    public UIPentagramDrawer pentagramDrawer;
    public YokaiStateController stateController;
    [SerializeField] MagicCircleSwipeController magicCircle;
    [SerializeField] GameObject purifyHoldRoot;

    private bool isCharging = false;
    private bool hasSucceeded = false;
    private float currentCharge = 0f;
    private Coroutine returnRoutine;

    private IEnumerator ReturnPentagram(float fromProgress, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Lerp(fromProgress, 0f, t / duration);

            if (pentagramDrawer != null)
                pentagramDrawer.SetProgress(p);

            yield return null;
        }

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);
    }

    public void StartCharging()
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY][HOLD] StartCharging");

        if (purifyHoldRoot != null && !purifyHoldRoot.activeSelf)
            purifyHoldRoot.SetActive(true);

        isCharging = true;
        currentCharge = 0f;
    }

    public void CancelCharging()
    {
        // 成功後は一切キャンセルさせない
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY][HOLD] CancelCharging");

        isCharging = false;

        if (purifyHoldRoot != null && purifyHoldRoot.activeSelf)
            purifyHoldRoot.SetActive(false);

        float currentProgress = Mathf.Clamp01(currentCharge / chargeDuration);

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        returnRoutine = StartCoroutine(ReturnPentagram(currentProgress, 0.3f));

        currentCharge = 0f;
    }

    private void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        bool isPurifyingState =
            stateController != null &&
            stateController.CurrentState == YokaiState.Purifying;

        // Purifying 以外では進行しない
        if (!isPurifyingState)
            return;

        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        Debug.Log($"[PURIFY][HOLD] Progress={progress:0.00}");

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(progress);

        if (progress >= 1f)
            Complete();
    }

    private void Complete()
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY][HOLD] Complete");

        hasSucceeded = true;
        isCharging = false;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);

        var controller = ResolveStateController();
        if (controller != null)
        {
            controller.StopPurifyingForSuccess();
        }

        if (purifyHoldRoot != null)
            purifyHoldRoot.SetActive(false);
    }

    // 外部からのリセット用（次回おきよめ用）
    public void ResetPurify()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController;
    }
}
