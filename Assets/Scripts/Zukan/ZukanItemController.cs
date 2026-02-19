using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ZukanItemController : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;

    private string yokaiId;

    public static event Action<string> OnItemClicked;

    public void Setup(string id, Sprite icon, string displayName)
    {
        yokaiId = id;

        if (iconImage != null)
            iconImage.sprite = icon;

        if (nameText != null)
            nameText.text = displayName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                OnItemClicked?.Invoke(yokaiId);
            });
        }
    }
}
