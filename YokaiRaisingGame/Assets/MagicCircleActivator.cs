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
        BindStateController(ResolveStateController());
        CurrentYokaiContext.OnCurrentYokaiConfirmed += HandleCurrentYokaiConfirmed;
        SyncFromStateController();
    }

    void OnDisable()
    {
        CurrentYokaiContext.OnCurrentYokaiConfirmed -= HandleCurrentYokaiConfirmed;
        BindStateController(null);
    }

    public void Show()
    {
        SetActive(true);
    }

    public void Hide()
    {
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
        BindStateController(ResolveStateController());
        SyncFromStateController();
    }

    YokaiStateController ResolveStateController()
    {
        return CurrentYokaiContext.ResolveStateController()
            ?? stateController
            ?? FindObjectOfType<YokaiStateController>(true);
    }

    void BindStateController(YokaiStateController controller)
    {
        if (stateController == controller)
            return;

        if (stateController != null)
            stateController.OnStateChanged -= HandleStateChanged;

        stateController = controller;

        if (stateController != null)
            stateController.OnStateChanged += HandleStateChanged;
        else
            WarnMissingStateController();
    }

    void HandleStateChanged(YokaiState previousState, YokaiState newState)
    {
        if (newState == YokaiState.Purifying)
            Show();
        else
            Hide();
    }

    void SyncFromStateController()
    {
        if (stateController == null)
        {
            BindStateController(ResolveStateController());
            if (stateController == null)
            {
                WarnMissingStateController();
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
}
