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

    [Header("Purify")]
    [SerializeField]
    float purifyTickInterval = 1f;

    [SerializeField]
    float purifyTickAmount = 2f;

    [SerializeField]
    bool enablePurifyTick = false;

    [SerializeField]
    bool enableStateLogs = false;

    float purifyTimer;
    PurityController registeredPurityController;
    SpiritController registeredSpiritController;
    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    const float EvolutionReadyScale = 2.0f;
    bool hasStarted;

    public bool IsSpiritEmpty => isSpiritEmpty;
    public bool IsPurityEmptyState => isPurityEmpty;
    public bool IsEvolving => isEvolving;

    void OnEnable()
    {
        RegisterPurityEvents();
        RegisterSpiritEvents();

        if (!hasStarted)
            StartCoroutine(InitialSync());
    }

    void Awake()
    {
        if (spiritController == null || purityController == null || growthController == null)
            Debug.LogError("[STATE] Dependencies not set in Inspector");
    }

    void OnDisable()
    {
        if (registeredPurityController != null)
        {
            registeredPurityController.EmergencyPurifyRequested -= ExecuteEmergencyPurifyFromButton;
            registeredPurityController.PurityChanged -= OnPurityChanged;
        }

        if (registeredSpiritController != null)
        {
            registeredSpiritController.OnSpiritEmpty -= OnSpiritEmpty;
            registeredSpiritController.OnSpiritRecovered -= OnSpiritRecovered;
        }
    }

    IEnumerator InitialSync()
    {
        yield return null;
        SyncManagerState();
        hasStarted = true;
        if (growthController != null)
        {
            growthController.currentScale = growthController.InitialScale;
            growthController.ApplyScale();
        }
    }

    void Update()
    {
        HandlePurifyTick();
    }

    void RegisterPurityEvents()
    {
        if (registeredPurityController == purityController)
            return;

        if (registeredPurityController != null)
        {
            registeredPurityController.EmergencyPurifyRequested -= ExecuteEmergencyPurifyFromButton;
            registeredPurityController.PurityChanged -= OnPurityChanged;
        }

        registeredPurityController = purityController;

        if (registeredPurityController != null)
        {
            registeredPurityController.EmergencyPurifyRequested += ExecuteEmergencyPurifyFromButton;
            registeredPurityController.PurityChanged += OnPurityChanged;
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
        if (spiritController != null)
            isSpiritEmpty = spiritController.HasNoSpirit();

        if (purityController != null)
            isPurityEmpty = purityController.IsPurityEmpty;
    }

    void OnPurityChanged(float current, float max)
    {
        bool previous = isPurityEmpty;
        if (purityController != null)
            isPurityEmpty = purityController.IsPurityEmpty;

        if (previous != isPurityEmpty)
            EvaluateState(reason: "PurityChanged");
    }

    public void OnSpiritEmpty()
    {
#if UNITY_EDITOR
        Debug.Log("[STATE] SpiritEmpty detected");
#endif
        if (!isSpiritEmpty)
            isSpiritEmpty = true;

        EvaluateState(reason: "SpiritEmpty");
    }

    public void OnSpiritRecovered()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
        EvaluateState(reason: "SpiritRecovered");
    }

    void EvaluateState(YokaiState? requestedState = null, string reason = "Auto")
    {
#if UNITY_EDITOR
        Debug.Log("[STATE] EvaluateState START");
#endif
        if (spiritController == null || purityController == null)
            return;

        YokaiState nextState = DetermineNextState(requestedState);
        SetState(nextState, reason);
    }

    YokaiState DetermineNextState(YokaiState? requestedState = null)
    {
        if (isSpiritEmpty)
        {
            return YokaiState.EnergyEmpty;
        }

        if (isPurityEmpty)
        {
            return YokaiState.PurityEmpty;
        }

        if (requestedState.HasValue)
        {
            return requestedState.Value;
        }

        if (isEvolving)
        {
            return YokaiState.Evolving;
        }

        if (isPurifying)
        {
            return YokaiState.Purifying;
        }

        if (growthController != null && growthController.isEvolutionReady && !IsEvolutionBlocked(out _))
        {
            return YokaiState.EvolutionReady;
        }

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
        purifyTimer = 0f;
        EvaluateState(reason: "BeginPurify");
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
        EvaluateState(reason: "StopPurify");
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
        if (activeYokai == null)
            return;

        if (currentState == YokaiState.Evolving)
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

        SetActiveYokai(activeYokai);
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

    public void ExecuteEmergencyPurify()
    {
        ExecuteEmergencyPurifyInternal(isExplicitRequest: false);
    }

    public void ExecuteEmergencyPurifyFromButton()
    {
        ExecuteEmergencyPurifyInternal(isExplicitRequest: true);
    }

    public void RecoverFromSpiritEmptyAd()
    {
        if (spiritController == null || purityController == null)
        {
            Debug.LogWarning("[STATE] Ad recovery failed: manager not found.");
            return;
        }

        spiritController.SetSpiritRatio(0.5f);
        purityController.AddPurityRatio(0.2f);
    }

    public void RecoverFromPurityEmptyAd()
    {
        if (spiritController == null || purityController == null)
        {
            Debug.LogWarning("[STATE] Ad recovery failed: manager not found.");
            return;
        }

        purityController.SetPurityRatio(0.5f);
        spiritController.AddSpiritRatio(0.2f);
        BeginPurifying();
    }

    public void OnPurityEmpty()
    {
#if UNITY_EDITOR
        Debug.Log("[STATE] PurityEmpty detected");
#endif
        if (!isPurityEmpty)
            isPurityEmpty = true;

        EvaluateState(reason: "PurityEmpty");
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

    void ExecuteEmergencyPurifyInternal(bool isExplicitRequest)
    {
        if (!isExplicitRequest)
        {
            if (purityController == null || purityController.purity > 0f)
                return;

            if (currentState != YokaiState.PurityEmpty)
                return;
        }

        if (purityController != null)
            purityController.ExecuteEmergencyPurify();

        BeginPurifying();
    }

    public bool IsPurityEmpty()
    {
        RegisterPurityEvents();
        if (purityController != null)
            isPurityEmpty = purityController.IsPurityEmpty;
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

    void HandlePurifyTick()
    {
        if (!isPurifying)
            return;

        if (!enablePurifyTick)
            return;

        if (purityController == null)
            return;

        purifyTimer += Time.deltaTime;
        if (purifyTimer < purifyTickInterval)
            return;

        purifyTimer = 0f;
        purityController.AddPurity(-purifyTickAmount);
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
