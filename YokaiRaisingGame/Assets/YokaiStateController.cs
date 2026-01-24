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
    bool isSpiritEmpty;
    bool isPurityEmpty;
    bool isEvolving;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiGrowthController growthController;

    [FormerlySerializedAs("kegareManager")]
    [SerializeField]
    PurityController purityController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    const float EvolutionReadyScale = 2.0f;
    bool isReady;
    bool hasWarnedUnknownState;
    Coroutine purifyRoutine;

    bool canEvaluateState =>
        isReady
        && CurrentYokaiContext.Current != null
        && purityController != null
        && spiritController != null;

    public bool IsSpiritEmpty => isSpiritEmpty;
    public bool IsPurityEmptyState => isPurityEmpty;
    public bool IsEvolving => isEvolving;
    public SpiritController SpiritController => spiritController;
    public PurityController PurityController => purityController;

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
            BindControllers(CurrentYokaiContext.Current);
        }
    }

    void OnDisable()
    {
        StopPurifyingInternal();
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

        EvaluateState(reason: "SpiritEmpty", forcePresentation: true);
    }

    public void OnSpiritRecovered()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
        EvaluateState(reason: "SpiritRecovered", forcePresentation: true);
    }

    public void ForceReevaluate(string reason)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(reason: reason, forcePresentation: true);
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
            return;
        }
    }

    YokaiState DetermineNextState(YokaiState? requestedState = null)
    {
        if (!requestedState.HasValue)
        {
            if (isPurityEmpty)
                return YokaiState.PurityEmpty;

            if (isSpiritEmpty)
                return YokaiState.EnergyEmpty;

            if ((currentState == YokaiState.Purifying && isPurifying)
                || currentState == YokaiState.Evolving
                || currentState == YokaiState.EvolutionReady)
                return currentState;

            return YokaiState.Normal;
        }

        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        return requestedState.Value;
    }

    public void SetState(YokaiState newState, string reason)
    {
        if (currentState == newState)
            return;

        var prev = currentState;
        currentState = newState;

        Debug.Log($"[STATE] {prev} -> {newState} ({reason})");
        OnStateChanged?.Invoke(prev, newState);
        CheckForUnknownStateWarning();
    }

    public void BeginPurifying()
    {
        if (currentState != YokaiState.Normal && currentState != YokaiState.PurityEmpty)
            return;

        if (isPurifying)
            return;

        purifyRoutine = StartCoroutine(PurifyRoutine());
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

        if (purifyRoutine != null)
        {
            StopCoroutine(purifyRoutine);
            purifyRoutine = null;
        }

        isPurifying = false;
        EvaluateState(reason: "StopPurify", forcePresentation: true);
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
            SyncManagerState();
            EvaluateState(reason: "FullyInitialized", forcePresentation: true);
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

        RegisterPurityEvents();
        RegisterSpiritEvents();
        isReady = true;

        Debug.Log($"[StateController] Purity={purityController != null}, Spirit={spiritController != null}");
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
        isPurityEmpty = true;

        EvaluateState(reason: "PurityEmpty", forcePresentation: true);
    }

    public void OnPurityRecovered()
    {
        if (!isPurityEmpty)
            return;

        isPurityEmpty = false;
        EvaluateState(reason: "PurityRecovered", forcePresentation: true);
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

    IEnumerator PurifyRoutine()
    {
        isPurifying = true;
        SetState(YokaiState.Purifying, "BeginPurify");

        yield return new WaitForSeconds(2.0f);

        if (purityController != null)
        {
            purityController.RecoverPurityByRatio(0.5f);
        }

        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);

        isPurifying = false;
        purifyRoutine = null;

        EvaluateState(reason: "PurifyFinished", forcePresentation: true);
    }

    public void RequestEvaluateState(string reason)
    {
        if (!canEvaluateState)
            return;

        SyncManagerState();
        EvaluateState(reason: reason, forcePresentation: true);
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
