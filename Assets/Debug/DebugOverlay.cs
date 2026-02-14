using UnityEngine;
using UnityEngine.SceneManagement;
using Yokai;

public class DebugOverlay : MonoBehaviour
{
    const float PanelWidth = 260f;
    const float PanelPadding = 8f;
    const float ButtonHeight = 24f;
    const float FastGrowthMultiplier = 240f;

    PurityController purityController;
    SpiritController spiritController;
    YokaiStateController stateController;
    YokaiGrowthController growthController;

    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    Rect panelRect;
    Vector2 historyScroll;
    Vector2 invariantScroll;

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
        ResolveDependencies();
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
        ResolveDependencies();
    }

    void ResolveDependencies()
    {
        stateController = CurrentYokaiContext.ResolveStateController();
        purityController = FindObjectOfType<PurityController>();
        spiritController = FindObjectOfType<SpiritController>();
        growthController = CurrentYokaiContext.Current != null
            ? CurrentYokaiContext.Current.GetComponentInChildren<YokaiGrowthController>(true)
            : null;
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

        float panelHeight = 680f;
        float x = Screen.width - PanelWidth - PanelPadding;
        panelRect = new Rect(x, PanelPadding, PanelWidth, panelHeight);
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
        string growthSpeedLabel = growthController != null
            ? $"x{growthController.DebugGrowthMultiplier:0.##}"
            : "Unknown";

        GUILayout.Label("▼ 状態概要", labelStyle);
        GUILayout.Label($"State: {stateLabel}", labelStyle);
        GUILayout.Label($"Yokai: {yokaiName}", labelStyle);
        GUILayout.Label($"Stage: {growthLabel}", labelStyle);
        GUILayout.Label($"Purity: {purityLabel}", labelStyle);
        GUILayout.Label($"Spirit: {spiritLabel}", labelStyle);
        GUILayout.Label($"Growth Speed: {growthSpeedLabel}", labelStyle);

        if (stateController != null)
        {
            GUILayout.Space(8f);
            GUILayout.Label("▼ Action Block 情報", labelStyle);
            GUILayout.Label($"LastFrame: {stateController.LastActionBlockFrame}", labelStyle);
            GUILayout.Label($"LastAction: {stateController.LastActionBlockedAction}", labelStyle);
            GUILayout.Label($"LastReason: {stateController.LastActionBlockReason}", labelStyle);

            GUILayout.Space(8f);
            GUILayout.Label("▼ Invariant Warnings", labelStyle);
            invariantScroll = GUILayout.BeginScrollView(invariantScroll, GUILayout.Height(120));
            foreach (var warning in stateController.LastInvariantWarnings)
            {
                GUILayout.Label(warning, labelStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.Label("▼ State 履歴", labelStyle);
            GUILayout.Label($"Current: {stateController.CurrentState}", labelStyle);
            GUILayout.Label($"LastReason: {stateController.LastStateChangeReason}", labelStyle);

            var history = stateController.GetStateHistory();
            historyScroll = GUILayout.BeginScrollView(historyScroll, GUILayout.Height(200));
            foreach (var entry in history)
            {
                GUILayout.Label(entry, labelStyle);
            }
            GUILayout.EndScrollView();
        }
    }

    void DrawControls()
    {
        GUILayout.Space(8f);
        GUILayout.Label("▼ Debug 操作", labelStyle);

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

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("成長速度 x1", buttonStyle, GUILayout.Height(ButtonHeight)))
            SetGrowthSpeedMultiplier(1f);
        if (GUILayout.Button("成長速度 x240", buttonStyle, GUILayout.Height(ButtonHeight)))
            SetGrowthSpeedMultiplier(FastGrowthMultiplier);
        GUILayout.EndHorizontal();

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

        if (Input.GetKeyDown(KeyCode.S))
            AdjustSpirit(-10f);

        if (Input.GetKeyDown(KeyCode.E))
            SetEvolutionReady();

        if (Input.GetKeyDown(KeyCode.G))
            ToggleFastGrowth();

        if (Input.GetKeyDown(KeyCode.R))
            ResetScene();
    }
#endif

    void AdjustPurity(float amount)
    {
        if (purityController == null)
            ResolveDependencies();

        if (purityController == null)
            return;

        purityController.AddPurity(amount);
    }

    void AdjustSpirit(float amount)
    {
        if (spiritController == null)
            ResolveDependencies();

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

    void SetGrowthSpeedMultiplier(float multiplier)
    {
        if (growthController == null)
            ResolveDependencies();

        if (growthController == null)
            return;

        growthController.SetDebugGrowthMultiplier(multiplier);
    }

    void ToggleFastGrowth()
    {
        if (growthController == null)
            ResolveDependencies();

        if (growthController == null)
            return;

        bool isFast = growthController.DebugGrowthMultiplier > 1f;
        SetGrowthSpeedMultiplier(isFast ? 1f : FastGrowthMultiplier);
    }

    void ResetScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}
