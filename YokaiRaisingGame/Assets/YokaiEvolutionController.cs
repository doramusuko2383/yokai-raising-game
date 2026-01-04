using System.Collections;
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

    bool isEvolving;

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

        if (isEvolving)
        {
            Debug.Log("[EVOLUTION] Tap ignored. Evolution already in progress.");
            return;
        }

        Debug.Log("[EVOLUTION][Start] Evolution triggered by tap");
        StartCoroutine(EvolutionSequence());
    }

    IEnumerator EvolutionSequence()
    {
        isEvolving = true;

        ResolveYokaiReferences();
        LogYokaiActiveState("[EVOLUTION][Before]");

        if (currentYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Current yokai prefab is not assigned.");
        if (nextYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Next yokai prefab is not assigned.");

        if (currentYokaiPrefab != null)
            yield return PlayEvolutionStartEffect(currentYokaiPrefab.transform);
        else if (characterRoot != null)
            yield return PlayEvolutionStartEffect(characterRoot);

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
        Debug.Log("[EVOLUTION][Complete] Evolution completed. Switching to Normal state.");
        stateController.CompleteEvolution();
        LogYokaiActiveState("[EVOLUTION][After]");
        isEvolving = false;
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

    IEnumerator PlayEvolutionStartEffect(Transform target)
    {
        if (target == null)
            yield break;

        SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(true);
        Image[] images = target.GetComponentsInChildren<Image>(true);
        Color[] spriteColors = new Color[sprites.Length];
        Color[] imageColors = new Color[images.Length];

        for (int i = 0; i < sprites.Length; i++)
            spriteColors[i] = sprites[i] != null ? sprites[i].color : Color.white;

        for (int i = 0; i < images.Length; i++)
            imageColors[i] = images[i] != null ? images[i].color : Color.white;

        Vector3 originalScale = target.localScale;
        Vector3 peakScale = originalScale * 1.1f;
        float upDuration = 0.08f;
        float downDuration = 0.12f;
        float timer = 0f;
        Color flashColor = Color.white;
        float flashIntensity = 0.8f;

        ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, 0f);

        while (timer < upDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / upDuration);
            target.localScale = Vector3.Lerp(originalScale, peakScale, t);
            yield return null;
        }

        timer = 0f;
        while (timer < downDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / downDuration);
            target.localScale = Vector3.Lerp(peakScale, originalScale, t);
            ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, t);
            yield return null;
        }

        target.localScale = originalScale;
        ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, 1f);
    }

    void ApplyFlashColors(SpriteRenderer[] sprites, Color[] spriteColors, Image[] images, Color[] imageColors, Color flashColor, float intensity, float returnT)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color flash = Color.Lerp(spriteColors[i], flashColor, intensity);
            sprites[i].color = Color.Lerp(flash, spriteColors[i], returnT);
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            Color flash = Color.Lerp(imageColors[i], flashColor, intensity);
            images[i].color = Color.Lerp(flash, imageColors[i], returnT);
        }
    }
}
