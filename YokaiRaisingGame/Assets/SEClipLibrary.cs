using UnityEngine;

[CreateAssetMenu(fileName = "SEClipLibrary", menuName = "Yokai/SE Clip Library")]
public class SEClipLibrary : ScriptableObject
{
    [Header("UI")]
    [SerializeField]
    AudioClip uiClick;

    [Header("Purify")]
    [SerializeField]
    AudioClip purifyStart;

    [SerializeField]
    AudioClip purifySuccess;

    [Header("Evolution")]
    [SerializeField]
    AudioClip evolutionStart;

    [Header("Kegare")]
    [SerializeField]
    AudioClip kegareMaxEnter;

    [SerializeField]
    AudioClip kegareMaxRelease;

    [Header("Spirit")]
    [SerializeField]
    AudioClip spiritEmpty;

    [SerializeField]
    AudioClip spiritRecover;

    public AudioClip ResolveClip(YokaiSE se)
    {
        switch (se)
        {
            case YokaiSE.SE_UI_CLICK:
                return uiClick;
            case YokaiSE.SE_PURIFY_START:
                return purifyStart;
            case YokaiSE.SE_PURIFY_SUCCESS:
                return purifySuccess;
            case YokaiSE.SE_EVOLUTION_START:
                return evolutionStart;
            case YokaiSE.SE_KEGARE_MAX_ENTER:
                return kegareMaxEnter;
            case YokaiSE.SE_KEGARE_MAX_RELEASE:
                return kegareMaxRelease;
            case YokaiSE.SE_SPIRIT_EMPTY:
                return spiritEmpty;
            case YokaiSE.SE_SPIRIT_RECOVER:
                return spiritRecover;
            default:
                return null;
        }
    }
}

public static class SEClipLibraryRuntime
{
    const string ResourcePath = "SEClipLibrary";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        var library = Resources.Load<SEClipLibrary>(ResourcePath);
        if (library == null)
        {
            Debug.LogWarning("[SE] SEClipLibrary not found in Resources.");
            return;
        }

        AudioHook.ClipResolver = library.ResolveClip;
    }
}
