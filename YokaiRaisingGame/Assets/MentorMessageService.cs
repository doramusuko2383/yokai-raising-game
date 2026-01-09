using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MentorMessageService : MonoBehaviour
{
    const string PrefabResourcePath = "UI/MentorMessageUI";

    static MentorMessageService instance;
    static readonly Dictionary<OnmyojiHintType, float> lastShownTimes = new Dictionary<OnmyojiHintType, float>();

    [Header("Settings")]
    [SerializeField]
    float messageCooldownSeconds = 15f;

    [SerializeField]
    float defaultDuration = 4f;

    MentorMessageUI messageUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        EnsureInstance();
    }

    public static void ShowHint(OnmyojiHintType type)
    {
        EnsureInstance().ShowMessage(type);
    }

    static MentorMessageService EnsureInstance()
    {
        if (instance != null)
            return instance;

        var host = new GameObject("MentorMessageService");
        instance = host.AddComponent<MentorMessageService>();
        DontDestroyOnLoad(host);
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureUI();
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUI();
    }

    void EnsureUI()
    {
        if (messageUI != null)
            return;

        var prefab = Resources.Load<MentorMessageUI>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("[Mentor] MentorMessageUI prefab not found. Ensure Resources/UI/MentorMessageUI.prefab exists.");
            return;
        }

        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
        {
            targetCanvas = CreateCanvas();
        }

        messageUI = Instantiate(prefab, targetCanvas.transform);
        messageUI.name = "MentorMessageUI";
        messageUI.HideMessage(immediate: true);
    }

    Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas bestCanvas = null;
        int bestSorting = int.MinValue;

        foreach (var canvas in canvases)
        {
            if (canvas == null)
                continue;

            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            int sorting = canvas.sortingOrder;
            if (sorting > bestSorting)
            {
                bestSorting = sorting;
                bestCanvas = canvas;
            }
        }

        return bestCanvas;
    }

    Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("MentorMessageCanvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        DontDestroyOnLoad(canvasObject);
        return canvas;
    }

    void ShowMessage(OnmyojiHintType type)
    {
        if (messageUI == null)
        {
            EnsureUI();
            if (messageUI == null)
                return;
        }

        if (!CanShowMessage(type))
            return;

        string message = OnmyojiHintCatalog.GetMessage(type);
        if (string.IsNullOrEmpty(message))
            return;

        messageUI.ShowMessage(message, defaultDuration, allowTapToClose: true);
    }

    bool CanShowMessage(OnmyojiHintType type)
    {
        float now = Time.unscaledTime;
        if (lastShownTimes.TryGetValue(type, out float lastTime))
        {
            if (now - lastTime < messageCooldownSeconds)
                return false;
        }

        lastShownTimes[type] = now;
        return true;
    }
}
