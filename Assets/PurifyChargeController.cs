using UnityEngine;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Settings")]
    public float chargeDuration = 2.0f;

    [Header("Refs")]
    public UIPentagramDrawer pentagramDrawer;
    public YokaiStateController stateController;

    private bool isCharging = false;
    private bool hasSucceeded = false;
    private float currentCharge = 0f;

    public void StartCharging()
    {
        if (hasSucceeded)
            return;

        isCharging = true;
        currentCharge = 0f;
    }

    public void CancelCharging()
    {
        if (!isCharging)
            return;

        isCharging = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
            pentagramDrawer.SetProgress(0f);
    }

    private void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        bool isPurifyingState =
            stateController != null &&
            stateController.CurrentState == YokaiState.Purifying;

        // Purifying 以外では進めない
        if (!isPurifyingState)
            return;

        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(progress);
            Debug.Log("[PENTAGRAM] SetProgress called: " + progress);
        }

        if (progress >= 1f)
        {
            CompletePurify();
        }
    }

    // =====================
    // Complete
    // =====================

    private void CompletePurify()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        Debug.Log("[PURIFY] Complete!");

        // ★ 完成フラッシュ
       // if (pentagramDrawer != null)
        //{
         //   pentagramDrawer.PlayCompleteFlash();
       // }

        // ★ 状態遷移確定
        if (stateController != null)
        {
            stateController.NotifyPurifySucceeded();
        }
    }

    // =====================
    // External reset (保険)
    // =====================

    public void ResetPurify()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

       // if (pentagramDrawer != null)
        //{
         //   pentagramDrawer.SetProgress(0f);
        //}
    }
}
