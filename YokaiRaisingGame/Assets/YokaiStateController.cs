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
    bool isSpiritEmpty;
    bool isPurityEmpty;
    bool isEvolving;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiGrowthController growthController;

    [SerializeField]
    YokaiStatePresentationController presentationController;

    [FormerlySerializedAs("kegareManager")]
    [SerializeField]
    PurityController purityController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    bool enableStateLogs = false;

    PurityController registeredPurityController;
    SpiritController registeredSpiritController;
    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    const float EvolutionReadyScale = 2.0f;
    bool hasInitialized;

    public bool IsSpiritEmpty => isSpiritEmpty;
    public bool IsPurityEmptyState => isPurityEmpty;
    public bool IsEvolving => isEvolving;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;

    void OnEnable()
    {
        CurrentYokaiContext.RegisterStateController(this);
        RegisterPurityEvents();
        RegisterSpiritEvents();
    }

    void Awake()
    {
    }

    void Start()
    {
        SyncManagerState();
        hasInitialized = true;
        EvaluateState(reason: "InitialSync");
    }

    void OnDisable()
    {
        if (registeredPurityController != null)
        {
            registeredPurityController.OnPurityEmpty -= OnPurityEmpty;
            registeredPurityController.OnPurityRecovered -= OnPurityRecovered;
        }

        if (registeredSpiritController != null)
        {
            registeredSpiritController.OnSpiritEmpty -= OnSpiritEmpty;
            registeredSpiritController.OnSpiritRecovered -= OnSpiritRecovered;
        }

        CurrentYokaiContext.UnregisterStateController(this);
    }

    void RegisterPurityEvents()
    {
        if (registeredPurityController == purityController)
            return;

        if (registeredPurityController != null)
        {
            registeredPurityController.OnPurityEmpty -= OnPurityEmpty;
            registeredPurityController.OnPurityRecovered -= OnPurityRecovered;
        }

        registeredPurityController = purityController;

        if (registeredPurityController != null)
        {
            registeredPurityController.OnPurityEmpty += OnPurityEmpty;
            registeredPurityController.OnPurityRecovered += OnPurityRecovered;
        }
    }

    void RegisterSpiritEvents()
    {
        if (registeredSpiritController == spiritController)
            return;

        if (registeredSpiritController != null)
        {
            registeredSpiritController.OnSpiritEmpty -= OnSpiritEmpty;
            registeredSpiritController.OnSpiritRecovered -= OnSpiritRecovered;
        }

        registeredSpiritController = spiritController;

        if (registeredSpiritController != null)
        {
            registeredSpiritController.OnSpiritEmpty += OnSpiritEmpty;
            registeredSpiritController.OnSpiritRecovered += OnSpiritRecovered;
        }
    }

    void SyncManagerState()
    {
        isSpiritEmpty = spiritController != null && spiritController.HasNoSpirit();
        isPurityEmpty = purityController != null && purityController.IsPurityEmpty;

        EvaluateState(null, "SyncManagerState");
    }

    public void OnSpiritEmpty()
    {
#if UNITY_EDITOR
        Debug.Log("[STATE] SpiritEmpty detected");
#endif
        if (isSpiritEmpty)
            return;

        isSpiritEmpty = true;

        EvaluateState(reason: "SpiritEmpty", forcePresentation: true);
    }

    public void OnSpiritRecovered()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
        EvaluateState(reason: "SpiritRecovered");
    }

    void EvaluateState(YokaiState? requestedState = null, string reason = "Auto", bool forcePresentation = false)
    {
#if UNITY_EDITOR
        Debug.Log("[STATE] EvaluateState START");
#endif
        if (!hasInitialized)
            return;

        YokaiState nextState = DetermineNextState(requestedState);
        bool stateChanged = currentState != nextState;
        if (stateChanged)
        {
            SetState(nextState, reason);
            return;
        }

        if (forcePresentation)
        {
            ResolvePresentationController()?.ApplyState(currentState, true);
        }
    }

    YokaiState DetermineNextState(YokaiState? requestedState = null)
    {
        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (requestedState.HasValue)
            return requestedState.Value;

        if (currentState == YokaiState.Purifying
            || currentState == YokaiState.Evolving
            || currentState == YokaiState.EvolutionReady)
            return currentState;

        return YokaiState.Normal;
    }

    public void SetState(YokaiState newState, string reason)
    {
        if (currentState == newState)
            return;

        var prev = currentState;
        currentState = newState;

        if (enableStateLogs)
            Debug.Log($"[STATE] {prev} -> {newState} ({reason})");

#if UNITY_EDITOR
        Debug.Log($"[STATE] StateChanged {prev} -> {currentState}");
#endif
        OnStateChanged?.Invoke(prev, newState);
        LogStateChange(prev, newState, reason);
    }

    public void BeginPurifying()
    {
        if (currentState != YokaiState.Normal && currentState != YokaiState.EnergyEmpty)
            return;

        if (isPurifying)
            return;

        isPurifying = true;
        EvaluateState(YokaiState.Purifying, reason: "BeginPurify");
    }

    public void StopPurifying()
    {
        StopPurifyingInternal();
    }

    public void StopPurifyingForSuccess()
    {
        StopPurifyingInternal();
    }

    void StopPurifyingInternal()
    {
        if (!isPurifying)
            return;

        isPurifying = false;
        EvaluateState(YokaiState.Normal, reason: "StopPurify");
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

    void BindControllers(GameObject activeYokai)
    {
        PurityController nextPurity = FindObjectOfType<PurityController>(true);
        SpiritController nextSpirit = FindObjectOfType<SpiritController>(true);
        YokaiGrowthController nextGrowth = activeYokai?.GetComponentInChildren<YokaiGrowthController>(true);

        purityController = nextPurity;
        spiritController = nextSpirit;
        growthController = nextGrowth;

        RegisterPurityEvents();
        RegisterSpiritEvents();
        SyncManagerState();
    }

    public void SetActiveYokai(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        if (currentState != YokaiState.Evolving)
            evolutionResultPending = false;

        SyncManagerState();
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
#if UNITY_EDITOR
        Debug.Log("[STATE] PurityEmpty detected");
#endif
        if (isPurityEmpty)
            return;

        isPurityEmpty = true;

        EvaluateState(reason: "PurityEmpty", forcePresentation: true);
    }

    public void OnPurityRecovered()
    {
        if (!isPurityEmpty)
            return;

        isPurityEmpty = false;
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

    YokaiStatePresentationController ResolvePresentationController()
    {
        if (presentationController != null)
            return presentationController;

        presentationController = YokaiStatePresentationController.Instance;

        if (presentationController == null)
            presentationController = FindObjectOfType<YokaiStatePresentationController>(true);

        return presentationController;
    }

    void LogStateChange(YokaiState previousState, YokaiState nextState, string reason)
    {
        if (!enableStateLogs)
            return;

        string yokaiName = growthController != null ? growthController.gameObject.name : gameObject.name;
        float currentPurity = purityController != null ? purityController.purity : 0f;
        float maxPurity = purityController != null ? purityController.maxPurity : 0f;
        float currentSpirit = spiritController != null ? spiritController.spirit : 0f;
        float maxSpirit = spiritController != null ? spiritController.maxSpirit : 0f;
#if UNITY_EDITOR
        Debug.Log($"[STATE] {yokaiName} {previousState}->{nextState} reason={reason} spirit={currentSpirit:0.##}/{maxSpirit:0.##} purity={currentPurity:0.##}/{maxPurity:0.##}");
#endif
    }
}
}
