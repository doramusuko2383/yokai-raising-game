using UnityEngine;
using UnityEngine.Serialization;

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
    AudioClip purifyCharge;

    [SerializeField]
    AudioClip purifySuccess;

    [Header("Evolution")]
    [SerializeField]
    AudioClip evolutionStart;

    [Header("Purity")]
    [FormerlySerializedAs("kegareMaxEnter")]
    [SerializeField]
    AudioClip purityEmptyEnter;

    [FormerlySerializedAs("kegareMaxRelease")]
    [SerializeField]
    AudioClip purityEmptyRelease;

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
            case YokaiSE.SE_PURIFY_CHARGE:
                return purifyCharge;
            case YokaiSE.SE_PURIFY_SUCCESS:
                return purifySuccess;
            case YokaiSE.SE_EVOLUTION_START:
                return evolutionStart;
            case YokaiSE.SE_PURITY_EMPTY_ENTER:
                return purityEmptyEnter;
            case YokaiSE.SE_PURITY_EMPTY_RELEASE:
                return purityEmptyRelease;
            case YokaiSE.SE_SPIRIT_EMPTY:
                return spiritEmpty;
            case YokaiSE.SE_SPIRIT_RECOVER:
                return spiritRecover;
            default:
                return null;
        }
    }
}
