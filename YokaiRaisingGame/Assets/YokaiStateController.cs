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

    [SerializeField]
    PurifyButtonHandler purifyButtonHandler;

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
        RefreshState();
    }

    void Update()
    {
        RefreshState();
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
    }

    public void RefreshState()
    {
        if (currentState == YokaiState.Evolving)
            return;

        if (IsKegareMax())
        {
            SetState(YokaiState.KegareMax);
            return;
        }

        if (currentState == YokaiState.EvolutionReady)
            return;

        SetState(YokaiState.Normal);
    }

    public void SetState(YokaiState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
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
        RefreshState();
    }

    public void SetEvolutionReady()
    {
        if (currentState != YokaiState.Normal)
            return;

        currentState = YokaiState.EvolutionReady;
        // DEBUG: EvolutionReady になったことを明示してタップ可能を知らせる
        Debug.Log("[EVOLUTION] Ready. Tap the yokai to evolve.");
        RefreshState();
    }

    public void ExecuteEmergencyPurify()
    {
        if (currentState != YokaiState.KegareMax)
            return;

        if (purifyButtonHandler != null)
            purifyButtonHandler.OnClickEmergencyPurify();

        SetState(YokaiState.Normal);
        RefreshState();
    }

    bool IsKegareMax()
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        return kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
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
