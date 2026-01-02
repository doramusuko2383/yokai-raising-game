using UnityEngine;
using Yokai;

public class YokaiEvolutionController : MonoBehaviour
{
    [SerializeField]
    YokaiGrowthController growthController;

    [SerializeField]
    GameObject currentYokaiPrefab;

    [SerializeField]
    GameObject nextYokaiPrefab;

    [SerializeField]
    YokaiStateController stateController;

    void Update()
    {
        if (growthController == null)
        {
            return;
        }

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null || stateController.currentState != YokaiState.EvolutionReady)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Debug.Log($"[EVOLUTION] Trigger received for {gameObject.name}");

        stateController.BeginEvolution();

        if (currentYokaiPrefab != null)
        {
            currentYokaiPrefab.SetActive(false);
        }

        if (nextYokaiPrefab != null)
        {
            nextYokaiPrefab.SetActive(true);
        }

        growthController.ResetGrowthState();
        stateController.CompleteEvolution();
    }
}
