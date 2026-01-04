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

    [SerializeField]
    string yokaiChildName = "YokaiChild";

    [SerializeField]
    string yokaiAdultName = "YokaiAdult";

    /// <summary>
    /// UI Button から呼ばれる進化トリガー
    /// </summary>
    public void OnClickEvolve()
    {
        if (growthController == null)
        {
            Debug.LogWarning("[EVOLUTION] GrowthController is null");
            return;
        }

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController == null)
        {
            Debug.LogWarning("[EVOLUTION] StateController not found");
            return;
        }

        if (stateController.currentState != YokaiState.EvolutionReady)
        {
            Debug.Log($"[EVOLUTION] Tap ignored. CurrentState={stateController.currentState}");
            return;
        }

        Debug.Log("[EVOLUTION] Evolution triggered by tap");

        ResolveYokaiReferences();

        if (currentYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Current yokai prefab is not assigned.");
        // 見た目切り替え
        if (currentYokaiPrefab != null)
            currentYokaiPrefab.SetActive(false);

        if (nextYokaiPrefab != null)
            nextYokaiPrefab.SetActive(true);
        else
            Debug.LogWarning("[EVOLUTION] Next yokai prefab is not assigned.");

        // 成長リセット
        if (nextYokaiPrefab != null)
        {
            var nextGrowthController = nextYokaiPrefab.GetComponent<YokaiGrowthController>();
            if (nextGrowthController != null)
                nextGrowthController.ResetGrowthState();
            else if (growthController != null)
                growthController.ResetGrowthState();
        }

        // 完了
        Debug.Log("[EVOLUTION] Evolution completed. Switching to Normal state.");
        stateController.CompleteEvolution();
    }

    void ResolveYokaiReferences()
    {
        if (currentYokaiPrefab == null)
        {
            var childTransform = transform.Find(yokaiChildName);
            if (childTransform != null)
                currentYokaiPrefab = childTransform.gameObject;
        }

        if (nextYokaiPrefab == null)
        {
            var adultTransform = transform.Find(yokaiAdultName);
            if (adultTransform != null)
                nextYokaiPrefab = adultTransform.gameObject;
        }
    }
}
