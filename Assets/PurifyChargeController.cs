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
        stateController = CurrentYokaiContext.ResolveStateController();
    }

    public void StartCharging()
    {
        if (isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] StartCharging CALLED");

        isCharging = true;
        currentCharge = 0f;
    }

    public void CancelCharging()
    {
        if (!isCharging)
            return;

        Debug.Log("[PURIFY HOLD] CancelCharging");

        isCharging = false;
        currentCharge = 0f;
    }

    private void Update()
    {
        if (hasSucceeded)
            return;

        if (!isCharging)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (!Input.GetMouseButton(0))
        {
            CancelCharging();
            return;
        }
#else
        if (Input.touchCount == 0)
        {
            CancelCharging();
            return;
        }
#endif

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

        if (stateController != null)
        {
            stateController.StopPurifyingForSuccess("PurifyHold");
        }
    }

    public void ResetCharge()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;
    }
}
