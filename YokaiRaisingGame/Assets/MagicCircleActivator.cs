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
    bool isVisible;

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
    }

    public void BindToStateController(YokaiStateController controller)
    {
        BindStateController(controller, warnIfMissing: true);
    }

    public void ApplyStateFromPresentation(YokaiState state)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] ApplyState from presentation: {state}");
#endif
        ApplyState(state);
    }

    public void Show()
    {
        if (isVisible)
            return;

        isVisible = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MAGIC_CIRCLE] Show");
#endif
        SetActive(true);
    }

    public void Hide()
    {
        if (!isVisible)
            return;

        isVisible = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MAGIC_CIRCLE] Hide()");
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogResolution($"[MAGIC_CIRCLE] CurrentYokai confirmed. StateController id={FormatController(stateController)}");
#endif
        SyncFromStateController(warnIfMissing: false);
    }

    YokaiStateController ResolveStateController()
    {
        var presentation = YokaiStatePresentationController.Instance;
        if (presentation != null && presentation.StateController != null)
            return presentation.StateController;

        return CurrentYokaiContext.ResolveStateController();
    }

    void BindStateController(YokaiStateController controller, bool warnIfMissing)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= HandleStateChanged;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var previous = stateController;
#endif
        stateController = controller;

        if (stateController != null)
        {
            stateController.OnStateChanged += HandleStateChanged;
            HandleStateChanged(stateController.currentState, stateController.currentState);
        }
        else if (warnIfMissing)
        {
            WarnMissingStateController();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogResolution($"[MAGIC_CIRCLE] Bind StateController: {FormatController(previous)} -> {FormatController(stateController)}");
#endif
    }

    void HandleStateChanged(YokaiState previousState, YokaiState newState)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] HandleStateChanged: {previousState} -> {newState}");
#endif
        Hide();
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

        ApplyState(stateController.currentState);
    }

    void SetActive(bool isActive)
    {
        this.isActive = isActive;
        isVisible = isActive;

        if (magicCircleRoot == null)
        {
            WarnMissingRoot();
            return;
        }

        magicCircleRoot.SetActive(isActive);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[MAGIC_CIRCLE] SetActive={isActive} root={magicCircleRoot.name} " +
            $"activeSelf={magicCircleRoot.activeSelf} activeInHierarchy={magicCircleRoot.activeInHierarchy}");
#endif
    }

    void ApplyState(YokaiState newState)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] ApplyState direct: {newState}");
#endif
        Hide();
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

    void BindToCurrentStateController(bool warnIfMissing)
    {
        var controller = ResolveStateController();
        BindStateController(controller, warnIfMissing);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogResolutionOnce()
    {
        if (hasLoggedResolution)
            return;

        LogResolution($"[MAGIC_CIRCLE] StateController resolved id={FormatController(stateController)}");
        hasLoggedResolution = true;
    }

    void LogResolution(string message)
    {
        Debug.Log(message);
    }

    string FormatController(YokaiStateController controller)
    {
        if (controller == null)
            return "null";

        return $"{controller.name}#{controller.GetInstanceID()}";
    }
#endif
}
