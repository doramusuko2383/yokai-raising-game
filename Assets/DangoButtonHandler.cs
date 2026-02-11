using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    UIActionController actionController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    float dangoAmount = 30f;

    [Header("UI References")]
    [SerializeField]
    GameObject dangoRoot;

    [SerializeField]
    Button dangoButton;
    bool hasWarnedMissingStateController;
    bool hasWarnedMissingSpiritController;
    bool hasWarnedMissingAudioHook;
    bool hasWarnedMissingAudioClip;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool hasLoggedDependencyResolution;
#endif

    void Awake()
    {
        ResolveDependencies(logIfMissingOnce: true);
    }

    public void OnClickDango()
    {
        ResolveDependencies(logIfMissingOnce: true);
        TryPlayDangoSE();

        if (actionController == null)
        {
            Debug.LogWarning("[DANGO] UIActionController not set in Inspector");
            return;
        }

        actionController.Execute(YokaiAction.EatDango);
    }

    public void RefreshUI()
    {
        ResolveDependencies(logIfMissingOnce: false);
        if (stateController == null)
        {
            SetAllDisabled();
            return;
        }

        bool canEat = stateController.CanDo(YokaiAction.EatDango);
        if (dangoRoot != null)
            dangoRoot.SetActive(canEat);
        if (dangoButton != null)
            dangoButton.interactable = canEat;
    }

    void SetAllDisabled()
    {
        if (dangoRoot != null)
            dangoRoot.SetActive(false);
        if (dangoButton != null)
            dangoButton.interactable = false;
    }

    void ResolveDependencies(bool logIfMissingOnce)
    {
        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>(true);

        if (spiritController == null)
            spiritController = FindObjectOfType<SpiritController>(true);

        bool hasAudioHook = EnsureAudioResolver(logIfMissingOnce);

        if (logIfMissingOnce)
        {
            if (stateController == null)
                WarnMissingStateController();

            if (spiritController == null)
                WarnMissingSpiritController();

            if (!hasAudioHook)
                WarnMissingAudioHook();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogDependencyResolutionOnce(hasAudioHook);
#endif
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[DANGO] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }

    void WarnMissingSpiritController()
    {
        if (hasWarnedMissingSpiritController)
            return;

        Debug.LogWarning("[SPIRIT] SpiritController が見つからないためだんごが使えません。");
        hasWarnedMissingSpiritController = true;
    }

    bool EnsureAudioResolver(bool logIfMissingOnce)
    {
        if (AudioHook.ClipResolver != null)
            return true;

        if (AudioHook.ClipResolver == null && logIfMissingOnce)
            WarnMissingAudioHook();

        return AudioHook.ClipResolver != null;
    }

    void TryPlayDangoSE()
    {
        bool hasAudioHook = EnsureAudioResolver(logIfMissingOnce: true);
        if (!hasAudioHook)
            return;

        try
        {
            if (!AudioHook.TryResolveClip(YokaiSE.SE_SPIRIT_RECOVER, out _))
                WarnMissingAudioClip();

            AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        }
        catch (System.Exception ex)
        {
            WarnMissingAudioHook(ex);
        }
    }

    void WarnMissingAudioClip()
    {
        if (hasWarnedMissingAudioClip)
            return;

        Debug.LogWarning("[SE] SE_SPIRIT_RECOVER clip is not resolved.");
        hasWarnedMissingAudioClip = true;
    }

    void WarnMissingAudioHook(System.Exception ex = null)
    {
        if (hasWarnedMissingAudioHook)
            return;

        if (ex != null)
            Debug.LogWarning($"[DANGO] Missing AudioHook or resolver. {ex.Message}");
        else
            Debug.LogWarning("[DANGO] Missing AudioHook or resolver.");

        hasWarnedMissingAudioHook = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogDependencyResolutionOnce(bool hasAudioHook)
    {
        if (hasLoggedDependencyResolution)
            return;

        string audioStatus = hasAudioHook ? "OK" : "Missing";
        string stateStatus = stateController != null ? "OK" : "Missing";
        string spiritStatus = spiritController != null ? "OK" : "Missing";
        Debug.Log($"[DANGO] deps: audioHook={audioStatus}, stateController={stateStatus}, spiritController={spiritStatus}");
        hasLoggedDependencyResolution = true;
    }
#endif
}
