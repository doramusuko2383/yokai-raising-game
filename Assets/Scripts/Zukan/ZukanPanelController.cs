using System.Collections;
using System;
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
    GameObject zukanPanel;

    [SerializeField]
    CanvasGroup zukanRootCanvasGroup;

    [SerializeField]
    GameObject zukanDetailPanel;

    [SerializeField]
    CanvasGroup detailCanvasGroup;

    [Header("Detail")]
    [SerializeField]
    Image fullImage;

    [SerializeField]
    TMP_Text nameText;

    [SerializeField]
    TMP_Text descriptionText;

    [Header("Animation")]
    [SerializeField]
    float fadeDuration = 0.2f;

    Coroutine fadeCoroutine;

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

        if (zukanRootCanvasGroup != null)
        {
            zukanRootCanvasGroup.alpha = 1f;
            zukanRootCanvasGroup.interactable = true;
            zukanRootCanvasGroup.blocksRaycasts = true;
        }

        if (zukanPanel != null)
            zukanPanel.SetActive(true);

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(true);

        Initialize();
    }

    public void CloseZukan()
    {
        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        if (zukanRootCanvasGroup != null)
        {
            zukanRootCanvasGroup.alpha = 0f;
            zukanRootCanvasGroup.interactable = false;
            zukanRootCanvasGroup.blocksRaycasts = false;
        }

        if (zukanPanel != null)
            zukanPanel.SetActive(false);
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

        StartFade(false);
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
        if (zukanPanel != null && zukanPanel.activeInHierarchy)
            Initialize();
    }

    void StartFade(bool isOpening)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeDetailCoroutine(isOpening));
    }

    IEnumerator FadeDetailCoroutine(bool isOpening)
    {
        if (zukanDetailPanel == null)
            yield break;

        if (detailCanvasGroup == null)
        {
            zukanDetailPanel.SetActive(isOpening);
            yield break;
        }

        if (isOpening)
        {
            zukanDetailPanel.SetActive(true);
            detailCanvasGroup.alpha = 0f;
        }

        float start = detailCanvasGroup.alpha;
        float target = isOpening ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            detailCanvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        detailCanvasGroup.alpha = target;

        if (!isOpening)
            zukanDetailPanel.SetActive(false);
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
