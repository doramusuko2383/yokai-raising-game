using UnityEngine;
using Yokai;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    [SerializeField]
    YokaiStateController stateController;

    bool hasWarnedMissingStateController;

    public void Show()
    {
        if (!HasStateController())
            return;

        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(true);
    }

    public void Hide()
    {
        if (!HasStateController())
            return;

        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(false);
    }

    bool HasStateController()
    {
        if (stateController != null)
            return true;

        if (!hasWarnedMissingStateController)
        {
            Debug.LogWarning("[MAGIC_CIRCLE] Missing StateController reference.");
            hasWarnedMissingStateController = true;
        }

        return false;
    }
}
