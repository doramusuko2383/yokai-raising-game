using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class SpiritUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [FormerlySerializedAs("energyManager")]
    [SerializeField] private SpiritController spiritController;
    [SerializeField] private Yokai.YokaiStateController stateController;

    [Header("Weak Visuals")]
    [SerializeField] private CanvasGroup yokaiCanvasGroup;
    [SerializeField] private Image yokaiImage;
    [SerializeField] private Transform yokaiTransform;
    [SerializeField] private float weakAlpha = 0.45f;
    [SerializeField] private float weakBrightness = 0.75f;

    [Header("UI")]
    [FormerlySerializedAs("energySlider")]
    [SerializeField] private Slider spiritSlider;
    [FormerlySerializedAs("energyText")]
    [SerializeField] private TMP_Text spiritText;

    float baseCanvasAlpha = 1f;
    Color baseImageColor = Color.white;
    bool hasCachedWeakVisuals;
    bool isWeakVisualsApplied;

    void Awake()
    {
        LogMissingDependencies();
        LogMissingWeakVisualTargets();
    }

    void OnEnable()
    {
        if (spiritController != null)
        {
            spiritController.SpiritChanged += OnSpiritChanged;
        }

        if (stateController == null)
            Debug.LogError("[SPIRIT UI] StateController not set in Inspector");

        if (stateController != null)
            stateController.OnStateChanged += OnStateChanged;

        CurrentYokaiContext.CurrentChanged += HandleCurrentYokaiChanged;
        CacheWeakVisualBase();
        SyncWeakVisualsWithState();
    }

    void OnDisable()
    {
        if (spiritController != null)
        {
            spiritController.SpiritChanged -= OnSpiritChanged;
        }

        if (stateController != null)
            stateController.OnStateChanged -= OnStateChanged;

        CurrentYokaiContext.CurrentChanged -= HandleCurrentYokaiChanged;
        ResetWeakVisuals();
    }

    void Start()
    {
        RefreshUI();
        if (stateController != null)
        {
            SyncWeakVisualsWithState();
        }
    }

    void OnSpiritChanged(float current, float max)
    {
        RefreshUI();
        SyncWeakVisualsWithState();
    }

    void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        SetWeakVisuals(IsWeakState(newState));
    }

    void RefreshUI()
    {
        if (spiritController == null)
            return;

        if (spiritSlider != null)
        {
            spiritSlider.maxValue = 1f;
            spiritSlider.value = spiritController.SpiritNormalized;
        }

        if (spiritText != null)
        {
            spiritText.text = $"{Mathf.RoundToInt(spiritController.SpiritNormalized * 100f)}%";
        }
    }

    void HandleCurrentYokaiChanged(GameObject activeYokai)
    {
        bool shouldApply = stateController != null && IsWeakState(stateController.currentState);
        ResetWeakVisuals();
        LogMissingWeakVisualTargets();
        CacheWeakVisualBase();
        SetWeakVisuals(shouldApply);
    }

    void LogMissingDependencies()
    {
        if (spiritController == null)
            Debug.LogError("[SPIRIT UI] SpiritController not set in Inspector");

        if (stateController == null)
            Debug.LogError("[SPIRIT UI] StateController not set in Inspector");
    }

    void LogMissingWeakVisualTargets()
    {
        if (yokaiTransform == null)
            Debug.LogError("[SPIRIT UI] Yokai transform not set in Inspector");

        if (yokaiCanvasGroup == null)
            Debug.LogError("[SPIRIT UI] Yokai canvas group not set in Inspector");
        if (yokaiImage == null)
            Debug.LogError("[SPIRIT UI] Yokai image not set in Inspector");
    }

    void CacheWeakVisualBase()
    {
        if (isWeakVisualsApplied)
            return;

        if (yokaiCanvasGroup != null)
            baseCanvasAlpha = yokaiCanvasGroup.alpha;

        if (yokaiImage != null)
            baseImageColor = yokaiImage.color;

        hasCachedWeakVisuals = true;
    }

    void SetWeakVisuals(bool enable)
    {
        if (enable)
            ApplyWeakVisuals();
        else
            ResetWeakVisuals();
    }

    void SyncWeakVisualsWithState()
    {
        if (stateController == null)
        {
            ResetWeakVisuals();
            return;
        }

        SetWeakVisuals(IsWeakState(stateController.currentState));
    }

    bool IsWeakState(YokaiState state)
    {
        return state == YokaiState.EnergyEmpty;
    }

    void ApplyWeakVisuals()
    {
        if (yokaiCanvasGroup == null && yokaiImage == null)
        {
            LogMissingWeakVisualTargets();
            return;
        }
        if (!hasCachedWeakVisuals)
            CacheWeakVisualBase();

        if (yokaiCanvasGroup != null)
            yokaiCanvasGroup.alpha = Mathf.Clamp01(weakAlpha);

        if (yokaiImage != null)
        {
            float brightness = Mathf.Max(0f, weakBrightness);
            yokaiImage.color = new Color(
                baseImageColor.r * brightness,
                baseImageColor.g * brightness,
                baseImageColor.b * brightness,
                baseImageColor.a);
        }

        isWeakVisualsApplied = true;
    }

    void ResetWeakVisuals()
    {
        if (!hasCachedWeakVisuals)
            return;

        if (yokaiCanvasGroup != null)
            yokaiCanvasGroup.alpha = baseCanvasAlpha;

        if (yokaiImage != null)
            yokaiImage.color = baseImageColor;

        isWeakVisualsApplied = false;
    }
}
