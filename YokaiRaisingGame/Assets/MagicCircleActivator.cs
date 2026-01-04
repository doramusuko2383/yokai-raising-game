using UnityEngine;
using UnityEngine.SceneManagement;

public class MagicCircleActivator : MonoBehaviour
{
    MagicCircleSwipeController controller;
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
        controller = FindObjectOfType<MagicCircleSwipeController>(true);

        if (controller == null)
        {
            Debug.LogWarning("MagicCircleActivator: MagicCircleSwipeController が見つかりません。");
            return;
        }

        controller.HealRequested -= OnHealRequested;
        controller.HealRequested += OnHealRequested;
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
        if (controller == null)
        {
            Debug.LogWarning("MagicCircleActivator: controller が未初期化なので再セットアップします。");
            SetupForScene();
        }

        if (controller == null)
        {
            Debug.LogWarning("MagicCircleActivator: MagicCircleSwipeUI が見つからないため表示できません。");
            return;
        }

        pendingRequest = requestType;
        Debug.Log($"[MAGIC CIRCLE] Request received: type={requestType}");
    }

    void OnHealRequested()
    {
        NotifySuccessHooks();

        if (pendingRequest == PurifyRequestType.Normal)
            Debug.Log("[MAGIC CIRCLE] Success route: 通常おきよめ");
        else if (pendingRequest == PurifyRequestType.Emergency)
            Debug.Log("[MAGIC CIRCLE] Success route: 緊急お祓い");
        else
            Debug.LogWarning("[MAGIC CIRCLE] Success route: request が未指定です。");

        pendingRequest = PurifyRequestType.None;
    }

    void NotifySuccessHooks()
    {
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
    }
}
