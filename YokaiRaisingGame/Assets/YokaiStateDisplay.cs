using TMPro;
using UnityEngine;

public class YokaiStateDisplay : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private KegareManager kegareManager;

    [Header("表示テキスト")]
    [SerializeField] private string normalLabel = "現在の状態：通常";
    [SerializeField] private string weakLabel = "現在の状態：瀕死";
    [SerializeField] private string mononokeLabel = "現在の状態：モノノケ化";

    void Start()
    {
        UpdateStateLabel();
    }

    void LateUpdate()
    {
        UpdateStateLabel();
    }

    void UpdateStateLabel()
    {
        if (stateText == null)
        {
            return;
        }

        if (kegareManager != null && kegareManager.IsMononoke)
        {
            stateText.text = mononokeLabel;
            return;
        }

        if (energyManager != null && energyManager.currentState == YokaiState.Weak)
        {
            stateText.text = weakLabel;
            return;
        }

        stateText.text = normalLabel;
    }
}
