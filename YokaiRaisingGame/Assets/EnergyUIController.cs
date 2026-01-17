using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private Yokai.YokaiStateController stateController;

    [Header("Weak Visuals")]
    [SerializeField] private CanvasGroup yokaiCanvasGroup;
    [SerializeField] private Image yokaiImage;
    [SerializeField] private Transform yokaiTransform;
    [SerializeField] private float weakAlpha = 0.45f;
    [SerializeField] private float weakBrightness = 0.75f;
    [SerializeField] private float weakScale = 0.85f;

    [Header("UI")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private GameObject adWatchButton;

    float baseCanvasAlpha = 1f;
    Color baseImageColor = Color.white;
    Vector3 baseScale = Vector3.one;
    bool hasCachedWeakVisuals;
    bool isWeakVisualsApplied;

    void OnEnable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
        }

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.OnStateChanged += OnStateChanged;

        CurrentYokaiContext.CurrentChanged += HandleCurrentYokaiChanged;
        ResolveWeakVisualTargets();
        CacheWeakVisualBase();
        SyncWeakVisualsWithState();
    }

    void OnDisable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
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
            UpdateAdWatchButton(stateController.currentState);
            SyncWeakVisualsWithState();
        }
    }

    void OnEnergyChanged(float current, float max)
    {
        RefreshUI();
        SyncWeakVisualsWithState();
    }

    void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        UpdateAdWatchButton(newState);
        SetWeakVisuals(IsWeakState(newState));
    }

    void RefreshUI()
    {
        if (energyManager == null)
            return;

        if (energySlider != null)
        {
            energySlider.maxValue = energyManager.maxEnergy;
            energySlider.value = energyManager.energy;
        }

        if (energyText != null)
        {
            energyText.text = $"{energyManager.energy:0}/{energyManager.maxEnergy}";
        }
    }

    void UpdateAdWatchButton(YokaiState state)
    {
        if (adWatchButton != null)
        {
            adWatchButton.SetActive(state == YokaiState.EnergyEmpty);
        }
    }

    void HandleCurrentYokaiChanged(GameObject activeYokai)
    {
        bool shouldApply = stateController != null && IsWeakState(stateController.currentState);
        ResetWeakVisuals();
        AssignWeakVisualTargets(activeYokai);
        CacheWeakVisualBase();
        SetWeakVisuals(shouldApply);
    }

    void AssignWeakVisualTargets(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        yokaiTransform = activeYokai.transform;
        if (yokaiCanvasGroup == null)
            yokaiCanvasGroup = activeYokai.GetComponentInChildren<CanvasGroup>(true);
        if (yokaiImage == null)
            yokaiImage = activeYokai.GetComponentInChildren<Image>(true);
    }

    void ResolveWeakVisualTargets()
    {
        if (yokaiTransform == null && CurrentYokaiContext.Current != null)
            yokaiTransform = CurrentYokaiContext.Current.transform;

        if (yokaiTransform != null)
        {
            if (yokaiCanvasGroup == null)
                yokaiCanvasGroup = yokaiTransform.GetComponentInChildren<CanvasGroup>(true);

            if (yokaiImage == null)
                yokaiImage = yokaiTransform.GetComponentInChildren<Image>(true);
        }
    }

    void CacheWeakVisualBase()
    {
        if (isWeakVisualsApplied)
            return;

        if (yokaiCanvasGroup != null)
            baseCanvasAlpha = yokaiCanvasGroup.alpha;

        if (yokaiImage != null)
            baseImageColor = yokaiImage.color;

        if (yokaiTransform != null)
            baseScale = yokaiTransform.localScale;

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
        return state == YokaiState.EnergyEmpty || state == YokaiState.PurityEmpty;
    }

    void ApplyWeakVisuals()
    {
        ResolveWeakVisualTargets();
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

        if (yokaiTransform != null)
        {
            float scaleMultiplier = Mathf.Max(0f, weakScale);
            yokaiTransform.localScale = baseScale * scaleMultiplier;
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

        if (yokaiTransform != null)
            yokaiTransform.localScale = baseScale;

        isWeakVisualsApplied = false;
    }
}
