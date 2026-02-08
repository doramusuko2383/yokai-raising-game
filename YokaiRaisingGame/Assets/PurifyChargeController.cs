using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 2.0f;

    bool isCharging = false;
    bool hasSucceeded = false;
    float currentCharge = 0f;
    YokaiStateController stateController;

    public void BindStateController(YokaiStateController controller)
    {
        stateController = controller;
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        StartCharging();
    }

    public void OnPointerUp(BaseEventData eventData)
    {
        CancelCharging();
    }

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

    void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        currentCharge += Time.deltaTime;

        if (currentCharge >= chargeDuration)
        {
            Complete();
        }
    }

    void Complete()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        Debug.Log("[PURIFY HOLD] Complete");

        EnsureStateController();
        stateController?.StopPurifyingForSuccess();
    }

    public void ResetCharge()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        Debug.Log("[PURIFY HOLD] ResetCharge");
    }

    void EnsureStateController()
    {
        if (stateController != null)
            return;

        stateController = CurrentYokaiContext.ResolveStateController()
            ?? FindObjectOfType<YokaiStateController>(true);
    }
}
