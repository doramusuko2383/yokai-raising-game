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

    void OnEnable()
    {
        BindStateController(ResolveStateController());
        BindCurrentYokai(CurrentYokaiContext.Current);
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;

        CacheWeakVisualBase();
        SyncWeakVisualsWithState();
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
        BindSpiritController(null);
        BindStateController(null);

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
        CacheWeakVisualBase();
        SetWeakVisuals(shouldApply);
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

    void BindCurrentYokai(GameObject activeYokai)
    {
        SpiritController controller = null;
        if (activeYokai != null)
            controller = activeYokai.GetComponentInChildren<SpiritController>(true);

        BindStateController(ResolveStateController());
        BindSpiritController(controller);
        ResolveWeakVisualTargets(activeYokai);
        HandleCurrentYokaiChanged(activeYokai);
    }

    Yokai.YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController() ?? stateController;
    }

    void BindStateController(Yokai.YokaiStateController controller)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= OnStateChanged;

        stateController = controller;

        if (stateController != null)
            stateController.OnStateChanged += OnStateChanged;
    }

    void BindSpiritController(SpiritController controller)
    {
        if (spiritController == controller)
            return;

        if (spiritController != null)
            spiritController.SpiritChanged -= OnSpiritChanged;

        spiritController = controller;

        if (spiritController != null)
            spiritController.SpiritChanged += OnSpiritChanged;

        RefreshUI();
        SyncWeakVisualsWithState();
    }

    void ResolveWeakVisualTargets(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        if (yokaiTransform == null)
            yokaiTransform = activeYokai.transform;

        if (yokaiCanvasGroup == null)
            yokaiCanvasGroup = activeYokai.GetComponentInChildren<CanvasGroup>(true);

        if (yokaiImage == null)
            yokaiImage = activeYokai.GetComponentInChildren<Image>(true);
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
