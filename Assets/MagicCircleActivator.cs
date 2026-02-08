using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    YokaiStateController stateController;
    bool isBound;

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

    public void ApplyState(YokaiState state)
    {
        if (state == YokaiState.Purifying)
            Show();
        else
            Hide();
    }

    private void Show()
    {
        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(true);
    }

    private void Hide()
    {
        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(false);
    }
}
