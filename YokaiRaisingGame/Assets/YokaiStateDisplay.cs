using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YokaiStateDisplay : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image stateBackground;

    [Header("表示テキスト")]
    [SerializeField] private string normalLabel = "現在の状態：通常";
    [SerializeField] private string weakLabel = "現在の状態：瀕死";
    [SerializeField] private string mononokeLabel = "現在の状態：モノノケ化";

    [Header("状態（Inspector用）")]
    [SerializeField] private YokaiState currentState = YokaiState.Normal;

    [Header("色設定")]
    [SerializeField] private Color normalTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color mononokeTextColor = new Color(0.55f, 0.3f, 0.75f, 1f);
    [SerializeField] private Color weakTextColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.35f);
    [SerializeField] private Color mononokeBackgroundColor = new Color(0.2f, 0.05f, 0.3f, 0.45f);
    [SerializeField] private Color weakBackgroundColor = new Color(0.4f, 0.05f, 0.05f, 0.45f);

    void Start()
    {
        UpdateStateLabel();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UpdateStateLabel();
    }
#endif

    void UpdateStateLabel()
    {
        if (stateText == null)
        {
            return;
        }

        switch (currentState)
        {
            case YokaiState.Mononoke:
                stateText.text = mononokeLabel;
                stateText.color = mononokeTextColor;
                stateText.fontStyle = FontStyles.Bold;
                SetBackgroundColor(mononokeBackgroundColor);
                return;
            case YokaiState.Weak:
                stateText.text = weakLabel;
                stateText.color = weakTextColor;
                stateText.fontStyle = FontStyles.Bold;
                SetBackgroundColor(weakBackgroundColor);
                return;
            default:
                stateText.text = normalLabel;
                stateText.color = normalTextColor;
                stateText.fontStyle = FontStyles.Normal;
                SetBackgroundColor(normalBackgroundColor);
                return;
        }
    }

    void SetBackgroundColor(Color color)
    {
        if (stateBackground == null)
        {
            return;
        }

        stateBackground.color = color;
    }
}
