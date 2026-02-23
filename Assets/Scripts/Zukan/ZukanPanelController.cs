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

    [Header("Detail")]
    [SerializeField]
    Image fullImage;

    [SerializeField]
    TMP_Text nameText;

    [SerializeField]
    TMP_Text descriptionText;

    [SerializeField]
    string lockedNameText = "???";

    [SerializeField]
    string lockedDescriptionText = "未解放";

    const int ColumnsPerPage = 3;
    const int RowsPerPage = 4;
    const int ItemsPerPage = ColumnsPerPage * RowsPerPage;

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

        if (fullImage != null)
            fullImage.raycastTarget = false;
    }

    void OnValidate()
    {
        EnsureWired();

        if (fullImage != null)
            fullImage.raycastTarget = false;
    }

    void OnEnable()
    {
        ZukanItemController.OnItemClicked += HandleItemClicked;

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(PrevPage);

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(NextPage);

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged += HandleSaveDataChanged;
    }

    void OnDisable()
    {
        ZukanItemController.OnItemClicked -= HandleItemClicked;

        if (leftArrowButton != null)
            leftArrowButton.onClick.RemoveListener(PrevPage);

        if (rightArrowButton != null)
            rightArrowButton.onClick.RemoveListener(NextPage);

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged -= HandleSaveDataChanged;
    }

    void LateUpdate()
    {
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

        StartCoroutine(InitializeAfterLayout());
    }

    IEnumerator InitializeAfterLayout()
    {
        // レイアウト確定待ち
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        if (viewportRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

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
        SetPage(0, false);
    }

    public void CloseDetailPanel()
    {
        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);
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

        pageCount = Mathf.Max(1, Mathf.CeilToInt(zukanManager.allYokaiList.Count / (float)ItemsPerPage));

        ConfigureContentAsPageContainer();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

        float pageWidth = viewportRect.rect.width;
        float pageHeight = viewportRect.rect.height;

        if (pageWidth < 100f || pageHeight < 100f)
        {
            Debug.LogWarning($"[ZukanPanelController] BuildPagedList() aborted due to invalid viewport size: " +
                             $"pageWidth={pageWidth}, pageHeight={pageHeight}, viewportRect={RectToString(viewportRect)}, contentRect={RectToString(contentRect)}");
            LogRectState("BuildPagedList(aborted)", pageCount, 0);
            return;
        }

        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(pageWidth * pageCount, pageHeight);

        int createdItems = 0;

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var pageRoot = new GameObject($"Page_{pageIndex + 1}", typeof(RectTransform), typeof(GridLayoutGroup));
            var pageRect = pageRoot.GetComponent<RectTransform>();
            pageRect.SetParent(contentParent, false);
            pageRect.anchorMin = Vector2.up;
            pageRect.anchorMax = Vector2.up;
            pageRect.pivot = new Vector2(0f, 1f);
            pageRect.anchoredPosition = new Vector2(pageWidth * pageIndex, 0f);
            pageRect.sizeDelta = new Vector2(Mathf.Max(1f, pageWidth), Mathf.Max(1f, pageHeight));

            var grid = pageRoot.GetComponent<GridLayoutGroup>();
            ConfigurePageGrid(grid, new Vector2(pageWidth, pageHeight));

            int pageStart = pageIndex * ItemsPerPage;
            int pageEnd = Mathf.Min(pageStart + ItemsPerPage, zukanManager.allYokaiList.Count);

            for (int i = pageStart; i < pageEnd; i++)
            {
                var data = zukanManager.allYokaiList[i];
                var item = Instantiate(zukanItemPrefab, pageRect);
                bool unlocked = IsUnlocked(data.id.ToString());
                item.Setup(data.id.ToString(), data.icon, data.displayName, unlocked, lockedNameText);
                itemOrder.Add(data);
                createdItems++;
            }
        }

        Debug.Log("[ZukanPanelController] BuildPagedList(): " +
                  "pageWidth=" + pageWidth +
                  ", pageHeight=" + pageHeight +
                  ", pageCount=" + pageCount +
                  ", createdItems=" + createdItems);
        LogRectState("BuildPagedList", pageCount, createdItems);

        Canvas.ForceUpdateCanvases();
        SnapToCurrentPage(true);
        UpdateArrowInteractable();
    }

    void ConfigureContentAsPageContainer()
    {
        if (!(contentParent is RectTransform parentRect))
            return;

        var grid = contentParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.enabled = false;
            Destroy(grid);
        }

        var horizontal = contentParent.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.enabled = false;
            Destroy(horizontal);
        }

        var vertical = contentParent.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.enabled = false;
            Destroy(vertical);
        }

        var fitter = contentParent.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
            Destroy(fitter);
        }

        parentRect.anchorMin = Vector2.up;
        parentRect.anchorMax = Vector2.up;
        parentRect.pivot = new Vector2(0f, 1f);
        parentRect.anchoredPosition = Vector2.zero;
    }

    void ConfigurePageGrid(GridLayoutGroup grid, Vector2 pageSize)
    {
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = ColumnsPerPage;
        grid.spacing = cellSpacing;
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        float totalSpacingX = cellSpacing.x * (ColumnsPerPage - 1);
        float totalSpacingY = cellSpacing.y * (RowsPerPage - 1);
        float cellWidth = Mathf.Max(200f, (pageSize.x - totalSpacingX) / ColumnsPerPage);
        float cellHeight = Mathf.Max(120f, (pageSize.y - totalSpacingY) / RowsPerPage);

        grid.cellSize = new Vector2(cellWidth, cellHeight);
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
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        return true;
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
