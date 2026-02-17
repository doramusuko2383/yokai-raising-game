using System.Collections;
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
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged += HandleSaveDataChanged;
    }

    void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveDataChanged -= HandleSaveDataChanged;
    }

    public void OpenZukanPanel()
    {
        if (zukanPanel != null)
            zukanPanel.SetActive(true);

        Initialize();
    }

    public void CloseZukanPanel()
    {
        if (zukanDetailPanel != null)
            zukanDetailPanel.SetActive(false);

        if (zukanPanel != null)
            zukanPanel.SetActive(false);
    }

    public void Initialize()
    {
        if (contentParent == null || zukanItemPrefab == null || zukanManager == null)
            return;

        ClearChildren(contentParent);

        foreach (var data in zukanManager.allYokaiList)
        {
            var item = Instantiate(zukanItemPrefab, contentParent);
            item.Setup(data, IsUnlocked(data.id), OnItemClick);
        }
    }

    public void CloseDetailPanel()
    {
        if (zukanDetailPanel == null)
            return;

        StartFade(false);
    }

    void OnItemClick(YokaiData data)
    {
        ShowDetail(data);
    }

    void ShowDetail(YokaiData data)
    {
        if (data == null)
            return;

        bool unlocked = IsUnlocked(data.id);

        if (fullImage != null)
        {
            fullImage.sprite = data.fullImage != null ? data.fullImage : data.icon;
            fullImage.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 1f);
        }

        if (nameText != null)
            nameText.text = unlocked ? data.displayName : "？？？";

        if (descriptionText != null)
            descriptionText.text = unlocked ? data.description : "？？？";

        StartFade(true);
    }

    bool IsUnlocked(int yokaiId)
    {
        var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
        return save != null && save.unlockedYokaiIds != null && save.unlockedYokaiIds.Contains(yokaiId);
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
