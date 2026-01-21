using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yokai
{
public class YokaiStatePresentationController : MonoBehaviour
{
    enum EmptyStateType
    {
        None,
        Energy,
        Purity
    }

    [Header("Dependencies")]
    [SerializeField]
    YokaiStateController stateController;

    [Header("UI")]
    [SerializeField]
    GameObject actionPanel;

    [SerializeField]
    GameObject purityRecoverAdButton;

    [SerializeField]
    GameObject purifyStopButton;

    [SerializeField]
    GameObject magicCircleOverlay;

    [SerializeField]
    CanvasGroup dangerOverlay;

    [Header("Danger Effect")]
    [SerializeField]
    YokaiDangerEffect[] dangerEffects;

    [Header("Purity Empty Visuals")]
    [SerializeField]
    float purityEmptyOverlayAlpha = 0.2f;

    [SerializeField]
    float purityEmptyDarkenIntensity = 0.2f;

    [SerializeField]
    float purityEmptyReleaseDelay = 0.15f;

    [SerializeField]
    float purityEmptyWobbleScale = 0.02f;

    [SerializeField]
    float purityEmptyWobbleSpeed = 2.6f;

    [SerializeField]
    float purityEmptyJitterAmplitude = 0.015f;

    [Header("Recovery Ad Buttons")]
    [SerializeField]
    GameObject energyRecoverAdButton;

    GameObject purityEmptyTargetRoot;
    readonly Dictionary<SpriteRenderer, Color> purityEmptySpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> purityEmptyImageColors = new Dictionary<Image, Color>();
    Vector3 purityEmptyBasePosition;
    Vector3 purityEmptyBaseScale;
    float purityEmptyNoiseSeed;
    bool isPurityEmptyVisualsActive;
    bool isPurityEmptyMotionApplied;
    Coroutine purityEmptyReleaseRoutine;
    bool lastPurifying;

    public bool IsPurityEmptyVisualsActive => isPurityEmptyVisualsActive;

    void OnEnable()
    {
        LogMissingDependencies();
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
        UpdatePurityEmptyMotion();
    }

    void LogMissingDependencies()
    {
        if (stateController == null)
            Debug.LogError("[PRESENTATION] StateController not set in Inspector");

        if (actionPanel == null)
            Debug.LogError("[PRESENTATION] Action panel not set in Inspector");

        if (energyRecoverAdButton == null)
            Debug.LogError("[PRESENTATION] Energy recover ad button not set in Inspector");

        if (purityRecoverAdButton == null)
            Debug.LogError("[PRESENTATION] Purity recover ad button not set in Inspector");

        if (purifyStopButton == null)
            Debug.LogError("[PRESENTATION] Purify stop button not set in Inspector");

        if (magicCircleOverlay == null)
            Debug.LogError("[PRESENTATION] Magic circle overlay not set in Inspector");

        if (dangerOverlay == null)
            Debug.LogError("[PRESENTATION] Danger overlay not set in Inspector");

        if (dangerEffects == null || dangerEffects.Length == 0)
            Debug.LogError("[PRESENTATION] Danger effects not set in Inspector");
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
        CachePurityEmptyTargets(activeYokai);
        RefreshDangerEffectOriginalColors();
        RefreshPresentation();
    }

    void HandleStateChanged(YokaiState previousState, YokaiState newState)
    {
        HandleEmptyStatePresentation(previousState, newState, () => HandleStateMessages(previousState, newState));
        if (stateController != null)
            lastPurifying = stateController.isPurifying;
    }

    bool IsEmptyState(YokaiState state)
    {
        return GetEmptyStateType(state) != EmptyStateType.None;
    }

    EmptyStateType GetEmptyStateType(YokaiState state)
    {
        switch (state)
        {
            case YokaiState.EnergyEmpty:
                return EmptyStateType.Energy;
            case YokaiState.PurityEmpty:
                return EmptyStateType.Purity;
            default:
                return EmptyStateType.None;
        }
    }

    void HandleEmptyStatePresentation(YokaiState previousState, YokaiState newState, System.Action afterEffects)
    {
        EmptyStateType previousEmptyState = GetEmptyStateType(previousState);
        EmptyStateType newEmptyState = GetEmptyStateType(newState);
        bool wasEmptyState = previousEmptyState != EmptyStateType.None;
        bool isEmptyState = newEmptyState != EmptyStateType.None;

        if (newEmptyState == EmptyStateType.Energy && (!wasEmptyState || previousEmptyState == EmptyStateType.Purity))
            PlayEnergyEmptyEnterEffects();
        else if (previousEmptyState == EmptyStateType.Energy && (!isEmptyState || newEmptyState == EmptyStateType.Purity))
            PlayEnergyEmptyExitEffects();

        if (newEmptyState == EmptyStateType.Purity && (!wasEmptyState || previousEmptyState == EmptyStateType.Energy))
            EnterPurityEmpty();
        else if (previousEmptyState == EmptyStateType.Purity && (!isEmptyState || newEmptyState == EmptyStateType.Energy))
            RequestReleasePurityEmpty();

        afterEffects?.Invoke();
        RefreshPresentation();
    }

    void SyncCurrentYokai()
    {
        if (CurrentYokaiContext.Current != null)
        {
            CachePurityEmptyTargets(CurrentYokaiContext.Current);
        }
    }

    void SyncVisualState()
    {
        if (stateController == null)
            return;

        if (stateController.currentState == YokaiState.PurityEmpty && !isPurityEmptyVisualsActive)
            EnterPurityEmpty();
        else if (stateController.currentState != YokaiState.PurityEmpty && isPurityEmptyVisualsActive)
            RequestReleasePurityEmpty();
    }

    void RefreshPresentation()
    {
        if (!AreDependenciesResolved())
            return;

        YokaiState visualState = ResolveVisualState();
        bool isPurityEmptyState = visualState == YokaiState.PurityEmpty;
        bool showPurityEmptyVisuals = isPurityEmptyVisualsActive && isPurityEmptyState;
        bool isEnergyEmptyState = visualState == YokaiState.EnergyEmpty;
        bool showActionPanel =
            (stateController.currentState == YokaiState.Normal
            || stateController.currentState == YokaiState.EvolutionReady
            || isPurityEmptyState
            || isEnergyEmptyState)
            && !stateController.isPurifying
            && visualState != YokaiState.Evolving;
        bool showMagicCircle = stateController.isPurifying;
        bool showStopPurify = stateController.isPurifying;
        bool showDangerOverlay = showPurityEmptyVisuals;

        ApplyCanvasGroup(actionPanel, showActionPanel, showActionPanel);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);
        if (magicCircleOverlay != null)
            magicCircleOverlay.SetActive(showMagicCircle);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? Mathf.Clamp01(purityEmptyOverlayAlpha) : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateRecoveryButtons(visualState);
        UpdateActionPanelButtons(isPurityEmptyState, isEnergyEmptyState);
        UpdateDangerEffects();
        UpdatePurityEmptyVisuals(showPurityEmptyVisuals);
    }

    YokaiState ResolveVisualState()
    {
        if (stateController != null && stateController.IsEvolving)
            return YokaiState.Evolving;

        if (stateController != null && stateController.IsSpiritEmpty)
            return YokaiState.EnergyEmpty;

    if (stateController != null && stateController.IsPurityEmpty())
                return YokaiState.PurityEmpty;

        return YokaiState.Normal;
    }

    bool AreDependenciesResolved()
    {
        if (stateController == null)
            return false;

        if (actionPanel == null || purifyStopButton == null || magicCircleOverlay == null)
            return false;

        return true;
    }

    void UpdateActionPanelButtons(bool isPurityEmpty, bool isEnergyEmpty)
    {
        if (actionPanel == null)
            return;

        var buttons = actionPanel.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            bool isEnergyRecoverAd =
                energyRecoverAdButton != null &&
                button.gameObject == energyRecoverAdButton;

            bool isPurityRecoverAd =
                purityRecoverAdButton != null &&
                button.gameObject == purityRecoverAdButton;

            bool shouldShow;

            bool isPurifyButton =
                button.GetComponent<PurifyButtonHandler>() != null;

            if (isEnergyRecoverAd || isPurityRecoverAd)
                continue;

            if (isEnergyEmpty)
            {
                shouldShow = isPurifyButton;
            }
            else if (isPurityEmpty)
            {
                shouldShow = false;
            }
            else
            {
                shouldShow = true;
            }

            ApplyCanvasGroup(button.gameObject, shouldShow, shouldShow);
            button.interactable = shouldShow;
            button.enabled = shouldShow;
        }
    }

    void UpdateRecoveryButtons(YokaiState state)
    {
        if (energyRecoverAdButton != null)
            energyRecoverAdButton.SetActive(state == YokaiState.EnergyEmpty);

        if (purityRecoverAdButton != null)
            purityRecoverAdButton.SetActive(state == YokaiState.PurityEmpty);
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

        bool enableBlink = isPurityEmptyVisualsActive;
        int intensityLevel = isPurityEmptyVisualsActive ? 2 : 1;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            bool shouldBlink = enableBlink && effect.gameObject.activeInHierarchy;
            effect.SetBlinking(shouldBlink);
            effect.SetIntensityLevel(intensityLevel);
        }
    }

    void UpdatePurityEmptyVisuals(bool enable)
    {
        if (purityEmptyTargetRoot == null || CurrentYokaiContext.Current != purityEmptyTargetRoot)
        {
            CachePurityEmptyTargets(CurrentYokaiContext.Current);
        }

        if (enable)
        {
            foreach (var pair in purityEmptySpriteColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(purityEmptyDarkenIntensity));
            }

            foreach (var pair in purityEmptyImageColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(purityEmptyDarkenIntensity));
            }
        }
        else
        {
            ResetPurityEmptyVisuals();
        }
    }

    void ResetPurityEmptyVisuals()
    {
        foreach (var pair in purityEmptySpriteColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        foreach (var pair in purityEmptyImageColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }
    }

    void UpdatePurityEmptyMotion()
    {
        if (!isPurityEmptyVisualsActive || ResolveVisualState() != YokaiState.PurityEmpty)
        {
            if (isPurityEmptyMotionApplied)
            {
                ResetPurityEmptyMotion();
                isPurityEmptyMotionApplied = false;
            }
            return;
        }

        if (purityEmptyTargetRoot == null || CurrentYokaiContext.Current != purityEmptyTargetRoot)
        {
            CachePurityEmptyTargets(CurrentYokaiContext.Current);
        }

        if (purityEmptyTargetRoot == null)
            return;

        float time = Time.time * purityEmptyWobbleSpeed;
        float pulse = Mathf.Sin(time) * purityEmptyWobbleScale;
        float noise = (Mathf.PerlinNoise(purityEmptyNoiseSeed, time) - 0.5f) * 2f * purityEmptyWobbleScale;
        float scaleMultiplier = 1f + pulse + noise;

        float jitterX = (Mathf.PerlinNoise(purityEmptyNoiseSeed + 1.4f, time) - 0.5f) * 2f * purityEmptyJitterAmplitude;
        float jitterY = (Mathf.PerlinNoise(purityEmptyNoiseSeed + 2.1f, time + 3.7f) - 0.5f) * 2f * purityEmptyJitterAmplitude;

        purityEmptyTargetRoot.transform.localScale = purityEmptyBaseScale * scaleMultiplier;
        purityEmptyTargetRoot.transform.localPosition = purityEmptyBasePosition + new Vector3(jitterX, jitterY, 0f);
        isPurityEmptyMotionApplied = true;
    }

    void ResetPurityEmptyMotion()
    {
        if (purityEmptyTargetRoot == null)
            return;

        purityEmptyTargetRoot.transform.localScale = purityEmptyBaseScale;
        purityEmptyTargetRoot.transform.localPosition = purityEmptyBasePosition;
    }

    void EnterPurityEmpty()
    {
        if (purityEmptyReleaseRoutine != null)
        {
            StopCoroutine(purityEmptyReleaseRoutine);
            purityEmptyReleaseRoutine = null;
        }

        if (isPurityEmptyVisualsActive)
            return;

        if (purityEmptyTargetRoot == null || CurrentYokaiContext.Current != purityEmptyTargetRoot)
        {
            CachePurityEmptyTargets(CurrentYokaiContext.Current);
        }

        CapturePurityEmptyBaseTransform();
        isPurityEmptyVisualsActive = true;
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_ENTER);
        MentorMessageService.ShowHint(OnmyojiHintType.PurityEmpty);
    }

    void RequestReleasePurityEmpty()
    {
        if (purityEmptyReleaseRoutine != null)
        {
            StopCoroutine(purityEmptyReleaseRoutine);
            purityEmptyReleaseRoutine = null;
        }

        if (!isPurityEmptyVisualsActive)
            return;

        purityEmptyReleaseRoutine = StartCoroutine(ReleasePurityEmptyAfterDelay());
    }

    IEnumerator ReleasePurityEmptyAfterDelay()
    {
        float delay = Mathf.Clamp(purityEmptyReleaseDelay, 0.1f, 0.2f);
        yield return new WaitForSeconds(delay);
        isPurityEmptyVisualsActive = false;
        RefreshPresentation();
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_RELEASE);
        MentorMessageService.ShowHint(OnmyojiHintType.PurityRecovered);
        purityEmptyReleaseRoutine = null;
    }

    void CapturePurityEmptyBaseTransform()
    {
        if (purityEmptyTargetRoot == null)
            return;

        purityEmptyBasePosition = purityEmptyTargetRoot.transform.localPosition;
        purityEmptyBaseScale = purityEmptyTargetRoot.transform.localScale;
    }

    void CachePurityEmptyTargets(GameObject targetRoot)
    {
        purityEmptyTargetRoot = targetRoot;
        purityEmptySpriteColors.Clear();
        purityEmptyImageColors.Clear();
        purityEmptyBasePosition = Vector3.zero;
        purityEmptyBaseScale = Vector3.zero;
        purityEmptyNoiseSeed = Random.value * 10f;

        if (purityEmptyTargetRoot == null)
            return;

        CapturePurityEmptyBaseTransform();

        foreach (var sprite in purityEmptyTargetRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null)
                continue;

            purityEmptySpriteColors[sprite] = sprite.color;
        }

        foreach (var image in purityEmptyTargetRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
                continue;

            purityEmptyImageColors[image] = image.color;
        }
    }

    void RefreshDangerEffectOriginalColors()
    {
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
