using UnityEngine;
using UnityEngine.UI;

public class PurityUIController : MonoBehaviour
{
    [SerializeField]
    PurityManager purityManager;

    [SerializeField]
    Slider puritySlider;

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
        if (purityManager == null)
            purityManager = FindObjectOfType<PurityManager>();

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (presentationController == null && stateController != null)
            presentationController = stateController.GetComponent<Yokai.YokaiStatePresentationController>();

        if (presentationController == null)
            presentationController = FindObjectOfType<Yokai.YokaiStatePresentationController>();

        if (purityManager != null)
            purityManager.PurityChanged += OnPurityChanged;

        CacheFillReferences();
        RefreshUI();
    }

    void OnDisable()
    {
        if (purityManager != null)
            purityManager.PurityChanged -= OnPurityChanged;

        ResetPulse();
    }

    void Update()
    {
        UpdatePulse();
    }

    void OnPurityChanged(float current, float max)
    {
        if (puritySlider != null)
            puritySlider.value = max > 0f ? Mathf.Clamp01((max - current) / max) : 0f;
    }

    void CacheFillReferences()
    {
        if (puritySlider == null)
            return;

        fillRect = puritySlider.fillRect;
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

        bool shouldPulse = presentationController != null && presentationController.IsPurityEmptyVisualsActive;
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
        if (purityManager == null)
            return;

        OnPurityChanged(purityManager.purityValue, purityManager.maxPurity);
    }
}
