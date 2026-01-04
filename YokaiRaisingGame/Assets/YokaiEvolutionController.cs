using System.Collections;
using System.Collections.Generic;
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
    const float ChargeScaleMultiplier = 1.06f;
    const float BurstScaleMultiplier = 1.18f;
    const float ChargeDuration = 0.55f;
    const float BurstDuration = 0.09f;
    const float SettleDuration = 0.32f;
    const float ChargeWobbleAmplitude = 0.015f;
    const float ChargeWobbleFrequency = 5.5f;
    const float BurstRecoilTimeScale = 0.85f;
    const float BurstRecoilDuration = 0.12f;

    bool isRecoilActive;
    float recoilOriginalTimeScale;

    struct DangerEffectState
    {
        public YokaiDangerEffect effect;
        public bool wasBlinking;
        public bool wasEnabled;
    }

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

        Debug.Log($"{FormatEvolutionLog("Start")} Evolution triggered by tap");
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

        var dangerEffectStates = new List<DangerEffectState>();
        PauseDangerEffects(dangerEffectStates);

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
        SEHub.Play(YokaiSE.Evolution_Complete);
        Debug.Log($"{FormatEvolutionLog("Complete")} Evolution completed. Switching to Normal state.");
        stateController.CompleteEvolution();
        LogYokaiActiveState("[EVOLUTION][After]");
        ResumeDangerEffects(dangerEffectStates);
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

    void PauseDangerEffects(List<DangerEffectState> states)
    {
        if (states == null)
            return;

        if (characterRoot == null)
        {
            Debug.LogWarning("[EVOLUTION][DangerEffect] CharacterRoot is not assigned. Cannot pause danger effects.");
            return;
        }

        var effects = characterRoot.GetComponentsInChildren<YokaiDangerEffect>(true);
        Debug.Log($"{FormatEvolutionLog("DangerEffect:Pause:Start")} Found {effects.Length} effects to pause.");

        foreach (var effect in effects)
        {
            if (effect == null)
                continue;

            states.Add(new DangerEffectState
            {
                effect = effect,
                wasBlinking = effect.IsBlinking,
                wasEnabled = effect.enabled
            });

            effect.SetSuppressed(true);
            if (effect.enabled && effect.IsBlinking)
                effect.SetBlinking(false);
        }

        Debug.Log($"{FormatEvolutionLog("DangerEffect:Pause:End")} Danger effects paused.");
    }

    void ResumeDangerEffects(List<DangerEffectState> states)
    {
        if (states == null || states.Count == 0)
            return;

        Debug.Log($"{FormatEvolutionLog("DangerEffect:Resume:Start")} Restoring {states.Count} effects.");

        foreach (var state in states)
        {
            if (state.effect == null)
                continue;

            state.effect.enabled = state.wasEnabled;
            if (state.wasEnabled)
                state.effect.SetBlinking(state.wasBlinking);
            state.effect.SetSuppressed(false);
        }

        Debug.Log($"{FormatEvolutionLog("DangerEffect:Resume:End")} Danger effects restored.");
    }

    IEnumerator PlayEvolutionStartEffect(Transform target)
    {
        if (target == null)
            yield break;

        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff("[EVOLUTION] Start effect skipped.");
            yield break;
        }

        SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(true);
        Image[] images = target.GetComponentsInChildren<Image>(true);
        Color[] spriteColors = new Color[sprites.Length];
        Color[] imageColors = new Color[images.Length];

        for (int i = 0; i < sprites.Length; i++)
            spriteColors[i] = sprites[i] != null ? sprites[i].color : Color.white;

        for (int i = 0; i < images.Length; i++)
            imageColors[i] = images[i] != null ? images[i].color : Color.white;

        Vector3 originalScale = target.localScale;
        Vector3 originalLocalPosition = target.localPosition;
        Vector3 chargeScale = originalScale * ChargeScaleMultiplier;
        Vector3 burstScale = originalScale * BurstScaleMultiplier;
        float timer = 0f;
        Color flashColor = new Color(1f, 0.95f, 0.8f, 1f);
        float flashIntensity = 0.92f;
        float chargeFlashIntensity = 0.18f;

        Debug.Log("[EVOLUTION] Charge start");
        SEHub.Play(YokaiSE.Evolution_Charge);
        Debug.Log($"{FormatEvolutionLog("Phase:Charge")} Charging evolution energy.");
        while (timer < ChargeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / ChargeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(originalScale, chargeScale, eased);
            float wobble = Mathf.Sin(Time.time * ChargeWobbleFrequency) * ChargeWobbleAmplitude * (0.3f + 0.7f * eased);
            float wobbleSecondary = Mathf.Cos(Time.time * (ChargeWobbleFrequency * 0.8f)) * ChargeWobbleAmplitude * 0.6f;
            target.localPosition = originalLocalPosition + new Vector3(wobble, wobbleSecondary, 0f);
            ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, chargeFlashIntensity, 1f);
            yield return null;
        }
        Debug.Log($"{FormatEvolutionLog("Phase:Charge")} Charge phase complete.");

        Debug.Log("[EVOLUTION] Burst");
        SEHub.Play(YokaiSE.Evolution_Burst);
        TriggerBurstRecoil();
        Debug.Log($"{FormatEvolutionLog("Phase:Burst")} Burst flash triggered.");
        timer = 0f;
        while (timer < BurstDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / BurstDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(chargeScale, burstScale, eased);
            target.localPosition = originalLocalPosition;
            ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, 0f);
            yield return null;
        }
        Debug.Log($"{FormatEvolutionLog("Phase:Burst")} Burst flash complete.");

        Debug.Log("[EVOLUTION] Settle");
        Debug.Log($"{FormatEvolutionLog("Phase:Settle")} Settling back to normal.");
        timer = 0f;
        while (timer < SettleDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / SettleDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(burstScale, originalScale, eased);
            target.localPosition = originalLocalPosition;
            ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, t);
            yield return null;
        }
        Debug.Log($"{FormatEvolutionLog("Phase:Settle")} Settle phase complete.");

        target.localScale = originalScale;
        target.localPosition = originalLocalPosition;
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

    string FormatEvolutionLog(string phase)
    {
        return $"[EVOLUTION][{Time.time:0.00}s][{phase}]";
    }

    void TriggerBurstRecoil()
    {
        if (isRecoilActive)
            return;

        if (!isEvolving && (stateController == null || stateController.currentState != YokaiState.Purifying))
            return;

        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff("[EVOLUTION] Burst recoil timeScale skipped.");
            return;
        }

        StartCoroutine(BurstRecoilRoutine());
    }

    IEnumerator BurstRecoilRoutine()
    {
        if (!EffectSettings.EnableEffects)
            yield break;

        isRecoilActive = true;
        recoilOriginalTimeScale = Time.timeScale;
        Time.timeScale = BurstRecoilTimeScale;

        yield return new WaitForSecondsRealtime(BurstRecoilDuration);

        Time.timeScale = recoilOriginalTimeScale;
        isRecoilActive = false;
    }
}
