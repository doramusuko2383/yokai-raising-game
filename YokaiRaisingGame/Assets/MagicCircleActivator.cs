using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    // Responsibility: MagicCircleActivator only handles showing/hiding the magic circle UI.
    // It must not drive visibility based on state changes.
    [SerializeField]
    GameObject magicCircleRoot;

    [SerializeField]
    CanvasGroup magicCircleCanvasGroup;

    bool hasWarnedMissingRoot;
    bool isActive;
    bool isVisible;

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    public void BindToStateController(YokaiStateController controller)
    {
        // Intentionally no-op: state should not drive magic circle visibility.
    }

    public void ApplyStateFromPresentation(YokaiState state)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MAGIC_CIRCLE] ApplyState from presentation: {state}");
#endif
        // Intentionally no-op: state should not drive magic circle visibility.
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

    public void EndMagicCircleUI()
    {
        isActive = false;
        isVisible = false;

        if (magicCircleRoot == null)
        {
            WarnMissingRoot();
            return;
        }

        if (magicCircleCanvasGroup == null)
            magicCircleCanvasGroup = magicCircleRoot.GetComponent<CanvasGroup>();

        if (magicCircleCanvasGroup != null)
        {
            magicCircleCanvasGroup.alpha = 0f;
            magicCircleCanvasGroup.interactable = false;
            magicCircleCanvasGroup.blocksRaycasts = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MAGIC_CIRCLE] EndMagicCircleUI");
#endif
        magicCircleRoot.SetActive(false);
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

    void WarnMissingRoot()
    {
        if (hasWarnedMissingRoot)
            return;

        Debug.LogWarning("[MAGIC_CIRCLE] Missing MagicCircleRoot reference.");
        hasWarnedMissingRoot = true;
    }
}
