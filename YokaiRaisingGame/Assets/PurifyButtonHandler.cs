using UnityEngine;

public class PurifyButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    public void OnClickPurify()
    {
        if (!IsState(YokaiState.Normal))
            return;

        FindObjectOfType<MagicCircleActivator>()?.RequestNormalPurify();
    }

    public void OnClickEmergencyPurify()
    {
        if (!IsState(YokaiState.KegareMax))
            return;

        ShowAd(() =>
        {
            FindObjectOfType<MagicCircleActivator>()?.RequestEmergencyPurify();
        });
    }

    bool IsState(YokaiState state)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        return stateController == null || stateController.currentState == state;
    }

    void ShowAd(System.Action onCompleted)
    {
        Debug.Log("[AD] Showing rewarded ad before emergency purify.");
        onCompleted?.Invoke();
    }
}
