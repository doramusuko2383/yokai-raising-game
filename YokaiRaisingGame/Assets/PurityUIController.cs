using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class PurityUIController : MonoBehaviour
{
    [FormerlySerializedAs("kegareManager")]
    [SerializeField]
    PurityController purityController;

    [SerializeField]
    [FormerlySerializedAs("kegareSlider")]
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
        BindCurrentYokai(CurrentYokaiContext.Current);
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;

        CacheFillReferences();
        RefreshUI();
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
        BindPurityController(null);

        ResetPulse();
    }

    void Update()
    {
        UpdatePulse();
    }

    void OnPurityChanged(float current, float max)
    {
        if (puritySlider != null)
        {
            puritySlider.maxValue = 1f;
            puritySlider.value = purityController != null ? purityController.PurityNormalized : 0f;
        }
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
        if (purityController == null)
            return;

        OnPurityChanged(purityController.purity, purityController.maxPurity);
    }

    void BindCurrentYokai(GameObject activeYokai)
    {
        PurityController controller = null;
        if (activeYokai != null)
            controller = activeYokai.GetComponentInChildren<PurityController>(true);

        BindPurityController(controller);
    }

    void BindPurityController(PurityController controller)
    {
        if (purityController == controller)
            return;

        if (purityController != null)
            purityController.PurityChanged -= OnPurityChanged;

        purityController = controller;

        if (purityController != null)
            purityController.PurityChanged += OnPurityChanged;

        RefreshUI();
    }
}
