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

    public void OnClickDango()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
        string stateName = stateController != null ? stateController.currentState.ToString() : "null";
        if (IsActionBlocked())
            return;

        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        if (spiritController != null)
        {
            spiritController.AddSpirit(dangoAmount);
            Debug.Log($"[SPIRIT] Dango +{dangoAmount:0.##} spirit={spiritController.spirit:0.##}/{spiritController.maxSpirit:0.##}");
            TutorialManager.NotifyDangoUsed();
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        }
        else
        {
            Debug.LogWarning("[SPIRIT] SpiritController が見つからないためだんごが使えません。");
        }
    }

    bool IsActionBlocked()
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null)
            return false;

        return stateController.currentState != YokaiState.Normal
            && stateController.currentState != YokaiState.EvolutionReady;
    }
}
