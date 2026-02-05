using UnityEngine;
using UnityEngine.EventSystems;
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

    // =====================
    // EventTrigger
    // =====================

    public void OnPointerDown(BaseEventData eventData)
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY] PointerDown");

        isCharging = true;
        currentCharge = 0f;

        // ★ 開始時は描画を0から
        //if (pentagramDrawer != null)
        //{
           // pentagramDrawer.SetProgress(0f);
        //}
    }

    public void OnPointerUp(BaseEventData eventData)
    {
        if (!isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY] PointerUp");

        isCharging = false;
        currentCharge = 0f;

        if (stateController != null && stateController.CurrentState == YokaiState.Purifying)
        {
            return;
        }

        // ★ 離したら逆再生で消す
       // if (pentagramDrawer != null)
        //{
         //   pentagramDrawer.ReverseAndClear();
        //}
    }

    // =====================
    // Update
    // =====================

    private void Update()
    {
        Debug.Log($"[PURIFY] Update isCharging={isCharging}");

        if (stateController != null && stateController.CurrentState != YokaiState.Purifying)
            return;

        if (!isCharging || hasSucceeded)
            return;

        currentCharge += Time.deltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(progress);
            Debug.Log("[PENTAGRAM] SetProgress called");
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
