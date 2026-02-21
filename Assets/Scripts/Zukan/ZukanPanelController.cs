using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZukanPanelController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    ZukanManager zukanManager;

    [Header("List")]
    [SerializeField]
    Transform contentParent;

    [SerializeField]
    ZukanItemController zukanItemPrefab;

    [Header("Panels")]
    [SerializeField]
    CanvasGroup zukanRootCanvasGroup;

    [SerializeField]
    GameObject zukanListPanel;

    [SerializeField]
    GameObject zukanDetailPanel;

    [Header("Detail")]
    [SerializeField]
    Image fullImage;

    [SerializeField]
    TMP_Text nameText;

    [SerializeField]
    TMP_Text descriptionText;

    void OnEnable()
    {
        Debug.Log("[ZUKAN] OnEnable");

        ZukanItemController.OnItemClicked += HandleItemClicked;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged += HandleSaveDataChanged;
    }

    void OnDisable()
    {
        Debug.Log("[ZUKAN] OnDisable");

        ZukanItemController.OnItemClicked -= HandleItemClicked;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged -= HandleSaveDataChanged;
    }

    public void OpenZukan()
    {
        Debug.Log("[ZUKAN] OpenZukan called");

        if (zukanRootCanvasGroup == null)
            return;

        zukanRootCanvasGroup.alpha = 1f;
        zukanRootCanvasGroup.interactable = true;
        zukanRootCanvasGroup.blocksRaycasts = true;

        if (zukanListPanel != null)
            zukanListPanel.SetActive(true);

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        Initialize();
    }

    public void ToggleZukan()
    {
        if (zukanRootCanvasGroup == null)
            return;

        bool isOpen = zukanRootCanvasGroup.alpha > 0.5f;

        if (isOpen)
            CloseZukan();
        else
            OpenZukan();
    }

    public void CloseZukan()
    {
        if (zukanRootCanvasGroup == null)
            return;

        zukanRootCanvasGroup.alpha = 0f;
        zukanRootCanvasGroup.interactable = false;
        zukanRootCanvasGroup.blocksRaycasts = false;

        if (zukanListPanel != null)
            zukanListPanel.SetActive(false);

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);
    }

    public void OpenZukanPanel() => OpenZukan();

    public void CloseZukanPanel() => CloseZukan();

    public void Initialize()
    {
        Debug.Log("[ZUKAN] Initialize called");

        if (contentParent == null || zukanItemPrefab == null || zukanManager == null)
        {
            Debug.LogWarning("[ZUKAN] Missing references in Initialize");
            return;
        }

        ClearChildren(contentParent);

        Debug.Log($"[ZUKAN] Yokai count: {zukanManager.allYokaiList.Count}");

        foreach (var data in zukanManager.allYokaiList)
        {
            var item = Instantiate(zukanItemPrefab, contentParent);
            item.Setup(data.id.ToString(), data.icon, data.displayName);
        }
    }

    public void CloseDetailPanel()
    {
        if (zukanDetailPanel == null)
            return;

        zukanDetailPanel.SetActive(false);
    }

    void HandleItemClicked(string id)
    {
        Debug.Log($"[ZUKAN] Item clicked: {id}");

        var data = zukanManager.GetData(id);

        if (data == null)
        {
            Debug.LogWarning("[ZUKAN] Data not found for id: " + id);
            return;
        }

        OpenDetail(data);
    }

    private void OpenDetail(YokaiData data)
    {
        if (fullImage != null)
            fullImage.sprite = data.fullImage;

        if (nameText != null)
            nameText.text = data.displayName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(true);
    }


    void HandleSaveDataChanged()
    {
        if (zukanRootCanvasGroup != null && zukanRootCanvasGroup.alpha > 0.5f)
            Initialize();
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
