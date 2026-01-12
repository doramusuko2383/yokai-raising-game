using UnityEngine;
using UnityEngine.SceneManagement;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    MagicCircleSwipeController controller;
    [SerializeField]
    Yokai.YokaiStateController stateController;
    Yokai.YokaiStateController subscribedStateController;

    [SerializeField]
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

    void Awake()
    {
        Debug.Log("[MagicCircleActivator][Awake][Enter]");
        InitializeHidden();
        Debug.Log("[MagicCircleActivator][Awake][Exit]");
    }

    void Start()
    {
        Debug.Log("[MagicCircleActivator][Start][Enter]");
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[MagicCircleActivator][Start][Exit]");
    }

    void OnDestroy()
    {
        Debug.Log("[MagicCircleActivator][OnDestroy][Enter]");
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterStateEvents();
        Debug.Log("[MagicCircleActivator][OnDestroy][Exit]");
    }

    void OnDisable()
    {
        Debug.Log("[MagicCircleActivator][OnDisable][Enter]");
        UnregisterStateEvents();
        Debug.Log("[MagicCircleActivator][OnDisable][Exit]");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MagicCircleActivator][OnSceneLoaded][Enter] scene={scene.name} mode={mode}");
        InitializeHidden();
        Debug.Log("[MagicCircleActivator][OnSceneLoaded][Exit]");
    }

    void InitializeHidden()
    {
        Debug.Log("[MagicCircleActivator][InitializeHidden][Enter]");
        controller = EnsureController();
        if (controller == null)
        {
            Debug.LogWarning("[PURIFY] MagicCircleSwipeController が見つかりません。");
            Debug.Log("[MagicCircleActivator][InitializeHidden][EarlyReturn] reason=controller null");
            return;
        }

        controller.HealRequested -= OnHealRequested;
        controller.HealRequested += OnHealRequested;
        controller.InitializeHidden();
        RegisterStateEvents();
#if UNITY_EDITOR
        Debug.Log("[PURIFY] MagicCircleSwipeUI を初期化しました。");
#endif
        Debug.Log("[MagicCircleActivator][InitializeHidden][Exit]");
    }

    public void RequestNormalPurify()
    {
        Debug.Log("[MagicCircleActivator][RequestNormalPurify][Enter]");
        RequestMagicCircle(PurifyRequestType.Normal);
        Debug.Log("[MagicCircleActivator][RequestNormalPurify][Exit]");
    }

    public void RequestEmergencyPurify()
    {
        Debug.Log("[MagicCircleActivator][RequestEmergencyPurify][Enter]");
        RequestMagicCircle(PurifyRequestType.Emergency);
        Debug.Log("[MagicCircleActivator][RequestEmergencyPurify][Exit]");
    }

    void RequestMagicCircle(PurifyRequestType requestType)
    {
        Debug.Log($"[MagicCircleActivator][RequestMagicCircle][Enter] requestType={requestType}");
        pendingRequest = requestType;
        MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeGuide);
#if UNITY_EDITOR
        Debug.Log($"[PURIFY] Request received: type={requestType}");
#endif
        Debug.Log("[MagicCircleActivator][RequestMagicCircle][Exit]");
    }

    void OnHealRequested()
    {
        Debug.Log($"[MagicCircleActivator][OnHealRequested][Enter] pendingRequest={pendingRequest}");
        NotifySuccessHooks();

        if (pendingRequest == PurifyRequestType.Normal)
            Debug.Log("[PURIFY] Success route: 通常おきよめ");
        else if (pendingRequest == PurifyRequestType.Emergency)
            Debug.Log("[PURIFY] Success route: 緊急お祓い");
        else
            Debug.LogWarning("[PURIFY] Success route: request が未指定です。");

        ApplyPurifySuccess();
        pendingRequest = PurifyRequestType.None;
        Debug.Log("[MagicCircleActivator][OnHealRequested][Exit]");
    }

    void NotifySuccessHooks()
    {
        Debug.Log("[MagicCircleActivator][NotifySuccessHooks][Enter]");
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
        Debug.Log("[MagicCircleActivator][NotifySuccessHooks][Exit]");
    }

    MagicCircleSwipeController EnsureController()
    {
        Debug.Log("[MagicCircleActivator][EnsureController][Enter]");
        var resolved = FindObjectOfType<MagicCircleSwipeController>(true);
        if (resolved != null)
        {
            Debug.Log("[MagicCircleActivator][EnsureController][Exit] resolved=existing");
            return resolved;
        }

        var magicCircleRoot = FindMagicCircleRoot();
        if (magicCircleRoot == null)
        {
            Debug.Log("[MagicCircleActivator][EnsureController][EarlyReturn] reason=magicCircleRoot null");
            return null;
        }

        var legacyHandler = magicCircleRoot.GetComponent<MagicCircleSwipeHandler>();
        if (legacyHandler != null)
            legacyHandler.enabled = false;

        var created = magicCircleRoot.GetComponent<MagicCircleSwipeController>();
        if (created == null)
            created = magicCircleRoot.gameObject.AddComponent<MagicCircleSwipeController>();

        Debug.Log("[MagicCircleActivator][EnsureController][Exit] resolved=created");
        return created;
    }

    Transform FindMagicCircleRoot()
    {
        Debug.Log("[MagicCircleActivator][FindMagicCircleRoot][Enter]");
        var rects = FindObjectsOfType<RectTransform>(true);
        foreach (var rect in rects)
        {
            if (rect != null && rect.name == "MagicCircleImage")
            {
                Debug.Log("[MagicCircleActivator][FindMagicCircleRoot][Exit] result=found");
                return rect;
            }
        }

        Debug.Log("[MagicCircleActivator][FindMagicCircleRoot][Exit] result=null");
        return null;
    }

    void ApplyPurifySuccess()
    {
        Debug.Log("[MagicCircleActivator][ApplyPurifySuccess][Enter]");
        if (kegareManager == null)
            kegareManager = CurrentYokaiContext.ResolveKegareManager();

        if (kegareManager != null)
        {
            kegareManager.ApplyPurifyFromMagicCircle();
            Debug.Log("[PURIFY] おきよめ成功");
        }
        else
        {
            Debug.LogWarning("[PURIFY] KegareManager が見つからないため穢れを減らせません。");
        }

        MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeSuccess);

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
        {
            stateController.StopPurifyingForSuccess();
        }
        else
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないため状態更新できません。");
        }
        Debug.Log("[MagicCircleActivator][ApplyPurifySuccess][Exit]");
    }

    void RegisterStateEvents()
    {
        Debug.Log("[MagicCircleActivator][RegisterStateEvents][Enter]");
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null)
        {
            Debug.Log("[MagicCircleActivator][RegisterStateEvents][EarlyReturn] reason=stateController null");
            return;
        }

        if (subscribedStateController == stateController)
        {
            Debug.Log("[MagicCircleActivator][RegisterStateEvents][EarlyReturn] reason=alreadySubscribed");
            return;
        }

        if (subscribedStateController != null)
            subscribedStateController.StateChanged -= OnStateChanged;

        subscribedStateController = stateController;
        subscribedStateController.StateChanged += OnStateChanged;
        Debug.Log("[MagicCircleActivator][RegisterStateEvents][Exit]");
    }

    void UnregisterStateEvents()
    {
        if (subscribedStateController == null)
        {
            Debug.Log("[MagicCircleActivator][UnregisterStateEvents][EarlyReturn] reason=subscribedStateController null");
            return;
        }

        subscribedStateController.StateChanged -= OnStateChanged;
        subscribedStateController = null;
        Debug.Log("[MagicCircleActivator][UnregisterStateEvents][Exit]");
    }

    void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        Debug.Log($"[MagicCircleActivator][OnStateChanged][Enter] previousState={previousState} newState={newState} controller={(controller == null ? "null" : "ok")}");
        if (controller == null)
            controller = EnsureController();

        if (controller == null)
        {
            Debug.LogWarning("[PURIFY] MagicCircleSwipeUI が見つからないため表示できません。");
            Debug.Log("[MagicCircleActivator][OnStateChanged][EarlyReturn] reason=controller null");
            return;
        }

        if (newState == YokaiState.Purifying)
        {
            controller.gameObject.SetActive(true);
            controller.Show();
        }
        else
        {
            controller.Hide();
            pendingRequest = PurifyRequestType.None;
        }
        Debug.Log("[MagicCircleActivator][OnStateChanged][Exit]");
    }

    void OnEnable()
    {
        Debug.Log("[MagicCircleActivator][OnEnable][Enter]");
        InitializeHidden();
        Debug.Log("[MagicCircleActivator][OnEnable][Exit]");
    }
}
