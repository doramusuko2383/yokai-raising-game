using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    GameObject magicCircleRoot;

    public event System.Action SuccessSeRequested;
    public event System.Action SuccessEffectRequested;

    void Awake()
    {
        LogMissingDependencies();
        ApplyState(stateController != null ? stateController.currentState : YokaiState.Normal);
    }

    void OnEnable()
    {
        BindStateController(ResolveStateController());
        LogMissingDependencies();
        ApplyState(stateController != null ? stateController.currentState : YokaiState.Normal);
        CurrentYokaiContext.CurrentChanged += HandleCurrentYokaiChanged;
    }

    void OnDisable()
    {
        CurrentYokaiContext.CurrentChanged -= HandleCurrentYokaiChanged;
        BindStateController(null);
    }

    void BindStateController(YokaiStateController controller)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= OnStateChanged;

        stateController = controller;

        if (stateController != null)
            stateController.OnStateChanged += OnStateChanged;
    }

    void HandleCurrentYokaiChanged(GameObject activeYokai)
    {
        BindStateController(ResolveStateController());
        ApplyState(stateController != null ? stateController.currentState : YokaiState.Normal);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController() ?? stateController;
    }

    void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        ApplyState(newState);
    }

    void ApplyState(YokaiState state)
    {
        if (magicCircleRoot == null)
            return;

        bool shouldShow = state == YokaiState.Purifying;
        if (magicCircleRoot.activeSelf != shouldShow)
            magicCircleRoot.SetActive(shouldShow);
    }

    void LogMissingDependencies()
    {
        if (stateController == null)
            Debug.LogError("[MAGIC CIRCLE] StateController not set in Inspector");
        if (magicCircleRoot == null)
            Debug.LogError("[MAGIC CIRCLE] Magic circle root not set in Inspector");
    }

    public void NotifySuccessHooks()
    {
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
    }
}
