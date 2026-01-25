using UnityEngine;
using UnityEngine.Serialization;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    float dangoAmount = 30f;
    bool hasWarnedMissingStateController;
    bool hasWarnedMissingSpiritController;

    public void OnClickDango()
    {
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        ResolveStateController();
        if (IsActionBlocked())
            return;

        ResolveSpiritController();

        if (spiritController != null)
        {
            spiritController.AddSpirit(dangoAmount);
            TutorialManager.NotifyDangoUsed();
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
            stateController?.RequestEvaluateState("SpiritRecovered");
        }
        else
        {
            WarnMissingSpiritController();
        }
    }

    bool IsActionBlocked()
    {
        ResolveStateController();

        if (stateController == null)
            return false;

        return stateController.currentState != YokaiState.Normal
            && stateController.currentState != YokaiState.EvolutionReady;
    }

    void ResolveStateController()
    {
        if (stateController != null)
            return;

        stateController = FindObjectOfType<YokaiStateController>(true);
        if (stateController == null)
            WarnMissingStateController();
    }

    void ResolveSpiritController()
    {
        if (spiritController != null)
            return;

        spiritController = FindObjectOfType<SpiritController>(true);
        if (spiritController == null)
            WarnMissingSpiritController();
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[DANGO] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }

    void WarnMissingSpiritController()
    {
        if (hasWarnedMissingSpiritController)
            return;

        Debug.LogWarning("[SPIRIT] SpiritController が見つからないためだんごが使えません。");
        hasWarnedMissingSpiritController = true;
    }
}
