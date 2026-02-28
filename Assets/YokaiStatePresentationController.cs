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

    [SerializeField]
    PurifyChargeController purifyChargeController;

    [Header("UI")]
    [SerializeField]
    GameObject actionPanel;

    [SerializeField]
    GameObject purifyButton;

    [SerializeField]
    GameObject dangoButton;

    [SerializeField]
    GameObject purifyStopButton;

    [SerializeField]
    GameObject purifyHoldButton;

    [SerializeField]
    MagicCircleActivator magicCircleActivator;

    [SerializeField]
    PentagramInputCatcher pentagramInputCatcher;


    [SerializeField]
    GameObject dangerOverlay;

    [Header("Danger Effect")]
    [SerializeField]
    YokaiDangerEffect[] dangerEffects;

    [Header("Purity Empty Visuals")]
    [SerializeField]
    float purityEmptyDarkenIntensity = 0.2f;

    [Header("Energy Empty Visuals")]
    [SerializeField]
    float energyEmptyFadeAlpha = 0.4f;

    [SerializeField]
    float purityEmptyReleaseDelay = 0.15f;

    [SerializeField]
    float purityEmptyWobbleScale = 0.02f;

    [SerializeField]
    float purityEmptyWobbleSpeed = 2.6f;

    [SerializeField]
    float purityEmptyJitterAmplitude = 0.015f;

    [SerializeField]
    DangoButtonHandler dangoButtonHandler;

    [SerializeField]
    PurifyButtonHandler purifyButtonHandler;

    GameObject purityEmptyTargetRoot;
    readonly Dictionary<SpriteRenderer, Color> purityEmptySpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> purityEmptyImageColors = new Dictionary<Image, Color>();
    Vector3 purityEmptyBasePosition;
    float purityEmptyNoiseSeed;
    bool isPurityEmptyVisualsActive;
    bool isPurityEmptyMotionApplied;
    Coroutine purityEmptyReleaseRoutine;
    YokaiState? lastAppliedState;
    YokaiState? lastUIAppliedState = null;
    bool hasPlayedPurityEmptyEnter;
    bool hasPlayedEnergyEmptyEnter;
    bool hasPlayedPurifyStartSE; // Purify開始SEの二重再生防止フラグ
    bool hasWarnedMissingDependencies;
    bool hasWarnedMissingOptionalDependencies;
    static YokaiStatePresentationController instance;
    Coroutine bindRetryRoutine;
    readonly Dictionary<SpriteRenderer, Color> energyEmptySpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> energyEmptyImageColors = new Dictionary<Image, Color>();
    bool isEnergyEmptyVisualsActive;
    bool hasEnsuredDangerOverlayLayout;
    bool hasLoggedResolvedReferences;

    public static YokaiStatePresentationController Instance => instance;

    public bool IsPurityEmptyVisualsActive => isPurityEmptyVisualsActive;
    public YokaiStateController StateController => stateController;

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
        BindPurifyChargeController(purifyChargeController);
        CachePurityEmptyTargets(CurrentYokaiContext.Current);
        CacheEnergyEmptyTargets(CurrentYokaiContext.Current);
        RefreshDangerEffectOriginalColors();
        lastAppliedState = null;
        lastUIAppliedState = null;
        hasPlayedPurityEmptyEnter = false;
        hasPlayedEnergyEmptyEnter = false;
        WarnMissingOptionalDependencies();
        BindMagicCircleActivator();
        SyncFromStateController();
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
        BindPurifyChargeController(null);
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
        {
            stateController.OnStateChanged -= OnStateChanged;
            stateController.OnPurifySucceeded -= HandlePurifySucceeded;
            stateController.OnPurifyCancelled -= HandlePurifyCancelled;
        }

        stateController = controller;

        if (stateController != null)
        {
            stateController.OnStateChanged += OnStateChanged;
            stateController.OnPurifySucceeded += HandlePurifySucceeded;
            stateController.OnPurifyCancelled += HandlePurifyCancelled;
        }

        purifyChargeController?.BindStateController(stateController);
    }

    void BindPurifyChargeController(PurifyChargeController controller)
    {
        if (purifyChargeController == controller)
            return;

        purifyChargeController = controller;
        purifyChargeController?.BindStateController(stateController);
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
        BindPurifyChargeController(purifyChargeController);
        CachePurityEmptyTargets(activeYokai);
        CacheEnergyEmptyTargets(activeYokai);
        RefreshDangerEffectOriginalColors();
        SyncFromStateController();
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
        if (stateController == null)
            WarnMissingDependencies();

        return stateController;
    }
    void SetPentagramInputEnabled(bool enabled)
    {
        if (pentagramInputCatcher == null)
            return;
        pentagramInputCatcher.enabled = enabled;
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
                CachePurityEmptyTargets(CurrentYokaiContext.Current);
                CacheEnergyEmptyTargets(CurrentYokaiContext.Current);
                RefreshDangerEffectOriginalColors();
                SyncFromStateController();
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
    }

    void UnbindMagicCircleActivator()
    {
    }

    public void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        if (TryResolveStateController() == null)
            return;

        ApplyState(newState, force: false, previousStateOverride: previousState);
    }

    public void SyncFromStateController()
    {
        if (TryResolveStateController() == null)
            return;

        ApplyState(stateController.currentState, force: false);
    }

    public void ApplyState(YokaiState state, bool force = false, YokaiState? previousStateOverride = null)
    {
        if (!AreDependenciesResolved())
            return;

        Debug.Log($"[PRESENTATION] ApplyState: {state}");

        YokaiState? previousState = previousStateOverride ?? lastAppliedState;
        bool shouldForceEnter = state == YokaiState.Purifying;
        bool isSameState = previousState.HasValue && previousState.Value == state;
        bool shouldSkipTransition = !force && !shouldForceEnter && isSameState;

        if (!shouldSkipTransition && !force && previousState.HasValue && previousState.Value != state)
            HandleStateExit(previousState.Value, state);

        if (!shouldSkipTransition && !(force && !shouldForceEnter))
            HandleStateEnter(state, previousState);

        SyncMagicCircleForState(state);
        ApplyPentagramInputForState(state);
        ApplyDangerEffectsForState(state);

        // UI updates are centralized here
        ApplyActionUIForState(state);

        lastAppliedState = state;
    }

    void HandleStateEnter(YokaiState state, YokaiState? previousState)
    {
        if (state == YokaiState.PurityEmpty)
        {
            EnterPurityEmpty();
            UpdatePurityEmptyVisuals(true);

            bool shouldPlay = ShouldPlayEmptyEnterEffects(previousState);
            if (shouldPlay && !hasPlayedPurityEmptyEnter)
            {
                PlayStateEnterSE(state);
                hasPlayedPurityEmptyEnter = true;
            }
        }
        else if (state == YokaiState.EnergyEmpty)
        {
            EnterEnergyEmpty();

            bool shouldPlay = ShouldPlayEmptyEnterEffects(previousState);
            if (shouldPlay && !hasPlayedEnergyEmptyEnter)
            {
                PlayStateEnterSE(state);
                hasPlayedEnergyEmptyEnter = true;
            }
            return;
        }

        if (state == YokaiState.Normal)
        {
            RestoreNormalVisual();
            ResetPurityEmptyMotion();
        }

        HandleStateMessages(previousState, state);
    }

    void HandleStateExit(YokaiState state, YokaiState nextState)
    {
        if (state == YokaiState.EnergyEmpty)
        {
            ResetEnergyEmptyVisuals();

            if (nextState == YokaiState.Normal)
            {
                hasPlayedEnergyEmptyEnter = false;
                AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
            }

            PlayEnergyEmptyExitEffects();
            return;
        }

        if (state == YokaiState.PurityEmpty)
        {
            RequestReleasePurityEmpty();
            ResetPurityEmptyMotion();

            if (nextState == YokaiState.Normal)
            {
                hasPlayedPurityEmptyEnter = false;
                AudioHook.RequestPlay(YokaiSE.SE_PURITY_EMPTY_RELEASE);
            }
            return;
        }

        if (state == YokaiState.Purifying)
            hasPlayedPurifyStartSE = false;
    }

    void SyncMagicCircleForState(YokaiState state)
    {
        if (state != YokaiState.Purifying)
            hasPlayedPurifyStartSE = false;

        if (magicCircleActivator == null)
            return;

        magicCircleActivator.ApplyState(state);
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
        bool showPurityEmptyVisuals = isPurityEmptyVisualsActive && visualState == YokaiState.PurityEmpty;

        UpdatePurityEmptyVisuals(showPurityEmptyVisuals);
    }

    YokaiState ResolveVisualState()
    {
        if (stateController != null && stateController.currentState == YokaiState.Evolving)
            return YokaiState.Evolving;

        if (stateController != null && stateController.currentState == YokaiState.Purifying)
            return YokaiState.Purifying;

        if (stateController != null && stateController.IsPurityEmptyState)
            return YokaiState.PurityEmpty;

        if (stateController != null && stateController.IsSpiritEmpty)
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
        if (purifyButton == null)
            missing.Add("PurifyButton");
        if (dangoButton == null)
            missing.Add("DangoButton");
        if (purifyStopButton == null)
            missing.Add("PurifyStopButton");
        if (purifyHoldButton == null)
            missing.Add("PurifyHoldButton");
        if (dangerOverlay == null)
            missing.Add("DangerOverlay");
        if (magicCircleActivator == null)
            missing.Add("MagicCircleActivator");
        if (dangoButtonHandler == null)
            missing.Add("DangoButtonHandler");
        if (purifyButtonHandler == null)
            missing.Add("PurifyButtonHandler");
        if (dangerEffects == null || dangerEffects.Length == 0)
            missing.Add("DangerEffects");

        if (missing.Count == 0)
            return;

        Debug.LogWarning($"[PRESENTATION] Optional Inspector references are not set: {string.Join(", ", missing)}");
        hasWarnedMissingOptionalDependencies = true;
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

    public void ApplyActionUIForState(YokaiState state)
    {
        bool isPurifying = state == YokaiState.Purifying;
        if (purifyHoldButton != null)
            purifyHoldButton.SetActive(isPurifying);

        if (actionPanel == null)
            return;

        // ★ Normal は必ず再構築（スキップも記録もしない）
        if (state != YokaiState.Normal &&
            lastUIAppliedState.HasValue &&
            lastUIAppliedState.Value == state)
        {
            return;
        }

        // ★ Normal 以外だけ記録
        if (state != YokaiState.Normal)
        {
            lastUIAppliedState = state;
        }
        else
        {
            lastUIAppliedState = null;
        }

        // --- ここからUI初期化 ---
        actionPanel.SetActive(false);
        purifyStopButton?.SetActive(false);
        switch (state)
        {
            case YokaiState.Normal:
            case YokaiState.PurityEmpty:
            case YokaiState.EnergyEmpty:
                actionPanel.SetActive(true);
                purifyButton?.SetActive(true);
                dangoButton?.SetActive(true);
                break;

            case YokaiState.Purifying:
                actionPanel.SetActive(false);
                break;

            case YokaiState.EvolutionReady:
                actionPanel.SetActive(false);
                break;
        }

        dangoButtonHandler?.RefreshUI();
        purifyButtonHandler?.RefreshUI();

        Debug.Log("UI FINAL CONFIRMED");
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

    void EnterEnergyEmpty()
    {
        if (energyEmptySpriteColors.Count == 0 && energyEmptyImageColors.Count == 0)
            CacheEnergyEmptyTargets(CurrentYokaiContext.Current);

        float targetAlpha = Mathf.Clamp01(energyEmptyFadeAlpha);

        foreach (var pair in energyEmptySpriteColors)
        {
            if (pair.Key == null)
                continue;

            Color color = pair.Value;
            color.a = targetAlpha;
            pair.Key.color = color;
        }

        foreach (var pair in energyEmptyImageColors)
        {
            if (pair.Key == null)
                continue;

            Color color = pair.Value;
            color.a = targetAlpha;
            pair.Key.color = color;
        }

        isEnergyEmptyVisualsActive = true;
    }

    void RestoreNormalVisual()
    {
        if (isEnergyEmptyVisualsActive)
            ResetEnergyEmptyVisuals();
    }

    void ResetEnergyEmptyVisuals()
    {
        foreach (var pair in energyEmptySpriteColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        foreach (var pair in energyEmptyImageColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        isEnergyEmptyVisualsActive = false;
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
        float jitterScale = 1f + purityEmptyWobbleScale;
        float jitterX = (Mathf.PerlinNoise(purityEmptyNoiseSeed + 1.4f, time) - 0.5f) * 2f * purityEmptyJitterAmplitude * jitterScale;
        float jitterY = (Mathf.PerlinNoise(purityEmptyNoiseSeed + 2.1f, time + 3.7f) - 0.5f) * 2f * purityEmptyJitterAmplitude * jitterScale;

        purityEmptyTargetRoot.transform.localPosition = purityEmptyBasePosition + new Vector3(jitterX, jitterY, 0f);
        isPurityEmptyMotionApplied = true;
    }

    void ResetPurityEmptyMotion()
    {
        if (purityEmptyTargetRoot == null)
            return;

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
    }

    void CachePurityEmptyTargets(GameObject targetRoot)
    {
        purityEmptyTargetRoot = targetRoot;
        purityEmptySpriteColors.Clear();
        purityEmptyImageColors.Clear();
        purityEmptyBasePosition = Vector3.zero;
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

    void CacheEnergyEmptyTargets(GameObject targetRoot)
    {
        energyEmptySpriteColors.Clear();
        energyEmptyImageColors.Clear();

        if (targetRoot == null)
            return;

        foreach (var sprite in targetRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null)
                continue;

            energyEmptySpriteColors[sprite] = sprite.color;
        }

        foreach (var image in targetRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
                continue;

            energyEmptyImageColors[image] = image.color;
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
        if (state == YokaiState.EnergyEmpty || state == YokaiState.PurityEmpty)
            return false;

        if (stateController == null)
            return true;

        if (state == YokaiState.Purifying)
            return !stateController.IsPurifyTriggeredByUser;

        return false;
    }

    bool ShouldPlayEmptyEnterEffects(YokaiState? previousState)
    {
        if (!previousState.HasValue)
            return false;

        if (previousState.Value == YokaiState.EnergyEmpty || previousState.Value == YokaiState.PurityEmpty)
            return false;

        return true;
    }

    void HandleStateMessages(YokaiState? previousState, YokaiState newState)
    {
        if (newState == YokaiState.Purifying && previousState != YokaiState.Purifying)
        {
            MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeGuide);
            if (!ShouldSuppressPresentationEffects(newState) && !hasPlayedPurifyStartSE)
            {
                AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
                hasPlayedPurifyStartSE = true;
            }

            if (stateController != null)
                stateController.ConsumePurifyTrigger();
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

    void HandlePurifySucceeded()
    {
        MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeSuccess);
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
    }

    void HandlePurifyCancelled()
    {
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_CANCEL);
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

        if (pentagramInputCatcher == null)
            pentagramInputCatcher = FindObjectOfType<PentagramInputCatcher>(true);

        if (purifyChargeController == null)
            purifyChargeController = FindObjectOfType<PurifyChargeController>(true);

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
                dangerOverlay = overlayObject;
        }

        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);

        if (dangoButtonHandler == null && dangoButton != null)
            dangoButtonHandler = dangoButton.GetComponent<DangoButtonHandler>();

        if (purifyButtonHandler == null && purifyButton != null)
            purifyButtonHandler = purifyButton.GetComponent<PurifyButtonHandler>();
    }

    void ApplyPentagramInputForState(YokaiState state)
    {
        // Pentagram は「Purifying（長押しチャージ中）」の時だけ入力を受ける。
        // それ以外の状態では、他UI（緊急おはらい等）への入力を絶対に奪わない。
        SetPentagramInputEnabled(state == YokaiState.Purifying);
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
