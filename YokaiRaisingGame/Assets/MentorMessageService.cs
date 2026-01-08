using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MentorMessageService : MonoBehaviour
{
    const string PrefabResourcePath = "UI/MentorMessageUI";

    static MentorMessageService instance;
    static readonly Dictionary<string, float> lastShownTimes = new Dictionary<string, float>();

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

    public static void NotifyDangerEntered()
    {
        EnsureInstance().ShowMessage("danger", "けがれが たまってきとるのう…");
    }

    public static void NotifySpiritEmpty()
    {
        EnsureInstance().ShowMessage("spirit_empty", "ちからが なくなっておる…");
    }

    public static void NotifyRecovered()
    {
        EnsureInstance().ShowMessage("recovered", "うむ、げんきが もどったのう");
    }

    public static void NotifyMononokeEntered()
    {
        EnsureInstance().ShowMessage("mononoke_enter", "けがれが たまりすぎて、モノノケに なってしまった…");
    }

    public static void NotifyMononokeReleased()
    {
        EnsureInstance().ShowMessage("mononoke_release", "なんだか わるい ゆめを みていた みたいじゃ");
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

    void ShowMessage(string messageKey, string message)
    {
        if (messageUI == null)
        {
            EnsureUI();
            if (messageUI == null)
                return;
        }

        if (!CanShowMessage(messageKey))
            return;

        messageUI.ShowMessage(message, defaultDuration, allowTapToClose: true);
    }

    bool CanShowMessage(string messageKey)
    {
        float now = Time.unscaledTime;
        if (lastShownTimes.TryGetValue(messageKey, out float lastTime))
        {
            if (now - lastTime < messageCooldownSeconds)
                return false;
        }

        lastShownTimes[messageKey] = now;
        return true;
    }
}
