using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Yokai
{
public class YokaiStateController : MonoBehaviour
{
    [Header("状態")]
    public YokaiState currentState = YokaiState.Normal;

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

    [Header("Purify")]
    [SerializeField]
    float purifyTickInterval = 1f;

    [SerializeField]
    float purifyTickAmount = 2f;

    [SerializeField]
    bool enablePurifyTick = false;

    float purifyTimer;
    KegareManager registeredKegareManager;
    EnergyManager registeredEnergyManager;
    bool evolutionReadyPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (FindObjectOfType<YokaiStateController>() != null)
            return;

        var controllerObject = new GameObject("YokaiStateController");
        controllerObject.AddComponent<YokaiStateController>();
        DontDestroyOnLoad(controllerObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged += BindCurrentYokai;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveDependencies();
        currentState = YokaiState.Normal;
        RefreshState();
    }

    void Update()
    {
        HandlePurifyTick();
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

        if (actionPanel == null)
            actionPanel = GameObject.Find("UI_Action");

        if (emergencyPurifyButton == null)
            emergencyPurifyButton = GameObject.Find("Button_MononokeHeal")
                ?? GameObject.Find("Btn_AdWatch");

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
    }

    void RegisterKegareEvents()
    {
        if (registeredKegareManager == kegareManager)
            return;

        if (registeredKegareManager != null)
        {
            registeredKegareManager.EmergencyPurifyRequested -= ExecuteEmergencyPurify;
            registeredKegareManager.KegareChanged -= OnKegareChanged;
        }

        registeredKegareManager = kegareManager;

        if (registeredKegareManager != null)
        {
            registeredKegareManager.EmergencyPurifyRequested += ExecuteEmergencyPurify;
            registeredKegareManager.KegareChanged += OnKegareChanged;
        }
    }

    void RegisterEnergyEvents()
    {
        if (registeredEnergyManager == energyManager)
            return;

        if (registeredEnergyManager != null)
            registeredEnergyManager.EnergyChanged -= OnEnergyChanged;

        registeredEnergyManager = energyManager;

        if (registeredEnergyManager != null)
            registeredEnergyManager.EnergyChanged += OnEnergyChanged;
    }

    void OnKegareChanged(float current, float max)
    {
        RefreshState();
    }

    void OnEnergyChanged(float current, float max)
    {
        RefreshState();
    }

    public void RefreshState()
    {
        if (CurrentYokaiContext.Current == null)
        {
            currentState = YokaiState.Normal;
            ApplyStateUI();
            return;
        }

        if (currentState == YokaiState.Evolving)
        {
            ApplyStateUI();
            return;
        }

        bool isKegareMax = IsKegareMax();
        bool isEnergyZero = IsEnergyZero();
        if (isKegareMax || isEnergyZero)
        {
            if (isKegareMax && isEnergyZero && currentState == YokaiState.EnergyEmpty)
            {
                ApplyStateUI();
                return;
            }

            SetState(isKegareMax ? YokaiState.KegareMax : YokaiState.EnergyEmpty);
            return;
        }

        if (evolutionReadyPending && !IsEvolutionBlocked(out _))
        {
            SetState(YokaiState.EvolutionReady);
            return;
        }

        if (currentState == YokaiState.EvolutionReady)
        {
            ApplyStateUI();
            return;
        }

        if (currentState == YokaiState.Purifying)
        {
            ApplyStateUI();
            return;
        }

        SetState(YokaiState.Normal);
    }

    public void SetState(YokaiState newState)
    {
        if (currentState == newState)
            return;

        YokaiState previousState = currentState;
        currentState = newState;
        if (currentState == YokaiState.Purifying)
            purifyTimer = 0f;

        if (currentState == YokaiState.KegareMax && previousState != YokaiState.KegareMax)
            Debug.Log("[STATE] 穢れMAX ON");
        else if (previousState == YokaiState.KegareMax && currentState != YokaiState.KegareMax)
            Debug.Log("[STATE] 穢れMAX OFF");

        if (currentState == YokaiState.EnergyEmpty && previousState != YokaiState.EnergyEmpty)
            Debug.Log("[ENERGY] 霊力0 ON");
        else if (previousState == YokaiState.EnergyEmpty && currentState != YokaiState.EnergyEmpty)
            Debug.Log("[ENERGY] 霊力0 OFF");

        HandleStateSeTransitions(previousState, currentState);
        ApplyStateUI();
        LogStateContext("StateChange");
    }

    public void BeginPurifying()
    {
        if (currentState != YokaiState.Normal)
            return;

        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
        SetState(YokaiState.Purifying);
        RefreshState();
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
        if (currentState != YokaiState.Purifying)
            return;

        if (playCancelSe)
            AudioHook.RequestPlay(YokaiSE.SE_PURIFY_CANCEL);

        SetState(YokaiState.Normal);
        RefreshState();
    }

    public void BeginEvolution()
    {
        if (currentState != YokaiState.EvolutionReady)
            return;

        evolutionReadyPending = false;
        SetState(YokaiState.Evolving);
    }

    public void CompleteEvolution()
    {
        evolutionReadyPending = false;
        SetState(YokaiState.Normal);
        RefreshDangerEffectOriginalColors();
        RefreshState();
    }

    public void BindCurrentYokai(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        SetActiveYokai(activeYokai);
        LogStateContext("Bind");
    }

    public void SetActiveYokai(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        growthController = activeYokai.GetComponent<YokaiGrowthController>();
        if (growthController != null && growthController.isEvolutionReady)
            evolutionReadyPending = true;
        dangerEffects = activeYokai.GetComponentsInChildren<YokaiDangerEffect>(true);
        RefreshDangerEffectOriginalColors();
        UpdateDangerEffects();
        if (currentState != YokaiState.Evolving)
            RefreshState();
        LogStateContext("Active");
    }

    public void SetEvolutionReady()
    {
        if (currentState == YokaiState.Evolving)
            return;

        evolutionReadyPending = true;
        if (IsEvolutionBlocked(out string reason))
        {
            Debug.Log($"[EVOLUTION] Ready blocked. reason={reason}");
            RefreshState();
            return;
        }

        if (!IsKegareMax())
            currentState = YokaiState.EvolutionReady;
        // DEBUG: EvolutionReady になったことを明示してタップ可能を知らせる
        Debug.Log("[EVOLUTION] Ready. Tap the yokai to evolve.");
        ApplyStateUI();
        RefreshState();
    }

    public void ExecuteEmergencyPurify()
    {
        if (currentState != YokaiState.KegareMax)
            return;

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager != null)
            kegareManager.ExecuteEmergencyPurify();

        SetState(YokaiState.Normal);
        RefreshState();
    }

    bool IsKegareMax()
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        return kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
    }

    bool IsEnergyZero()
    {
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        return energyManager != null && energyManager.energy <= 0f;
    }

    bool IsEvolutionBlocked(out string reason)
    {
        bool isKegareMax = IsKegareMax();
        bool isEnergyZero = IsEnergyZero();
        if (!isKegareMax && !isEnergyZero)
        {
            reason = string.Empty;
            return false;
        }

        if (isKegareMax && isEnergyZero)
            reason = "穢れMAX / 霊力0";
        else if (isKegareMax)
            reason = "穢れMAX";
        else
            reason = "霊力0";

        return true;
    }

    void HandlePurifyTick()
    {
        if (currentState != YokaiState.Purifying)
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
        LogStateContext("PurifyTick");
    }

    void ApplyStateUI()
    {
        bool isKegareMax = currentState == YokaiState.KegareMax;
        bool isEnergyEmpty = currentState == YokaiState.EnergyEmpty;
        bool showActionPanel = currentState == YokaiState.Normal || currentState == YokaiState.EvolutionReady || isKegareMax;
        bool showEmergency = isKegareMax;
        bool showMagicCircle = currentState == YokaiState.Purifying;
        bool showStopPurify = currentState == YokaiState.Purifying;
        bool showDangerOverlay = false;

        ApplyCanvasGroup(actionPanel, showActionPanel && !isEnergyEmpty, showActionPanel && !isEnergyEmpty);
        ApplyCanvasGroup(emergencyPurifyButton, showEmergency, showEmergency);
        ApplyCanvasGroup(purifyStopButton, showStopPurify, showStopPurify);
        ApplyCanvasGroup(magicCircleOverlay, showMagicCircle, showMagicCircle);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? 1f : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateActionPanelButtons(isKegareMax, isEnergyEmpty);
        UpdateDangerEffects();
    }

    void UpdateActionPanelButtons(bool isKegareMax, bool isEnergyEmpty)
    {
        if (actionPanel == null)
            return;

        var buttons = actionPanel.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            bool isEmergency = emergencyPurifyButton != null && button.gameObject == emergencyPurifyButton;
            bool shouldShow = !isEnergyEmpty && (isEmergency ? isKegareMax : !isKegareMax);
            ApplyCanvasGroup(button.gameObject, shouldShow, shouldShow);
            button.interactable = shouldShow;
            button.enabled = shouldShow;
        }
    }

    void ApplyCanvasGroup(GameObject target, bool visible, bool interactable)
    {
        if (target == null)
            return;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;

        var selectable = target.GetComponent<Selectable>();
        if (selectable != null)
            selectable.interactable = interactable;
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

    void UpdateDangerEffects()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        bool enableBlink = currentState == YokaiState.KegareMax;
        int intensityLevel = currentState == YokaiState.KegareMax ? 2 : 1;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            bool shouldBlink = enableBlink && effect.gameObject.activeInHierarchy;
            effect.SetBlinking(shouldBlink);
            effect.SetIntensityLevel(intensityLevel);
        }
    }

    void HandleStateSeTransitions(YokaiState previousState, YokaiState newState)
    {
        if (newState == YokaiState.KegareMax && previousState != YokaiState.KegareMax)
        {
            AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_ENTER);
            return;
        }

        if (previousState == YokaiState.KegareMax && newState != YokaiState.KegareMax)
        {
            AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_RELEASE);
        }
    }

    void RefreshDangerEffectOriginalColors()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            dangerEffects = FindObjectsOfType<YokaiDangerEffect>(true);
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            effect.RefreshOriginalColor();
        }
    }

    void LogStateContext(string label)
    {
        string yokaiName = CurrentYokaiContext.CurrentName();
        float currentKegare = kegareManager != null ? kegareManager.kegare : 0f;
        float maxKegare = kegareManager != null ? kegareManager.maxKegare : 0f;
        float currentEnergy = energyManager != null ? energyManager.energy : 0f;
        float maxEnergy = energyManager != null ? energyManager.maxEnergy : 0f;
#if UNITY_EDITOR
        Debug.Log($"[STATE][{label}] yokai={yokaiName} state={currentState} kegare={currentKegare:0.##}/{maxKegare:0.##} energy={currentEnergy:0.##}/{maxEnergy:0.##}");
#endif
    }

}
}
