using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClickPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (IsActionBlocked())
            return;

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            stateController.BeginPurifying();
            TutorialManager.NotifyPurifyUsed();
            RequestMagicCircle(PurifyRequestType.Normal);
        }
        else
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないためおきよめできません。");
        }
    }

    public void OnClickEmergencyPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.KegareMax))
            return;

        ShowAd(() =>
        {
            if (stateController == null)
                stateController = CurrentYokaiContext.ResolveStateController();

            if (stateController != null)
            {
                stateController.ExecuteEmergencyPurify();
                TutorialManager.NotifyPurifyUsed();
            }
            else
            {
                Debug.LogWarning("[PURIFY] StateController が見つからないため緊急お祓いできません。");
            }
        });
    }

    public void OnClickStopPurify()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (!IsState(YokaiState.Purifying))
            return;

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.StopPurifying();
    }

    bool IsActionBlocked()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        // 不具合③: 状態未同期時はブロックせず、霊力0状態のみを明示的に弾く。
        return stateController != null
            && (stateController.isPurifying || stateController.currentState == YokaiState.EnergyEmpty);
    }

    bool IsState(YokaiState state)
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null || stateController.currentState == state)
            return true;

        return false;
    }

    void ShowAd(System.Action onCompleted)
    {
        onCompleted?.Invoke();
    }

    void RequestMagicCircle(PurifyRequestType requestType)
    {
        var activator = FindObjectOfType<MagicCircleActivator>();
        if (activator == null)
        {
            Debug.LogWarning("[PURIFY] MagicCircleActivator が見つからないため魔法陣を起動できません。");
            return;
        }

        if (requestType == PurifyRequestType.Emergency)
            activator.RequestEmergencyPurify();
        else
            activator.RequestNormalPurify();
    }

    enum PurifyRequestType
    {
        Normal,
        Emergency
    }
}
