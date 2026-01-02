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
        if (!IsState(YokaiState.Normal))
            return;

        StartPurifyEffect();

        if (kegareManager != null)
            kegareManager.Purify();
    }

    public void OnClickEmergencyPurify()
    {
        if (!IsState(YokaiState.KegareMax))
            return;

        ShowAd(() =>
        {
            StartPurifyEffect(isEmergency: true);

            if (kegareManager != null)
                kegareManager.ExecuteEmergencyPurify();
        });
    }

    bool IsState(YokaiState state)
    {
        return stateController == null || stateController.currentState == state;
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
