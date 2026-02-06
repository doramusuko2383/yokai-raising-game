using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    const string LegacyBlockedMessage = "[LEGACY BLOCKED] MagicCircleActivator";
    const string LegacyUnexpectedMessage = "[LEGACY][ERROR] unexpected magic circle activation";

    [SerializeField]
    GameObject magicCircleRoot;

    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    bool isLegacyDisabled = true;

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
        if (LogLegacyBlocked(nameof(OnEnable)))
            return;

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
        if (LogLegacyBlocked(nameof(ApplyStateFromPresentation)))
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] ApplyState from presentation: {state}");
#endif
        ApplyState(state);
    }

    public void Show()
    {
        LogLegacyUnexpected(nameof(Show));
    }

    public void Hide()
    {
        LogLegacyUnexpected(nameof(Hide));
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
        if (LogLegacyBlocked(nameof(HandleStateChanged)))
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] HandleStateChanged: {previousState} -> {newState}");
#endif
        Hide();
    }

    void SyncFromStateController(bool warnIfMissing)
    {
        if (LogLegacyBlocked(nameof(SyncFromStateController)))
            return;

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
        LogLegacyUnexpected(nameof(SetActive));
    }

    void Awake()
    {
        LogLegacyBlocked(nameof(Awake));
    }

    void Start()
    {
        LogLegacyBlocked(nameof(Start));
    }

    void ApplyState(YokaiState newState)
    {
        if (LogLegacyBlocked(nameof(ApplyState)))
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] ApplyState direct: {newState}");
#endif
        Hide();
    }

    bool LogLegacyBlocked(string methodName)
    {
        if (!isLegacyDisabled)
            return false;

        Debug.Log($"{LegacyBlockedMessage}::{methodName}");
        return true;
    }

    bool LogLegacyUnexpected(string methodName)
    {
        if (!isLegacyDisabled)
            return false;

        Debug.LogError($"{LegacyUnexpectedMessage}::{methodName}");
        return true;
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
