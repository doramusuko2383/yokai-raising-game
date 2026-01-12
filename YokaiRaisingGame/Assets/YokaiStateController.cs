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
    public bool isSpiritEmpty { get; private set; }

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
        ResolveDependencies();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged -= BindCurrentYokai;
    }

    void Start()
    {
        StartCoroutine(DelayedInitialState());
    }

    IEnumerator DelayedInitialState()
    {
        yield return null;
        ResolveDependencies();
        ApplyStateFromManagers();
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
            ApplyStateFromManagers();
    }

    void LateUpdate()
    {
        UpdateKegareMaxMotion();
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
    }

    void OnEnergyChanged(float current, float max)
    {
        isSpiritEmpty = current <= 0;
    }

    void ApplyStateFromManagers(YokaiState? requestedState = null, bool forceApplyUI = false)
    {
        if (energyManager == null || kegareManager == null)
            return;

        bool wasSpiritEmpty = isSpiritEmpty;
        isSpiritEmpty = energyManager.energy <= 0;

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
        else if (growthController != null && growthController.isEvolutionReady && !IsEvolutionBlocked(out _))
        {
            nextState = YokaiState.EvolutionReady;
        }
        else if (kegareManager.kegare >= kegareManager.maxKegare)
        {
            nextState = YokaiState.KegareMax;
        }
        else
        {
            nextState = YokaiState.Normal;
        }

        bool stateChanged = SetState(nextState);
        if (stateChanged || forceApplyUI || wasSpiritEmpty != isSpiritEmpty)
            ApplyStateUI();
    }

    bool SetState(YokaiState newState)
    {
        if (currentState == newState)
            return false;

        YokaiState previousState = currentState;
        currentState = newState;
        if (currentState == YokaiState.Purifying)
            isPurifying = true;
        if (previousState == YokaiState.Purifying && currentState != YokaiState.Purifying)
            isPurifying = false;
        if (currentState == YokaiState.Purifying)
            purifyTimer = 0f;

        if (enableStateLogs)
        {
            if (currentState == YokaiState.KegareMax && previousState != YokaiState.KegareMax)
                Debug.Log("[STATE] 穢れMAX ON");
            else if (previousState == YokaiState.KegareMax && currentState != YokaiState.KegareMax)
                Debug.Log("[STATE] 穢れMAX OFF");

        }

        HandleStateSeTransitions(previousState, currentState);
        LogStateContext("StateChange");
        return true;
    }

    public void EnterSpiritEmptyState()
    {
        if (isSpiritEmpty)
            return;

        isSpiritEmpty = true;
    }

    public void ExitSpiritEmptyState()
    {
        if (!isSpiritEmpty)
            return;

        isSpiritEmpty = false;
    }

    public void BeginPurifying()
    {
        if (currentState != YokaiState.Normal)
            return;

        isPurifying = true;
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
        ApplyStateFromManagers();
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

        isPurifying = false;
        ApplyStateFromManagers();
    }

    public void BeginEvolution()
    {
        if (currentState != YokaiState.EvolutionReady)
            return;

        ApplyStateFromManagers(YokaiState.Evolving, forceApplyUI: true);
    }

    public void CompleteEvolution()
    {
        ApplyStateFromManagers(YokaiState.Normal, forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
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
        LogStateContext("Bind");
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
        isSpiritEmpty = false;
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
    }

    public void SetEvolutionReady()
    {
        if (currentState == YokaiState.Evolving)
            return;

        if (IsEvolutionBlocked(out string reason))
        {
            Debug.Log($"[EVOLUTION] Ready blocked. reason={reason}");
            return;
        }

        if (!HasReachedEvolutionScale())
        {
            Debug.Log("[EVOLUTION] Ready blocked. reason=Scale");
            return;
        }

        if (!IsKegareMax())
            ApplyStateFromManagers(YokaiState.EvolutionReady, forceApplyUI: true);
        // DEBUG: EvolutionReady になったことを明示してタップ可能を知らせる
        Debug.Log("[EVOLUTION] Ready. Tap the yokai to evolve.");
    }

    public void ExecuteEmergencyPurify()
    {
        if (currentState != YokaiState.KegareMax)
            return;

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager != null)
            kegareManager.ExecuteEmergencyPurify();

        ApplyStateFromManagers();
    }

    bool IsKegareMax()
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        RegisterKegareEvents();
        return kegareManager != null && kegareManager.isKegareMax;
    }

    bool IsEnergyZero()
    {
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        RegisterEnergyEvents();
        return energyManager != null && energyManager.energy <= 0f;
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
        LogStateContext("PurifyTick");
    }

    void ApplyStateUI()
    {
        if (!AreDependenciesResolved())
            return;

        bool isKegareMax = currentState == YokaiState.KegareMax;
        bool showKegareMaxVisuals = isKegareMaxVisualsActive;
        bool isEnergyEmpty = isSpiritEmpty;
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
    }

    bool AreDependenciesResolved()
    {
        if (CurrentYokaiContext.Current == null)
            return false;

        if (energyManager == null || kegareManager == null)
            return false;

        if (actionPanel == null || emergencyPurifyButton == null || purifyStopButton == null || magicCircleOverlay == null)
            return false;

        return true;
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
    }

    void HandleStateSeTransitions(YokaiState previousState, YokaiState newState)
    {
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

    void CacheEnergyEmptyTargets(GameObject targetRoot)
    {
        energyEmptyTargetRoot = targetRoot;
        energyEmptySpriteColors.Clear();
        energyEmptyImageColors.Clear();

        if (energyEmptyTargetRoot == null)
            return;

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
    }

    void CacheKegareMaxTargets(GameObject targetRoot)
    {
        kegareMaxTargetRoot = targetRoot;
        kegareMaxSpriteColors.Clear();
        kegareMaxImageColors.Clear();
        kegareMaxBasePosition = Vector3.zero;
        kegareMaxBaseScale = Vector3.zero;
        kegareMaxNoiseSeed = Random.value * 10f;

        if (kegareMaxTargetRoot == null)
            return;

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
    }

    void UpdateEnergyEmptyVisuals(bool isEnergyEmpty)
    {
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
        }
        else
        {
            ResetEnergyEmptyVisuals();
        }
    }

    void ResetEnergyEmptyVisuals()
    {
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
    }

    void UpdateKegareMaxVisuals(bool enable)
    {
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
        }
        else
        {
            ResetKegareMaxVisuals();
        }
    }

    void ResetKegareMaxVisuals()
    {
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
    }

    void UpdateKegareMaxMotion()
    {
        if (!isKegareMaxVisualsActive)
        {
            if (isKegareMaxMotionApplied)
            {
                ResetKegareMaxMotion();
                isKegareMaxMotionApplied = false;
            }
            return;
        }

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        if (kegareMaxTargetRoot == null)
            return;

        float time = Time.time * kegareMaxWobbleSpeed;
        float pulse = Mathf.Sin(time) * kegareMaxWobbleScale;
        float noise = (Mathf.PerlinNoise(kegareMaxNoiseSeed, time) - 0.5f) * 2f * kegareMaxWobbleScale;
        float scaleMultiplier = 1f + pulse + noise;

        float jitterX = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 1.4f, time) - 0.5f) * 2f * kegareMaxJitterAmplitude;
        float jitterY = (Mathf.PerlinNoise(kegareMaxNoiseSeed + 2.1f, time + 3.7f) - 0.5f) * 2f * kegareMaxJitterAmplitude;

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale * scaleMultiplier;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition + new Vector3(jitterX, jitterY, 0f);
        isKegareMaxMotionApplied = true;
    }

    void ResetKegareMaxMotion()
    {
        if (kegareMaxTargetRoot == null)
            return;

        kegareMaxTargetRoot.transform.localScale = kegareMaxBaseScale;
        kegareMaxTargetRoot.transform.localPosition = kegareMaxBasePosition;
    }

    public void EnterKegareMax()
    {
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (isKegareMaxVisualsActive)
            return;

        if (kegareMaxTargetRoot == null || CurrentYokaiContext.Current != kegareMaxTargetRoot)
        {
            CacheKegareMaxTargets(CurrentYokaiContext.Current);
        }

        CaptureKegareMaxBaseTransform();
        isKegareMaxVisualsActive = true;
        ApplyStateFromManagers(forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_ENTER);
    }

    public void RequestReleaseKegareMax()
    {
        if (kegareMaxReleaseRoutine != null)
        {
            StopCoroutine(kegareMaxReleaseRoutine);
            kegareMaxReleaseRoutine = null;
        }

        if (!isKegareMaxVisualsActive)
            return;

        kegareMaxReleaseRoutine = StartCoroutine(ReleaseKegareMaxAfterDelay());
    }

    System.Collections.IEnumerator ReleaseKegareMaxAfterDelay()
    {
        float delay = Mathf.Clamp(kegareMaxReleaseDelay, 0.1f, 0.2f);
        yield return new WaitForSeconds(delay);
        isKegareMaxVisualsActive = false;
        ApplyStateFromManagers(forceApplyUI: true);
        RefreshDangerEffectOriginalColors();
        AudioHook.RequestPlay(YokaiSE.SE_KEGARE_MAX_RELEASE);
        kegareMaxReleaseRoutine = null;
    }

    void CaptureKegareMaxBaseTransform()
    {
        if (kegareMaxTargetRoot == null)
            return;

        kegareMaxBasePosition = kegareMaxTargetRoot.transform.localPosition;
        kegareMaxBaseScale = kegareMaxTargetRoot.transform.localScale;
    }

    void LogStateContext(string label)
    {
        if (!enableStateLogs)
            return;

        string yokaiName = CurrentYokaiContext.CurrentName();
        float currentKegare = kegareManager != null ? kegareManager.kegare : 0f;
        float maxKegare = kegareManager != null ? kegareManager.maxKegare : 0f;
        float currentEnergy = energyManager != null ? energyManager.energy : 0f;
        float maxEnergy = energyManager != null ? energyManager.maxEnergy : 0f;
#if UNITY_EDITOR
        Debug.Log($"[STATE][{label}] yokai={yokaiName} state={currentState} kegare={currentKegare:0.##}/{maxKegare:0.##} energy={currentEnergy:0.##}/{maxEnergy:0.##}");
#endif
    }

    void LogDependencyState(string context)
    {
        string energyStatus = energyManager == null
            ? "null"
            : $"{energyManager.energy:0.##}/{energyManager.maxEnergy:0.##}";
        string kegareStatus = kegareManager == null
            ? "null"
            : $"{kegareManager.kegare:0.##}/{kegareManager.maxKegare:0.##}";
        Debug.Log($"[STATE][{context}] energyManager={(energyManager == null ? "null" : "ok")} energy={energyStatus} kegareManager={(kegareManager == null ? "null" : "ok")} kegare={kegareStatus}");
    }

    }
}
