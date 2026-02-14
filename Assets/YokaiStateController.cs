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
    private YokaiSideEffectService sideEffectService;

    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public event System.Action<YokaiState, YokaiState> OnStateChanged;
    public event System.Action OnPurifySucceeded;
    public event System.Action OnPurifyCancelled;
    public YokaiState CurrentState => currentState;

    bool isSpiritEmpty;
    bool isPurityEmpty;
    bool isPurifying;
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
    private int lastStateChangeFrame = -1;

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
    public bool IsPurifying => isPurifying;
    public bool IsPurifyCharging => isPurifyCharging;
    public bool IsPurifyTriggerReady => isPurifyTriggerReady;
    public bool CanUseSpecialDango => canUseSpecialDango;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;
    internal YokaiGrowthController GrowthController => growthController;
    public string LastStateChangeReason => lastStateChangeReason;
    internal PurifyStateMachine PurifyMachine => purifyMachine ??= new PurifyStateMachine();
    internal EvolutionStateMachine EvolutionMachine => evolutionMachine ??= new EvolutionStateMachine(this);

    public bool CanDo(YokaiAction action)
    {
        return YokaiActionRuleEngine.CanDo(
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
            YokaiLogger.Action("[LEGACY] Purify action disabled");
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


    internal void SetPurifying(bool value)
    {
        isPurifying = value;
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
        YokaiLogger.Action("[YokaiStateController] ExecuteAction EmergencySpiritRecover reached");
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
        purifyMachine = new PurifyStateMachine();
        sideEffectService = new YokaiSideEffectService(this);
    }

    void Start()
    {
        ResolveSceneControllers();
        if (CurrentYokaiContext.Current != null)
        {
            BindControllers(CurrentYokaiContext.Current);
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
        RequestEvaluateState(reason);
    }

    void EvaluateState(YokaiState? requestedState = null, string reason = "Auto")
    {
        if (!canEvaluateState)
        {
            return;
        }

        if (requestedState.HasValue)
        {
            YokaiLogger.FSM($"[EVOLUTION] DetermineRequestedState requested={requestedState.Value} current={currentState}");
        }

        var nextState = YokaiStateEngine.DetermineNextState(
            currentState,
            requestedState,
            isPurityEmpty,
            isSpiritEmpty,
            isPurifying,
            currentState == YokaiState.Evolving,
            currentState == YokaiState.EvolutionReady
        );
        SetState(nextState, reason);
    }

    public void SetState(YokaiState newState, string reason)
    {
        // 同じ状態への変更は不要
        if (currentState == newState)
            return;

        // 同じフレーム内での複数回変更を防止
        if (lastStateChangeFrame == UnityEngine.Time.frameCount)
            return;

        // フレーム番号を記録
        lastStateChangeFrame = UnityEngine.Time.frameCount;

        var prev = currentState;
        currentState = newState;
        lastStateChangeReason = reason;

        YokaiLogger.State($"{prev} -> {newState} ({reason})");
        OnStateChanged?.Invoke(prev, newState);

        sideEffectService.ApplyStateSideEffects(newState);
        CheckForUnknownStateWarning();
    }

    public void BeginPurifying(string reason = "BeginPurify")
    {
        var command = PurifyMachine.StartPurify(reason);

        switch (command)
        {
            case PurifyCommand.BeginPurifying:
                SetHasUserInteracted(false);
                SetPurifying(true);
                SetPurifyCharging(false);
                SetPurifyTriggeredByUser(true);
                SetState(YokaiState.Purifying, reason ?? "BeginPurify");
                break;
        }
    }

    public void ConsumePurifyTrigger()
    {
        IsPurifyTriggeredByUser = false;
    }

    public void BeginEvolution()
    {
        var command = EvolutionMachine.BeginEvolution();

        switch (command)
        {
            case EvolutionCommand.BeginEvolving:
                SetState(YokaiState.Evolving, "BeginEvolution");
                break;
        }
    }

    public void CompleteEvolution()
    {
        var command = EvolutionMachine.CompleteEvolution();

        switch (command)
        {
            case EvolutionCommand.CompleteEvolution:
                SpiritController.SetSpirit(80f);
                PurityController.SetPurity(80f);
                SetState(YokaiState.Normal, "EvolutionComplete");
                break;
        }
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

        BindControllers(activeYokai);
        SetActiveYokai(activeYokai);
    }

    public void MarkReady()
    {
        isReady = true;
        CheckForUnknownStateWarning();
    }

    void BindControllers(GameObject activeYokai)
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
            RequestEvaluateState("FullyInitialized");
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
            RequestEvaluateStateRequested(YokaiState.EvolutionReady, "EvolutionReady");
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
            YokaiLogger.Warning("[PURIFY] PurityController is missing for purify recovery.");
            hasWarnedMissingPurifyControllers = true;
        }

        isPurifying = false;
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
        isPurifying = false;
        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifyCancelled?.Invoke();
        RequestEvaluateState("PurifyCancelled");
    }


    internal void StopPurifyingForSuccess()
    {
        StopPurifyingForSuccess("PurifySuccess");
    }

    internal void StopPurifyingForSuccess(string reason)
    {
        NotifyPurifySucceeded();
        SetState(YokaiState.Normal, reason ?? "PurifySuccess");
    }

    internal void CancelPurifying(string reason)
    {
        NotifyPurifyCancelled();
        SetState(YokaiState.Normal, reason ?? "Cancelled");
    }

    public void ExecuteEmergencyPurify(string reason)
    {
        if (currentState != YokaiState.PurityEmpty)
            return;

        YokaiLogger.Action("[EMERGENCY] EmergencyPurify requested");

        isPurifying = false;
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
        isPurifying = false;
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
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(reason: reason);
    }

    public void RequestEvaluateStateRequested(YokaiState requestedState, string reason)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(requestedState, reason: reason);
    }

    internal YokaiStatePresentationController ResolvePresentationController()
    {
        if (presentationController != null)
            return presentationController;

        return YokaiStatePresentationController.Instance;
    }

    void SyncPresentation(YokaiState state, bool force)
    {
        var controller = ResolvePresentationController();
        if (controller == null)
            return;

        YokaiLogger.State($"[SYNC] ApplyState {state} force={force}");
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
            YokaiLogger.Warning("[PURIFY] MagicCircleRoot is missing; using fallback timer.");
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

        StopPurifyingForSuccess(reason ?? "PurifyFallback");
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
                YokaiLogger.State($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={currentState})");
            }
        }

        if (purityController != null)
        {
            if (purityController.SetNaturalDecayEnabled(shouldEnableDecay))
            {
                YokaiLogger.State($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={currentState})");
            }
        }

        if (growthController != null)
        {
            if (growthController.SetGrowthEnabled(shouldEnableGrowth))
            {
                YokaiLogger.State($"[GROWTH] Growth {(shouldEnableGrowth ? "enabled" : "disabled")} (State={currentState})");
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
