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
    public bool IsEvolving => isEvolving;
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

        ExecuteAction(action, reason);
        return true;
    }

    void ExecuteAction(YokaiAction action, string reason)
    {
        switch (action)
        {
            case YokaiAction.Purify:
                Debug.Log("[LEGACY] Purify action disabled");
                return;

            case YokaiAction.PurifyStart:
                if (reason == null)
                    BeginPurifying();
                else
                    BeginPurifying(reason);
                break;

            case YokaiAction.PurifyCancel:
                if (reason == null)
                    CancelPurifying();
                else
                    CancelPurifying(reason);
                break;

            case YokaiAction.PurifyHoldStart:
                BeginPurifying(reason ?? "PurifyHoldStart");
                break;

            case YokaiAction.PurifyHoldCancel:
                CancelPurifying(reason ?? "PurifyHoldCancel");
                break;

            case YokaiAction.EmergencyPurifyAd:
                ExecuteEmergencyPurify(reason ?? "EmergencyPurify");
                break;

            case YokaiAction.StartEvolution:
                BeginEvolution();
                break;

            case YokaiAction.EmergencySpiritRecover:
                Debug.Log("[YokaiStateController] ExecuteAction EmergencySpiritRecover reached");
                RecoverSpirit();
                break;

            case YokaiAction.EatDango:
                RecoverSpirit();
                break;
            default:
                Debug.LogError($"Unhandled YokaiAction in ExecuteAction: {action}");
                break;
        }
    }

    private void RecoverSpirit()
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
            case YokaiAction.PurifyHoldStart:
            case YokaiAction.PurifyHoldCancel:
                // おきよめ長押しはおきよめ中のみ
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
            case YokaiAction.PurifyHoldStart:
            case YokaiAction.PurifyHoldCancel:
                // [Action Condition] おきよめ長押しはおきよめ中のみ
                return currentState == YokaiState.Purifying && isPurifying;

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

        EvaluateState(reason: "SpiritEmpty");
    }

    public void OnSpiritRecovered()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
        canUseSpecialDango = false;
        EvaluateState(reason: "SpiritRecovered");
    }

    public void ForceReevaluate(string reason)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(reason: reason, forcePresentation: ShouldAllowRebindPresentationSync());
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
        if (isPurifying)
        {
            return YokaiState.Purifying;
        }

        if (requestedState.HasValue)
        {
            if (isPurityEmpty)
                return YokaiState.PurityEmpty;

            if (isSpiritEmpty)
                return YokaiState.EnergyEmpty;

            if ((currentState == YokaiState.Purifying && !isPurifying)
                || currentState == YokaiState.Evolving
                || currentState == YokaiState.EvolutionReady)
                return currentState;

            return YokaiState.Normal;
        }

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
        IsPurifyTriggeredByUser = true;
        HasUserInteracted = false;
        Debug.Log("[PURIFY HOLD] BeginPurifying started (UI will handle charge)");
        EvaluateState(reason: reason, forcePresentation: false);
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

        NotifyPurifySucceeded();
        EvaluateState(reason: "PurifySuccess", forcePresentation: true);
    }

    public void CancelPurifying(string reason = "Cancelled")
    {
        if (!isPurifying)
            return;

        NotifyPurifyCancelled();
        EvaluateState(reason: reason, forcePresentation: true);
    }

    public void BeginEvolution()
    {
        if (currentState != YokaiState.EvolutionReady)
            return;

        isEvolving = true;
        EvaluateState(YokaiState.Evolving, reason: "BeginEvolution");
    }

    public void CompleteEvolution()
    {
        isEvolving = false;
        EvaluateState(YokaiState.Normal, reason: "EvolutionComplete");
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
            SyncManagerState();
            EvaluateState(reason: "FullyInitialized", forcePresentation: allowRebindPresentationSync);
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
            EvaluateState(YokaiState.EvolutionReady, reason: "EvolutionReady");
    }

    public void OnPurityEmpty()
    {
        isPurityEmpty = true;
        isPurifyTriggerReady = true;

        EvaluateState(reason: "PurityEmpty");
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
        EvaluateState(reason: "PurityRecovered");
    }

    void HandleThresholdReached(ref bool stateFlag, string reason)
    {
        if (stateFlag)
            return;

        stateFlag = true;
        EvaluateState(reason: reason);
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
        IsPurifyTriggeredByUser = false;
        SyncManagerState();
        OnPurifyCancelled?.Invoke();
        EvaluateState(reason: "PurifyCancelled", forcePresentation: false);
    }

    public void ExecuteEmergencyPurify(string reason)
    {
        if (currentState != YokaiState.PurityEmpty)
            return;

        Debug.Log("[EMERGENCY] EmergencyPurify requested");

        isPurifying = false;
        IsPurifyTriggeredByUser = false;

        if (purityController != null)
        {
            float recoveredPurity = Mathf.Max(purityController.purity, minimumRecoverValue);
            purityController.SetPurity(recoveredPurity, reason ?? "EmergencyPurify");
        }

        SetState(YokaiState.Normal, "EmergencyPurify");
        RequestEvaluateState("EmergencyPurify");
    }

    void ResetPurifyingState()
    {
        isPurifying = false;
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
        EvaluateState(reason: reason, forcePresentation: false);
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
