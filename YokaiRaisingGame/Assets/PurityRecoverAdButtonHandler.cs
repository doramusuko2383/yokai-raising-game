using UnityEngine;
using Yokai;

public class PurityRecoverAdButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    PurityController purityController;

    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    float purityRecoverRatio = 0.5f;

    [SerializeField]
    float spiritRecoverRatio = 0.2f;

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurityRecoverAd()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        bool isPurityEmptyState = stateController != null
            ? stateController.currentState == YokaiState.PurityEmpty
            : purityController != null && purityController.IsPurityEmpty;

        if (!isPurityEmptyState)
            return;

        if (purityController == null || spiritController == null)
        {
            Debug.LogWarning("[PURIFY] Recovery failed: controller not found.");
            return;
        }

        float targetPurity = purityController.maxPurity * purityRecoverRatio;
        float purityDelta = targetPurity - purityController.purity;
        if (purityDelta > 0f)
            purityController.AddPurity(purityDelta, "PurityRecoverAd");

        float spiritAmount = spiritController.maxSpirit * spiritRecoverRatio;
        if (spiritAmount > 0f)
            spiritController.AddSpirit(spiritAmount);
    }
}
