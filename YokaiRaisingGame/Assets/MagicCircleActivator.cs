using UnityEngine;
using UnityEngine.SceneManagement;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    MagicCircleSwipeController controller;
    [SerializeField]
    Yokai.YokaiStateController stateController;

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
        controller = EnsureController();
        if (controller == null)
        {
            Debug.LogWarning("[PURIFY] MagicCircleSwipeController が見つかりません。");
            return;
        }

        controller.HealRequested -= OnHealRequested;
        controller.HealRequested += OnHealRequested;
#if UNITY_EDITOR
        Debug.Log("[PURIFY] MagicCircleSwipeUI を初期化しました。");
#endif
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
        if (controller == null)
        {
            Debug.LogWarning("[PURIFY] controller が未初期化なので再セットアップします。");
            SetupForScene();
        }

        if (controller == null)
        {
            Debug.LogWarning("[PURIFY] MagicCircleSwipeUI が見つからないため表示できません。");
            return;
        }

        if (!CanShowMagicCircle())
        {
            Debug.Log("[PURIFY] 魔法陣の表示条件を満たしていないため表示をスキップします。");
            return;
        }

        pendingRequest = requestType;
        controller.gameObject.SetActive(true);
        controller.Show();
        MentorMessageService.ShowHint(OnmyojiHintType.OkIYomeGuide);
#if UNITY_EDITOR
        Debug.Log($"[PURIFY] Request received: type={requestType}");
#endif
    }

    void OnHealRequested()
    {
        NotifySuccessHooks();

        if (pendingRequest == PurifyRequestType.Normal)
            Debug.Log("[PURIFY] Success route: 通常おきよめ");
        else if (pendingRequest == PurifyRequestType.Emergency)
            Debug.Log("[PURIFY] Success route: 緊急お祓い");
        else
            Debug.LogWarning("[PURIFY] Success route: request が未指定です。");

        ApplyPurifySuccess();
        pendingRequest = PurifyRequestType.None;
    }

    void NotifySuccessHooks()
    {
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
    }

    MagicCircleSwipeController EnsureController()
    {
        var resolved = FindObjectOfType<MagicCircleSwipeController>(true);
        if (resolved != null)
            return resolved;

        var magicCircleRoot = FindMagicCircleRoot();
        if (magicCircleRoot == null)
            return null;

        var legacyHandler = magicCircleRoot.GetComponent<MagicCircleSwipeHandler>();
        if (legacyHandler != null)
            legacyHandler.enabled = false;

        var created = magicCircleRoot.GetComponent<MagicCircleSwipeController>();
        if (created == null)
            created = magicCircleRoot.gameObject.AddComponent<MagicCircleSwipeController>();

        return created;
    }

    Transform FindMagicCircleRoot()
    {
        var rects = FindObjectsOfType<RectTransform>(true);
        foreach (var rect in rects)
        {
            if (rect != null && rect.name == "MagicCircleImage")
                return rect;
        }

        return null;
    }

    void ApplyPurifySuccess()
    {
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
    }

    bool CanShowMagicCircle()
    {
        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController == null)
        {
            Debug.LogWarning("[PURIFY] StateController が見つからないため表示条件を判定できません。");
            return false;
        }

        var energyManager = FindObjectOfType<EnergyManager>();
        if (energyManager != null && !energyManager.HasEverHadEnergy)
            return false;

        return stateController.isPurifying || stateController.currentState == YokaiState.KegareMax;
    }
}
