using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;
    public bool isPurifying;
    public event System.Action<YokaiState, YokaiState> StateChanged;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiGrowthController growthController;

    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    EnergyManager energyManager;

    [Header("UI")]
    [SerializeField]
    GameObject actionPanel;

    [SerializeField]
    GameObject emergencyPurifyButton;

    [SerializeField]
    GameObject purifyStopButton;

    [SerializeField]
    GameObject magicCircleOverlay;

    [SerializeField]
    CanvasGroup dangerOverlay;

    [Header("Danger Effect")]
    [SerializeField]
    YokaiDangerEffect[] dangerEffects;

    [Header("Kegare Max Visuals")]
    [SerializeField]
    float kegareMaxOverlayAlpha = 0.2f;

    [SerializeField]
    float kegareMaxDarkenIntensity = 0.2f;

    [SerializeField]
    float kegareMaxReleaseDelay = 0.15f;

    [SerializeField]
    float kegareMaxWobbleScale = 0.02f;

    [SerializeField]
    float kegareMaxWobbleSpeed = 2.6f;

    [SerializeField]
    float kegareMaxJitterAmplitude = 0.015f;

    [Header("Purify")]
    [SerializeField]
    float purifyTickInterval = 1f;

    [SerializeField]
    float purifyTickAmount = 2f;

    [SerializeField]
    bool enablePurifyTick = false;

    [Header("Energy Empty Visuals")]
    [SerializeField]
    float energyEmptyAlpha = 0.45f;

    [SerializeField]
    bool enableStateLogs = false;

    [SerializeField] private Button specialDangoButton;

    float purifyTimer;
    KegareManager registeredKegareManager;
    EnergyManager registeredEnergyManager;
    bool evolutionResultPending;
    YokaiEvolutionStage evolutionResultStage;
    GameObject energyEmptyTargetRoot;
    GameObject kegareMaxTargetRoot;
    bool lastEnergyEmpty;
    readonly Dictionary<SpriteRenderer, Color> energyEmptySpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> energyEmptyImageColors = new Dictionary<Image, Color>();
    readonly Dictionary<SpriteRenderer, Color> kegareMaxSpriteColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<Image, Color> kegareMaxImageColors = new Dictionary<Image, Color>();
    Vector3 kegareMaxBasePosition;
    Vector3 kegareMaxBaseScale;
    float kegareMaxNoiseSeed;
    bool isKegareMaxVisualsActive;
    bool isKegareMaxMotionApplied;
    Coroutine kegareMaxReleaseRoutine;
    const float EvolutionReadyScale = 2.0f;
    bool hasLoggedDependencies;
    bool hasStarted;

    public bool IsKegareMaxVisualsActive => isKegareMaxVisualsActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    //static void Initialize()
   // {
    //    if (FindObjectOfType<YokaiStateController>() != null)
    //        return;

   //    var controllerObject = new GameObject("YokaiStateController");
   //     controllerObject.AddComponent<YokaiStateController>();
   //     DontDestroyOnLoad(controllerObject);
   // }

    void OnEnable()
    {
        Debug.Log($"[YokaiStateController][OnEnable][Enter] currentState={currentState} isPurifying={isPurifying}");
        Debug.Log("[YokaiStateController][OnEnable][ENTER] currentState=" + currentState + " isPurifying=" + isPurifying + " energyManager=" + (energyManager == null ? "null" : "ok") + " kegareManager=" + (kegareManager == null ? "null" : "ok"));
        SceneManager.sceneLoaded += OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
        ResolveDependencies();
        Debug.Log("[YokaiStateController][OnEnable][Exit]");
        Debug.Log("[YokaiStateController][OnEnable][EXIT] currentState=" + currentState + " isPurifying=" + isPurifying);
    }

    void Awake()
    {
        Debug.Log($"[YokaiStateController][Awake][Enter] currentState={currentState} isPurifying={isPurifying}");
        Debug.Log("[YokaiStateController][Awake][ENTER] currentState=" + currentState + " isPurifying=" + isPurifying);
        isPurifying = false;
        currentState = YokaiState.Normal;
        Debug.Log($"[YokaiStateController][Awake][Exit] currentState={currentState} isPurifying={isPurifying}");
        Debug.Log("[YokaiStateController][Awake][EXIT] currentState=" + currentState + " isPurifying=" + isPurifying);
    }

    void OnDisable()
    {
        Debug.Log("[YokaiStateController][OnDisable][Enter]");
        Debug.Log("[YokaiStateController][OnDisable][ENTER] currentState=" + currentState + " isPurifying=" + isPurifying);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
        Debug.Log("[YokaiStateController][OnDisable][Exit]");
        Debug.Log("[YokaiStateController][OnDisable][EXIT] currentState=" + currentState + " isPurifying=" + isPurifying);
    }

    void Start()
    {
        Debug.Log("[YokaiStateController][Start][Enter]");
        Debug.Log("[YokaiStateController][Start][ENTER] hasStarted=" + hasStarted);
        StartCoroutine(InitialSync());
        Debug.Log("[YokaiStateController][Start][Exit]");
        Debug.Log("[YokaiStateController][Start][EXIT] hasStarted=" + hasStarted);
    }

    IEnumerator InitialSync()
    {
        Debug.Log("[YokaiStateController][InitialSync][Enter]");
        yield return null;
        ResolveDependencies();
        ApplyStateFromManagers();
        hasStarted = true;
        Debug.Log("[YokaiStateController][InitialSync][Exit]");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[YokaiStateController][OnSceneLoaded][Enter] scene={scene.name} mode={mode}");
        Debug.Log("[YokaiStateController][OnSceneLoaded][ENTER] scene=" + scene.name + " mode=" + mode + " currentState=" + currentState);
        ResolveDependencies();
        Debug.Log("[YokaiStateController][OnSceneLoaded][Exit]");
        Debug.Log("[YokaiStateController][OnSceneLoaded][EXIT] currentState=" + currentState);
    }

    void Update()
    {
        Debug.Log($"[YokaiStateController][Update][Enter] currentState={currentState} isPurifying={isPurifying} hasStarted={hasStarted}");
        Debug.Log("[YokaiStateController][Update][ENTER] currentState=" + currentState + " isPurifying=" + isPurifying + " hasStarted=" + hasStarted);
        HandlePurifyTick();
        if (hasStarted)
            ApplyStateFromManagers();
        Debug.Log($"[YokaiStateController][Update][Exit] currentState={currentState} isPurifying={isPurifying}");
        Debug.Log("[YokaiStateController][Update][EXIT] currentState=" + currentState + " isPurifying=" + isPurifying);
    }

    void LateUpdate()
    {
        Debug.Log("[YokaiStateController][LateUpdate][Enter]");
        UpdateKegareMaxMotion();
        Debug.Log("[YokaiStateController][LateUpdate][Exit]");
    }

    void ResolveDependencies()
    {
        Debug.Log("[YokaiStateController][ResolveDependencies][Enter]");
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

        if (actionPanel == null)
            actionPanel = GameObject.Find("UI_Action");

        if (emergencyPurifyButton == null)
            emergencyPurifyButton = GameObject.Find("Button_MononokeHeal");

        if (purifyStopButton == null)
            purifyStopButton = GameObject.Find("Btn_StopPurify");

        if (magicCircleOverlay == null)
            magicCircleOverlay = GameObject.Find("MagicCircleImage");

        if (dangerOverlay == null)
        {
            var dangerObject = GameObject.Find("Overlay_Danger");
            if (dangerObject != null)
                dangerOverlay = dangerObject.GetComponent<CanvasGroup>();
        }

        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);

        if (!hasLoggedDependencies)
        {
            LogDependencyState("ResolveDependencies");
            hasLoggedDependencies = true;
        }
        Debug.Log("[YokaiStateController][ResolveDependencies][Exit]");
    }

    void RegisterKegareEvents()
    {
        if (registeredKegareManager == kegareManager)
        {
            Debug.Log("[YokaiStateController][RegisterKegareEvents][EarlyReturn] reason=alreadyRegistered");
            Debug.Log("[YokaiStateController][RegisterKegareEvents][EARLY_RETURN] reason=alreadyRegistered kegareManager=" + (kegareManager == null ? "null" : "ok"));
            return;
        }

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
        Debug.Log("[YokaiStateController][RegisterKegareEvents][Exit]");
    }

    void RegisterEnergyEvents()
    {
        if (registeredEnergyManager == energyManager)
        {
            Debug.Log("[YokaiStateController][RegisterEnergyEvents][EarlyReturn] reason=alreadyRegistered");
            Debug.Log("[YokaiStateController][RegisterEnergyEvents][EARLY_RETURN] reason=alreadyRegistered energyManager=" + (energyManager == null ? "null" : "ok"));
            return;
        }

        if (registeredEnergyManager != null)
            registeredEnergyManager.EnergyChanged -= OnEnergyChanged;

        registeredEnergyManager = energyManager;

        if (registeredEnergyManager != null)
            registeredEnergyManager.EnergyChanged += OnEnergyChanged;
        Debug.Log("[YokaiStateController][RegisterEnergyEvents][Exit]");
    }

    void OnKegareChanged(float current, float max)
    {
        Debug.Log($"[YokaiStateController][OnKegareChanged][Enter] current={current:0.##} max={max:0.##}");
        Debug.Log("[YokaiStateController][OnKegareChanged][ENTER] current=" + current.ToString("0.##") + " max=" + max.ToString("0.##") + " currentState=" + currentState);
        Debug.Log("[YokaiStateController][OnKegareChanged][Exit]");
        Debug.Log("[YokaiStateController][OnKegareChanged][EXIT] currentState=" + currentState);
    }

    void OnEnergyChanged(float current, float max)
    {
        Debug.Log($"[YokaiStateController][OnEnergyChanged][Enter] current={current:0.##} max={max:0.##} hasStarted={hasStarted}");
        Debug.Log("[YokaiStateController][OnEnergyChanged][ENTER] current=" + current.ToString("0.##") + " max=" + max.ToString("0.##") + " hasStarted=" + hasStarted + " currentState=" + currentState);
        if (!hasStarted)
        {
            Debug.Log("[YokaiStateController][OnEnergyChanged][EarlyReturn] reason=notStarted");
            Debug.Log("[YokaiStateController][OnEnergyChanged][EARLY_RETURN] reason=notStarted currentState=" + currentState);
            return;
        }

        ApplyStateFromManagers();
        Debug.Log("[YokaiStateController][OnEnergyChanged][Exit]");
        Debug.Log("[YokaiStateController][OnEnergyChanged][EXIT] currentState=" + currentState);
    }

    void ApplyStateFromManagers(YokaiState? requestedState = null, bool forceApplyUI = false)
    {
        Debug.Log($"[YokaiStateController][ApplyStateFromManagers][Enter] requestedState={(requestedState.HasValue ? requestedState.Value.ToString() : "null")} forceApplyUI={forceApplyUI} currentState={currentState}");
        if (energyManager == null || kegareManager == null)
        {
            Debug.Log("[YokaiStateController][ApplyStateFromManagers][EarlyReturn] reason=missingManagers");
            Debug.Log("[YokaiStateController][ApplyStateFromManagers][EARLY_RETURN] reason=missingManagers energyManager=" + (energyManager == null ? "null" : "ok") + " kegareManager=" + (kegareManager == null ? "null" : "ok"));
            return;
        }

        bool isEnergyEmpty = IsEnergyEmpty();

        YokaiState nextState = currentState;
        if (requestedState.HasValue)
        {
            nextState = requestedState.Value;
        }
        else if (currentState == YokaiState.Evolving)
        {
            nextState = YokaiState.Evolving;
        }
        else if (isPurifying)
        {
            nextState = YokaiState.Purifying;
        }
        else if (kegareManager.kegare >= kegareManager.maxKegare)
        {
            nextState = YokaiState.KegareMax;
        }
        else if (growthController != null && growthController.isEvolutionReady && !IsEvolutionBlocked(out _))
        {
            nextState = YokaiState.EvolutionReady;
        }
        else
        {
            nextState = YokaiState.Normal;
        }

        bool stateChanged = SetState(nextState);
        if (stateChanged || forceApplyUI || isEnergyEmpty != lastEnergyEmpty)
            ApplyStateUI();

        lastEnergyEmpty = isEnergyEmpty;
        Debug.Log($"[YokaiStateController][ApplyStateFromManagers][Exit] nextState={currentState} stateChanged={stateChanged} isEnergyEmpty={isEnergyEmpty}");
    }

    bool SetState(YokaiState newState)
    {
        Debug.Log($"[YokaiStateController][SetState][Enter] currentState={currentState} newState={newState}");
        if (currentState == newState)
        {
            Debug.Log("[YokaiStateController][SetState][EarlyReturn] reason=stateUnchanged");
            Debug.Log("[YokaiStateController][SetState][EARLY_RETURN] reason=stateUnchanged currentState=" + currentState);
            return false;
        }

        YokaiState previousState = currentState;
        currentState = newState;
        if (currentState == YokaiState.Purifying)
            isPurifying = true;
        if (previousState == YokaiState.Purifying && currentState != YokaiState.Purifying)
            isPurifying = false;
        if (currentState == YokaiState.Purifying)
            purifyTimer = 0f;
        StateChanged?.Invoke(previousState, currentState);

        if (enableStateLogs)
        {
            if (currentState == YokaiState.KegareMax && previousState != YokaiState.KegareMax)
                Debug.Log("[STATE] 穢れMAX ON");
            else if (previousState == YokaiState.KegareMax && currentState != YokaiState.KegareMax)
                Debug.Log("[STATE] 穢れMAX OFF");

        }

        HandleStateSeTransitions(previousState, currentState);
        LogStateContext("StateChange");
        Debug.Log($"[YokaiStateController][SetState][Exit] previousState={previousState} currentState={currentState}");
        return true;
    }

    public void BeginPurifying()
    {
        Debug.Log($"[YokaiStateController][BeginPurifying][Enter] currentState={currentState}");
        if (currentState != YokaiState.Normal)
        {
            Debug.Log("[YokaiStateController][BeginPurifying][EarlyReturn] reason=stateNotNormal");
            Debug.Log("[YokaiStateController][BeginPurifying][EARLY_RETURN] reason=stateNotNormal currentState=" + currentState);
            return;
        }

        isPurifying = true;
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
        ApplyStateFromManagers();
        Debug.Log("[YokaiStateController][BeginPurifying][Exit]");
    }

    public void StopPurifying()
    {
        Debug.Log("[YokaiStateController][StopPurifying][Enter]");
        StopPurifyingInternal(playCancelSe: true);
        Debug.Log("[YokaiStateController][StopPurifying][Exit]");
    }

    public void StopPurifyingForSuccess()
    {
        Debug.Log("[YokaiStateController][StopPurifyingForSuccess][Enter]");
        StopPurifyingInternal(playCancelSe: false);
        Debug.Log("[YokaiStateController][StopPurifyingForSuccess][Exit]");
    }

    void StopPurifyingInternal(bool playCancelSe)
    {
        Debug.Log($"[YokaiStateController][StopPurifyingInternal][Enter] currentState={currentState} playCancelSe={playCancelSe}");
        if (currentState != YokaiState.Purifying)
        {
            Debug.Log("[YokaiStateController][StopPurifyingInternal][EarlyReturn] reason=stateNotPurifying");
            Debug.Log("[YokaiStateController][StopPurifyingInternal][EARLY_RETURN] reason=stateNotPurifying currentState=" + currentState);
            return;
        }

        if (playCancelSe)
            AudioHook.RequestPlay(YokaiSE.SE_PURIFY_CANCEL);

        isPurifying = false;
        ApplyStateFromManagers();
        Debug.Log("[YokaiStateController][StopPurifyingInternal][Exit]");
    }

    public void BeginEvolution()
    {
        Debug.Log($"[YokaiStateController][BeginEvolution][Enter] currentState={currentState}");
        if (currentState != YokaiState.EvolutionReady)
        {
            Debug.Log("[YokaiStateController][BeginEvolution][EarlyReturn] reason=stateNotEvolutionReady");
            Debug.Log("[YokaiStateController][BeginEvolution][EARLY_RETURN] reason=stateNotEvolutionReady currentState=" + currentState);
            return;
        }

        ApplyStateFromManagers(YokaiState.Evolving, forceApplyUI: true);
        Debug.Log("[YokaiStateController][BeginEvolution][Exit]");
    }

    public void CompleteEvolution()
    {
        Debug.Log("[YokaiStateController][CompleteEvolution][Enter]");
        ApplyStateFromManagers(YokaiState.Normal, forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
        Debug.Log("[YokaiStateController][CompleteEvolution][Exit]");
    }

    public void BindCurrentYokai(GameObject activeYokai)
    {
        Debug.Log($"[YokaiStateController][BindCurrentYokai][Enter] activeYokai={(activeYokai == null ? "null" : activeYokai.name)} currentState={currentState}");
        Debug.Log("[YokaiStateController][BindCurrentYokai][ENTER] activeYokai=" + (activeYokai == null ? "null" : activeYokai.name) + " currentState=" + currentState);
        if (activeYokai == null)
        {
            Debug.Log("[YokaiStateController][BindCurrentYokai][EarlyReturn] reason=activeYokai null");
            Debug.Log("[YokaiStateController][BindCurrentYokai][EARLY_RETURN] reason=activeYokai null currentState=" + currentState);
            return;
        }

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
        LogStateContext("Bind");
        Debug.Log("[YokaiStateController][BindCurrentYokai][Exit]");
    }

    public void SetActiveYokai(GameObject activeYokai)
    {
        Debug.Log($"[YokaiStateController][SetActiveYokai][Enter] activeYokai={(activeYokai == null ? "null" : activeYokai.name)}");
        if (activeYokai == null)
        {
            Debug.Log("[YokaiStateController][SetActiveYokai][EarlyReturn] reason=activeYokai null");
            Debug.Log("[YokaiStateController][SetActiveYokai][EARLY_RETURN] reason=activeYokai null");
            return;
        }

        kegareManager = CurrentYokaiContext.ResolveKegareManager();
        energyManager = FindObjectOfType<EnergyManager>();
        RegisterKegareEvents();
        RegisterEnergyEvents();

        if (currentState != YokaiState.Evolving)
            evolutionResultPending = false;
        isKegareMaxVisualsActive = false;
        isKegareMaxMotionApplied = false;
        kegareMaxTargetRoot = null;
        energyEmptyTargetRoot = null;
        energyEmptySpriteColors.Clear();
        energyEmptyImageColors.Clear();
        kegareMaxSpriteColors.Clear();
        kegareMaxImageColors.Clear();
        growthController = activeYokai.GetComponent<YokaiGrowthController>();
        dangerEffects = activeYokai.GetComponentsInChildren<YokaiDangerEffect>(true);
        CacheEnergyEmptyTargets(activeYokai);
        CacheKegareMaxTargets(activeYokai);
        LogStateContext("Active");
        Debug.Log("[YokaiStateController][SetActiveYokai][Exit]");
    }

    public void SetEvolutionReady()
    {
        Debug.Log($"[YokaiStateController][SetEvolutionReady][Enter] currentState={currentState}");
        if (currentState == YokaiState.Evolving)
        {
            Debug.Log("[YokaiStateController][SetEvolutionReady][EarlyReturn] reason=alreadyEvolving");
            Debug.Log("[YokaiStateController][SetEvolutionReady][EARLY_RETURN] reason=alreadyEvolving currentState=" + currentState);
            return;
        }

        if (IsEvolutionBlocked(out string reason))
        {
            Debug.Log($"[EVOLUTION] Ready blocked. reason={reason}");
            Debug.Log($"[YokaiStateController][SetEvolutionReady][EarlyReturn] reason=blocked detail={reason}");
            Debug.Log("[YokaiStateController][SetEvolutionReady][EARLY_RETURN] reason=blocked detail=" + reason);
            return;
        }

        if (!HasReachedEvolutionScale())
        {
            Debug.Log("[EVOLUTION] Ready blocked. reason=Scale");
            Debug.Log("[YokaiStateController][SetEvolutionReady][EarlyReturn] reason=scaleNotReached");
            Debug.Log("[YokaiStateController][SetEvolutionReady][EARLY_RETURN] reason=scaleNotReached currentState=" + currentState);
            return;
        }

        if (!IsKegareMax())
            ApplyStateFromManagers(YokaiState.EvolutionReady, forceApplyUI: true);
        // DEBUG: EvolutionReady になったことを明示してタップ可能を知らせる
        Debug.Log("[EVOLUTION] Ready. Tap the yokai to evolve.");
        Debug.Log("[YokaiStateController][SetEvolutionReady][Exit]");
    }

    public void ExecuteEmergencyPurify()
    {
        Debug.Log("[YokaiStateController][ExecuteEmergencyPurify][Enter]");
        ExecuteEmergencyPurifyInternal(isExplicitRequest: false);
        Debug.Log("[YokaiStateController][ExecuteEmergencyPurify][Exit]");
    }

    public void ExecuteEmergencyPurifyFromButton()
    {
        Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyFromButton][Enter]");
        ExecuteEmergencyPurifyInternal(isExplicitRequest: true);
        Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyFromButton][Exit]");
    }

    void ExecuteEmergencyPurifyInternal(bool isExplicitRequest)
    {
        Debug.Log($"[YokaiStateController][ExecuteEmergencyPurifyInternal][Enter] isExplicitRequest={isExplicitRequest}");
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (!isExplicitRequest)
        {
            if (kegareManager == null || kegareManager.kegare < kegareManager.maxKegare)
            {
                Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyInternal][EarlyReturn] reason=notKegareMax");
                Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyInternal][EARLY_RETURN] reason=notKegareMax kegareManager=" + (kegareManager == null ? "null" : "ok"));
                return;
            }

            if (currentState != YokaiState.KegareMax)
            {
                Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyInternal][EarlyReturn] reason=stateNotKegareMax");
                Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyInternal][EARLY_RETURN] reason=stateNotKegareMax currentState=" + currentState);
                return;
            }
        }

        if (kegareManager != null)
            kegareManager.ExecuteEmergencyPurify();

        ApplyStateFromManagers();
        Debug.Log("[YokaiStateController][ExecuteEmergencyPurifyInternal][Exit]");
    }

    bool IsKegareMax()
    {
        Debug.Log("[YokaiStateController][IsKegareMax][Enter]");
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        RegisterKegareEvents();
        Debug.Log($"[YokaiStateController][IsKegareMax][Exit] isKegareMax={(kegareManager != null && kegareManager.isKegareMax)}");
        return kegareManager != null && kegareManager.isKegareMax;
    }

    public bool IsEnergyEmpty()
    {
        Debug.Log("[YokaiStateController][IsEnergyEmpty][Enter]");
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager == null)
        {
            Debug.Log("[YokaiStateController][IsEnergyEmpty][EarlyReturn] reason=energyManager null");
            Debug.Log("[YokaiStateController][IsEnergyEmpty][EARLY_RETURN] reason=energyManager null");
            return false;
        }

        Debug.Log($"[YokaiStateController][IsEnergyEmpty][Exit] hasEverHadEnergy={energyManager.HasEverHadEnergy} energy={energyManager.energy:0.##}");
        return energyManager.HasEverHadEnergy && energyManager.energy <= 0f;
    }

    bool HasReachedEvolutionScale()
    {
        if (growthController == null)
        {
            Debug.Log("[YokaiStateController][HasReachedEvolutionScale][EarlyReturn] reason=growthController null");
            Debug.Log("[YokaiStateController][HasReachedEvolutionScale][EARLY_RETURN] reason=growthController null");
            return false;
        }

        float scale = growthController.currentScale;
        Debug.Log($"[YokaiStateController][HasReachedEvolutionScale][Exit] scale={scale:0.##} threshold={EvolutionReadyScale:0.##}");
        return scale >= EvolutionReadyScale;
    }

    bool IsEvolutionBlocked(out string reason)
    {
        Debug.Log("[YokaiStateController][IsEvolutionBlocked][Enter]");
        bool isKegareMax = IsKegareMax();
        bool isEnergyEmpty = IsEnergyEmpty();
        if (!isKegareMax && !isEnergyEmpty)
        {
            reason = string.Empty;
            Debug.Log("[YokaiStateController][IsEvolutionBlocked][Exit] blocked=false");
            return false;
        }

        if (isKegareMax && isEnergyEmpty)
            reason = "穢れMAX / 霊力0";
        else if (isKegareMax)
            reason = "穢れMAX";
        else
            reason = "霊力0";

        Debug.Log($"[YokaiStateController][IsEvolutionBlocked][Exit] blocked=true reason={reason}");
        return true;
    }

    void HandlePurifyTick()
    {
        if (!isPurifying)
        {
            Debug.Log("[YokaiStateController][HandlePurifyTick][EarlyReturn] reason=isPurifying false");
            Debug.Log("[YokaiStateController][HandlePurifyTick][EARLY_RETURN] reason=isPurifying false");
            return;
        }

        if (!enablePurifyTick)
        {
            Debug.Log("[YokaiStateController][HandlePurifyTick][EarlyReturn] reason=enablePurifyTick false");
            Debug.Log("[YokaiStateController][HandlePurifyTick][EARLY_RETURN] reason=enablePurifyTick false");
            return;
        }

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager == null)
        {
            Debug.Log("[YokaiStateController][HandlePurifyTick][EarlyReturn] reason=kegareManager null");
            Debug.Log("[YokaiStateController][HandlePurifyTick][EARLY_RETURN] reason=kegareManager null");
            return;
        }

        purifyTimer += Time.deltaTime;
        if (purifyTimer < purifyTickInterval)
        {
            Debug.Log($"[YokaiStateController][HandlePurifyTick][EarlyReturn] reason=intervalNotReached purifyTimer={purifyTimer:0.##} interval={purifyTickInterval:0.##}");
            Debug.Log("[YokaiStateController][HandlePurifyTick][EARLY_RETURN] reason=intervalNotReached purifyTimer=" + purifyTimer.ToString("0.##") + " interval=" + purifyTickInterval.ToString("0.##"));
            return;
        }

        purifyTimer = 0f;
        kegareManager.AddKegare(-purifyTickAmount);
        LogStateContext("PurifyTick");
        Debug.Log("[YokaiStateController][HandlePurifyTick][Exit]");
    }

    void ApplyStateUI()
    {
        Debug.Log("[YokaiStateController][ApplyStateUI][Enter]");
        if (!AreDependenciesResolved())
        {
            Debug.Log("[YokaiStateController][ApplyStateUI][EarlyReturn] reason=dependenciesNotResolved");
            Debug.Log("[YokaiStateController][ApplyStateUI][EARLY_RETURN] reason=dependenciesNotResolved");
            return;
        }

        bool isKegareMax = currentState == YokaiState.KegareMax;
        bool showKegareMaxVisuals = isKegareMaxVisualsActive;
        bool isEnergyEmpty = IsEnergyEmpty();
        // 不具合②: 霊力0の時は通常だんご/おきよめパネルを隠し、特別だんごのみを表示する。
        bool showActionPanel =
            currentState == YokaiState.Normal
            || currentState == YokaiState.EvolutionReady
            || isKegareMax;
        bool showEmergency = isKegareMax;
        bool showMagicCircle = currentState == YokaiState.Purifying;
        bool showStopPurify = isPurifying;
        bool showDangerOverlay = showKegareMaxVisuals;

        ApplyCanvasGroup(actionPanel, showActionPanel, showActionPanel);
        ApplyCanvasGroup(emergencyPurifyButton, showEmergency, showEmergency);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);
        if (magicCircleOverlay != null)
            magicCircleOverlay.SetActive(showMagicCircle);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? Mathf.Clamp01(kegareMaxOverlayAlpha) : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateActionPanelButtons(isKegareMax, isEnergyEmpty);
        UpdateEnergyEmptyVisuals(isEnergyEmpty);
        UpdateDangerEffects();
        UpdateKegareMaxVisuals(showKegareMaxVisuals);
        Debug.Log($"[YokaiStateController][ApplyStateUI][Exit] isKegareMax={isKegareMax} isEnergyEmpty={isEnergyEmpty}");
    }

    bool AreDependenciesResolved()
    {
        if (CurrentYokaiContext.Current == null)
        {
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EarlyReturn] reason=CurrentYokaiContext.Current null");
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EARLY_RETURN] reason=CurrentYokaiContext.Current null");
            return false;
        }

        if (energyManager == null || kegareManager == null)
        {
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EarlyReturn] reason=manager null");
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EARLY_RETURN] reason=manager null energyManager=" + (energyManager == null ? "null" : "ok") + " kegareManager=" + (kegareManager == null ? "null" : "ok"));
            return false;
        }

        if (actionPanel == null || emergencyPurifyButton == null || purifyStopButton == null || magicCircleOverlay == null)
        {
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EarlyReturn] reason=ui null");
            Debug.Log("[YokaiStateController][AreDependenciesResolved][EARLY_RETURN] reason=ui null actionPanel=" + (actionPanel == null ? "null" : "ok") + " emergencyPurifyButton=" + (emergencyPurifyButton == null ? "null" : "ok") + " purifyStopButton=" + (purifyStopButton == null ? "null" : "ok") + " magicCircleOverlay=" + (magicCircleOverlay == null ? "null" : "ok"));
            return false;
        }

        Debug.Log("[YokaiStateController][AreDependenciesResolved][Exit] resolved=true");
        return true;
    }

    void UpdateActionPanelButtons(bool isKegareMax, bool isEnergyEmpty)
    {
        Debug.Log($"[YokaiStateController][UpdateActionPanelButtons][Enter] isKegareMax={isKegareMax} isEnergyEmpty={isEnergyEmpty} actionPanel={(actionPanel == null ? \"null\" : \"ok\")}");
        if (actionPanel == null)
        {
            Debug.Log("[YokaiStateController][UpdateActionPanelButtons][EarlyReturn] reason=actionPanel null");
            Debug.Log("[YokaiStateController][UpdateActionPanelButtons][EARLY_RETURN] reason=actionPanel null");
            return;
        }

        var buttons = actionPanel.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            bool isEmergency =
                emergencyPurifyButton != null &&
                button.gameObject == emergencyPurifyButton;

            bool isSpecialDango =
                specialDangoButton != null &&
                button == specialDangoButton;

            bool shouldShow;

            if (isKegareMax)
            {
                shouldShow = isEmergency;
            }
            else if (isEnergyEmpty)
            {
                shouldShow = isSpecialDango;
            }
            else
            {
                shouldShow = !isEmergency;
            }

            ApplyCanvasGroup(button.gameObject, shouldShow, shouldShow);
            button.interactable = shouldShow;
            button.enabled = shouldShow;
        }
        Debug.Log("[YokaiStateController][UpdateActionPanelButtons][Exit]");
    }

    void ApplyCanvasGroup(GameObject target, bool visible, bool interactable)
    {
        Debug.Log($"[YokaiStateController][ApplyCanvasGroup][Enter] target={(target == null ? \"null\" : target.name)} visible={visible} interactable={interactable}");
        if (target == null)
        {
            Debug.Log("[YokaiStateController][ApplyCanvasGroup][EarlyReturn] reason=target null");
            Debug.Log("[YokaiStateController][ApplyCanvasGroup][EARLY_RETURN] reason=target null");
            return;
        }

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;

        var selectable = target.GetComponent<Selectable>();
        if (selectable != null)
            selectable.interactable = interactable;
        Debug.Log("[YokaiStateController][ApplyCanvasGroup][Exit]");
    }

    YokaiGrowthController FindActiveGrowthController()
    {
        Debug.Log("[YokaiStateController][FindActiveGrowthController][Enter]");
        var controllers = FindObjectsOfType<YokaiGrowthController>(true);
        foreach (var controller in controllers)
        {
            if (controller != null && controller.gameObject.activeInHierarchy)
            {
                Debug.Log($"[YokaiStateController][FindActiveGrowthController][Exit] result={controller.name}");
                return controller;
            }
        }

        if (controllers.Length == 1)
        {
            Debug.Log($"[YokaiStateController][FindActiveGrowthController][Exit] result={controllers[0].name}");
            return controllers[0];
        }

        Debug.Log("[YokaiStateController][FindActiveGrowthController][Exit] result=null");
        return null;
    }

    void UpdateDangerEffects()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
        {
            Debug.Log("[YokaiStateController][UpdateDangerEffects][EarlyReturn] reason=dangerEffects empty");
            Debug.Log("[YokaiStateController][UpdateDangerEffects][EARLY_RETURN] reason=dangerEffects empty");
            return;
        }

        bool enableBlink = isKegareMaxVisualsActive;
        int intensityLevel = isKegareMaxVisualsActive ? 2 : 1;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            bool shouldBlink = enableBlink && effect.gameObject.activeInHierarchy;
            effect.SetBlinking(shouldBlink);
            effect.SetIntensityLevel(intensityLevel);
        }
        Debug.Log($"[YokaiStateController][UpdateDangerEffects][Exit] enableBlink={enableBlink} intensityLevel={intensityLevel}");
    }

    void HandleStateSeTransitions(YokaiState previousState, YokaiState newState)
    {
        Debug.Log($"[YokaiStateController][HandleStateSeTransitions][Enter] previousState={previousState} newState={newState}");
        if (newState == YokaiState.EvolutionReady && previousState != YokaiState.EvolutionReady)
        {
            // 不具合④: 進化準備状態に入った瞬間にメッセージを出す。
            MentorMessageService.ShowHint(OnmyojiHintType.EvolutionStart);
        }

        if (previousState == YokaiState.Evolving && newState == YokaiState.Normal && evolutionResultPending)
        {
            // 不具合④: 進化完了後に段階別メッセージを出す。
            if (evolutionResultStage == YokaiEvolutionStage.Child)
                MentorMessageService.ShowHint(OnmyojiHintType.EvolutionCompleteChild);
            else if (evolutionResultStage == YokaiEvolutionStage.Adult)
                MentorMessageService.ShowHint(OnmyojiHintType.EvolutionCompleteAdult);
            evolutionResultPending = false;
        }
        Debug.Log("[YokaiStateController][HandleStateSeTransitions][Exit]");
    }

    void RefreshDangerEffectOriginalColors()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);
        if (dangerEffects == null || dangerEffects.Length == 0)
        {
            Debug.Log("[YokaiStateController][RefreshDangerEffectOriginalColors][EarlyReturn] reason=dangerEffects empty");
            Debug.Log("[YokaiStateController][RefreshDangerEffectOriginalColors][EARLY_RETURN] reason=dangerEffects empty");
            return;
        }

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            effect.RefreshOriginalColor();
        }
        Debug.Log("[YokaiStateController][RefreshDangerEffectOriginalColors][Exit]");
    }

    void CacheEnergyEmptyTargets(GameObject targetRoot)
    {
        Debug.Log($"[YokaiStateController][CacheEnergyEmptyTargets][Enter] targetRoot={(targetRoot == null ? \"null\" : targetRoot.name)}");
        energyEmptyTargetRoot = targetRoot;
        energyEmptySpriteColors.Clear();
        energyEmptyImageColors.Clear();

        if (energyEmptyTargetRoot == null)
        {
            Debug.Log("[YokaiStateController][CacheEnergyEmptyTargets][EarlyReturn] reason=targetRoot null");
            Debug.Log("[YokaiStateController][CacheEnergyEmptyTargets][EARLY_RETURN] reason=targetRoot null");
            return;
        }

        foreach (var sprite in energyEmptyTargetRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null)
                continue;

            energyEmptySpriteColors[sprite] = sprite.color;
        }

        foreach (var image in energyEmptyTargetRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
                continue;

            energyEmptyImageColors[image] = image.color;
        }
        Debug.Log("[YokaiStateController][CacheEnergyEmptyTargets][Exit]");
    }

    void CacheKegareMaxTargets(GameObject targetRoot)
    {
        Debug.Log($"[YokaiStateController][CacheKegareMaxTargets][Enter] targetRoot={(targetRoot == null ? \"null\" : targetRoot.name)}");
        kegareMaxTargetRoot = targetRoot;
        kegareMaxSpriteColors.Clear();
        kegareMaxImageColors.Clear();
        kegareMaxBasePosition = Vector3.zero;
        kegareMaxBaseScale = Vector3.zero;
        kegareMaxNoiseSeed = Random.value * 10f;

        if (kegareMaxTargetRoot == null)
        {
            Debug.Log("[YokaiStateController][CacheKegareMaxTargets][EarlyReturn] reason=targetRoot null");
            Debug.Log("[YokaiStateController][CacheKegareMaxTargets][EARLY_RETURN] reason=targetRoot null");
            return;
        }

        CaptureKegareMaxBaseTransform();

        foreach (var sprite in kegareMaxTargetRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null)
                continue;

            kegareMaxSpriteColors[sprite] = sprite.color;
        }

        foreach (var image in kegareMaxTargetRoot.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
                continue;

            kegareMaxImageColors[image] = image.color;
        }
        Debug.Log("[YokaiStateController][CacheKegareMaxTargets][Exit]");
    }

    void UpdateEnergyEmptyVisuals(bool isEnergyEmpty)
    {
        Debug.Log($"[YokaiStateController][UpdateEnergyEmptyVisuals][Enter] isEnergyEmpty={isEnergyEmpty}");
        if (energyEmptyTargetRoot == null || CurrentYokaiContext.Current != energyEmptyTargetRoot)
        {
            CacheEnergyEmptyTargets(CurrentYokaiContext.Current);
        }

        if (isEnergyEmpty)
        {
            foreach (var pair in energyEmptySpriteColors)
            {
                if (pair.Key == null)
                    continue;

                Color color = pair.Value;
                color.a *= Mathf.Clamp01(energyEmptyAlpha);
                pair.Key.color = color;
            }

            foreach (var pair in energyEmptyImageColors)
            {
                if (pair.Key == null)
                    continue;

                Color color = pair.Value;
                color.a *= Mathf.Clamp01(energyEmptyAlpha);
                pair.Key.color = color;
            }
            Debug.Log("[YokaiStateController][UpdateEnergyEmptyVisuals][Exit] state=energyEmpty");
        }
        else
        {
            ResetEnergyEmptyVisuals();
            Debug.Log("[YokaiStateController][UpdateEnergyEmptyVisuals][Exit] state=normal");
        }
    }

    void ResetEnergyEmptyVisuals()
    {
        Debug.Log("[YokaiStateController][ResetEnergyEmptyVisuals][Enter]");
        foreach (var pair in energyEmptySpriteColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        foreach (var pair in energyEmptyImageColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }
        Debug.Log("[YokaiStateController][ResetEnergyEmptyVisuals][Exit]");
    }

    void UpdateKegareMaxVisuals(bool enable)
    {
        Debug.Log($"[YokaiStateController][UpdateKegareMaxVisuals][Enter] enable={enable}");
        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        if (enable)
        {
            foreach (var pair in kegareMaxSpriteColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(kegareMaxDarkenIntensity));
            }

            foreach (var pair in kegareMaxImageColors)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.color = Color.Lerp(pair.Value, Color.black, Mathf.Clamp01(kegareMaxDarkenIntensity));
            }
            Debug.Log("[YokaiStateController][UpdateKegareMaxVisuals][Exit] state=enabled");
        }
        else
        {
            ResetKegareMaxVisuals();
            Debug.Log("[YokaiStateController][UpdateKegareMaxVisuals][Exit] state=disabled");
        }
    }

    void ResetKegareMaxVisuals()
    {
        Debug.Log("[YokaiStateController][ResetKegareMaxVisuals][Enter]");
        foreach (var pair in kegareMaxSpriteColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }

        foreach (var pair in kegareMaxImageColors)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value;
        }
        Debug.Log("[YokaiStateController][ResetKegareMaxVisuals][Exit]");
    }

    void UpdateKegareMaxMotion()
    {
        Debug.Log($"[YokaiStateController][UpdateKegareMaxMotion][Enter] isKegareMaxVisualsActive={isKegareMaxVisualsActive} isKegareMaxMotionApplied={isKegareMaxMotionApplied}");
        if (!isKegareMaxVisualsActive)
        {
            if (isKegareMaxMotionApplied)
            {
                ResetKegareMaxMotion();
                isKegareMaxMotionApplied = false;
            }
            Debug.Log("[YokaiStateController][UpdateKegareMaxMotion][EarlyReturn] reason=visualsInactive");
            Debug.Log("[YokaiStateController][UpdateKegareMaxMotion][EARLY_RETURN] reason=visualsInactive");
            return;
        }

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        if (kegareMaxTargetRoot == null)
        {
            Debug.Log("[YokaiStateController][UpdateKegareMaxMotion][EarlyReturn] reason=targetRoot null");
            Debug.Log("[YokaiStateController][UpdateKegareMaxMotion][EARLY_RETURN] reason=targetRoot null");
            return;
        }

        float time = Time.time * kegareMaxWobbleSpeed;
        float pulse = Mathf.Sin(time) * kegareMaxWobbleScale;
        float noise = (Mathf.PerlinNoise(kegareMaxNoiseSeed, time) - 0.5f) * 2f * kegareMaxWobbleScale;
        float scaleMultiplier = 1f + pulse + noise;

        float jitterX = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 1.4f, time) - 0.5f) * 2f * kegareMaxJitterAmplitude;
        float jitterY = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 2.1f, time + 3.7f) - 0.5f) * 2f * kegareMaxJitterAmplitude;

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale * scaleMultiplier;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition + new Vector3(jitterX, jitterY, 0f);
        isKegareMaxMotionApplied = true;
        Debug.Log("[YokaiStateController][UpdateKegareMaxMotion][Exit]");
    }

    void ResetKegareMaxMotion()
    {
        Debug.Log("[YokaiStateController][ResetKegareMaxMotion][Enter]");
        if (kegareMaxTargetRoot == null)
        {
            Debug.Log("[YokaiStateController][ResetKegareMaxMotion][EarlyReturn] reason=targetRoot null");
            Debug.Log("[YokaiStateController][ResetKegareMaxMotion][EARLY_RETURN] reason=targetRoot null");
            return;
        }

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition;
        Debug.Log("[YokaiStateController][ResetKegareMaxMotion][Exit]");
    }

    public void EnterKegareMax()
    {
        Debug.Log("[YokaiStateController][EnterKegareMax][Enter]");
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (isKegareMaxVisualsActive)
        {
            Debug.Log("[YokaiStateController][EnterKegareMax][EarlyReturn] reason=alreadyActive");
            Debug.Log("[YokaiStateController][EnterKegareMax][EARLY_RETURN] reason=alreadyActive");
            return;
        }

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        CaptureKegareMaxBaseTransform();
        isKegareMaxVisualsActive = true;
        ApplyStateFromManagers(forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_ENTER);
        Debug.Log("[YokaiStateController][EnterKegareMax][Exit]");
    }

    public void RequestReleaseKegareMax()
    {
        Debug.Log("[YokaiStateController][RequestReleaseKegareMax][Enter]");
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (!isKegareMaxVisualsActive)
        {
            Debug.Log("[YokaiStateController][RequestReleaseKegareMax][EarlyReturn] reason=visualsInactive");
            Debug.Log("[YokaiStateController][RequestReleaseKegareMax][EARLY_RETURN] reason=visualsInactive");
            return;
        }

        kegareMaxReleaseRoutine = StartCoroutine(ReleaseKegareMaxAfterDelay());
        Debug.Log("[YokaiStateController][RequestReleaseKegareMax][Exit]");
    }

    System.Collections.IEnumerator ReleaseKegareMaxAfterDelay()
    {
        Debug.Log("[YokaiStateController][ReleaseKegareMaxAfterDelay][Enter]");
        float delay = Mathf.Clamp(kegareMaxReleaseDelay, 0.1f, 0.2f);
        yield return new WaitForSeconds(delay);
        isKegareMaxVisualsActive = false;
        ApplyStateFromManagers(forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_RELEASE);
        kegareMaxReleaseRoutine = null;
        Debug.Log("[YokaiStateController][ReleaseKegareMaxAfterDelay][Exit]");
    }

    void CaptureKegareMaxBaseTransform()
    {
        if (kegareMaxTargetRoot == null)
        {
            Debug.Log("[YokaiStateController][CaptureKegareMaxBaseTransform][EarlyReturn] reason=targetRoot null");
            Debug.Log("[YokaiStateController][CaptureKegareMaxBaseTransform][EARLY_RETURN] reason=targetRoot null");
            return;
        }

        kegareMaxBasePosition = kegareMaxTargetRoot.transform.localPosition;
        kegareMaxBaseScale = kegareMaxTargetRoot.transform.localScale;
        Debug.Log("[YokaiStateController][CaptureKegareMaxBaseTransform][Exit]");
    }

    void LogStateContext(string label)
    {
        if (!enableStateLogs)
        {
            Debug.Log($"[YokaiStateController][LogStateContext][EarlyReturn] reason=enableStateLogs false label={label}");
            Debug.Log("[YokaiStateController][LogStateContext][EARLY_RETURN] reason=enableStateLogs false label=" + label);
            return;
        }

        string yokaiName = CurrentYokaiContext.CurrentName();
        float currentKegare = kegareManager != null ? kegareManager.kegare : 0f;
        float maxKegare = kegareManager != null ? kegareManager.maxKegare : 0f;
        float currentEnergy = energyManager != null ? energyManager.energy : 0f;
        float maxEnergy = energyManager != null ? energyManager.maxEnergy : 0f;
#if UNITY_EDITOR
        Debug.Log($"[STATE][{label}] yokai={yokaiName} state={currentState} kegare={currentKegare:0.##}/{maxKegare:0.##} energy={currentEnergy:0.##}/{maxEnergy:0.##}");
#endif
        Debug.Log($"[YokaiStateController][LogStateContext][Exit] label={label} yokai={yokaiName} state={currentState} kegare={currentKegare:0.##}/{maxKegare:0.##} energy={currentEnergy:0.##}/{maxEnergy:0.##}");
    }

    void LogDependencyState(string context)
    {
        Debug.Log($"[YokaiStateController][LogDependencyState][Enter] context={context}");
        string energyStatus = energyManager == null
            ? "null"
            : $"{energyManager.energy:0.##}/{energyManager.maxEnergy:0.##}";
        string kegareStatus = kegareManager == null
            ? "null"
            : $"{kegareManager.kegare:0.##}/{kegareManager.maxKegare:0.##}";
        Debug.Log($"[STATE][{context}] energyManager={(energyManager == null ? "null" : "ok")} energy={energyStatus} kegareManager={(kegareManager == null ? "null" : "ok")} kegare={kegareStatus}");
        Debug.Log("[YokaiStateController][LogDependencyState][Exit]");
    }

    }
}
