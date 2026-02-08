using UnityEngine;
using Yokai;
public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    bool hasWarnedMissingRoot;
    bool isVisible;
    YokaiStateController stateController;

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    void OnEnable()
    {
        ResolveAndBind();
    }

    void OnDisable()
    {
        Unbind();
    }

    void ResolveAndBind()
    {
        Unbind();

        stateController =
            CurrentYokaiContext.ResolveStateController()
            ?? FindObjectOfType<YokaiStateController>(true);

        if (stateController == null)
        {
            Debug.LogError("[MAGIC_CIRCLE] StateController not resolved");
            return;
        }

        stateController.OnStateChanged += HandleStateChanged;

        ApplyState(stateController.CurrentState);
    }

    void Unbind()
    {
        if (stateController != null)
            stateController.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(YokaiState previous, YokaiState next)
    {
        ApplyState(next);
    }

    public void ApplyState(YokaiState state)
    {
        if (state == YokaiState.Purifying)
            Show();
        else
            Hide();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void RequestSuccess()
    {
        SuccessRequested?.Invoke();
    }

    public void RequestSuccessEffect()
    {
        SuccessEffectRequested?.Invoke();
    }

    void Awake()
    {
    }

    void Start()
    {
    }

    void SetVisible(bool shouldShow)
    {
        if (magicCircleRoot == null)
        {
            WarnMissingRoot();
            return;
        }

        if (isVisible == shouldShow && magicCircleRoot.activeSelf == shouldShow)
            return;

        magicCircleRoot.SetActive(shouldShow);
        isVisible = shouldShow;
    }

    void WarnMissingRoot()
    {
        if (hasWarnedMissingRoot)
            return;

        Debug.LogWarning("[MAGIC_CIRCLE] Missing MagicCircleRoot reference.");
        hasWarnedMissingRoot = true;
    }

}
