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

        if (nextYokaiPrefab == null)
        {
            Debug.LogWarning("[EVOLUTION] Next yokai prefab is not assigned.");
            stateController.CompleteEvolution();
            return;
        }

        if (currentYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Current yokai prefab is not assigned.");
        else
        nextYokaiPrefab.SetActive(true);
        // 見た目切り替え
        if (currentYokaiPrefab != null)
            currentYokaiPrefab.SetActive(false);

        if (nextYokaiPrefab != null)
            nextYokaiPrefab.SetActive(true);

        // 成長リセット
        growthController.ResetGrowthState();

        // 完了
        stateController.CompleteEvolution();
    }
}
