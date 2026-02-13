using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    private YokaiActionExecutor actionExecutor;
    private PurifyStateMachine purifyMachine;
    private EvolutionStateMachine evolutionMachine;

    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public event System.Action<YokaiState, YokaiState> OnStateChanged;
    public event System.Action OnPurifySucceeded;
    public event System.Action OnPurifyCancelled;
    public YokaiState CurrentState => currentState;

    bool isSpiritEmpty;
    bool isPurityEmpty;
    bool isPurifyCharging;
    bool isPurifyTriggerReady;
    bool canUseSpecialDango;
    public bool HasUserInteracted { get; private set; } = false;
    public bool IsPurifyTriggeredByUser { get; private set; }

    [Header("Dependencies")]
    [SerializeField]
    private YokaiGrowthController growthController;

    [SerializeField]
    YokaiStatePresentationController presentationController;

    [SerializeField]
    MagicCircleActivator magicCircleActivator;

    [FormerlySerializedAs("kegareManager")]
    [SerializeField]
    PurityController purityController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    float dangoAmount = 30f;

    [SerializeField]
    float minimumRecoverValue = 10f;

    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    const float EvolutionReadyScale = 2.0f;
    bool isReady;
    bool hasWarnedUnknownState;
    bool hasWarnedMissingPurifyControllers;
    bool hasWarnedMissingMagicCircle;
    Coroutine purifyFallbackRoutine;
    string lastStateChangeReason;
    string lastPurityRecoveredReason;
    int lastPurityRecoveredFrame = -1;

    [Header("Purify Fallback")]
    [SerializeField]
    bool debugAutoCompletePurify;

    [SerializeField]
    float purifyFallbackSeconds = 2.5f;

    bool canEvaluateState =>
        isReady
        && CurrentYokaiContext.Current != null
        && purityController != null
        && spiritController != null;

    public bool IsSpiritEmpty => isSpiritEmpty;
    public bool IsPurityEmptyState => isPurityEmpty;
    public bool IsPurifyCharging => isPurifyCharging;
    public bool IsPurifyTriggerReady => isPurifyTriggerReady;
    public bool CanUseSpecialDango => canUseSpecialDango;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;
    public string LastStateChangeReason => lastStateChangeReason;
    internal PurifyStateMachine PurifyMachine => purifyMachine ??= new PurifyStateMachine(this);
    internal EvolutionStateMachine EvolutionMachine => evolutionMachine ??= new EvolutionStateMachine(this);

    public bool CanDo(YokaiAction action)
    {
        return YokaiStateRules.CanDo(
            currentState,
            action,
            isPurifyCharging,
            isPurityEmpty,
            isSpiritEmpty
        );
    }

    public bool TryDo(YokaiAction action, string reason = null)
    {
        if (action == YokaiAction.Purify)
        {
            Debug.Log("[LEGACY] Purify action disabled");
            return false;
        }

        if (!CanDo(action))
        {
            return false;
        }

        if (actionExecutor == null)
            actionExecutor = new YokaiActionExecutor(this);

        actionExecutor.Execute(action, reason);
        return true;
    }

    private void RecoverSpirit()
    {
        spiritController.AddSpirit(dangoAmount);
        TutorialManager.NotifyDangoUsed();
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        RequestEvaluateState("SpiritRecovered");
    }

    internal void HandlePurifyStart(string reason)
    {
        PurifyMachine.StartPurify(reason);
    }

    internal void HandlePurifyCancel(string reason)
    {
        if (reason == "ChargeComplete")
            PurifyMachine.CompleteCharging();
        else
            PurifyMachine.CancelPurify(reason);
    }

    internal void HandlePurifyHoldCancel(string reason)
    {
        PurifyMachine.CancelCharging(reason);
    }

    internal void SetPurifyCharging(bool value)
    {
        isPurifyCharging = value;
    }

    internal void SetPurifyTriggeredByUser(bool value)
    {
        IsPurifyTriggeredByUser = value;
    }

    internal void SetHasUserInteracted(bool value)
    {
        HasUserInteracted = value;
    }

    internal void MarkUserInteracted()
    {
        HasUserInteracted = true;
    }

    internal void HandleEmergencySpiritRecover()
    {
        Debug.Log("[YokaiStateController] ExecuteAction EmergencySpiritRecover reached");
        RecoverSpirit();
    }

    internal void HandleEatDango()
    {
        RecoverSpirit();
    }

    void OnEnable()
    {
        CurrentYokaiContext.RegisterStateController(this);
        CurrentYokaiContext.OnCurrentYokaiConfirmed += HandleCurrentYokaiConfirmed;
        isReady = false;
    }

    void Awake()
    {
        actionExecutor = new YokaiActionExecutor(this);
        purifyMachine = new PurifyStateMachine(this);
    }

    void Start()
    {
        ResolveSceneControllers();
        if (CurrentYokaiContext.Current != null)
        {
            BindControllers(CurrentYokaiContext.Current, ShouldAllowRebindPresentationSync());
        }
    }

    void OnDisable()
    {
        ResetPurifyingState();
        UnregisterPurityEvents();
        UnregisterSpiritEvents();

        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        CurrentYokaiContext.UnregisterStateController(this);
    }

    void RegisterPurityEvents()
    {
        if (purityController != null)
        {
            purityController.OnPurityEmpty += OnPurityEmpty;
            purityController.OnPurityRecovered += OnPurityRecovered;
        }
    }

    void RegisterSpiritEvents()
    {
        if (spiritController != null)
        {
            spiritController.OnSpiritEmpty += OnSpiritEmpty;
            spiritController.OnSpiritRecovered += OnSpiritRecovered;
        }
    }

    void UnregisterPurityEvents()
    {
        if (purityController != null)
        {
            purityController.OnPurityEmpty -= OnPurityEmpty;
            purityController.OnPurityRecovered -= OnPurityRecovered;
        }
    }

    void UnregisterSpiritEvents()
    {
        if (spiritController != null)
        {
            spiritController.OnSpiritEmpty -= OnSpiritEmpty;
            spiritController.OnSpiritRecovered -= OnSpiritRecovered;
        }
    }

    void SyncManagerState()
    {
        isSpiritEmpty = spiritController != null && spiritController.HasNoSpirit();
        isPurityEmpty = purityController != null && purityController.IsPurityEmpty;
    }

    public void OnSpiritEmpty()
    {
        if (currentState == YokaiState.EvolutionReady)
            return;

        isSpiritEmpty = true;
        canUseSpecialDango = true;

        RequestEvaluateState("SpiritEmpty");
    }

    public void OnSpiritRecovered()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
        canUseSpecialDango = false;
        RequestEvaluateState("SpiritRecovered");
    }

    public void ForceReevaluate(string reason)
    {
        RequestEvaluateState(reason, ShouldAllowRebindPresentationSync());
    }

    void EvaluateState(YokaiState? requestedState = null, string reason = "Auto", bool forcePresentation = false)
    {
        if (!canEvaluateState)
        {
            return;
        }

        YokaiState nextState = DetermineNextState(requestedState);
        bool stateChanged = currentState != nextState;
        if (stateChanged)
        {
            SetState(nextState, reason);
            ApplyEmptyStateEffects();
            SyncPresentation(nextState, force: false);
            CheckForUnknownStateWarning();
            return;
        }

        if (forcePresentation)
        {
            SyncPresentation(currentState, force: true);
        }

        ApplyEmptyStateEffects();
    }

    YokaiState DetermineNextState(YokaiState? requestedState = null)
    {
        // 優先順位:
        // 1) 強制状態 (Purifying / Evolving / EvolutionReady)
        // 2) requestedState 評価時の維持状態
        // 3) Empty 系状態
        // 4) Normal
        YokaiState? forcedState = DetermineForcedState();
        if (forcedState.HasValue)
            return forcedState.Value;

        if (requestedState.HasValue)
            return DetermineRequestedState(requestedState.Value);

        return DetermineDefaultState();
    }

    YokaiState? DetermineForcedState()
    {
        return YokaiStateRules.DetermineForcedState(currentState);
    }

    YokaiState DetermineRequestedState(YokaiState requestedState)
    {
        Debug.Log($"[EVOLUTION] DetermineRequestedState requested={requestedState} current={currentState}");
        return YokaiStateRules.DetermineRequestedState(currentState, requestedState, isPurityEmpty, isSpiritEmpty);
    }

    YokaiState DetermineDefaultState()
    {
        return YokaiStateRules.DetermineDefaultState(isPurityEmpty, isSpiritEmpty);
    }

    public void SetState(YokaiState newState, string reason)
    {
        if (currentState == newState)
        {
            Debug.Log($"[STATE SKIP] {newState} already active ({reason})");
            return;
        }

        var prev = currentState;
        currentState = newState;
        lastStateChangeReason = reason;

        Debug.Log($"[STATE] {prev} -> {newState} ({reason})");
        OnStateChanged?.Invoke(prev, newState);
    }

    public void BeginPurifying(string reason = "BeginPurify")
    {
        PurifyMachine.StartPurify(reason);
    }

    public void ConsumePurifyTrigger()
    {
        IsPurifyTriggeredByUser = false;
    }

    public void BeginEvolution()
    {
        EvolutionMachine.StartEvolution("BeginEvolution");
    }

    public void CompleteEvolution()
    {
        EvolutionMachine.CompleteEvolution();
    }

    public void BindCurrentYokai(GameObject activeYokai)
    {
        isReady = false;
        if (currentState == YokaiState.Evolving && activeYokai != null)
        {
            // 不具合④: 進化演出中に切り替わった妖怪情報を保持して完了メッセージを出す。
            if (YokaiEncyclopedia.TryResolveYokaiId(activeYokai.name, out _, out YokaiEvolutionStage stage))
            {
                if (stage == YokaiEvolutionStage.Child || stage == YokaiEvolutionStage.Adult)
                {
                    evolutionResultStage = stage;
                    evolutionResultPending = true;
                }
            }
        }

        BindControllers(activeYokai, ShouldAllowRebindPresentationSync());
        SetActiveYokai(activeYokai);
    }

    public void MarkReady()
    {
        isReady = true;
        CheckForUnknownStateWarning();
    }

    void BindControllers(GameObject activeYokai, bool allowRebindPresentationSync)
    {
        YokaiGrowthController nextGrowth = null;

        if (activeYokai != null)
        {
            nextGrowth = activeYokai.GetComponentInChildren<YokaiGrowthController>(true);
        }

        growthController = nextGrowth;
        SyncManagerState();
        
        isReady = true;
        
        if (canEvaluateState)
        {
            RequestEvaluateState("FullyInitialized", allowRebindPresentationSync);
        }
    }

    void ResolveSceneControllers()
    {
        if (purityController == null)
        {
            purityController = FindObjectOfType<PurityController>(true);
        }

        if (spiritController == null)
        {
            spiritController = FindObjectOfType<SpiritController>(true);
        }

        if (magicCircleActivator == null)
        {
            magicCircleActivator = FindObjectOfType<MagicCircleActivator>(true);
        }

        RegisterPurityEvents();
        RegisterSpiritEvents();
        isReady = true;
    }

    public void SetActiveYokai(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        if (currentState != YokaiState.Evolving)
            evolutionResultPending = false;

        SyncManagerState();
        ApplyEmptyStateEffects();
    }

    public void SetEvolutionReady()
    {
        if (currentState == YokaiState.Evolving)
            return;

        if (IsEvolutionBlocked(out string reason))
        {
            return;
        }

        if (!HasReachedEvolutionScale())
        {
            return;
        }

        if (!IsPurityEmpty())
            RequestEvaluateStateRequested(YokaiState.EvolutionReady, "EvolutionReady", false);
    }

    public void OnPurityEmpty()
    {
        if (currentState == YokaiState.EvolutionReady)
            return;

        isPurityEmpty = true;
        isPurifyTriggerReady = true;

        RequestEvaluateState("PurityEmpty");
    }

    public void OnPurityRecovered(string reason)
    {
        if (reason == lastPurityRecoveredReason
            && Time.frameCount <= lastPurityRecoveredFrame + 1)
        {
            return;
        }

        lastPurityRecoveredReason = reason;
        lastPurityRecoveredFrame = Time.frameCount;

        if (!isPurityEmpty)
            return;

        isPurityEmpty = false;
        isPurifyTriggerReady = false;
        RequestEvaluateState("PurityRecovered");
    }

    void HandleThresholdReached(ref bool stateFlag, string reason)
    {
        if (stateFlag)
            return;

        stateFlag = true;
        RequestEvaluateState(reason);
    }

    public bool IsPurityEmpty()
    {
        return isPurityEmpty;
    }

    bool HasReachedEvolutionScale()
    {
        if (growthController == null)
            return false;

        float scale = growthController.currentScale;
        return scale >= EvolutionReadyScale;
    }

    bool IsEvolutionBlocked(out string reason)
    {
        bool hasPurityEmpty = isPurityEmpty;
        bool hasSpiritEmpty = isSpiritEmpty;
        if (!hasPurityEmpty && !hasSpiritEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (hasPurityEmpty && hasSpiritEmpty)
            reason = "清浄度0 / 霊力0";
        else if (hasPurityEmpty)
            reason = "清浄度0";
        else
            reason = "霊力0";

        return true;
    }

    public void EnterSpiritEmpty()
    {
        OnSpiritEmpty();
    }

    public void EnterPurityEmpty()
    {
        OnPurityEmpty();
    }

    public bool TryConsumeEvolutionResult(out YokaiEvolutionStage stage)
    {
        if (evolutionResultPending)
        {
            stage = evolutionResultStage;
            evolutionResultPending = false;
            return true;
        }

        stage = evolutionResultStage;
        return false;
    }

    void HandleCurrentYokaiConfirmed(GameObject activeYokai)
    {
        MarkReady();
        ForceReevaluate("CurrentYokaiConfirmed");
    }

    public void NotifyPurifySucceeded()
    {
        if (currentState != YokaiState.Purifying)
            return;

        StopPurifyFallback();
        if (purityController != null)
        {
            purityController.RecoverPurityByRatio(0.5f);
        }
        else if (!hasWarnedMissingPurifyControllers)
        {
            Debug.LogWarning("[PURIFY] PurityController is missing for purify recovery.");
            hasWarnedMissingPurifyControllers = true;
        }

        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifySucceeded?.Invoke();
    }

    public void NotifyPurifyCancelled()
    {
        if (currentState != YokaiState.Purifying)
            return;

        StopPurifyFallback();
        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifyCancelled?.Invoke();
        RequestEvaluateState("PurifyCancelled", false);
    }

    public void ExecuteEmergencyPurify(string reason)
    {
        if (currentState != YokaiState.PurityEmpty)
            return;

        Debug.Log("[EMERGENCY] EmergencyPurify requested");

        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;

        if (purityController != null)
        {
            float recoveredPurity = Mathf.Max(purityController.purity, minimumRecoverValue);
            purityController.SetPurity(recoveredPurity, reason ?? "EmergencyPurify");
        }

        RequestEvaluateState("EmergencyPurify");
    }

    void ResetPurifyingState()
    {
        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;
        StopPurifyFallback();
    }

    public void NotifyUserInteraction()
    {
        if (HasUserInteracted)
            return;

        HasUserInteracted = true;
    }

    public void RequestEvaluateState(string reason)
    {
        RequestEvaluateState(reason, false);
    }

    public void RequestEvaluateState(string reason, bool forcePresentation)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(reason: reason, forcePresentation: forcePresentation);
    }

    public void RequestEvaluateStateRequested(
        YokaiState requestedState,
        string reason,
        bool forcePresentation)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(requestedState, reason: reason, forcePresentation: forcePresentation);
    }

    bool ShouldAllowRebindPresentationSync()
    {
        return false;
    }

    YokaiStatePresentationController ResolvePresentationController()
    {
        if (presentationController != null)
            return presentationController;

        return YokaiStatePresentationController.Instance;
    }

    void ForceSyncPresentation(YokaiState state)
    {
        if (state != currentState)
        {
            Debug.LogWarning(
                $"[STATE] ForceSyncPresentation ignored. state={state}, currentState={currentState}");
        }

        SyncPresentation(currentState, force: true);
    }

    void SyncPresentation(YokaiState state, bool force)
    {
        var controller = ResolvePresentationController();
        if (controller == null)
            return;

        Debug.Log($"[SYNC] ApplyState {state} force={force}");
        controller.ApplyState(state, force: force);
    }

    MagicCircleActivator ResolveMagicCircleActivator()
    {
        if (magicCircleActivator != null)
            return magicCircleActivator;

        magicCircleActivator = FindObjectOfType<MagicCircleActivator>(true);
        return magicCircleActivator;
    }

    void StartPurifyFallbackIfNeeded()
    {
        StopPurifyFallback();

        if (!debugAutoCompletePurify)
            return;

        var activator = ResolveMagicCircleActivator();
        if (activator != null && activator.HasMagicCircleRoot)
            return;

        if (!hasWarnedMissingMagicCircle)
        {
            Debug.LogWarning("[PURIFY] MagicCircleRoot is missing; using fallback timer.");
            hasWarnedMissingMagicCircle = true;
        }

        purifyFallbackRoutine = StartCoroutine(PurifyFallbackRoutine());
    }

    void StopPurifyFallback()
    {
        if (purifyFallbackRoutine == null)
            return;

        StopCoroutine(purifyFallbackRoutine);
        purifyFallbackRoutine = null;
    }

    IEnumerator PurifyFallbackRoutine()
    {
        float delay = Mathf.Max(0.2f, purifyFallbackSeconds);
        yield return new WaitForSeconds(delay);

        purifyFallbackRoutine = null;

        if (currentState == YokaiState.Purifying)
            CompletePurifySuccess("PurifyFallback");
    }

    void CompletePurifySuccess(string reason)
    {
        if (CurrentState != YokaiState.Purifying)
            return;

        NotifyPurifySucceeded();
        SetState(YokaiState.Normal, "PurifySuccess");
    }

    void ApplyEmptyStateEffects()
    {
        bool isLockedState = currentState == YokaiState.EvolutionReady || currentState == YokaiState.Evolving;
        bool shouldEnableDecay = !isLockedState && currentState != YokaiState.EnergyEmpty && currentState != YokaiState.PurityEmpty;
        bool shouldEnableGrowth = shouldEnableDecay;

        if (spiritController != null)
        {
            if (spiritController.SetNaturalDecayEnabled(shouldEnableDecay))
            {
                Debug.Log($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={currentState})");
            }
        }

        if (purityController != null)
        {
            if (purityController.SetNaturalDecayEnabled(shouldEnableDecay))
            {
                Debug.Log($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={currentState})");
            }
        }

        if (growthController != null)
        {
            if (growthController.SetGrowthEnabled(shouldEnableGrowth))
            {
                Debug.Log($"[GROWTH] Growth {(shouldEnableGrowth ? "enabled" : "disabled")} (State={currentState})");
            }
        }
    }

    void CheckForUnknownStateWarning()
    {
#if UNITY_EDITOR
        if (hasWarnedUnknownState)
            return;
#endif
    }
}
}
