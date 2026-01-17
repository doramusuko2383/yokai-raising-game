using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yokai
{
public class YokaiStatePresentationController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    YokaiStateController stateController;

    [Header("UI")]
    [SerializeField]
    GameObject actionPanel;

    [SerializeField]
    GameObject emergencyPurifyButton;

    [SerializeField]
    GameObject purifyStopButton;

    [SerializeField]
    GameObject magicCircleOverlay;

    [SerializeField]
    CanvasGroup dangerOverlay;

    [Header("Danger Effect")]
    [SerializeField]
    YokaiDangerEffect[] dangerEffects;

    [Header("Kegare Max Visuals")]
    [SerializeField]
    float kegareMaxOverlayAlpha = 0.2f;

    [SerializeField]
    float kegareMaxDarkenIntensity = 0.2f;

    [SerializeField]
    float kegareMaxReleaseDelay = 0.15f;

    [SerializeField]
    float kegareMaxWobbleScale = 0.02f;

    [SerializeField]
    float kegareMaxWobbleSpeed = 2.6f;

    [SerializeField]
    float kegareMaxJitterAmplitude = 0.015f;

    [Header("Energy Empty Visuals")]
    [SerializeField] private Button specialDangoButton;

    GameObject kegareMaxTargetRoot;
    readonly Dictionary<SpriteRenderer, Color> kegareMaxSpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> kegareMaxImageColors = new Dictionary<Image, Color>();
    Vector3 kegareMaxBasePosition;
    Vector3 kegareMaxBaseScale;
    float kegareMaxNoiseSeed;
    bool isKegareMaxVisualsActive;
    bool isKegareMaxMotionApplied;
    Coroutine kegareMaxReleaseRoutine;
    bool lastPurifying;

    public bool IsKegareMaxVisualsActive => isKegareMaxVisualsActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsurePresentationController()
    {
        var controllers = Object.FindObjectsOfType<YokaiStateController>(true);
        foreach (var controller in controllers)
        {
            if (controller == null)
                continue;

            if (controller.GetComponent<YokaiStatePresentationController>() == null)
                controller.gameObject.AddComponent<YokaiStatePresentationController>();
        }
    }

    void OnEnable()
    {
        ResolveDependencies();
        RegisterStateEvents();
        CurrentYokaiContext.CurrentChanged += HandleCurrentYokaiChanged;
        SyncCurrentYokai();
        SyncVisualState();
        RefreshPresentation();
        lastPurifying = stateController != null && stateController.isPurifying;
    }

    void OnDisable()
    {
        UnregisterStateEvents();
        CurrentYokaiContext.CurrentChanged -= HandleCurrentYokaiChanged;
    }

    void Update()
    {
        if (stateController == null)
            return;

        bool isPurifying = stateController.isPurifying;
        if (isPurifying == lastPurifying)
            return;

        lastPurifying = isPurifying;
        RefreshPresentation();
    }

    void LateUpdate()
    {
        UpdateKegareMaxMotion();
    }

    void ResolveDependencies()
    {
        if (stateController == null)
            stateController = GetComponent<YokaiStateController>();

        if (actionPanel == null)
            actionPanel = GameObject.Find("UI_Action");

        if (emergencyPurifyButton == null)
            emergencyPurifyButton = GameObject.Find("Button_MononokeHeal");

        if (purifyStopButton == null)
            purifyStopButton = GameObject.Find("Btn_StopPurify");

        if (magicCircleOverlay == null)
            magicCircleOverlay = GameObject.Find("MagicCircleImage");

        if (dangerOverlay == null)
        {
            var dangerObject = GameObject.Find("Overlay_Danger");
            if (dangerObject != null)
                dangerOverlay = dangerObject.GetComponent<CanvasGroup>();
        }

        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);
    }

    void RegisterStateEvents()
    {
        if (stateController == null)
            return;

        stateController.OnStateChanged += HandleStateChanged;
    }

    void UnregisterStateEvents()
    {
        if (stateController == null)
            return;

        stateController.OnStateChanged -= HandleStateChanged;
    }

    void HandleCurrentYokaiChanged(GameObject activeYokai)
    {
        CacheKegareMaxTargets(activeYokai);
        RefreshDangerEffectOriginalColors();
        RefreshPresentation();
    }

    void HandleStateChanged(YokaiState previousState, YokaiState newState)
    {
        if (newState == YokaiState.EnergyEmpty && previousState != YokaiState.EnergyEmpty)
            PlayEnergyEmptyEnterEffects();
        else if (previousState == YokaiState.EnergyEmpty && newState != YokaiState.EnergyEmpty)
            PlayEnergyEmptyExitEffects();

        if (newState == YokaiState.KegareMax && previousState != YokaiState.KegareMax)
            EnterKegareMax();
        else if (previousState == YokaiState.KegareMax && newState != YokaiState.KegareMax)
            RequestReleaseKegareMax();

        HandleStateMessages(previousState, newState);
        RefreshPresentation();
        if (stateController != null)
            lastPurifying = stateController.isPurifying;
    }

    void SyncCurrentYokai()
    {
        if (CurrentYokaiContext.Current != null)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }
    }

    void SyncVisualState()
    {
        if (stateController == null)
            return;

        if (stateController.currentState == YokaiState.KegareMax && !isKegareMaxVisualsActive)
            EnterKegareMax();
        else if (stateController.currentState != YokaiState.KegareMax && isKegareMaxVisualsActive)
            RequestReleaseKegareMax();
    }

    void RefreshPresentation()
    {
        if (!AreDependenciesResolved())
            return;

        YokaiState visualState = ResolveVisualState();
        bool isKegareMaxState = visualState == YokaiState.KegareMax;
        bool showKegareMaxVisuals = isKegareMaxVisualsActive && isKegareMaxState;
        bool isEnergyEmptyState = visualState == YokaiState.EnergyEmpty;
        bool showActionPanel =
            (stateController.currentState == YokaiState.Normal
            || stateController.currentState == YokaiState.EvolutionReady
            || isKegareMaxState
            || isEnergyEmptyState)
            && !stateController.isPurifying
            && visualState != YokaiState.Evolving;
        bool showEmergency = isKegareMaxState;
        bool showMagicCircle = stateController.isPurifying;
        bool showStopPurify = stateController.isPurifying;
        bool showDangerOverlay = showKegareMaxVisuals;

        ApplyCanvasGroup(actionPanel, showActionPanel, showActionPanel);
        ApplyCanvasGroup(emergencyPurifyButton, showEmergency, showEmergency);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);
        if (magicCircleOverlay != null)
            magicCircleOverlay.SetActive(showMagicCircle);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? Mathf.Clamp01(kegareMaxOverlayAlpha) : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateActionPanelButtons(isKegareMaxState, isEnergyEmptyState);
        UpdateDangerEffects();
        UpdateKegareMaxVisuals(showKegareMaxVisuals);
    }

    YokaiState ResolveVisualState()
    {
        if (stateController != null && stateController.IsEvolving)
            return YokaiState.Evolving;

        if (stateController != null && stateController.IsEnergyEmpty)
            return YokaiState.EnergyEmpty;

        if (stateController != null && stateController.IsKegareMax)
            return YokaiState.KegareMax;

        return YokaiState.Normal;
    }

    bool AreDependenciesResolved()
    {
        if (stateController == null)
            return false;

        if (actionPanel == null || emergencyPurifyButton == null || purifyStopButton == null || magicCircleOverlay == null)
            return false;

        return true;
    }

    void UpdateActionPanelButtons(bool isKegareMax, bool isEnergyEmpty)
    {
        if (actionPanel == null)
            return;

        var buttons = actionPanel.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            bool isEmergency =
                emergencyPurifyButton != null &&
                button.gameObject == emergencyPurifyButton;

            bool isSpecialDango =
                specialDangoButton != null &&
                button == specialDangoButton;

            bool shouldShow;

            bool isPurifyButton =
                !isEmergency &&
                button.GetComponent<PurifyButtonHandler>() != null;

            if (isEnergyEmpty)
            {
                shouldShow = isSpecialDango || isPurifyButton;
            }
            else if (isKegareMax)
            {
                shouldShow = isEmergency;
            }
            else
            {
                shouldShow = !isEmergency && !isSpecialDango;
            }

            ApplyCanvasGroup(button.gameObject, shouldShow, shouldShow);
            button.interactable = shouldShow;
            button.enabled = shouldShow;
        }
    }

    void ApplyCanvasGroup(GameObject target, bool visible, bool interactable)
    {
        if (target == null)
            return;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;

        var selectable = target.GetComponent<Selectable>();
        if (selectable != null)
            selectable.interactable = interactable;
    }

    void UpdateDangerEffects()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        bool enableBlink = isKegareMaxVisualsActive;
        int intensityLevel = isKegareMaxVisualsActive ? 2 : 1;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            bool shouldBlink = enableBlink && effect.gameObject.activeInHierarchy;
            effect.SetBlinking(shouldBlink);
            effect.SetIntensityLevel(intensityLevel);
        }
    }

    void UpdateKegareMaxVisuals(bool enable)
    {
        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        if (enable)
        {
            foreach (var pair in kegareMaxSpriteColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(kegareMaxDarkenIntensity));
            }

            foreach (var pair in kegareMaxImageColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(kegareMaxDarkenIntensity));
            }
        }
        else
        {
            ResetKegareMaxVisuals();
        }
    }

    void ResetKegareMaxVisuals()
    {
        foreach (var pair in kegareMaxSpriteColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        foreach (var pair in kegareMaxImageColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }
    }

    void UpdateKegareMaxMotion()
    {
        if (!isKegareMaxVisualsActive || ResolveVisualState() != YokaiState.KegareMax)
        {
            if (isKegareMaxMotionApplied)
            {
                ResetKegareMaxMotion();
                isKegareMaxMotionApplied = false;
            }
            return;
        }

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        if (kegareMaxTargetRoot == null)
            return;

        float time = Time.time * kegareMaxWobbleSpeed;
        float pulse = Mathf.Sin(time) * kegareMaxWobbleScale;
        float noise = (Mathf.PerlinNoise(kegareMaxNoiseSeed, time) - 0.5f) * 2f * kegareMaxWobbleScale;
        float scaleMultiplier = 1f + pulse + noise;

        float jitterX = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 1.4f, time) - 0.5f) * 2f * kegareMaxJitterAmplitude;
        float jitterY = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 2.1f, time + 3.7f) - 0.5f) * 2f * kegareMaxJitterAmplitude;

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale * scaleMultiplier;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition + new Vector3(jitterX, jitterY, 0f);
        isKegareMaxMotionApplied = true;
    }

    void ResetKegareMaxMotion()
    {
        if (kegareMaxTargetRoot == null)
            return;

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition;
    }

    void EnterKegareMax()
    {
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (isKegareMaxVisualsActive)
            return;

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        CaptureKegareMaxBaseTransform();
        isKegareMaxVisualsActive = true;
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_ENTER);
        MentorMessageService.ShowHint(OnmyojiHintType.KegareMax);
    }

    void RequestReleaseKegareMax()
    {
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (!isKegareMaxVisualsActive)
            return;

        kegareMaxReleaseRoutine = StartCoroutine(ReleaseKegareMaxAfterDelay());
    }

    IEnumerator ReleaseKegareMaxAfterDelay()
    {
        float delay = Mathf.Clamp(kegareMaxReleaseDelay, 0.1f, 0.2f);
        yield return new WaitForSeconds(delay);
        isKegareMaxVisualsActive = false;
        RefreshPresentation();
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_RELEASE);
        MentorMessageService.ShowHint(OnmyojiHintType.KegareRecovered);
        kegareMaxReleaseRoutine = null;
    }

    void CaptureKegareMaxBaseTransform()
    {
        if (kegareMaxTargetRoot == null)
            return;

        kegareMaxBasePosition = kegareMaxTargetRoot.transform.localPosition;
        kegareMaxBaseScale = kegareMaxTargetRoot.transform.localScale;
    }

    void CacheKegareMaxTargets(GameObject targetRoot)
    {
        kegareMaxTargetRoot = targetRoot;
        kegareMaxSpriteColors.Clear();
        kegareMaxImageColors.Clear();
        kegareMaxBasePosition = Vector3.zero;
        kegareMaxBaseScale = Vector3.zero;
        kegareMaxNoiseSeed = Random.value * 10f;

        if (kegareMaxTargetRoot == null)
            return;

        CaptureKegareMaxBaseTransform();

        foreach (var sprite in kegareMaxTargetRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null)
                continue;

            kegareMaxSpriteColors[sprite] = sprite.color;
        }

        foreach (var image in kegareMaxTargetRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
                continue;

            kegareMaxImageColors[image] = image.color;
        }
    }

    void RefreshDangerEffectOriginalColors()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            effect.RefreshOriginalColor();
        }
    }

    void PlayEnergyEmptyEnterEffects()
    {
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyZero);
    }

    void PlayEnergyEmptyExitEffects()
    {
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        MentorMessageService.NotifyRecovered();
    }

    void HandleStateMessages(YokaiState previousState, YokaiState newState)
    {
        if (newState == YokaiState.EvolutionReady && previousState != YokaiState.EvolutionReady)
        {
            MentorMessageService.ShowHint(OnmyojiHintType.EvolutionStart);
        }

        if (previousState == YokaiState.Evolving && newState == YokaiState.Normal && stateController != null)
        {
            if (stateController.TryConsumeEvolutionResult(out YokaiEvolutionStage stage))
            {
                if (stage == YokaiEvolutionStage.Child)
                    MentorMessageService.ShowHint(OnmyojiHintType.EvolutionCompleteChild);
                else if (stage == YokaiEvolutionStage.Adult)
                    MentorMessageService.ShowHint(OnmyojiHintType.EvolutionCompleteAdult);
            }
        }
    }
}
}
