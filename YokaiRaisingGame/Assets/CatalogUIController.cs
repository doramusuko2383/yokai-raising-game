using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogUIController : MonoBehaviour
{
    [Header("Catalog Canvas")]
    [SerializeField] private CanvasGroup catalogCanvasGroup;
    [SerializeField] private GameObject catalogPanelRoot;
    [SerializeField] private GameObject zukanListPanel;
    [SerializeField] private GameObject zukanDetailPanel;

    [Header("Entries")]
    [SerializeField] private GameObject fireBallEntry;

    [Header("Buttons")]
    [SerializeField] private Button openCatalogButton;
    [SerializeField] private Button closeCatalogButton;

    [Header("Registration Message")]
    [SerializeField] private TMP_Text registrationMessageText;
    [SerializeField] private float registrationMessageDuration = 2.5f;

    Coroutine registrationRoutine;

    void Awake()
    {
        if (catalogCanvasGroup == null)
        {
            catalogCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (catalogPanelRoot == null)
        {
            catalogPanelRoot = transform.Find("CatalogPanel")?.gameObject;
        }

        if (fireBallEntry == null && catalogPanelRoot != null)
        {
            fireBallEntry = catalogPanelRoot.transform.Find("Entries/Entry_FireBall")?.gameObject;
        }

        if (zukanListPanel == null)
        {
            zukanListPanel = catalogPanelRoot;
        }

        if (zukanDetailPanel == null)
        {
            zukanDetailPanel = transform.Find("CatalogPanel/DetailPanel")?.gameObject;
        }

        if (openCatalogButton == null)
        {
            openCatalogButton = GameObject.Find("Btn_OpenCatalog")?.GetComponent<Button>();
        }

        if (closeCatalogButton == null)
        {
            closeCatalogButton = transform.Find("CatalogPanel/Btn_CloseCatalog")?.GetComponent<Button>();
        }

        if (registrationMessageText == null)
        {
            registrationMessageText = GameObject.Find("Text_CatalogNotice")?.GetComponent<TMP_Text>();
        }

        if (openCatalogButton != null)
        {
            openCatalogButton.onClick.AddListener(OpenCatalog);
        }

        if (closeCatalogButton != null)
        {
            closeCatalogButton.onClick.AddListener(CloseCatalog);
        }

        SetCatalogVisible(false, immediate: true);

        if (fireBallEntry != null)
        {
            fireBallEntry.SetActive(false);
        }

        if (registrationMessageText != null)
        {
            registrationMessageText.text = string.Empty;
        }
    }

    public void RegisterYokai(string yokaiName)
    {
        bool isFireBall = yokaiName == "FireBall";
        if (isFireBall && fireBallEntry != null)
        {
            fireBallEntry.SetActive(true);
        }

        string displayName = isFireBall ? "ひのたま" : yokaiName;
        ShowRegistrationMessage($"{displayName}が図鑑に登録されたのう");
    }

    public void OpenCatalog()
    {
        SetCatalogVisible(true, immediate: false);

        if (zukanListPanel != null)
        {
            zukanListPanel.SetActive(true);
        }

        if (zukanDetailPanel != null)
        {
            zukanDetailPanel.SetActive(false);
        }
    }

    public void OpenZukan()
    {
        OpenCatalog();
    }

    public void CloseCatalog()
    {
        SetCatalogVisible(false, immediate: false);
    }

    public void CloseDetailPanel()
    {
        if (zukanDetailPanel != null)
        {
            zukanDetailPanel.SetActive(false);
        }
    }

    void SetCatalogVisible(bool visible, bool immediate)
    {
        if (catalogCanvasGroup == null)
        {
            if (catalogPanelRoot != null)
            {
                catalogPanelRoot.SetActive(visible);
            }

            return;
        }

        float targetAlpha = visible ? 1f : 0f;
        catalogCanvasGroup.alpha = targetAlpha;
        catalogCanvasGroup.interactable = visible;
        catalogCanvasGroup.blocksRaycasts = visible;

        if (catalogPanelRoot != null)
        {
            catalogPanelRoot.SetActive(true);
        }
    }

    void ShowRegistrationMessage(string message)
    {
        if (registrationMessageText == null)
        {
            return;
        }

        if (registrationRoutine != null)
        {
            StopCoroutine(registrationRoutine);
        }

        registrationRoutine = StartCoroutine(ShowRegistrationRoutine(message));
    }

    IEnumerator ShowRegistrationRoutine(string message)
    {
        registrationMessageText.text = MentorSpeechFormatter.Format(message);
        yield return new WaitForSeconds(registrationMessageDuration);
        registrationMessageText.text = string.Empty;
    }
}
