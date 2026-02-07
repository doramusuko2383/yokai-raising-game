using UnityEngine;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 2.0f;

    private bool isCharging = false;
    private bool hasSucceeded = false;
    private float currentCharge = 0f;

    private YokaiStateController stateController;

    private void Awake()
    {

    }

    /// <summary>
    /// 五芒星の PointerDown から呼ばれる
    /// </summary>
    public void StartCharging()
    {
        if (hasSucceeded)
            return;

        if (isCharging)
            return;

        Debug.Log("[PURIFY HOLD] StartCharging CALLED");

        isCharging = true;
        currentCharge = 0f;
    }

    /// <summary>
    /// PointerUp / Exit から呼ばれる
    /// </summary>
    public void CancelCharging()
    {
        if (!isCharging)
            return;

        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] CancelCharging");

        isCharging = false;
        currentCharge = 0f;
    }

    private void Update()
    {
        Debug.Log("[PURIFY HOLD] Update tick");

        if (!isCharging)
            return;

        if (hasSucceeded)
            return;

        currentCharge += Time.deltaTime;
        float progress = currentCharge / chargeDuration;

        Debug.Log($"[PURIFY HOLD] Progress={progress:F2}");

        if (currentCharge >= chargeDuration)
        {
            Complete();
        }
    }

    private void Complete()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        Debug.Log("[PURIFY HOLD] Complete");

        var stateController = CurrentYokaiContext.ResolveStateController();
        if (stateController != null)
        {
            stateController.StopPurifyingForSuccess();

        }
        else
        {
            Debug.LogWarning("[PURIFY HOLD] StateController missing on Complete");
        }
    }

    /// <summary>
    /// State 側から「おきよめ終了・中断」された場合に呼ぶ
    /// </summary>
    public void ResetCharge()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        Debug.Log("[PURIFY HOLD] ResetCharge");
    }
}
