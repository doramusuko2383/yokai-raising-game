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

        Debug.Log($"[EVOLUTION] Trigger received for {gameObject.name}");

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
