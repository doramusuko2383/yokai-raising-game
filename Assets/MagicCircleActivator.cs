using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;
    [SerializeField]
    CanvasGroup canvasGroup;

    YokaiStateController stateController;
    bool isBound;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    void Awake()
    {
        ResolveCanvasGroup();
    }

    void OnValidate()
    {
        ResolveCanvasGroup();
    }

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
        isBound = true;

        // ★ 初期状態を必ず反映
        ApplyState(stateController.CurrentState);
    }

    void Unbind()
    {
        if (isBound && stateController != null)
        {
            stateController.OnStateChanged -= HandleStateChanged;
        }

        isBound = false;
        stateController = null;
    }

    void HandleStateChanged(YokaiState previous, YokaiState next)
    {
        ApplyState(next);
    }

    void ResolveCanvasGroup()
    {
        if (canvasGroup == null && magicCircleRoot != null)
            canvasGroup = magicCircleRoot.GetComponent<CanvasGroup>();
    }

    public void ApplyState(YokaiState state)
    {
        if (state == YokaiState.Purifying)
            Show();
        else
            Hide();
    }

    private void Show()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    private void Hide()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
