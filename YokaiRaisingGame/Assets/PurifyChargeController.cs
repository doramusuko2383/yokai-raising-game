using UnityEngine;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [SerializeField]
    float chargeDuration = 2.0f;

    [SerializeField]
    PentagramDrawer pentagramDrawer;

    [SerializeField]
    YokaiStateController stateController;

    float currentCharge;
    bool isCharging;
    bool hasSucceeded;

    void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        float duration = Mathf.Max(0.01f, chargeDuration);
        currentCharge += Time.deltaTime;

        float normalized = Mathf.Clamp01(currentCharge / duration);
        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(normalized);
        }

        if (normalized >= 1f)
        {
            CompletePurify();
        }
    }

    public void OnPointerDown()
    {
        if (hasSucceeded)
            return;

        isCharging = true;
        currentCharge = 0f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(0f);
        }
    }

    public void OnPointerUp()
    {
        if (hasSucceeded)
            return;

        if (!isCharging)
            return;

        isCharging = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.ReverseAndClear();
        }
    }

    void OnDisable()
    {
        isCharging = false;
        currentCharge = 0f;
        hasSucceeded = false;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.ReverseAndClear();
        }
    }

    void CompletePurify()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(1f);
            pentagramDrawer.PlayCompleteFlash();
        }

        if (stateController != null)
        {
            stateController.NotifyPurifySucceeded();
        }
    }
}
