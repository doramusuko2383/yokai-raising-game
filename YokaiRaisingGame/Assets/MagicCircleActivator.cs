using UnityEngine;
using UnityEngine.SceneManagement;

public class MagicCircleActivator : MonoBehaviour
{
    const string PrefabPath = "Prefabs/MagicCircleSwipeUI";

    MagicCircleSwipeController controller;
    GameObject uiInstance;
    KegareManager kegareManager;
    EnergyManager energyManager;
    bool? lastShouldShow;
    float lastLogTime;

    public event System.Action SuccessSeRequested;
    public event System.Action SuccessEffectRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        var activatorObject = new GameObject("MagicCircleActivator");
        activatorObject.AddComponent<MagicCircleActivator>();
        DontDestroyOnLoad(activatorObject);
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (controller == null)
            return;

        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        bool shouldShow = ShouldShowMagicCircle();
        LogMagicCircleState(shouldShow);
        if (shouldShow)
        {
            if (!uiInstance.activeSelf)
                uiInstance.SetActive(true);

            controller.Show();
        }
        else
        {
            controller.Hide();
            if (uiInstance.activeSelf)
                uiInstance.SetActive(false);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupForScene();
    }

    void SetupForScene()
    {
        kegareManager = FindObjectOfType<KegareManager>();
        energyManager = FindObjectOfType<EnergyManager>();

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("MagicCircleActivator: Canvas が見つかりません。");
            return;
        }

        if (uiInstance != null)
            Destroy(uiInstance);

        var prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"MagicCircleActivator: Prefab が見つかりません ({PrefabPath})");
            return;
        }

        uiInstance = Instantiate(prefab, canvas.transform);
        controller = uiInstance.GetComponent<MagicCircleSwipeController>();

        if (controller == null)
        {
            Debug.LogWarning("MagicCircleActivator: MagicCircleSwipeController が見つかりません。");
            return;
        }

        controller.HealRequested -= OnHealRequested;
        controller.HealRequested += OnHealRequested;
        controller.Hide();
        uiInstance.SetActive(false);
    }

    bool ShouldShowMagicCircle()
    {
        bool kegareNeeded = kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
        bool energyNeeded = energyManager != null && energyManager.energy <= 0f;
        return kegareNeeded || energyNeeded;
    }

    void LogMagicCircleState(bool shouldShow)
    {
        float kegareValue = kegareManager != null ? kegareManager.kegare : -1f;
        float maxKegare = kegareManager != null ? kegareManager.maxKegare : -1f;
        float energyValue = energyManager != null ? energyManager.energy : -1f;
        float maxEnergy = energyManager != null ? energyManager.maxEnergy : -1f;
        string reason = string.Empty;

        if (shouldShow)
        {
            reason = "threshold met";
        }
        else if (kegareManager == null || energyManager == null)
        {
            reason = "manager missing";
        }
        else
        {
            bool kegareReady = kegareValue >= maxKegare;
            bool energyReady = energyValue <= 0f;
            if (!kegareReady && !energyReady)
            {
                reason = "threshold not met";
            }
        }

        bool shouldLog = !lastShouldShow.HasValue || lastShouldShow.Value != shouldShow || Time.unscaledTime - lastLogTime > 5f;
        if (!shouldLog)
        {
            return;
        }

        lastShouldShow = shouldShow;
        lastLogTime = Time.unscaledTime;

        string stateLabel = shouldShow ? "shown" : "not shown";
        Debug.Log($"[MAGIC CIRCLE] {stateLabel}: kegare={kegareValue:F0}/{maxKegare:F0} energy={energyValue:F0}/{maxEnergy:F0} reason={reason}");
    }

    void OnHealRequested()
    {
        NotifySuccessHooks();

        if (kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare)
        {
            kegareManager.ApplyPurifyFromMagicCircle();
        }

        if (energyManager != null && energyManager.energy <= 0f)
        {
            energyManager.ApplyHealFromMagicCircle();
        }

        controller.Hide();
        if (uiInstance != null)
            uiInstance.SetActive(false);
    }

    void NotifySuccessHooks()
    {
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
    }
}
