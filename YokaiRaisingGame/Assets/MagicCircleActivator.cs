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

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    void OnEnable()
    {
        EnsureStateController(warnIfMissing: false);
        CurrentYokaiContext.OnCurrentYokaiConfirmed += HandleCurrentYokaiConfirmed;
        SyncFromStateController(warnIfMissing: false);
    }

    void OnDisable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        BindStateController(null, warnIfMissing: false);
    }

    public void Show()
    {
        EnsureStateController(warnIfMissing: true);
        SetActive(true);
    }

    public void Hide()
    {
        EnsureStateController(warnIfMissing: true);
        SetActive(false);
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
        EnsureStateController(warnIfMissing: true);
        SyncFromStateController(warnIfMissing: true);
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
        if (newState == YokaiState.Purifying)
            Show();
        else
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

        if (stateController.currentState == YokaiState.Purifying)
            Show();
        else
            Hide();
    }

    void SetActive(bool isActive)
    {
        if (magicCircleRoot == null)
        {
            WarnMissingRoot();
            return;
        }

        magicCircleRoot.SetActive(isActive);
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
}
