using System.Collections;
using UnityEngine;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float chargeDuration = 2.0f;

    [Header("Refs")]
    [SerializeField] private UIPentagramDrawer pentagramDrawer;
    [SerializeField] private YokaiStateController stateController;
    [SerializeField] private MagicCircleSwipeController magicCircle;
    [SerializeField] private GameObject purifyHoldRoot;

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
        Debug.Log("[PURIFY HOLD] StartCharging CALLED");
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY][HOLD] StartCharging");

        isCharging = true;
        currentCharge = 0f;

        if (purifyHoldRoot != null)
            purifyHoldRoot.SetActive(true);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.gameObject.SetActive(true);
            pentagramDrawer.SetProgress(0f);
        }
    }

    public void CancelCharging()
    {
        // 成功後は一切キャンセルさせない
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY][HOLD] CancelCharging");

        isCharging = false;

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);

        if (magicCircle != null)
            magicCircle.Hide();

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
        if (!isCharging)
            return;

        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        Debug.Log("[PURIFY HOLD] Progress=" + progress);

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
    private void ResetPurify()
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
