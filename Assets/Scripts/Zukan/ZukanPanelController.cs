using System.Collections;
using System.Collections.Generic;
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

    [SerializeField]
    ScrollRect listScrollRect;

    [SerializeField]
    Button leftArrowButton;

    [SerializeField]
    Button rightArrowButton;

    [SerializeField]
    Vector2 cellSpacing = new Vector2(16f, 16f);

    [Header("Panels")]
    [SerializeField]
    CanvasGroup zukanRootCanvasGroup;

    [SerializeField]
    GameObject zukanListPanel;

    [SerializeField]
    GameObject zukanDetailPanel;

    [SerializeField]
    RectTransform detailAreaRect;

    [Header("Detail")]
    [SerializeField]
    Image fullImage;

    [SerializeField]
    TMP_Text nameText;

    [SerializeField]
    TMP_Text descriptionText;

    [SerializeField]
    Button backButton;

    [SerializeField]
    TMP_Text pageText;

    [SerializeField]
    string lockedNameText = "???";

    [SerializeField]
    string lockedDescriptionText = "未解放";

    const int ColumnsPerPage = 3;
    const int RowsPerPage = 4;
    const int ItemsPerPage = ColumnsPerPage * RowsPerPage;
    const float MinValidViewportSize = 100f;

    readonly List<YokaiData> itemOrder = new List<YokaiData>();
    readonly Dictionary<string, bool> unlockCache = new Dictionary<string, bool>();

    RectTransform contentRect;
    RectTransform viewportRect;
    int pageCount = 1;
    int currentPage;
    bool isSnapping;

    void Awake()
    {
        EnsureWired();
        EnsureLayoutStability();

        if (fullImage != null)
            fullImage.raycastTarget = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            return;
    }
#endif

    void OnEnable()
    {
        ZukanItemController.OnItemClicked += HandleItemClicked;

        // 一時的にページ切替機能を停止。
        // if (leftArrowButton != null)
        //     leftArrowButton.onClick.AddListener(PrevPage);

        // if (rightArrowButton != null)
        //     rightArrowButton.onClick.AddListener(NextPage);

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged += HandleSaveDataChanged;
    }

    void OnDisable()
    {
        ZukanItemController.OnItemClicked -= HandleItemClicked;

        // 一時的にページ切替機能を停止。
        // if (leftArrowButton != null)
        //     leftArrowButton.onClick.RemoveListener(PrevPage);

        // if (rightArrowButton != null)
        //     rightArrowButton.onClick.RemoveListener(NextPage);

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged -= HandleSaveDataChanged;
    }

    void LateUpdate()
    {
        // 一時的にページ切替機能を停止。
        /*
        if (zukanRootCanvasGroup == null ||
            zukanRootCanvasGroup.alpha <= 0.5f ||
            listScrollRect == null ||
            pageCount <= 1 ||
            isSnapping)
            return;

        if (!Input.GetMouseButton(0))
        {
            int nearestPage = GetNearestPage();
            if (nearestPage != currentPage)
                SetPage(nearestPage, true);
            else
                SnapToCurrentPage(false);
        }
        */
    }

    public void OpenZukan()
    {
        if (zukanRootCanvasGroup == null || !EnsureWired())
            return;

        Debug.Log("[ZukanPanelController] OpenZukan()");

        zukanRootCanvasGroup.alpha = 1f;
        zukanRootCanvasGroup.interactable = true;
        zukanRootCanvasGroup.blocksRaycasts = true;

        if (zukanListPanel != null)
            zukanListPanel.SetActive(true);

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        if (fullImage != null)
            fullImage.raycastTarget = false;

        EnsureLayoutStability();

        StartCoroutine(InitializeAfterLayout());
    }

    IEnumerator InitializeAfterLayout()
    {
        const int MaxWaitFrames = 12;

        for (int frame = 0; frame < MaxWaitFrames; frame++)
        {
            Canvas.ForceUpdateCanvases();

            if (viewportRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

                Rect rect = viewportRect.rect;
                if (rect.width >= MinValidViewportSize && rect.height >= MinValidViewportSize)
                    break;
            }

            yield return null;
        }

        Canvas.ForceUpdateCanvases();

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

        Debug.Log("[ZukanPanelController] CloseZukan()");

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
        if (!EnsureWired() || contentParent == null || zukanItemPrefab == null || zukanManager == null)
            return;

        CacheUnlockedStatus();
        BuildPagedList();
        // 一時的にページ切替機能を停止。
        // SetPage(0, false);
    }

    public void CloseDetailPanel()
    {
        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        if (listScrollRect != null)
            listScrollRect.enabled = true;
    }

    public void OnClickBack()
    {
        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        if (listScrollRect != null)
            listScrollRect.enabled = true;

        Canvas.ForceUpdateCanvases();

        var rootRect = GetComponentInParent<RectTransform>();
        if (rootRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }

    void BuildPagedList()
    {
        if (!EnsureWired())
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

        ClearChildren(contentParent);
        itemOrder.Clear();

        if (zukanManager.allYokaiList == null)
        {
            pageCount = 1;
            return;
        }

        pageCount = 1;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

        int createdItems = 0;

        for (int i = 0; i < zukanManager.allYokaiList.Count; i++)
        {
            var data = zukanManager.allYokaiList[i];
            var item = Instantiate(zukanItemPrefab, contentParent);
            bool unlocked = IsUnlocked(data.id.ToString());
            item.Setup(data.id.ToString(), data.icon, data.displayName, unlocked, lockedNameText);
            itemOrder.Add(data);
            createdItems++;
        }

        Debug.Log("[ZukanPanelController] BuildPagedList(): " +
                  ", pageCount=" + pageCount +
                  ", createdItems=" + createdItems);
        LogRectState("BuildPagedList", pageCount, createdItems);

        Canvas.ForceUpdateCanvases();
        // 一時的にページ切替機能を停止。
        // SnapToCurrentPage(true);
        // UpdateArrowInteractable();
    }

    void HandleItemClicked(string id)
    {
        var data = zukanManager.GetData(id);
        if (data == null)
            return;

        OpenDetail(data);
    }

    void OpenDetail(YokaiData data)
    {
        if (data == null)
            return;

        bool unlocked = IsUnlocked(data.id.ToString());

        if (fullImage != null)
        {
            fullImage.sprite = data.fullImage != null ? data.fullImage : data.icon;
            fullImage.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 0.85f);
        }

        if (nameText != null)
            nameText.text = unlocked ? data.displayName : lockedNameText;

        if (descriptionText != null)
            descriptionText.text = unlocked ? data.description : lockedDescriptionText;

        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(true);

        if (listScrollRect != null)
            listScrollRect.enabled = false;

        int index = itemOrder.IndexOf(data);
        int totalCount = zukanManager != null && zukanManager.allYokaiList != null ? zukanManager.allYokaiList.Count : 0;
        if (pageText != null)
        {
            string current = (index + 1).ToString("000");
            string total = totalCount.ToString("000");
            pageText.text = $"No.{current} / {total}";
        }

        Debug.Log($"[ZukanPanelController] OpenDetail(): id={data.id}, unlocked={unlocked}");
    }

    void CacheUnlockedStatus()
    {
        unlockCache.Clear();

        if (zukanManager == null || zukanManager.allYokaiList == null)
            return;

        foreach (var data in zukanManager.allYokaiList)
            unlockCache[data.id.ToString()] = IsUnlockedFromSave(data.id);
    }

    bool IsUnlocked(string id)
    {
        return unlockCache.TryGetValue(id, out bool unlocked) && unlocked;
    }

    bool IsUnlockedFromSave(int yokaiId)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null || SaveManager.Instance.CurrentSave.unlockedYokaiIds == null)
            return false;

        return SaveManager.Instance.CurrentSave.unlockedYokaiIds.Contains(yokaiId);
    }

    void PrevPage()
    {
        SetPage(currentPage - 1, true);
    }

    void NextPage()
    {
        SetPage(currentPage + 1, true);
    }

    void SetPage(int page, bool smooth)
    {
        int clamped = Mathf.Clamp(page, 0, pageCount - 1);
        if (clamped != currentPage)
            currentPage = clamped;

        SnapToCurrentPage(smooth);
        UpdateArrowInteractable();
    }

    void SnapToCurrentPage(bool smooth)
    {
        if (listScrollRect == null || pageCount <= 1)
            return;

        float target = currentPage / (float)(pageCount - 1);
        if (smooth)
            StartCoroutine(SmoothSnap(target));
        else
            listScrollRect.horizontalNormalizedPosition = target;
    }

    IEnumerator SmoothSnap(float target)
    {
        isSnapping = true;
        float velocity = 0f;

        while (Mathf.Abs(listScrollRect.horizontalNormalizedPosition - target) > 0.001f)
        {
            float value = Mathf.SmoothDamp(
                listScrollRect.horizontalNormalizedPosition,
                target,
                ref velocity,
                0.12f,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            listScrollRect.horizontalNormalizedPosition = value;
            yield return null;
        }

        listScrollRect.horizontalNormalizedPosition = target;
        isSnapping = false;
    }

    int GetNearestPage()
    {
        if (pageCount <= 1)
            return 0;

        float t = listScrollRect.horizontalNormalizedPosition;
        return Mathf.RoundToInt(t * (pageCount - 1));
    }

    float GetViewportWidth()
    {
        if (listScrollRect != null)
        {
            RectTransform viewport = listScrollRect.viewport;
            if (viewport != null)
            {
                float width = viewport.GetComponent<RectTransform>().rect.width;
                if (width > 10f)
                    return width;
            }
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                return canvasRect.rect.width;
        }

        return 1080f;
    }

    float GetViewportHeight()
    {
        if (listScrollRect != null)
        {
            RectTransform viewport = listScrollRect.viewport;
            if (viewport != null)
            {
                float height = viewport.GetComponent<RectTransform>().rect.height;
                if (height > 10f)
                    return height;
            }
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                return canvasRect.rect.height;
        }

        return 1920f;
    }

    void UpdateArrowInteractable()
    {
        if (leftArrowButton != null)
            leftArrowButton.interactable = currentPage > 0;

        if (rightArrowButton != null)
            rightArrowButton.interactable = currentPage < pageCount - 1;
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

    bool EnsureWired()
    {
        if (listScrollRect == null && zukanListPanel != null)
            listScrollRect = zukanListPanel.GetComponentInChildren<ScrollRect>(true);

        if (listScrollRect == null)
            listScrollRect = GetComponentInChildren<ScrollRect>(true);

        if (listScrollRect == null)
        {
            Debug.LogWarning("[ZukanPanelController] EnsureWired() failed: ScrollRect is missing.");
            return false;
        }

        listScrollRect.horizontal = true;
        listScrollRect.vertical = false;

        RectTransform parentRect = contentParent as RectTransform;
        if (parentRect == null)
            parentRect = listScrollRect.content;

        if (parentRect == null)
        {
            Debug.LogWarning("[ZukanPanelController] EnsureWired() failed: contentParent/content is missing RectTransform.");
            return false;
        }

        if (listScrollRect.content != parentRect)
        {
            listScrollRect.content = parentRect;
            Debug.Log("[ZukanPanelController] EnsureWired(): repaired ScrollRect.content reference.");
        }

        contentParent = parentRect;
        contentRect = listScrollRect.content;

        viewportRect = listScrollRect.viewport;
        if (viewportRect == null)
        {
            ScrollRect fallback = GetComponentInChildren<ScrollRect>(true);
            if (fallback != null)
            {
                listScrollRect = fallback;
                if (listScrollRect.content != parentRect)
                    listScrollRect.content = parentRect;

                viewportRect = listScrollRect.viewport;
            }
        }

        if (viewportRect == null || contentRect == null)
        {
            Debug.LogWarning($"[ZukanPanelController] EnsureWired() failed: viewport/content missing. viewportNull={viewportRect == null}, contentNull={contentRect == null}");
            return false;
        }

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        EnsureLayoutStability();

        if (detailAreaRect != null)
        {
            var le = detailAreaRect.GetComponent<LayoutElement>();
            if (le == null)
                le = detailAreaRect.gameObject.AddComponent<LayoutElement>();

            le.ignoreLayout = true;
        }

        return true;
    }

    void EnsureLayoutStability()
    {
        if (listScrollRect != null)
        {
            RectTransform listAreaRect = listScrollRect.transform.parent as RectTransform;
            if (listAreaRect == null)
                listAreaRect = listScrollRect.transform as RectTransform;

            if (listAreaRect != null)
            {
                var listAreaLayout = listAreaRect.GetComponent<LayoutElement>();
                if (listAreaLayout == null)
                    listAreaLayout = listAreaRect.gameObject.AddComponent<LayoutElement>();

                listAreaLayout.flexibleHeight = 1f;
                listAreaLayout.flexibleWidth = 1f;
                listAreaLayout.ignoreLayout = false;

                listAreaRect.anchorMin = Vector2.zero;
                listAreaRect.anchorMax = Vector2.one;
                listAreaRect.offsetMin = Vector2.zero;
                listAreaRect.offsetMax = Vector2.zero;
            }
        }

        if (zukanDetailPanel != null)
        {
            RectTransform detailRect = zukanDetailPanel.transform as RectTransform;
            if (detailRect != null)
            {
                var detailLayout = detailRect.GetComponent<LayoutElement>();
                if (detailLayout == null)
                    detailLayout = detailRect.gameObject.AddComponent<LayoutElement>();

                detailLayout.ignoreLayout = true;

                detailRect.anchorMin = Vector2.zero;
                detailRect.anchorMax = Vector2.one;
                detailRect.offsetMin = Vector2.zero;
                detailRect.offsetMax = Vector2.zero;
            }
        }

        if (fullImage != null)
            fullImage.raycastTarget = false;
    }

    void LogRectState(string context, int pages, int createdItems)
    {
        Debug.Log($"[ZukanPanelController] {context} rects: viewportRect={RectToString(viewportRect)}, " +
                  $"contentRect={RectToString(contentRect)}, contentAnchored={contentRect.anchoredPosition}, " +
                  $"contentSizeDelta={contentRect.sizeDelta}, pageCount={pages}, createdItems={createdItems}");
    }

    static string RectToString(RectTransform rect)
    {
        return rect == null ? "null" : rect.rect.ToString();
    }
}
