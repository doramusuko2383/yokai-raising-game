using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

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
    YokaiState? lastVisualEffectState;
    bool hasWarnedMissingDependencies;
    bool hasWarnedMissingOptionalDependencies;
    static YokaiStatePresentationController instance;
    Coroutine bindRetryRoutine;
    bool isMagicCircleBound;
    PurityController boundPurityController;
    SpiritController boundSpiritController;
    bool isPurityZeroVisualOverride;
    bool isSpiritZeroVisualOverride;
    bool hasEnsuredDangerOverlayLayout;
    bool hasLoggedResolvedReferences;
    bool hasUserInteraction;

    public static YokaiStatePresentationController Instance => instance;

    public bool IsPurityEmptyVisualsActive => isPurityEmptyVisualsActive;

    public void NotifyUserInteraction()
    {
        hasUserInteraction = true;
    }

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
        ResolveOptionalDependencies();
        BindVitalControllers();
        CachePurityEmptyTargets(CurrentYokaiContext.Current);
        RefreshDangerEffectOriginalColors();
        lastAppliedState = null;
        lastVisualEffectState = null;
        WarnMissingOptionalDependencies();
        BindMagicCircleActivator();
        SyncFromStateController(force: true);
        StartBindRetryIfNeeded();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogResolvedReferencesOnce();
#endif
    }

    void OnDisable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        StopBindRetry();
        UnbindMagicCircleActivator();
        UnbindVitalControllers();
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

    void StartBindRetryIfNeeded()
    {
        if (stateController != null)
            return;

        if (bindRetryRoutine != null)
            return;

        bindRetryRoutine = StartCoroutine(CoBindRetry());
    }

    void StopBindRetry()
    {
        if (bindRetryRoutine == null)
            return;

        StopCoroutine(bindRetryRoutine);
        bindRetryRoutine = null;
    }

    void HandleCurrentYokaiConfirmed(GameObject activeYokai)
    {
        BindStateController(ResolveStateController());
        ResolveOptionalDependencies();
        BindVitalControllers();
        CachePurityEmptyTargets(activeYokai);
        RefreshDangerEffectOriginalColors();
        SyncFromStateController(force: true);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);
    }

    YokaiStateController TryResolveStateController()
    {
        if (stateController != null)
            return stateController;

        BindStateController(ResolveStateController());
        if (stateController != null)
            BindVitalControllers();
        if (stateController == null)
            WarnMissingDependencies();

        return stateController;
    }

    IEnumerator CoBindRetry()
    {
        const int maxAttempts = 20;
        const float intervalSeconds = 0.2f;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (stateController != null)
                break;

            BindStateController(ResolveStateController());
            if (stateController != null)
            {
                BindVitalControllers();
                CachePurityEmptyTargets(CurrentYokaiContext.Current);
                RefreshDangerEffectOriginalColors();
                SyncFromStateController(force: true);
                break;
            }

            yield return new WaitForSeconds(intervalSeconds);
        }

        if (stateController == null)
            WarnMissingDependencies();

        bindRetryRoutine = null;
    }

    void BindMagicCircleActivator()
    {
        if (magicCircleActivator == null)
            magicCircleActivator = FindObjectOfType<MagicCircleActivator>(true);

        if (isMagicCircleBound || magicCircleActivator == null)
            return;

        magicCircleActivator.SuccessRequested += HandleMagicCircleSuccess;
        isMagicCircleBound = true;
    }

    void UnbindMagicCircleActivator()
    {
        if (!isMagicCircleBound || magicCircleActivator == null)
            return;

        magicCircleActivator.SuccessRequested -= HandleMagicCircleSuccess;
        isMagicCircleBound = false;
    }

    void HandleMagicCircleSuccess()
    {
        if (stateController == null)
        {
            BindStateController(ResolveStateController());
            if (stateController == null)
                return;
        }

        stateController.NotifyPurifySucceeded();
    }

    void BindVitalControllers()
    {
        PurityController nextPurity = stateController != null ? stateController.PurityController : null;
        SpiritController nextSpirit = stateController != null ? stateController.SpiritController : null;

        if (nextPurity == null)
            nextPurity = FindObjectOfType<PurityController>(true);

        if (nextSpirit == null)
            nextSpirit = FindObjectOfType<SpiritController>(true);

        BindPurityController(nextPurity);
        BindSpiritController(nextSpirit);
    }

    void BindPurityController(PurityController controller)
    {
        if (boundPurityController == controller)
            return;

        if (boundPurityController != null)
            boundPurityController.PurityChanged -= HandlePurityChanged;

        boundPurityController = controller;

        if (boundPurityController != null)
            boundPurityController.PurityChanged += HandlePurityChanged;

        SyncValueDrivenPurityState();
    }

    void BindSpiritController(SpiritController controller)
    {
        if (boundSpiritController == controller)
            return;

        if (boundSpiritController != null)
            boundSpiritController.SpiritChanged -= HandleSpiritChanged;

        boundSpiritController = controller;

        if (boundSpiritController != null)
            boundSpiritController.SpiritChanged += HandleSpiritChanged;

        SyncValueDrivenSpiritState();
    }

    void UnbindVitalControllers()
    {
        if (boundPurityController != null)
            boundPurityController.PurityChanged -= HandlePurityChanged;

        if (boundSpiritController != null)
            boundSpiritController.SpiritChanged -= HandleSpiritChanged;

        boundPurityController = null;
        boundSpiritController = null;
        isPurityZeroVisualOverride = false;
        isSpiritZeroVisualOverride = false;
    }

    void HandlePurityChanged(float current, float max)
    {
        bool isZero = current <= 0f;
        if (isPurityZeroVisualOverride == isZero)
            return;

        isPurityZeroVisualOverride = isZero;
        ApplyValueDrivenVisualState();
    }

    void HandleSpiritChanged(float current, float max)
    {
        bool isZero = current <= 0f;
        if (isSpiritZeroVisualOverride == isZero)
            return;

        isSpiritZeroVisualOverride = isZero;
        ApplyValueDrivenVisualState();
    }

    void SyncValueDrivenPurityState()
    {
        bool isZero = boundPurityController != null && boundPurityController.PurityNormalized <= 0f;
        if (isPurityZeroVisualOverride == isZero)
            return;

        isPurityZeroVisualOverride = isZero;
        ApplyValueDrivenVisualState();
    }

    void SyncValueDrivenSpiritState()
    {
        bool isZero = boundSpiritController != null && boundSpiritController.SpiritNormalized <= 0f;
        if (isSpiritZeroVisualOverride == isZero)
            return;

        isSpiritZeroVisualOverride = isZero;
        ApplyValueDrivenVisualState();
    }

    void ApplyValueDrivenVisualState()
    {
        YokaiState visualState = ResolveVisualState();
        ApplyVisualEffectsOnce(visualState);
        RefreshPresentation();
    }

    public void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        if (TryResolveStateController() == null)
            return;

        ApplyState(newState, force: false);
    }

    public void SyncFromStateController(bool force = false)
    {
        if (TryResolveStateController() == null)
            return;

        ApplyState(stateController.currentState, force);
    }

    public void ApplyState(YokaiState state, bool force = false)
    {
        if (!AreDependenciesResolved())
            return;

        if (ShouldSuppressPresentationEffects(state))
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!lastAppliedState.HasValue || lastAppliedState.Value != state)
            Debug.Log($"[PRESENTATION] ApplyState: {state} (force: {force})");
#endif

        if (!force && lastAppliedState.HasValue && lastAppliedState.Value == state)
            return;

        lastAppliedState = state;
        ApplyStateInternal(state, force);
    }

    void HandleStateEntered(YokaiState state)
    {
        bool isEmpty =
            state == YokaiState.EnergyEmpty ||
            state == YokaiState.PurityEmpty;

        SyncFromStateController(force: isEmpty);
    }

    void ApplyVisualEffectsOnce(YokaiState state)
    {
        if (ShouldSuppressPresentationEffects(state))
            return;

        if (lastVisualEffectState.HasValue && lastVisualEffectState.Value == state)
            return;

        YokaiState? previousState = lastVisualEffectState;
        lastVisualEffectState = state;

        if (previousState == YokaiState.EnergyEmpty && state != YokaiState.EnergyEmpty)
            PlayEnergyEmptyExitEffects();

        ApplyPurityEmptyVisualsForState(state);
        ApplyDangerOverlayForState(state);
        ApplyDangerEffectsForState(state);
        ApplyMagicCircleForState(state);
        PlayStateEnterSE(state);

        if (!previousState.HasValue || previousState.Value != state)
            HandleStateMessages(previousState, state);
    }

    void ApplyStateInternal(YokaiState state, bool force)
    {
        RefreshPresentation();
        ApplyVisualEffectsOnce(state);
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
        if (stateController == null)
        {
            WarnMissingDependencies();
            return;
        }

        WarnMissingOptionalDependencies();

        YokaiState visualState = ResolveVisualState();
        YokaiState currentState = stateController != null ? stateController.currentState : YokaiState.Normal;
        bool isPurityEmptyState = visualState == YokaiState.PurityEmpty;
        bool showPurityEmptyVisuals = isPurityEmptyVisualsActive && isPurityEmptyState;
        bool isEnergyEmptyState = visualState == YokaiState.EnergyEmpty;
        bool isPurifyingState = visualState == YokaiState.Purifying;
        bool showActionPanel =
            stateController != null
            && (currentState == YokaiState.Normal
            || currentState == YokaiState.EvolutionReady)
            && visualState != YokaiState.Evolving;
        bool showStopPurify = isPurifyingState;

        ApplyCanvasGroup(actionPanel, showActionPanel, showActionPanel);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);

        UpdateSpecialRecoveryButtons(visualState);
        UpdateActionPanelButtons(isPurityEmptyState, isEnergyEmptyState, isPurifyingState);
        UpdatePurityEmptyVisuals(showPurityEmptyVisuals);
    }

    YokaiState ResolveVisualState()
    {
        if (stateController != null && stateController.IsEvolving)
            return YokaiState.Evolving;

        if (stateController != null && stateController.currentState == YokaiState.Purifying)
            return YokaiState.Purifying;

        if ((stateController != null && stateController.IsPurityEmptyState) || isPurityZeroVisualOverride)
            return YokaiState.PurityEmpty;

        if ((stateController != null && stateController.IsSpiritEmpty) || isSpiritZeroVisualOverride)
            return YokaiState.EnergyEmpty;

        return YokaiState.Normal;
    }

    void WarnMissingDependencies()
    {
        bool missingStateController = stateController == null;

        if (missingStateController)
        {
            if (!hasWarnedMissingDependencies)
            {
                List<string> missing = new List<string>();
                if (missingStateController)
                    missing.Add("StateController");

                Debug.LogWarning($"[PRESENTATION] Missing Inspector references: {string.Join(", ", missing)}");
                hasWarnedMissingDependencies = true;
            }
        }
    }

    bool AreDependenciesResolved()
    {
        return TryResolveStateController() != null;
    }

    void WarnMissingOptionalDependencies()
    {
        if (hasWarnedMissingOptionalDependencies)
            return;

        List<string> missing = new List<string>();

        if (actionPanel == null)
            missing.Add("ActionPanel");
        if (purityRecoverAdButton == null)
            missing.Add("PurityRecoverAdButton");
        if (purifyStopButton == null)
            missing.Add("PurifyStopButton");
        if (dangerOverlay == null)
            missing.Add("DangerOverlay");
        if (magicCircleActivator == null)
            missing.Add("MagicCircleActivator");
        if (recoverAdButton == null)
            missing.Add("RecoverAdButton");
        if (legacyPurityRecoverAdButton == null)
            missing.Add("LegacyPurityRecoverAdButton");
        if (dangerEffects == null || dangerEffects.Length == 0)
            missing.Add("DangerEffects");

        if (missing.Count == 0)
            return;

        Debug.LogWarning($"[PRESENTATION] Optional Inspector references are not set: {string.Join(", ", missing)}");
        hasWarnedMissingOptionalDependencies = true;
    }

    void ApplyDangerOverlayForState(YokaiState visualState)
    {
        if (dangerOverlay == null)
            return;

        EnsureDangerOverlayLayout();
        bool showDangerOverlay = visualState == YokaiState.PurityEmpty;
        dangerOverlay.alpha = showDangerOverlay ? Mathf.Clamp01(purityEmptyOverlayAlpha) : 0f;
        dangerOverlay.blocksRaycasts = false;
        dangerOverlay.interactable = false;
    }

    void ApplyDangerEffectsForState(YokaiState visualState)
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        bool shouldPlay = visualState == YokaiState.PurityEmpty;
        int intensityLevel = shouldPlay ? 2 : 1;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            if (shouldPlay)
                effect.Play();
            else
                effect.Stop();

            effect.SetIntensityLevel(intensityLevel);
        }
    }

    void ApplyMagicCircleForState(YokaiState visualState)
    {
        if (magicCircleActivator == null)
            return;

        if (visualState == YokaiState.Purifying)
            magicCircleActivator.Show();
        else
            magicCircleActivator.Hide();
    }

    void ApplyPurityEmptyVisualsForState(YokaiState state)
    {
        if (state == YokaiState.PurityEmpty)
        {
            EnterPurityEmpty();
            return;
        }

        if (isPurityEmptyVisualsActive)
            RequestReleasePurityEmpty();
        else
            ResetPurityEmptyVisuals();

        ResetPurityEmptyMotion();
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

    void PlayEnergyEmptyEffects()
    {
        PlayEnergyEmptyEnterEffects();
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_EMPTY);
    }

    void PlayEnergyEmptyExitEffects()
    {
        MentorMessageService.NotifyRecovered();
    }

    void PlayStateEnterSE(YokaiState state)
    {
        if (ShouldSuppressPresentationEffects(state))
            return;

        if (state == YokaiState.EnergyEmpty)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PRESENTATION] EmptyEffect Fired: {state}");
#endif
            PlayEnergyEmptyEffects();
            return;
        }

        if (state == YokaiState.PurityEmpty)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PRESENTATION] EmptyEffect Fired: {state}");
#endif
            AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_ENTER);
        }
    }

    bool ShouldSuppressPresentationEffects(YokaiState state)
    {
        if (!hasUserInteraction)
            return true;

        if (state == YokaiState.Purifying && !IsPurifyEffectAllowed())
            return true;

        return false;
    }

    bool IsPurifyEffectAllowed()
    {
        if (stateController == null)
            return false;

        return stateController.LastStateChangeReason == "BeginPurify";
    }

    void HandleStateMessages(YokaiState? previousState, YokaiState newState)
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

    void EnsureDangerOverlayLayout()
    {
        if (hasEnsuredDangerOverlayLayout || dangerOverlay == null)
            return;

        RectTransform rectTransform = dangerOverlay.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        hasEnsuredDangerOverlayLayout = true;
    }

    void ResolveOptionalDependencies()
    {
        if (magicCircleActivator == null)
            magicCircleActivator = FindObjectOfType<MagicCircleActivator>(true);

        if (dangerOverlay == null)
        {
            GameObject overlayObject = GameObject.Find("DangerOverlay");
            if (overlayObject == null)
            {
                var overlayRoot = GameObject.Find("UI_Overlay");
                if (overlayRoot != null)
                {
                    var child = overlayRoot.transform.Find("DangerOverlay");
                    overlayObject = child != null ? child.gameObject : null;
                }
            }

            if (overlayObject != null)
                dangerOverlay = overlayObject.GetComponent<CanvasGroup>();
        }

        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogResolvedReferencesOnce()
    {
        if (hasLoggedResolvedReferences)
            return;

        string dangerEffectCount = dangerEffects == null ? "0" : dangerEffects.Length.ToString();
        Debug.Log(
            "[PRESENTATION] Resolved references: " +
            $"stateController={(stateController != null)} " +
            $"magicCircleActivator={(magicCircleActivator != null)} " +
            $"dangerOverlay={(dangerOverlay != null)} " +
            $"dangerEffects={dangerEffectCount}");
        hasLoggedResolvedReferences = true;
    }
#endif
}
}
