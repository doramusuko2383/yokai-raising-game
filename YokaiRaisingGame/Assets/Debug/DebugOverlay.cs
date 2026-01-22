using UnityEngine;
using UnityEngine.SceneManagement;
using Yokai;

public class DebugOverlay : MonoBehaviour
{
    const float PanelWidth = 260f;
    const float PanelPadding = 8f;
    const float ButtonHeight = 24f;

    PurityController purityController;
    SpiritController spiritController;
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
        ResolveYokaiControllers(yokai);
    }

    void ResolveDependencies()
    {
        stateController = CurrentYokaiContext.ResolveStateController();
        ResolveYokaiControllers(CurrentYokaiContext.Current);
    }

    void ResolveYokaiControllers(GameObject yokai)
    {
        purityController = null;
        spiritController = null;
        growthController = null;

        if (yokai != null)
        {
            purityController = yokai.GetComponentInChildren<PurityController>(true);
            spiritController = yokai.GetComponentInChildren<SpiritController>(true);
            growthController = yokai.GetComponentInChildren<YokaiGrowthController>(true);
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

        float panelHeight = 200f;
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
        string purityLabel = purityController != null
            ? $"{purityController.purity:0.##}/{purityController.maxPurity:0.##}"
            : "Unknown";
        string spiritLabel = spiritController != null
            ? $"{spiritController.spirit:0.##}/{spiritController.maxSpirit:0.##}"
            : "Unknown";

        GUILayout.Label($"State: {stateLabel}", labelStyle);
        GUILayout.Label($"Yokai: {yokaiName}", labelStyle);
        GUILayout.Label($"Stage: {growthLabel}", labelStyle);
        GUILayout.Label($"Purity: {purityLabel}", labelStyle);
        GUILayout.Label($"Spirit: {spiritLabel}", labelStyle);
    }

    void DrawControls()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("清浄度 +", buttonStyle, GUILayout.Height(ButtonHeight)))
            AdjustPurity(10f);
        if (GUILayout.Button("清浄度 -", buttonStyle, GUILayout.Height(ButtonHeight)))
            AdjustPurity(-10f);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Spirit -", buttonStyle, GUILayout.Height(ButtonHeight)))
            AdjustSpirit(-10f);

        if (GUILayout.Button("進化Ready", buttonStyle, GUILayout.Height(ButtonHeight)))
            SetEvolutionReady();

        if (GUILayout.Button("シーンリセット", buttonStyle, GUILayout.Height(ButtonHeight)))
            ResetScene();
    }

#if UNITY_EDITOR
    void HandleEditorShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.K))
            AdjustPurity(10f);

        if (Input.GetKeyDown(KeyCode.J))
            AdjustPurity(-10f);

        if (Input.GetKeyDown(KeyCode.E))
            SetEvolutionReady();

        if (Input.GetKeyDown(KeyCode.R))
            ResetScene();
    }
#endif

    void AdjustPurity(float amount)
    {
        if (purityController == null)
            purityController = CurrentYokaiContext.ResolvePurityController();

        if (purityController == null)
            return;

        purityController.AddPurity(amount);
    }

    void AdjustSpirit(float amount)
    {
        if (spiritController == null)
            spiritController = CurrentYokaiContext.Current != null
                ? CurrentYokaiContext.Current.GetComponentInChildren<SpiritController>(true)
                : null;

        if (spiritController == null)
            return;

        spiritController.ChangeSpirit(amount);
    }

    void SetEvolutionReady()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            stateController.SetEvolutionReady();
        }
    }

    void ResetScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}
