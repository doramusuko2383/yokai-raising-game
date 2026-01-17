using UnityEngine;
using UnityEngine.UI;

public class KegareUIController : MonoBehaviour
{
    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    Slider kegareSlider;

    [SerializeField]
    Yokai.YokaiStateController stateController;

    [SerializeField]
    Yokai.YokaiStatePresentationController presentationController;

    [SerializeField]
    float pulseScale = 1.08f;

    [SerializeField]
    float pulseSpeed = 3.2f;

    [SerializeField]
    float pulseAlpha = 0.85f;

    RectTransform fillRect;
    Vector3 fillBaseScale = Vector3.one;
    Image fillImage;
    Color fillBaseColor = Color.white;

    void OnEnable()
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (presentationController == null && stateController != null)
            presentationController = stateController.GetComponent<Yokai.YokaiStatePresentationController>();

        if (presentationController == null)
            presentationController = FindObjectOfType<Yokai.YokaiStatePresentationController>();

        if (kegareManager != null)
            kegareManager.KegareChanged += OnKegareChanged;

        CacheFillReferences();
        RefreshUI();
    }

    void OnDisable()
    {
        if (kegareManager != null)
            kegareManager.KegareChanged -= OnKegareChanged;

        ResetPulse();
    }

    void Update()
    {
        UpdatePulse();
    }

    void OnKegareChanged(float current, float max)
    {
        if (kegareSlider != null)
            kegareSlider.value = max > 0f ? Mathf.Clamp01((max - current) / max) : 0f;
    }

    void CacheFillReferences()
    {
        if (kegareSlider == null)
            return;

        fillRect = kegareSlider.fillRect;
        if (fillRect != null)
            fillBaseScale = fillRect.localScale;

        if (fillRect != null)
            fillImage = fillRect.GetComponent<Image>();

        if (fillImage != null)
            fillBaseColor = fillImage.color;
    }

    void UpdatePulse()
    {
        if (fillRect == null)
            return;

        bool shouldPulse = presentationController != null && presentationController.IsKegareMaxVisualsActive;
        if (!shouldPulse)
        {
            ResetPulse();
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(1f, pulseScale, pulse);
        fillRect.localScale = fillBaseScale * scale;

        if (fillImage != null)
        {
            Color color = fillBaseColor;
            color.a *= Mathf.Lerp(1f, pulseAlpha, pulse);
            fillImage.color = color;
        }
    }

    void ResetPulse()
    {
        if (fillRect != null)
            fillRect.localScale = fillBaseScale;

        if (fillImage != null)
            fillImage.color = fillBaseColor;
    }

    void RefreshUI()
    {
        if (kegareManager == null)
            return;

        OnKegareChanged(kegareManager.kegare, kegareManager.maxKegare);
    }
}
