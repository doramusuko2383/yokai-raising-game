using UnityEngine;

public class YokaiEvolutionController : MonoBehaviour
{
    [SerializeField]
    YokaiGrowthController growthController;

    [SerializeField]
    GameObject currentYokaiPrefab;

    [SerializeField]
    GameObject nextYokaiPrefab;

    void Update()
    {
        if (growthController == null)
        {
            return;
        }

        if (!growthController.isEvolutionReady)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (currentYokaiPrefab != null)
        {
            currentYokaiPrefab.SetActive(false);
        }

        if (nextYokaiPrefab != null)
        {
            nextYokaiPrefab.SetActive(true);
        }

        growthController.ResetGrowthState();
    }
}
