using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public bool isPurifying;
    public event System.Action<YokaiState, YokaiState> OnStateChanged;
    public event System.Action<bool> OnPurifyChanged;
    public event System.Action OnPurifyStarted;
    public event System.Action OnPurifyCanceled;
    public event System.Action<GameObject> OnActiveYokaiChanged;
    bool isEnergyEmpty;
    bool isKegareMax;
    bool isEvolving;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiGrowthController growthController;

    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    EnergyManager energyManager;

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
    KegareManager registeredKegareManager;
    EnergyManager registeredEnergyManager;
    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    const float EvolutionReadyScale = 2.0f;
    bool hasStarted;

    public bool IsEnergyEmpty => isEnergyEmpty;
    public bool IsKegareMax => isKegareMax;
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

        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        RegisterKegareEvents();
        RegisterEnergyEvents();
    }

    void RegisterKegareEvents()
    {
        if (registeredKegareManager == kegareManager)
            return;

        if (registeredKegareManager != null)
        {
            registeredKegareManager.EmergencyPurifyRequested -= ExecuteEmergencyPurifyFromButton;
            registeredKegareManager.KegareChanged -= OnKegareChanged;
        }

        registeredKegareManager = kegareManager;

        if (registeredKegareManager != null)
        {
            registeredKegareManager.EmergencyPurifyRequested += ExecuteEmergencyPurifyFromButton;
            registeredKegareManager.KegareChanged += OnKegareChanged;
        }
    }

    void RegisterEnergyEvents()
    {
        if (registeredEnergyManager == energyManager)
            return;

        if (registeredEnergyManager != null)
        {
            registeredEnergyManager.OnEnergyEmpty -= OnEnergyEmpty;
            registeredEnergyManager.OnEnergyRecovered -= OnEnergyRecovered;
        }

        registeredEnergyManager = energyManager;

        if (registeredEnergyManager != null)
        {
            registeredEnergyManager.OnEnergyEmpty += OnEnergyEmpty;
            registeredEnergyManager.OnEnergyRecovered += OnEnergyRecovered;
        }
    }

    void SyncManagerState()
    {
        if (energyManager != null)
            isEnergyEmpty = energyManager.HasNoEnergy() && energyManager.HasEverHadEnergy;

        if (kegareManager != null)
            isKegareMax = kegareManager.isKegareMax;
    }

    void OnKegareChanged(float current, float max)
    {
        bool previous = isKegareMax;
        if (kegareManager != null)
            isKegareMax = kegareManager.isKegareMax;

        if (previous != isKegareMax)
            EvaluateState(reason: "KegareChanged");
    }

    public void OnEnergyEmpty()
    {
        if (isEnergyEmpty)
            return;

        isEnergyEmpty = true;
        EvaluateState(reason: "EnergyZero");
    }

    public void OnEnergyRecovered()
    {
        if (!isEnergyEmpty)
            return;

        isEnergyEmpty = false;
        EvaluateState(reason: "EnergyRecovered");
    }

    void EvaluateState(YokaiState? requestedState = null, string reason = "Auto")
    {
        if (energyManager == null || kegareManager == null)
            return;

        YokaiState nextState = DetermineNextState(requestedState);
        SetState(nextState, reason);
    }

    YokaiState DetermineNextState(YokaiState? requestedState = null)
    {
        if (isEnergyEmpty)
        {
            return YokaiState.EnergyEmpty;
        }

        if (isKegareMax)
        {
            return YokaiState.KegareMax;
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
        OnPurifyChanged?.Invoke(true);
        OnPurifyStarted?.Invoke();
        EvaluateState(reason: "BeginPurify");
    }

    public void StopPurifying()
    {
        StopPurifyingInternal(playCancelSe: true);
    }

    public void StopPurifyingForSuccess()
    {
        StopPurifyingInternal(playCancelSe: false);
    }

    void StopPurifyingInternal(bool playCancelSe)
    {
        if (!isPurifying)
            return;

        isPurifying = false;
        OnPurifyChanged?.Invoke(false);
        if (playCancelSe)
            OnPurifyCanceled?.Invoke();
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

        kegareManager = CurrentYokaiContext.ResolveKegareManager();
        energyManager = FindObjectOfType<EnergyManager>();
        RegisterKegareEvents();
        RegisterEnergyEvents();

        if (currentState != YokaiState.Evolving)
            evolutionResultPending = false;

        growthController = activeYokai.GetComponent<YokaiGrowthController>();
        SyncManagerState();
        EvaluateState(reason: "ActiveYokaiChanged");
        OnActiveYokaiChanged?.Invoke(activeYokai);
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

    public void OnKegareMax()
    {
        if (isKegareMax)
            return;

        isKegareMax = true;
        EvaluateState(reason: "KegareMax");
    }

    void ExecuteEmergencyPurifyInternal(bool isExplicitRequest)
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (!isExplicitRequest)
        {
            if (kegareManager == null || kegareManager.kegare < kegareManager.maxKegare)
                return;

            if (currentState != YokaiState.KegareMax)
                return;
        }

        if (kegareManager != null)
            kegareManager.ExecuteEmergencyPurify();

        EvaluateState(reason: "EmergencyPurify");
    }

    bool IsPurityEmpty()
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        RegisterKegareEvents();
        if (kegareManager != null)
            isKegareMax = kegareManager.isKegareMax;
        return isKegareMax;
    }

    public bool IsEnergyEmpty()
    {
        return currentState == YokaiState.EnergyEmpty;
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
        bool hasKegareMax = isKegareMax;
        bool hasEnergyEmpty = isEnergyEmpty;
        if (!hasKegareMax && !hasEnergyEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (hasKegareMax && hasEnergyEmpty)
            reason = "穢れMAX / 霊力0";
        else if (hasKegareMax)
            reason = "穢れMAX";
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

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager == null)
            return;

        purifyTimer += Time.deltaTime;
        if (purifyTimer < purifyTickInterval)
            return;

        purifyTimer = 0f;
        kegareManager.AddKegare(-purifyTickAmount);
    }

    public void EnterEnergyEmpty()
    {
        SetState(YokaiState.EnergyEmpty, "EnergyZero");
    }

    public void EnterKegareMax()
    {
        SetState(YokaiState.KegareMax, "KegareMax");
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
        float currentKegare = kegareManager != null ? kegareManager.kegare : 0f;
        float maxKegare = kegareManager != null ? kegareManager.maxKegare : 0f;
        float currentEnergy = energyManager != null ? energyManager.energy : 0f;
        float maxEnergy = energyManager != null ? energyManager.maxEnergy : 0f;
#if UNITY_EDITOR
        Debug.Log($"[STATE] {yokaiName} {previousState}->{nextState} reason={reason} energy={currentEnergy:0.##}/{maxEnergy:0.##} kegare={currentKegare:0.##}/{maxKegare:0.##}");
#endif
    }
}
}
