using UnityEngine;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    public event System.Action SuccessSeRequested;
    public event System.Action SuccessEffectRequested;
    bool hasWarnedMissingDependencies;

    void Awake()
    {
        LogMissingDependencies();
    }

    void OnEnable()
    {
        LogMissingDependencies();
    }

    void LogMissingDependencies()
    {
        if (hasWarnedMissingDependencies)
            return;

        if (magicCircleRoot == null)
            Debug.LogWarning("[MAGIC CIRCLE] Magic circle root not set in Inspector");

        hasWarnedMissingDependencies = true;
    }

    public void NotifySuccessHooks()
    {
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        SuccessSeRequested?.Invoke();
        SuccessEffectRequested?.Invoke();
    }
}
