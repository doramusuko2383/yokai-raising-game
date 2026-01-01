using UnityEngine;
using UnityEngine.SceneManagement;

public class MagicCircleActivator : MonoBehaviour
{
    const string PrefabPath = "Prefabs/MagicCircleSwipeUI";

    MagicCircleSwipeController controller;
    GameObject uiInstance;
    KegareManager kegareManager;
    EnergyManager energyManager;

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

    void OnHealRequested()
    {
        NotifySuccessHooks();

        if (kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare)
        {
            kegareManager.ApplyPurify();
        }

        if (energyManager != null && energyManager.energy <= 0f)
        {
            energyManager.ApplyHeal();
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
