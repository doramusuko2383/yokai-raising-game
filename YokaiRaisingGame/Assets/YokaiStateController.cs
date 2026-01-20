using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    //static void Initialize()
    //{
    //    if (FindObjectOfType<YokaiStateController>() != null)
    //        return;

    //    var controllerObject = new GameObject("YokaiStateController");
    //    controllerObject.AddComponent<YokaiStateController>();
    //    DontDestroyOnLoad(controllerObject);
    //}

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
        ResolveDependencies();
    }

    void Awake()
    {
        isPurifying = false;
        currentState = YokaiState.Normal;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
    }

    void Start()
    {
        StartCoroutine(InitialSync());
    }

    IEnumerator InitialSync()
    {
        yield return null;
        ResolveDependencies();
        SyncManagerState();
        EvaluateState(reason: "InitialSync");
        hasStarted = true;
        if (growthController != null)
        {
            growthController.currentScale = growthController.InitialScale;
            growthController.ApplyScale();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveDependencies();
    }

    void Update()
    {
        HandlePurifyTick();
        if (hasStarted)
            EvaluateState(reason: "Update");
    }

    void ResolveDependencies()
    {
        if (growthController == null || !growthController.gameObject.activeInHierarchy)
        {
            growthController = FindActiveGrowthController();
            if (growthController != null)
                SetActiveYokai(growthController.gameObject);
        }

        if (purityController == null)
            purityController = FindObjectOfType<PurityController>();

        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        RegisterPurityEvents();
        RegisterSpiritEvents();
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
        HandleThresholdReached(ref isSpiritEmpty, "SpiritZero");
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
        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        RegisterPurityEvents();
        RegisterSpiritEvents();

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

        purityController = CurrentYokaiContext.ResolvePurityController();
        spiritController = FindObjectOfType<SpiritController>();
        RegisterPurityEvents();
        RegisterSpiritEvents();

        if (currentState != YokaiState.Evolving)
            evolutionResultPending = false;

        growthController = activeYokai.GetComponent<YokaiGrowthController>();
        SyncManagerState();
        EvaluateState(reason: "ActiveYokaiChanged");
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
        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (spiritController == null || purityController == null)
        {
            Debug.LogWarning("[STATE] Ad recovery failed: manager not found.");
            return;
        }

        spiritController.SetSpiritRatio(0.5f);
        purityController.AddPurityRatio(0.2f);
        EvaluateState(reason: "SpiritAdRecover");
    }

    public void RecoverFromPurityEmptyAd()
    {
        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>();

        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (spiritController == null || purityController == null)
        {
            Debug.LogWarning("[STATE] Ad recovery failed: manager not found.");
            return;
        }

        purityController.SetPurityRatio(0.5f);
        spiritController.AddSpiritRatio(0.2f);
        EvaluateState(reason: "PurityAdRecover");
        BeginPurifying();
    }

    public void OnPurityEmpty()
    {
        HandleThresholdReached(ref isPurityEmpty, "PurityEmpty");
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
        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (!isExplicitRequest)
        {
            if (purityController == null || purityController.purity > 0f)
                return;

            if (currentState != YokaiState.PurityEmpty)
                return;
        }

        if (purityController != null)
            purityController.ExecuteEmergencyPurify();

        EvaluateState(reason: "EmergencyPurify");
        BeginPurifying();
    }

    public bool IsPurityEmpty()
    {
        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

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
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (purityController == null)
            return;

        purifyTimer += Time.deltaTime;
        if (purifyTimer < purifyTickInterval)
            return;

        purifyTimer = 0f;
        purityController.AddPurity(purifyTickAmount);
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

    YokaiGrowthController FindActiveGrowthController()
    {
        var controllers = FindObjectsOfType<YokaiGrowthController>(true);
        foreach (var controller in controllers)
        {
            if (controller != null && controller.gameObject.activeInHierarchy)
                return controller;
        }

        if (controllers.Length == 1)
            return controllers[0];

        return null;
    }

    void LogStateChange(YokaiState previousState, YokaiState nextState, string reason)
    {
        if (!enableStateLogs)
            return;

        string yokaiName = CurrentYokaiContext.CurrentName();
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
