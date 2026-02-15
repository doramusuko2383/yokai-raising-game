using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    public static YokaiStateController Instance { get; private set; }
    private YokaiActionExecutor actionExecutor;
    private PurifyStateMachine purifyMachine;
    private EvolutionStateMachine evolutionMachine;
    private YokaiSideEffectService sideEffectService;
    private YokaiStateHistoryService historyService;

    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public event System.Action<YokaiState, YokaiState> OnStateChanged;
    public event System.Action OnPurifySucceeded;
    public event System.Action OnPurifyCancelled;
    public YokaiState CurrentState => currentState;

    bool isSpiritEmpty;
    bool isPurityEmpty;
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
    private bool isApplyingSideEffects = false;
    private List<string> invariantWarnings = new List<string>();

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
    public bool IsPurifying => PurifyMachine.IsPurifying;
    public bool IsPurifyCharging => PurifyMachine.IsCharging;
    public bool IsPurifyTriggerReady => isPurifyTriggerReady;
    public bool CanUseSpecialDango => canUseSpecialDango;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;
    internal YokaiGrowthController GrowthController => growthController;
    public string LastStateChangeReason => lastStateChangeReason;
    public string LastActionBlockReason { get; private set; }
    public string LastActionBlockedAction { get; private set; }
    public int LastActionBlockFrame { get; private set; }
    public IReadOnlyList<string> LastInvariantWarnings => invariantWarnings;

    public IReadOnlyList<string> GetStateHistory()
    {
        return historyService.GetHistoryList();
    }
    internal PurifyStateMachine PurifyMachine => purifyMachine ??= new PurifyStateMachine();
    internal EvolutionStateMachine EvolutionMachine => evolutionMachine ??= new EvolutionStateMachine(this);

    public bool CanDo(YokaiAction action)
    {
        return CanDo(action, out _);
    }

    public bool CanDo(YokaiAction action, out string reason)
    {
        return YokaiActionRuleEngine.CanDo(
            currentState,
            action,
            IsPurifyCharging,
            isPurityEmpty,
            isSpiritEmpty,
            out reason
        );
    }

    public bool TryDo(YokaiAction action, string reason = null)
    {
        if (action == YokaiAction.Purify)
        {
            YokaiLogger.Action("[LEGACY] Purify action disabled");
            return false;
        }

        if (!CanDo(action, out string denyReason))
        {
            LastActionBlockReason = denyReason;
            LastActionBlockedAction = action.ToString();
            LastActionBlockFrame = Time.frameCount;
            YokaiLogger.Warning($"[ACTION BLOCK] action={action} blocked. reason={denyReason}");
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
    internal void SetPurifyTriggeredByUser(bool value)
    {
        IsPurifyTriggeredByUser = value;
    }

    internal void HandleEmergencySpiritRecover()
    {
        YokaiLogger.Action("[YokaiStateController] ExecuteAction EmergencySpiritRecover reached");
        RecoverSpirit();
    }

    internal void HandleEatDango()
    {
        ExecuteEatDango();
    }

    void ExecuteEatDango()
    {
        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
        {
            Debug.LogWarning("[DANGO] Save data missing");
            return;
        }

        if (save.dango.currentCount <= 0)
        {
            Debug.Log("[DANGO] No dango to consume");
            return;
        }

        // --- 団子消費 ---
        save.dango.currentCount--;

        SaveManager.Instance.MarkDirty();
        SaveManager.Instance.NotifyDangoChanged();

        Debug.Log("[DANGO] Consumed 1 dango");

        // --- 霊力回復 ---
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        actionExecutor = new YokaiActionExecutor(this);
        purifyMachine = new PurifyStateMachine();
        sideEffectService = new YokaiSideEffectService(this);
        historyService = new YokaiStateHistoryService();
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
        if (Instance == this)
            Instance = null;

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
            IsPurifying,
            currentState == YokaiState.Evolving,
            currentState == YokaiState.EvolutionReady
        );
        SetState(nextState, reason);
    }

    public void SetState(YokaiState newState, string reason)
    {
        // 副作用実行中の再入を防止
        if (isApplyingSideEffects)
            return;

        // 同じ状態への変更は不要
        if (currentState == newState)
            return;

        // 同一フレーム内の重複遷移防止
        if (lastStateChangeFrame == UnityEngine.Time.frameCount)
            return;

        lastStateChangeFrame = UnityEngine.Time.frameCount;

        var prev = currentState;
        currentState = newState;
        lastStateChangeReason = reason;

        YokaiLogger.State($"{prev} -> {newState} ({reason})");
        historyService.Record(
            prev,
            newState,
            reason,
            UnityEngine.Time.frameCount
        );
        OnStateChanged?.Invoke(prev, newState);

        isApplyingSideEffects = true;
        try
        {
            sideEffectService.ApplyStateSideEffects(newState);
        }
        finally
        {
            isApplyingSideEffects = false;
        }
        CheckForUnknownStateWarning();
        InvariantCheck();
    }

    public void BeginPurifying(string reason = "BeginPurify")
    {
        var command = PurifyMachine.StartPurify(reason);

        switch (command)
        {
            case PurifyCommand.BeginPurifying:
                HasUserInteracted = false;
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

        PurifyMachine.Reset();
        HasUserInteracted = PurifyMachine.HasUserInteracted;
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifySucceeded?.Invoke();
    }

    public void NotifyPurifyCancelled()
    {
        if (currentState != YokaiState.Purifying)
            return;

        StopPurifyFallback();
        PurifyMachine.Reset();
        HasUserInteracted = PurifyMachine.HasUserInteracted;
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

        PurifyMachine.Reset();
        HasUserInteracted = PurifyMachine.HasUserInteracted;
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
        PurifyMachine.Reset();
        HasUserInteracted = PurifyMachine.HasUserInteracted;
        IsPurifyTriggeredByUser = false;
        StopPurifyFallback();
    }

    public void NotifyUserInteraction()
    {
        if (HasUserInteracted)
            return;

        PurifyMachine.MarkUserInteracted();
        HasUserInteracted = PurifyMachine.HasUserInteracted;
    }



    public void ApplyOfflineProgress(long deltaSeconds, long now)
    {
        if (deltaSeconds <= 0)
            return;

        if (purityController == null || spiritController == null || growthController == null)
            return;

        var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
        var boost = save != null ? save.boost : null;

        long growthBoostSeconds = 0;
        long decayHalfSeconds = 0;

        if (boost != null)
        {
            if (boost.growthBoostExpireUnixTime > now)
                growthBoostSeconds = Math.Min(deltaSeconds, boost.growthBoostExpireUnixTime - now);

            if (boost.decayHalfBoostExpireUnixTime > now)
                decayHalfSeconds = Math.Min(deltaSeconds, boost.decayHalfBoostExpireUnixTime - now);
        }

        float purityRatePerSecond = GetPurityDecayRatePerSecond();
        float spiritRatePerSecond = GetSpiritDecayRatePerSecond();
        float growthRatePerSecond = GetGrowthRatePerSecond();

        long decayNormalSeconds = deltaSeconds - decayHalfSeconds;
        float purityDelta =
            -(purityRatePerSecond * decayNormalSeconds)
            + -(purityRatePerSecond * 0.5f * decayHalfSeconds);

        float spiritDelta =
            -(spiritRatePerSecond * decayNormalSeconds)
            + -(spiritRatePerSecond * 0.5f * decayHalfSeconds);

        long growthNormalSeconds = deltaSeconds - growthBoostSeconds;
        float growthDelta =
            (growthRatePerSecond * growthNormalSeconds)
            + (growthRatePerSecond * 2f * growthBoostSeconds);

        purityController.AddPurity(purityDelta);
        spiritController.ChangeSpirit(spiritDelta);
        growthController.AddGrowth(growthDelta);

        HandleZeroOvertimeStress(deltaSeconds);

        RequestEvaluateState("OfflineProgress");
    }

    float GetPurityDecayRatePerSecond()
    {
        if (purityController == null)
            return 0f;

        const float defaultPurityDecayPerMinute = 2f;
        return Mathf.Max(0f, defaultPurityDecayPerMinute / 60f);
    }

    float GetSpiritDecayRatePerSecond()
    {
        if (spiritController == null)
            return 0f;

        const float defaultSpiritDecayPerMinute = 2.5f;
        return Mathf.Max(0f, defaultSpiritDecayPerMinute / 60f);
    }

    float GetGrowthRatePerSecond()
    {
        if (growthController == null)
            return 0f;

        return Mathf.Max(0f, growthController.growthRatePerSecond);
    }

    void HandleZeroOvertimeStress(float delta)
    {
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


    private void InvariantCheck()
    {
        invariantWarnings.Clear();

        if (currentState == YokaiState.Purifying && !IsPurifying)
        {
            string warn = "[INVARIANT] currentState=Purifying but isPurifying=false";
            invariantWarnings.Add(warn);
            YokaiLogger.Warning(warn);
        }

        if (IsPurifying && currentState != YokaiState.Purifying)
        {
            string warn = $"[INVARIANT] isPurifying=true but currentState={currentState}";
            invariantWarnings.Add(warn);
            YokaiLogger.Warning(warn);
        }

        if (IsPurifyCharging && currentState != YokaiState.Purifying)
        {
            string warn = $"[INVARIANT] isPurifyCharging=true but state={currentState}";
            invariantWarnings.Add(warn);
            YokaiLogger.Warning(warn);
        }

        if (isPurityEmpty && currentState != YokaiState.PurityEmpty)
        {
            string warn = $"[INVARIANT] purityEmpty flag=true but state={currentState}";
            invariantWarnings.Add(warn);
            YokaiLogger.Warning(warn);
        }

        if (isSpiritEmpty && currentState != YokaiState.EnergyEmpty)
        {
            string warn = $"[INVARIANT] spiritEmpty flag=true but state={currentState}";
            invariantWarnings.Add(warn);
            YokaiLogger.Warning(warn);
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
