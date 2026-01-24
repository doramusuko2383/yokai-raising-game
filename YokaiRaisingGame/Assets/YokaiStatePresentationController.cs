using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

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
    MagicCircleActivator magicCircleActivator;

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
    [FormerlySerializedAs("energyRecoverAdButton")]
    GameObject recoverAdButton;

    [SerializeField]
    [FormerlySerializedAs("purityRecoverAdButton")]
    GameObject legacyPurityRecoverAdButton;

    GameObject purityEmptyTargetRoot;
    readonly Dictionary<SpriteRenderer, Color> purityEmptySpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> purityEmptyImageColors = new Dictionary<Image, Color>();
    Vector3 purityEmptyBasePosition;
    Vector3 purityEmptyBaseScale;
    float purityEmptyNoiseSeed;
    bool isPurityEmptyVisualsActive;
    bool isPurityEmptyMotionApplied;
    Coroutine purityEmptyReleaseRoutine;
    YokaiState? lastAppliedState;
    bool hasWarnedMissingDependencies;
    static YokaiStatePresentationController instance;

    public static YokaiStatePresentationController Instance => instance;

    public bool IsPurityEmptyVisualsActive => isPurityEmptyVisualsActive;

    public void ClearWeakVisuals()
    {
        RefreshPresentation();
    }

    public void ClearDangerVisuals()
    {
        if (isPurityEmptyVisualsActive)
            RequestReleasePurityEmpty();
        else
            ResetPurityEmptyVisuals();

        ResetPurityEmptyMotion();
        RefreshPresentation();
    }

    void Awake()
    {
        if (instance != null && instance != this)
            Debug.LogError("[PRESENTATION] Multiple YokaiStatePresentationController instances detected.");
        else
            instance = this;
    }

    void OnEnable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed += HandleCurrentYokaiConfirmed;
        BindStateController(ResolveStateController());
        lastAppliedState = null;
        if (stateController != null)
            ApplyState(stateController.currentState, force: true);
    }

    void OnDisable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        BindStateController(null);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void LateUpdate()
    {
        UpdatePurityEmptyMotion();
    }

    void BindStateController(YokaiStateController controller)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= OnStateChanged;

        stateController = controller;

        if (stateController != null)
            stateController.OnStateChanged += OnStateChanged;
    }

    void HandleCurrentYokaiConfirmed(GameObject activeYokai)
    {
        BindStateController(ResolveStateController());
        CachePurityEmptyTargets(activeYokai);
        RefreshDangerEffectOriginalColors();
        RefreshPresentation();
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController() ?? stateController;
    }

    public void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        if (stateController == null)
            BindStateController(ResolveStateController());

        Debug.Log($"[PRESENTATION] State changed: {previousState} -> {newState}");
        if (!AreDependenciesResolved())
            return;

        PlayStateTransitionSe(previousState, newState);
        lastAppliedState = previousState;
        ApplyState(newState, false);
    }

    void UpdateMagicCircleState(YokaiState state)
    {
        if (magicCircleActivator == null)
            return;

        if (state == YokaiState.Purifying)
        {
            magicCircleActivator.Show();
            return;
        }

        magicCircleActivator.Hide();
    }

    public void ApplyState(YokaiState state, bool force = false)
    {
        if (!force && lastAppliedState.HasValue && lastAppliedState.Value == state)
            return;

        YokaiState? previousState = lastAppliedState;
        lastAppliedState = state;

        if (!previousState.HasValue)
        {
            Debug.Log($"[PRESENTATION] ApplyState: {state}");
            PlayStateTransitionSe(state, state);
        }

        if (previousState.HasValue)
        {
            if (previousState.Value == state)
            {
                ApplyStateInternal(state, force);
                RefreshPresentation();
            }
            else
            {
                HandleEmptyStatePresentation(previousState.Value, state, () => HandleStateMessages(previousState.Value, state));
            }
        }
        else
        {
            ApplyStateInternal(state, force);
            RefreshPresentation();
        }
    }

    void HandleStateEntered(YokaiState state)
    {
        bool isEmpty =
            state == YokaiState.EnergyEmpty ||
            state == YokaiState.PurityEmpty;

        ApplyState(state, force: isEmpty);
    }

    void ApplyStateInternal(YokaiState state, bool force)
    {
        UpdateMagicCircleState(state);

        if (force && IsEmptyState(state))
        {
            ReplayEmptyStateEffects(state);
            return;
        }

        if (HandleEmptyState(state, force))
            return;

        HandleNormalState(state);
    }

    bool HandleEmptyState(YokaiState state, bool force)
    {
        switch (state)
        {
            case YokaiState.EnergyEmpty:
                PlayEnergyEmptyEnterEffects();
                return true;
            case YokaiState.PurityEmpty:
                EnterPurityEmpty(force);
                return true;
        }

        return false;
    }

    void ReplayEmptyStateEffects(YokaiState state)
    {
        switch (state)
        {
            case YokaiState.EnergyEmpty:
                PlayEnergyEmptyEnterEffects();
                break;
            case YokaiState.PurityEmpty:
                EnterPurityEmpty(true);
                break;
        }
    }

    void HandleNormalState(YokaiState state)
    {
        switch (state)
        {
            case YokaiState.EvolutionReady:
                MentorMessageService.ShowHint(OnmyojiHintType.EvolutionStart);
                break;
            case YokaiState.Normal:
            case YokaiState.Purifying:
            case YokaiState.Evolving:
                break;
        }
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
        bool isPurifyingState = visualState == YokaiState.Purifying;
        bool showActionPanel =
            (stateController.currentState == YokaiState.Normal
            || stateController.currentState == YokaiState.EvolutionReady)
            && visualState != YokaiState.Evolving;
        bool showStopPurify = false;
        bool showDangerOverlay = showPurityEmptyVisuals;

        ApplyCanvasGroup(actionPanel, showActionPanel, showActionPanel);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? Mathf.Clamp01(purityEmptyOverlayAlpha) : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateSpecialRecoveryButtons(visualState);
        UpdateActionPanelButtons(isPurityEmptyState, isEnergyEmptyState, isPurifyingState);
        UpdateDangerEffects();
        UpdatePurityEmptyVisuals(showPurityEmptyVisuals);
    }

    YokaiState ResolveVisualState()
    {
        if (stateController != null && stateController.IsEvolving)
            return YokaiState.Evolving;

        if (stateController != null && stateController.currentState == YokaiState.Purifying)
            return YokaiState.Purifying;

        if (stateController != null && stateController.IsPurityEmptyState)
            return YokaiState.PurityEmpty;

        if (stateController != null && stateController.IsSpiritEmpty)
            return YokaiState.EnergyEmpty;

        return YokaiState.Normal;
    }

    bool AreDependenciesResolved()
    {
        bool missingStateController = stateController == null;
        bool missingUi =
            actionPanel == null ||
            purifyStopButton == null;

        if (missingStateController || missingUi)
        {
            if (!hasWarnedMissingDependencies)
            {
                List<string> missing = new List<string>();
                if (missingStateController)
                    missing.Add("StateController");
                if (actionPanel == null)
                    missing.Add("ActionPanel");
                if (purifyStopButton == null)
                    missing.Add("PurifyStopButton");

                Debug.LogWarning($"[PRESENTATION] Missing Inspector references: {string.Join(", ", missing)}");
                hasWarnedMissingDependencies = true;
            }
            return false;
        }

        return true;
    }

    void UpdateActionPanelButtons(bool isPurityEmpty, bool isEnergyEmpty, bool isPurifying)
    {
        if (actionPanel == null)
            return;

        var buttons = actionPanel.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            bool isEnergyRecoverAd =
                recoverAdButton != null &&
                button.gameObject == recoverAdButton;

            bool isPurityRecoverAd =
                legacyPurityRecoverAdButton != null &&
                button.gameObject == legacyPurityRecoverAdButton;

            bool shouldShow;

            bool isPurifyButton =
                button.GetComponent<PurifyButtonHandler>() != null;

            if (isEnergyRecoverAd || isPurityRecoverAd)
                continue;

            if (isPurifying)
            {
                shouldShow = false;
            }
            else if (isEnergyEmpty)
            {
                shouldShow = false;
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

    void UpdateSpecialRecoveryButtons(YokaiState state)
    {
        bool showSpecialDango = false;
        bool showEmergencyPurify = false;

        switch (state)
        {
            case YokaiState.EnergyEmpty:
                showSpecialDango = true;
                break;
            case YokaiState.PurityEmpty:
                showEmergencyPurify = true;
                break;
        }

        if (recoverAdButton != null)
            recoverAdButton.SetActive(showSpecialDango);

        if (purityRecoverAdButton != null)
            purityRecoverAdButton.SetActive(showEmergencyPurify);

        if (legacyPurityRecoverAdButton != null)
            legacyPurityRecoverAdButton.SetActive(false);
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

    void EnterPurityEmpty(bool force = false)
    {
        if (purityEmptyReleaseRoutine != null)
        {
            StopCoroutine(purityEmptyReleaseRoutine);
            purityEmptyReleaseRoutine = null;
        }

        if (isPurityEmptyVisualsActive && !force)
            return;

        if (purityEmptyTargetRoot == null || CurrentYokaiContext.Current != purityEmptyTargetRoot)
        {
            CachePurityEmptyTargets(CurrentYokaiContext.Current);
        }

        CapturePurityEmptyBaseTransform();
        isPurityEmptyVisualsActive = true;
        RefreshDangerEffectOriginalColors();
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
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyZero);
    }

    void PlayEnergyEmptyExitEffects()
    {
        MentorMessageService.NotifyRecovered();
    }

    void HandleStateMessages(YokaiState previousState, YokaiState newState)
    {
        if (newState == YokaiState.Purifying && previousState != YokaiState.Purifying)
        {
            MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeGuide);
        }

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

    void PlayStateTransitionSe(YokaiState previousState, YokaiState newState)
    {
        switch (newState)
        {
            case YokaiState.Purifying:
                AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
                break;
            case YokaiState.PurityEmpty:
                AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_ENTER);
                break;
            case YokaiState.EnergyEmpty:
                AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
                break;
        }

        if (previousState == YokaiState.PurityEmpty
            && newState != YokaiState.PurityEmpty
            && stateController != null
            && !stateController.IsPurityEmptyState)
        {
            AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_RELEASE);
        }

        if (previousState == YokaiState.EnergyEmpty
            && newState != YokaiState.EnergyEmpty
            && stateController != null
            && !stateController.IsSpiritEmpty)
        {
            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        }
    }
}
}
