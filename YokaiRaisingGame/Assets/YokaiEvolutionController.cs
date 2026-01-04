using UnityEngine;
using UnityEngine.UI;
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
    Transform characterRoot;

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
        LogYokaiActiveState("[EVOLUTION][Before]");

        if (currentYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Current yokai prefab is not assigned.");
        if (nextYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Next yokai prefab is not assigned.");

        // 見た目切り替え
        SwitchYokaiVisibility();

        // 成長リセット
        if (nextYokaiPrefab != null)
        {
            var nextGrowthController = nextYokaiPrefab.GetComponent<YokaiGrowthController>();
            if (nextGrowthController != null)
            {
                nextGrowthController.ResetGrowthState();
                growthController = nextGrowthController;
            }
            else if (growthController != null)
                growthController.ResetGrowthState();

            if (stateController != null)
                stateController.SetActiveYokai(nextYokaiPrefab);
        }

        // 完了
        Debug.Log("[EVOLUTION] Evolution completed. Switching to Normal state.");
        stateController.CompleteEvolution();
        LogYokaiActiveState("[EVOLUTION][After]");
    }

    void ResolveYokaiReferences()
    {
        if (characterRoot == null)
        {
            Debug.LogWarning("[EVOLUTION] CharacterRoot is not assigned.");
            return;
        }

        if (currentYokaiPrefab == null)
        {
            currentYokaiPrefab = FindYokaiByName(yokaiChildName);
            if (currentYokaiPrefab == null)
                Debug.LogWarning($"[EVOLUTION] Child yokai not found under CharacterRoot: {yokaiChildName}");
        }

        if (nextYokaiPrefab == null)
        {
            nextYokaiPrefab = FindYokaiByName(yokaiAdultName);
            if (nextYokaiPrefab == null)
                Debug.LogWarning($"[EVOLUTION] Adult yokai not found under CharacterRoot: {yokaiAdultName}");
        }
    }

    GameObject FindYokaiByName(string targetName)
    {
        if (characterRoot == null || string.IsNullOrEmpty(targetName))
            return null;

        var transforms = characterRoot.GetComponentsInChildren<Transform>(true);
        foreach (var child in transforms)
        {
            if (child == null || child == characterRoot)
                continue;

            if (child.name == targetName)
                return child.gameObject;
        }

        return null;
    }

    void SwitchYokaiVisibility()
    {
        if (currentYokaiPrefab != null)
        {
            SetYokaiInteractivity(currentYokaiPrefab, false);
            currentYokaiPrefab.SetActive(false);
        }

        if (nextYokaiPrefab != null)
        {
            nextYokaiPrefab.SetActive(true);
            SetYokaiInteractivity(nextYokaiPrefab, true);
        }
    }

    void SetYokaiInteractivity(GameObject target, bool enabled)
    {
        if (target == null)
            return;

        foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            collider.enabled = enabled;

        foreach (var collider2d in target.GetComponentsInChildren<Collider2D>(true))
            collider2d.enabled = enabled;

        foreach (var button in target.GetComponentsInChildren<Button>(true))
            button.enabled = enabled;

        foreach (var controller in target.GetComponentsInChildren<YokaiGrowthController>(true))
            controller.enabled = enabled;

        foreach (var effect in target.GetComponentsInChildren<YokaiDangerEffect>(true))
            effect.enabled = enabled;
    }

    void LogYokaiActiveState(string prefix)
    {
        if (characterRoot == null)
        {
            Debug.LogWarning($"{prefix} CharacterRoot is not assigned.");
            return;
        }

        int activeCount = 0;
        activeCount += currentYokaiPrefab != null && currentYokaiPrefab.activeInHierarchy ? 1 : 0;
        activeCount += nextYokaiPrefab != null && nextYokaiPrefab.activeInHierarchy ? 1 : 0;

        Debug.Log($"{prefix} Child={(currentYokaiPrefab != null ? currentYokaiPrefab.name : "null")} active={currentYokaiPrefab != null && currentYokaiPrefab.activeInHierarchy}, " +
                  $"Adult={(nextYokaiPrefab != null ? nextYokaiPrefab.name : "null")} active={nextYokaiPrefab != null && nextYokaiPrefab.activeInHierarchy}, " +
                  $"ActiveCount={activeCount}");

        if (activeCount != 1)
            Debug.LogWarning($"{prefix} Expected exactly one active yokai under CharacterRoot. ActiveCount={activeCount}");
    }
}
