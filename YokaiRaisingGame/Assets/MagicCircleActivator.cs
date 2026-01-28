using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    [SerializeField]
    YokaiStateController stateController;

    bool hasWarnedMissingRoot;
    bool hasWarnedMissingStateController;
    bool hasLoggedResolution;
    bool isActive;
    YokaiState? lastObservedState;

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    void OnEnable()
    {
        EnsureStateController(warnIfMissing: false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogResolutionOnce();
#endif
        CurrentYokaiContext.OnCurrentYokaiConfirmed += HandleCurrentYokaiConfirmed;
        SyncFromStateController(warnIfMissing: false);
    }

    void OnDisable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        BindStateController(null, warnIfMissing: false);
        lastObservedState = null;
    }

    public void Show()
    {
        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
    }

    public void Activate()
    {
        if (isActive)
            return;

        SetActive(true);
    }

    public void RequestSuccess()
    {
        SuccessRequested?.Invoke();
    }

    public void RequestSuccessEffect()
    {
        SuccessEffectRequested?.Invoke();
    }

    void HandleCurrentYokaiConfirmed(GameObject activeYokai)
    {
        EnsureStateController(warnIfMissing: false);
        SyncFromStateController(warnIfMissing: false);
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);
    }

    void BindStateController(YokaiStateController controller, bool warnIfMissing)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= HandleStateChanged;

        stateController = controller;

        if (stateController != null)
            stateController.OnStateChanged += HandleStateChanged;
        else if (warnIfMissing)
            WarnMissingStateController();
    }

    void HandleStateChanged(YokaiState previousState, YokaiState newState)
    {
        HandleStateTransition(previousState, newState);
    }

    void SyncFromStateController(bool warnIfMissing)
    {
        if (stateController == null)
        {
            EnsureStateController(warnIfMissing);
            if (stateController == null)
            {
                return;
            }
        }

        HandleStateTransition(lastObservedState, stateController.currentState);
    }

    void SetActive(bool isActive)
    {
        this.isActive = isActive;

        if (magicCircleRoot == null)
        {
            WarnMissingRoot();
            return;
        }

        magicCircleRoot.SetActive(isActive);
    }

    void HandleStateTransition(YokaiState? previousState, YokaiState newState)
    {
        if (previousState.HasValue && previousState.Value == newState)
            return;

        if (newState == YokaiState.Purifying && previousState != YokaiState.Purifying)
            Show();
        else if (previousState == YokaiState.Purifying && newState != YokaiState.Purifying)
            Hide();
        else if (!previousState.HasValue)
        {
            if (newState == YokaiState.Purifying)
                Show();
            else
                Hide();
        }

        lastObservedState = newState;
    }

    void WarnMissingRoot()
    {
        if (hasWarnedMissingRoot)
            return;

        Debug.LogWarning("[MAGIC_CIRCLE] Missing MagicCircleRoot reference.");
        hasWarnedMissingRoot = true;
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[MAGIC_CIRCLE] Missing StateController reference.");
        hasWarnedMissingStateController = true;
    }

    void EnsureStateController(bool warnIfMissing)
    {
        BindStateController(ResolveStateController(), warnIfMissing);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogResolutionOnce()
    {
        if (hasLoggedResolution)
            return;

        Debug.Log($"[MAGIC_CIRCLE] StateController resolved: {stateController != null}");
        hasLoggedResolution = true;
    }
#endif
}
