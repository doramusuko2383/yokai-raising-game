using UnityEngine;
using UnityEngine.SceneManagement;

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

    float purifyTimer;
    KegareManager registeredKegareManager;
    EnergyManager registeredEnergyManager;

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
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
#if UNITY_EDITOR
        HandleEditorDebugInput();
#endif
    }

    void ResolveDependencies()
    {
        if (growthController == null)
            growthController = FindObjectOfType<YokaiGrowthController>();

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
        if (currentState == YokaiState.Evolving)
        {
            ApplyStateUI();
            return;
        }

        if (IsKegareMax())
        {
            SetState(YokaiState.KegareMax);
            return;
        }

        if (currentState == YokaiState.Purifying)
        {
            ApplyStateUI();
            return;
        }

        if (currentState == YokaiState.EvolutionReady)
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

        currentState = newState;
        if (currentState == YokaiState.Purifying)
            purifyTimer = 0f;

        ApplyStateUI();
    }

    public void BeginPurifying()
    {
        if (currentState != YokaiState.Normal)
            return;

        SetState(YokaiState.Purifying);
        RefreshState();
    }

    public void StopPurifying()
    {
        if (currentState != YokaiState.Purifying)
            return;

        SetState(YokaiState.Normal);
        RefreshState();
    }

    public void BeginEvolution()
    {
        if (currentState != YokaiState.EvolutionReady)
            return;

        SetState(YokaiState.Evolving);
    }

    public void CompleteEvolution()
    {
        SetState(YokaiState.Normal);
        RefreshDangerEffectOriginalColors();
        RefreshState();
    }

    public void SetEvolutionReady()
    {
        if (currentState != YokaiState.Normal)
            return;

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
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager != null)
            kegareManager.ExecuteEmergencyPurify();

        SetState(YokaiState.Normal);
        RefreshState();
    }

    bool IsKegareMax()
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        return kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
    }

    void HandlePurifyTick()
    {
        if (currentState != YokaiState.Purifying)
            return;

        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager == null)
            return;

        purifyTimer += Time.deltaTime;
        if (purifyTimer < purifyTickInterval)
            return;

        purifyTimer = 0f;
        kegareManager.AddKegare(-purifyTickAmount);
    }

    void ApplyStateUI()
    {
        bool showActionPanel = currentState == YokaiState.Normal || currentState == YokaiState.EvolutionReady;
        bool showEmergency = currentState == YokaiState.KegareMax;
        bool showMagicCircle = currentState == YokaiState.Purifying;
        bool showStopPurify = currentState == YokaiState.Purifying;
        bool showDangerOverlay = currentState == YokaiState.KegareMax;

        if (actionPanel != null)
            actionPanel.SetActive(showActionPanel);

        if (emergencyPurifyButton != null)
            emergencyPurifyButton.SetActive(showEmergency);

        if (purifyStopButton != null)
            purifyStopButton.SetActive(showStopPurify);

        if (magicCircleOverlay != null)
            magicCircleOverlay.SetActive(showMagicCircle);

        if (dangerOverlay != null)
        {
            dangerOverlay.alpha = showDangerOverlay ? 1f : 0f;
            dangerOverlay.blocksRaycasts = showDangerOverlay;
            dangerOverlay.interactable = showDangerOverlay;
        }

        UpdateDangerEffects();
    }

    void UpdateDangerEffects()
    {
        if (dangerEffects == null || dangerEffects.Length == 0)
            return;

        bool enableBlink = currentState == YokaiState.KegareMax;

        foreach (var effect in dangerEffects)
        {
            if (effect == null)
                continue;

            bool shouldBlink = enableBlink && effect.gameObject.activeInHierarchy;
            effect.SetBlinking(shouldBlink);
        }
    }

    void RefreshDangerEffectOriginalColors()
    {
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

#if UNITY_EDITOR
    void HandleEditorDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
            AdjustKegare(10f);

        if (Input.GetKeyDown(KeyCode.J))
            AdjustKegare(-10f);

        if (Input.GetKeyDown(KeyCode.R))
            AdjustEnergy(10f);

        if (Input.GetKeyDown(KeyCode.F))
            AdjustEnergy(-10f);

        if (Input.GetKeyDown(KeyCode.E))
            SetState(YokaiState.EvolutionReady);
    }

    void AdjustKegare(float amount)
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager != null)
            kegareManager.AddKegare(amount);
    }

    void AdjustEnergy(float amount)
    {
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
            energyManager.ChangeEnergy(amount);
    }
#endif
}
}
