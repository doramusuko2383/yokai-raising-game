using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MentorMessageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    CanvasGroup canvasGroup;

    [SerializeField]
    RectTransform panelRect;

    [SerializeField]
    Image mentorFace;

    [SerializeField]
    TextMeshProUGUI messageText;

    [SerializeField]
    Button tapCatcher;

    [Header("Animation")]
    [SerializeField]
    float fadeDuration = 0.25f;

    [SerializeField]
    float slideDistance = 40f;

    Coroutine messageRoutine;
    Vector2 visiblePosition;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        visiblePosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;

        if (tapCatcher != null)
        {
            tapCatcher.onClick.RemoveListener(HandleTap);
            tapCatcher.onClick.AddListener(HandleTap);
        }

        HideMessage(immediate: true);
    }

    public void ShowMessage(string message, float duration = 4f, bool allowTapToClose = true)
    {
        if (messageText != null)
            messageText.text = message;

        if (tapCatcher != null)
            tapCatcher.gameObject.SetActive(allowTapToClose);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = allowTapToClose;
            canvasGroup.interactable = allowTapToClose;
        }

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowRoutine(duration));
    }

    public void HideMessage(bool immediate = false)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        if (immediate)
        {
            ApplyHiddenState();
            return;
        }

        messageRoutine = StartCoroutine(HideRoutine());
    }

    void HandleTap()
    {
        HideMessage();
    }

    IEnumerator ShowRoutine(float duration)
    {
        Vector2 hiddenPosition = GetHiddenPosition();
        float elapsed = 0f;

        ApplyHiddenState();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            ApplyVisualState(t, Vector2.Lerp(hiddenPosition, visiblePosition, t));
            yield return null;
        }

        ApplyVisualState(1f, visiblePosition);

        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);

        messageRoutine = StartCoroutine(HideRoutine());
    }

    IEnumerator HideRoutine()
    {
        Vector2 hiddenPosition = GetHiddenPosition();
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);
            ApplyVisualState(alpha, Vector2.Lerp(visiblePosition, hiddenPosition, t));
            yield return null;
        }

        ApplyHiddenState();
        messageRoutine = null;
    }

    void ApplyHiddenState()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (panelRect != null)
            panelRect.anchoredPosition = GetHiddenPosition();
    }

    void ApplyVisualState(float alpha, Vector2 position)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (panelRect != null)
            panelRect.anchoredPosition = position;
    }

    Vector2 GetHiddenPosition()
    {
        return visiblePosition + Vector2.down * slideDistance;
    }
}
