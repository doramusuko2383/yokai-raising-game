using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public bool isPurifying;
    public event System.Action<YokaiState, YokaiState> OnStateChanged;
    public event System.Action OnPurifySucceeded;
    public event System.Action OnPurifyCancelled;
    public YokaiState CurrentState => currentState;

    bool isSpiritEmpty;
    bool isPurityEmpty;
    bool isEvolving;
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
    YokaiActionExecutor actionExecutor;

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
    public bool IsEvolving => isEvolving;
    public bool IsPurifyCharging => isPurifyCharging;
    public bool IsPurifyTriggerReady => isPurifyTriggerReady;
    public bool CanUseSpecialDango => canUseSpecialDango;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;
    public string LastStateChangeReason => lastStateChangeReason;

    public bool CanDo(YokaiAction action)
    {
        bool isAllowedByState = IsAllowedByState(currentState, action);
        if (!isAllowedByState)
            return false;

        return IsActionConditionSatisfied(action);
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

        actionExecutor.Execute(action, reason);
        return true;
    }

    internal void RecoverSpiritInternal()
    {
        spiritController.AddSpirit(dangoAmount);
        TutorialManager.NotifyDangoUsed();
        MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
        RequestEvaluateState("SpiritRecovered");
    }

    private bool IsAllowedByState(YokaiState state, YokaiAction action)
    {
        // 進化中は基本なにもできない（演出中の誤操作を防ぐ）
        // [State Rule] State のみで決まるルール
        if (state == YokaiState.Evolving)
            return false;

        // 進化待ちは「進化開始」以外なにもできない（あなたの仕様）
        // [State Rule] State のみで決まるルール
        if (state == YokaiState.EvolutionReady)
            return action == YokaiAction.StartEvolution;

        switch (action)
        {
            case YokaiAction.PurifyStart:
                // 通常のおきよめ開始（通常状態のみ）
                return state == YokaiState.Normal;

            case YokaiAction.PurifyCancel:
                // おきよめ中のキャンセル
                return state == YokaiState.Purifying;

            case YokaiAction.PurifyHold:
                // おきよめ長押しはおきよめ中のみ
                return state == YokaiState.Purifying;

            case YokaiAction.PurifyHoldStart:
            case YokaiAction.PurifyHoldCancel:
                return state == YokaiState.Purifying;

            case YokaiAction.EatDango:
                // [State Rule] State のみで決まるルール
                // 通常だんご（通常状態のみ）
                return state == YokaiState.Normal;

            case YokaiAction.EmergencySpiritRecover:
                // [State Rule] State のみで決まるルール
                // 霊力0の救済（EnergyEmpty のみ）
                return state == YokaiState.EnergyEmpty;

            case YokaiAction.EmergencyPurifyAd:
                // 清浄度0の救済（緊急おきよめ）
                return state == YokaiState.PurityEmpty;

            case YokaiAction.StartEvolution:
                // [State Rule] State のみで決まるルール
                // EvolutionReady 以外は上で弾いてるので一応 false
                return false;
        }

        return false;
    }

    private bool IsActionConditionSatisfied(YokaiAction action)
    {
        switch (action)
        {
            case YokaiAction.PurifyStart:
                // [Action Condition] フラグ等が絡むため、将来切り出す予定の条件
                // 通常のおきよめ開始（通常状態のみ）
                return currentState == YokaiState.Normal && !isPurifying;

            case YokaiAction.PurifyCancel:
                // [Action Condition] フラグ等が絡むため、将来切り出す予定の条件
                // おきよめ中のキャンセル
                return currentState == YokaiState.Purifying && isPurifying;

            case YokaiAction.PurifyHold:
                // [Action Condition] おきよめ長押しはおきよめ中のみ
                return currentState == YokaiState.Purifying && isPurifying;

            case YokaiAction.PurifyHoldStart:
                return currentState == YokaiState.Purifying && isPurifying && !isPurifyCharging;

            case YokaiAction.PurifyHoldCancel:
                return currentState == YokaiState.Purifying && isPurifying && isPurifyCharging;

            case YokaiAction.EmergencyPurifyAd:
                // [Action Condition] フラグ等が絡むため、将来切り出す予定の条件
                // 清浄度0の救済（緊急おきよめ）
                return currentState == YokaiState.PurityEmpty && !isPurifying;

        }

        return true;
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
        // 1) 強制状態 (Purifying)
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
        if (isPurifying)
        {
            return YokaiState.Purifying;
        }

        return null;
    }

    YokaiState DetermineRequestedState(YokaiState requestedState)
    {
        _ = requestedState;

        // requestedState 評価時も Empty は最優先
        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        // requestedState がある場合のみ、遷移中/維持中の状態を保持
        if ((currentState == YokaiState.Purifying && !isPurifying)
            || currentState == YokaiState.Evolving
            || currentState == YokaiState.EvolutionReady)
            return currentState;

        return YokaiState.Normal;
    }

    YokaiState DetermineDefaultState()
    {
        // 通常評価
        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        return YokaiState.Normal;
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
        isPurifying = true;
        isPurifyCharging = false;
        IsPurifyTriggeredByUser = true;
        HasUserInteracted = false;
        Debug.Log("[PURIFY HOLD] BeginPurifying started (UI will handle charge)");
        RequestEvaluateState(reason, false);
    }

    public void ConsumePurifyTrigger()
    {
        IsPurifyTriggeredByUser = false;
    }

    public void StopPurifying()
    {
        CancelPurifying("StopPurify");
    }

    public void StopPurifyingForSuccess()
    {
        if (currentState != YokaiState.Purifying)
        {
            Debug.LogWarning($"[PURIFY] StopPurifyingForSuccess ignored. currentState={currentState}");
            return;
        }

        Debug.Log("[PURIFY] StopPurifyingForSuccess -> SetState Normal (PurifySuccess)");

        isPurifyCharging = false;
        NotifyPurifySucceeded();
        RequestEvaluateState("PurifySuccess", true);
    }

    public void CancelPurifying(string reason = "Cancelled")
    {
        if (!isPurifying)
            return;

        isPurifyCharging = false;
        NotifyPurifyCancelled();
        RequestEvaluateState(reason, true);
    }

    public void BeginEvolution()
    {
        if (currentState != YokaiState.EvolutionReady)
            return;

        isEvolving = true;
        RequestEvaluateStateRequested(YokaiState.Evolving, "BeginEvolution", false);
    }

    public void CompleteEvolution()
    {
        isEvolving = false;
        RequestEvaluateStateRequested(YokaiState.Normal, "EvolutionComplete", false);
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

    internal bool IsPurifying => isPurifying;

    internal void SetPurifyCharging(bool value)
    {
        isPurifyCharging = value;
    }

    internal void MarkUserInteracted()
    {
        HasUserInteracted = true;
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
        if (!isPurifying)
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

        isPurifying = false;
        isPurifyCharging = false;
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifySucceeded?.Invoke();
    }

    public void NotifyPurifyCancelled()
    {
        if (!isPurifying)
            return;

        StopPurifyFallback();
        isPurifying = false;
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

        if (isPurifying)
            CompletePurifySuccess("PurifyFallback");
    }

    void CompletePurifySuccess(string reason)
    {
        if (CurrentState != YokaiState.Purifying)
            return;

        StopPurifyingForSuccess();
    }

    void ApplyEmptyStateEffects()
    {
        bool shouldEnableDecay = currentState != YokaiState.EnergyEmpty && currentState != YokaiState.PurityEmpty;
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
