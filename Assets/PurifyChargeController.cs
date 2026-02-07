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

    public void StartCharging()
    {
        if (hasSucceeded || isCharging)
            return;

        Debug.Log("[PURIFY HOLD] StartCharging");

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
        if (!isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] CancelCharging");

        isCharging = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);

        if (purifyHoldRoot != null)
            purifyHoldRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(progress);

        if (progress >= 1f)
            Complete();
    }

    private void Complete()
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] Complete");

        hasSucceeded = true;
        isCharging = false;

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);

        var controller = ResolveStateController();
        if (controller != null)
            controller.StopPurifyingForSuccess();

        if (purifyHoldRoot != null)
            purifyHoldRoot.SetActive(false);
    }

    // 外部からのリセット用（次回おきよめ用）
    private void ResetPurify()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController;
    }
}
