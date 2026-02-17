using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZukanItemController : MonoBehaviour
{
    [SerializeField]
    Image iconImage;

    [SerializeField]
    Button button;

    [SerializeField]
    TMP_Text nameText;

    YokaiData yokaiData;

    public void Setup(YokaiData data, bool isUnlocked, System.Action<YokaiData> onClick)
    {
        yokaiData = data;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.color = isUnlocked ? Color.white : new Color(0f, 0f, 0f, 1f);
        }

        if (nameText != null)
            nameText.text = isUnlocked && data != null ? data.displayName : "？？？";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = isUnlocked;
            button.onClick.AddListener(() =>
            {
                if (yokaiData != null)
                    onClick?.Invoke(yokaiData);
            });
        }
    }
}
