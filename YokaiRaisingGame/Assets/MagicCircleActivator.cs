using UnityEngine;

public class MagicCircleActivator : MonoBehaviour
{
    [SerializeField]
    GameObject magicCircleRoot;

    public void Show()
    {
        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(true);
    }

    public void Hide()
    {
        if (magicCircleRoot != null)
            magicCircleRoot.SetActive(false);
    }
}
