using UnityEngine;
using UnityEngine.SceneManagement;

public class MagicCircleActivator : MonoBehaviour
{
    const string PrefabPath = "Prefabs/MagicCircleSwipeUI";

    MagicCircleSwipeController controller;
    GameObject uiInstance;
    KegareManager kegareManager;
    PurifyRequestType pendingRequest = PurifyRequestType.None;

    public event System.Action SuccessSeRequested;
    public event System.Action SuccessEffectRequested;

    enum PurifyRequestType
    {
        None,
        Normal,
        Emergency
    }

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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupForScene();
    }

    void SetupForScene()
    {
        kegareManager = FindObjectOfType<KegareManager>();

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
        Debug.Log("MagicCircleActivator: MagicCircleSwipeUI を初期化しました。");
    }

    public void RequestNormalPurify()
    {
        RequestMagicCircle(PurifyRequestType.Normal);
    }

    public void RequestEmergencyPurify()
    {
        RequestMagicCircle(PurifyRequestType.Emergency);
    }

    void RequestMagicCircle(PurifyRequestType requestType)
    {
        if (controller == null || uiInstance == null)
        {
            Debug.LogWarning("MagicCircleActivator: controller が未初期化なので再セットアップします。");
            SetupForScene();
        }

        if (controller == null || uiInstance == null)
        {
            Debug.LogWarning("MagicCircleActivator: MagicCircleSwipeUI が見つからないため表示できません。");
            return;
        }

        pendingRequest = requestType;
        Debug.Log($"[MAGIC CIRCLE] Request received: type={requestType}");

        if (!uiInstance.activeSelf)
            uiInstance.SetActive(true);

        controller.Show();
    }

    void OnHealRequested()
    {
        NotifySuccessHooks();

        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (pendingRequest == PurifyRequestType.Normal)
        {
            Debug.Log("[MAGIC CIRCLE] Success route: 通常おきよめ");
            if (kegareManager != null)
                kegareManager.ApplyPurify();
        }
        else if (pendingRequest == PurifyRequestType.Emergency)
        {
            Debug.Log("[MAGIC CIRCLE] Success route: 緊急お祓い");
            if (kegareManager != null)
                kegareManager.ApplyPurifyFromMagicCircle();

            var energyManager = FindObjectOfType<EnergyManager>();
            if (energyManager != null)
                energyManager.ApplyHealFromMagicCircle();
        }
        else
        {
            Debug.LogWarning("[MAGIC CIRCLE] Success route: request が未指定です。");
        }

        pendingRequest = PurifyRequestType.None;

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
