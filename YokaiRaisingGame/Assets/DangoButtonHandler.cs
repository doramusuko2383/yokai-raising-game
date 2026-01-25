using UnityEngine;
using UnityEngine.Serialization;
using Yokai;

public class DangoButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [FormerlySerializedAs("energyManager")]
    [SerializeField]
    SpiritController spiritController;

    [SerializeField]
    float dangoAmount = 30f;
    bool hasWarnedMissingStateController;
    bool hasWarnedMissingSpiritController;
    bool hasWarnedMissingAudioClip;
    bool hasWarnedMissingAudioLibrary;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool hasLoggedAudioResolution;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogAudioResolutionOnce();
#endif
    }

    public void OnClickDango()
    {
        EnsureAudioClipResolver();
        if (!AudioHook.TryResolveClip(YokaiSE.SE_SPIRIT_RECOVER, out _))
            WarnMissingAudioClip();
        AudioHook.RequestPlay(YokaiSE.SE_SPIRIT_RECOVER);
        ResolveStateController();
        if (IsActionBlocked())
            return;

        ResolveSpiritController();

        if (spiritController != null)
        {
            spiritController.AddSpirit(dangoAmount);
            TutorialManager.NotifyDangoUsed();
            MentorMessageService.ShowHint(OnmyojiHintType.EnergyRecovered);
            stateController?.RequestEvaluateState("SpiritRecovered");
        }
        else
        {
            WarnMissingSpiritController();
        }
    }

    bool IsActionBlocked()
    {
        ResolveStateController();

        if (stateController == null)
            return false;

        return stateController.currentState != YokaiState.Normal
            && stateController.currentState != YokaiState.EvolutionReady;
    }

    void ResolveStateController()
    {
        if (stateController != null)
            return;

        stateController = FindObjectOfType<YokaiStateController>(true);
        if (stateController == null)
            WarnMissingStateController();
    }

    void ResolveSpiritController()
    {
        if (spiritController != null)
            return;

        spiritController = FindObjectOfType<SpiritController>(true);
        if (spiritController == null)
            WarnMissingSpiritController();
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

    void EnsureAudioClipResolver()
    {
        if (AudioHook.ClipResolver != null)
            return;

        var library = Resources.Load<SEClipLibrary>("SEClipLibrary");
        if (library != null)
        {
            AudioHook.ClipResolver = library.ResolveClip;
            return;
        }

        WarnMissingAudioLibrary();
    }

    void WarnMissingAudioClip()
    {
        if (hasWarnedMissingAudioClip)
            return;

        Debug.LogWarning("[SE] SE_SPIRIT_RECOVER clip is not resolved.");
        hasWarnedMissingAudioClip = true;
    }

    void WarnMissingAudioLibrary()
    {
        if (hasWarnedMissingAudioLibrary)
            return;

        Debug.LogWarning("[SE] SEClipLibrary not found in Resources.");
        hasWarnedMissingAudioLibrary = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogAudioResolutionOnce()
    {
        if (hasLoggedAudioResolution)
            return;

        bool hasResolver = AudioHook.ClipResolver != null;
        Debug.Log($"[SE] Dango AudioHook resolver ready: {hasResolver}");
        hasLoggedAudioResolution = true;
    }
#endif
}
