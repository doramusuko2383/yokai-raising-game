using UnityEngine;
using Yokai;
public class PurifyChargeController : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 2.0f;
    [SerializeField] YokaiStateController stateController;

    private bool isCharging = false;
    private bool hasSucceeded = false;
    private float currentCharge = 0f;

    public void BindStateController(YokaiStateController controller)
    {
        stateController = controller;
    }

    private void Awake()
    {

    }

    private void OnEnable()
    {
        ResolveStateController(warn: false);
    }

    YokaiStateController ResolveStateController(bool warn)
    {
        var resolved =
            CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);

        stateController = resolved;

        if (warn && resolved == null)
            Debug.LogError("[PURIFY HOLD] StateController could not be resolved.");

        return resolved;
    }

    /// <summary>
    /// 五芒星の PointerDown から呼ばれる
    /// </summary>
    public void StartCharging()
    {
        ResolveStateController(warn: true);
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
        if (!isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] Update tick");

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
        var sc = ResolveStateController(warn: true);
        if (sc == null)
            return;

        sc.StopPurifyingForSuccess();
        Debug.Log($"[PURIFY HOLD] StopPurifyingForSuccess called sc={(sc != null)}");
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
