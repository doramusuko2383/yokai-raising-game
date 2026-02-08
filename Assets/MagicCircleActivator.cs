using UnityEngine;
public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    bool hasWarnedMissingRoot;
    bool isVisible;

    public event System.Action SuccessRequested;
    public event System.Action SuccessEffectRequested;

    public bool HasMagicCircleRoot => magicCircleRoot != null;

    void OnEnable()
    {
        SetVisible(false);
    }

    void OnDisable()
    {
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
