using UnityEngine;
using Yokai;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    MagicCircleActivator magicCircleActivator;

    public void OnClickPurify()
    {
        if (!IsState(YokaiState.Normal, "おきよめ"))
            return;

        StartPurifyEffect();

        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager != null)
            kegareManager.Purify();
        else
            Debug.LogWarning("[PURIFY] KegareManager が見つからないためおきよめできません。");
    }

    public void OnClickEmergencyPurify()
    {
        if (!IsState(YokaiState.KegareMax, "緊急お祓い"))
            return;

        ShowAd(() =>
        {
            StartPurifyEffect(isEmergency: true);

            if (kegareManager == null)
                kegareManager = FindObjectOfType<KegareManager>();

            if (kegareManager != null)
                kegareManager.ExecuteEmergencyPurify();
            else
                Debug.LogWarning("[PURIFY] KegareManager が見つからないため緊急お祓いできません。");
        });
    }

    bool IsState(YokaiState state, string actionLabel)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null || stateController.currentState == state)
            return true;

        // DEBUG: 状態不一致で処理が止まった理由を明示する
        Debug.Log($"[ACTION BLOCK] {actionLabel} blocked. state={stateController.currentState}");
        return false;
    }

    void StartPurifyEffect()
    {
        if (magicCircleActivator != null)
            magicCircleActivator.RequestNormalPurify();
    }

    void StartPurifyEffect(bool isEmergency)
    {
        if (magicCircleActivator == null)
            return;

        if (isEmergency)
        {
            magicCircleActivator.RequestEmergencyPurify();
        }
        else
        {
            magicCircleActivator.RequestNormalPurify();
        }
    }

    void ShowAd(System.Action onCompleted)
    {
        Debug.Log("[AD] Showing rewarded ad before emergency purify.");
        onCompleted?.Invoke();
    }
}
