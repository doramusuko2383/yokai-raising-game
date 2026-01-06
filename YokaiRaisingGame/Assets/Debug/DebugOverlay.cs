using UnityEngine;
using UnityEngine.SceneManagement;
using Yokai;

public class DebugOverlay : MonoBehaviour
{
    const float PanelWidth = 260f;
    const float PanelPadding = 8f;
    const float ButtonHeight = 24f;

    KegareManager kegareManager;
    EnergyManager energyManager;
    YokaiStateController stateController;
    YokaiGrowthController growthController;

    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    Rect panelRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (!DebugToolsConfig.IsAvailable || !DebugToolsConfig.Enabled)
            return;

        if (FindObjectOfType<DebugOverlay>() != null)
            return;

        var overlayObject = new GameObject("DebugOverlay");
        DontDestroyOnLoad(overlayObject);
        overlayObject.AddComponent<DebugOverlay>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged += OnCurrentYokaiChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CurrentYokaiContext.CurrentChanged -= OnCurrentYokaiChanged;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!DebugToolsConfig.Enabled)
            return;

        HandleEditorShortcuts();
#endif
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveDependencies();
    }

    void OnCurrentYokaiChanged(GameObject yokai)
    {
        ResolveGrowthController(yokai);
    }

    void ResolveDependencies()
    {
        kegareManager = CurrentYokaiContext.ResolveKegareManager();
        energyManager = FindObjectOfType<EnergyManager>();
        stateController = CurrentYokaiContext.ResolveStateController();
        ResolveGrowthController(CurrentYokaiContext.Current);
    }

    void ResolveGrowthController(GameObject yokai)
    {
        if (yokai != null)
        {
            growthController = yokai.GetComponent<YokaiGrowthController>();
        }
        else
        {
            growthController = FindObjectOfType<YokaiGrowthController>();
        }
    }

    void EnsureStyles()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11
            };
        }
    }

    void OnGUI()
    {
        if (!DebugToolsConfig.Enabled)
            return;

        EnsureStyles();

        float panelHeight = 220f;
        panelRect = new Rect(PanelPadding, PanelPadding, PanelWidth, panelHeight);
        GUI.Box(panelRect, "DEBUG", GUI.skin.box);

        GUILayout.BeginArea(new Rect(panelRect.x + PanelPadding, panelRect.y + 24f, PanelWidth - PanelPadding * 2f, panelHeight - PanelPadding * 2f));
        DrawStatus();
        GUILayout.Space(6f);
        DrawControls();
        GUILayout.EndArea();
    }

    void DrawStatus()
    {
        string yokaiName = CurrentYokaiContext.CurrentName();
        string stateLabel = stateController != null ? stateController.currentState.ToString() : "Unknown";
        string growthLabel = growthController != null
            ? $"{growthController.currentScale:0.##}/{growthController.maxScale:0.##}"
            : "Unknown";
        string kegareLabel = kegareManager != null
            ? $"{kegareManager.kegare:0.##}/{kegareManager.maxKegare:0.##}"
            : "Unknown";
        string energyLabel = energyManager != null
            ? $"{energyManager.energy:0.##}/{energyManager.maxEnergy:0.##}"
            : "Unknown";

        GUILayout.Label($"State: {stateLabel}", labelStyle);
        GUILayout.Label($"Yokai: {yokaiName}", labelStyle);
        GUILayout.Label($"Stage: {growthLabel}", labelStyle);
        GUILayout.Label($"Kegare: {kegareLabel}", labelStyle);
        GUILayout.Label($"Energy: {energyLabel}", labelStyle);
    }

    void DrawControls()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("穢れ +", buttonStyle, GUILayout.Height(ButtonHeight)))
            AdjustKegare(10f, "Button +");
        if (GUILayout.Button("穢れ -", buttonStyle, GUILayout.Height(ButtonHeight)))
            AdjustKegare(-10f, "Button -");
        GUILayout.EndHorizontal();

        if (GUILayout.Button("穢れMAX", buttonStyle, GUILayout.Height(ButtonHeight)))
            SetKegareMax();

        if (GUILayout.Button("緊急おきよめ", buttonStyle, GUILayout.Height(ButtonHeight)))
            ExecuteEmergencyPurify();

        if (GUILayout.Button("進化Ready", buttonStyle, GUILayout.Height(ButtonHeight)))
            SetEvolutionReady();

        if (GUILayout.Button("シーンリセット", buttonStyle, GUILayout.Height(ButtonHeight)))
            ResetScene();
    }

#if UNITY_EDITOR
    void HandleEditorShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.K))
            AdjustKegare(10f, "Key K");

        if (Input.GetKeyDown(KeyCode.J))
            AdjustKegare(-10f, "Key J");

        if (Input.GetKeyDown(KeyCode.M))
            SetKegareMax();

        if (Input.GetKeyDown(KeyCode.P))
            ExecuteEmergencyPurify();

        if (Input.GetKeyDown(KeyCode.E))
            SetEvolutionReady();

        if (Input.GetKeyDown(KeyCode.R))
            ResetScene();
    }
#endif

    void AdjustKegare(float amount, string reason)
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager == null)
            return;

        kegareManager.AddKegare(amount);
        Debug.Log($"[DEBUG][KEGARE] Adjust {amount:0.##} reason={reason}");
    }

    void SetKegareMax()
    {
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager == null)
            return;

        kegareManager.SetKegare(kegareManager.maxKegare, "Debug KegareMax");
    }

    void ExecuteEmergencyPurify()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            Debug.Log("[DEBUG][PURIFY] Execute emergency purify");
            stateController.ExecuteEmergencyPurify();
            return;
        }

        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager != null)
        {
            Debug.Log("[DEBUG][PURIFY] Execute emergency purify (fallback)");
            kegareManager.ExecuteEmergencyPurify();
        }
    }

    void SetEvolutionReady()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            Debug.Log("[DEBUG][EVOLUTION] Force ready");
            stateController.SetEvolutionReady();
        }
    }

    void ResetScene()
    {
        Debug.Log("[DEBUG][SCENE] Reload active scene");
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}
