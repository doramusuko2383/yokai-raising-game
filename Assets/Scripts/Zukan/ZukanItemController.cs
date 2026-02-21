using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZukanItemController : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Color lockedTint = new Color(0f, 0f, 0f, 0.85f);

    string yokaiId;

    public static event Action<string> OnItemClicked;

    public void Setup(string id, Sprite icon, string displayName)
    {
        Setup(id, icon, displayName, true, "???");
    }

    public void Setup(string id, Sprite icon, string displayName, bool unlocked, string lockedName)
    {
        yokaiId = id;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = unlocked ? Color.white : lockedTint;
        }

        if (nameText != null)
            nameText.text = unlocked ? displayName : lockedName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { OnItemClicked?.Invoke(yokaiId); });
        }
    }
}
