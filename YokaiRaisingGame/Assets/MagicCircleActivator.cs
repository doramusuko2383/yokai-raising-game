using UnityEngine;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;
    [SerializeField]
    CanvasGroup canvasGroup;

    bool hasWarnedMissingRoot;
    bool isVisible;

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

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
        SetVisible(false);
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

    void ResolveCanvasGroup()
    {
        if (canvasGroup == null && magicCircleRoot != null)
            canvasGroup = magicCircleRoot.GetComponent<CanvasGroup>();
    }

    void SetVisible(bool shouldShow)
    {
        if (magicCircleRoot == null || canvasGroup == null)
        {
            WarnMissingRoot();
            return;
        }

        if (isVisible == shouldShow)
            return;

        canvasGroup.alpha = shouldShow ? 1f : 0f;
        canvasGroup.blocksRaycasts = shouldShow;
        canvasGroup.interactable = shouldShow;
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
