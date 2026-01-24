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
    PurityController purityController;

    [SerializeField]
    Transform characterRoot;

    [Header("Evolution Rules")]
    [SerializeField]
    List<EvolutionRule> evolutionRules = new List<EvolutionRule>();

    [SerializeField]
    string fireBallName = "FireBall";

    [SerializeField]
    string yokaiChildName = "YokaiChild";

    [SerializeField]
    string yokaiAdultName = "YokaiAdult";

    [Header("UI References")]
    [SerializeField]
    PurifyButtonHandler[] purifyButtonHandlers;

    bool isEvolving;
    const float ChargeScaleMultiplier = 0.95f;
    const float FlashScaleMultiplier = 1.12f;
    const float ChargeDurationMin = 0.5f;
    const float ChargeDurationMax = 1.0f;
    const float FlashDurationMin = 0.2f;
    const float FlashDurationMax = 0.3f;
    const float ChargeWobbleAmplitude = 0.02f;
    const float ChargeWobbleFrequency = 7.5f;
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

    struct UIElementState
    {
        public GameObject target;
        public bool wasActive;
    }

    /// <summary>
    /// UI Button から呼ばれる進化トリガー
    /// </summary>
    public void OnClickEvolve()
    {
        AudioHook.RequestPlay(YokaiSE.SE_UI_CLICK);
        if (stateController == null)
        {
            Debug.LogError("[EVOLUTION] StateController not set in Inspector");
            return;
        }

        if (stateController.currentState != YokaiState.EvolutionReady)
        {
            Debug.Log($"[EVOLUTION] Tap ignored. CurrentState={stateController.currentState}");
            return;
        }

        if (IsEvolutionBlocked(out string reason))
        {
            Debug.Log($"[EVOLUTION] Tap blocked. reason={reason}");
            return;
        }

        if (isEvolving)
        {
            Debug.Log("[EVOLUTION] Tap ignored. Evolution already in progress.");
            return;
        }

        EnsureEvolutionRules();
        ResolveYokaiReferences();

        Debug.Log("[EVOLUTION] 進化開始");
        Debug.Log($"[EVOLUTION] 進化前キャラ名={GetYokaiName(currentYokaiPrefab)}");
        Debug.Log($"[EVOLUTION] 進化後キャラ名={GetYokaiName(nextYokaiPrefab)}");

        stateController.BeginEvolution();
        TutorialManager.NotifyEvolutionStarted();
        Debug.Log($"{FormatEvolutionLog("Start")} Evolution triggered by tap");
        StartCoroutine(EvolutionSequence());
    }

    void Start()
    {
        EnsureEvolutionRules();
        InitializeActiveYokai();
    }

    IEnumerator EvolutionSequence()
    {
        isEvolving = true;
        SetEvolutionInputEnabled(false);

        EnsureEvolutionRules();
        ResolveYokaiReferences();
        LogYokaiActiveState("[EVOLUTION][Before]");

        AudioHook.RequestPlay(YokaiSE.SE_EVOLUTION_START);
        if (currentYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Current yokai prefab is not assigned.");
        if (nextYokaiPrefab == null)
            Debug.LogWarning("[EVOLUTION] Next yokai prefab is not assigned.");

        var dangerEffectStates = new List<DangerEffectState>();
        var uiStates = new List<UIElementState>();
        PauseDangerEffects(dangerEffectStates);
        PauseEvolutionUI(uiStates);

        AudioHook.RequestPlay(YokaiSE.SE_EVOLUTION_CHARGE);
        Debug.Log($"{FormatEvolutionLog("Start")} Evolution start.");

        if (currentYokaiPrefab != null)
            yield return PlayEvolutionCharge(currentYokaiPrefab.transform);
        else if (characterRoot != null)
            yield return PlayEvolutionCharge(characterRoot);

        if (currentYokaiPrefab == null || nextYokaiPrefab == null)
        {
            Debug.LogWarning("[EVOLUTION] Evolution aborted due to missing yokai references.");
            isEvolving = false;
            stateController.CompleteEvolution();
            ResumeDangerEffects(dangerEffectStates);
            ResumeEvolutionUI(uiStates);
            SetEvolutionInputEnabled(true);
            yield break;
        }

        if (currentYokaiPrefab != null)
            yield return PlayEvolutionFlash(currentYokaiPrefab.transform);
        else if (characterRoot != null)
            yield return PlayEvolutionFlash(characterRoot);

        AudioHook.RequestPlay(YokaiSE.SE_EVOLUTION_SWAP);
        Debug.Log($"{FormatEvolutionLog("Swap")} Evolution swap.");

        // 見た目切り替え
        SwitchYokaiVisibility(currentYokaiPrefab, nextYokaiPrefab);
        EnsureFireBallHidden(nextYokaiPrefab);

        // 成長リセット
        if (nextYokaiPrefab != null)
        {
            var nextGrowthController = nextYokaiPrefab.GetComponent<YokaiGrowthController>();
            if (nextGrowthController != null)
            {
                nextGrowthController.ResetGrowthState();
            }
            else if (growthController != null)
                growthController.ResetGrowthState();

            if (stateController != null)
                stateController.SetActiveYokai(nextYokaiPrefab);
        }

        currentYokaiPrefab = nextYokaiPrefab;
        nextYokaiPrefab = FindNextYokaiPrefab(currentYokaiPrefab);
        Debug.Log($"[EVOLUTION] currentYokaiPrefab 更新結果={GetYokaiName(currentYokaiPrefab)} next={GetYokaiName(nextYokaiPrefab)}");
        UpdateCurrentYokai(currentYokaiPrefab, "EvolutionComplete");
        RegisterEncyclopediaEvolution(currentYokaiPrefab);

        // 完了
        AudioHook.RequestPlay(YokaiSE.SE_EVOLUTION_COMPLETE);
        Debug.Log($"{FormatEvolutionLog("Complete")} Evolution completed. Switching to Normal state.");
        stateController.CompleteEvolution();
        LogYokaiActiveState("[EVOLUTION][After]");
        ResumeDangerEffects(dangerEffectStates);
        ResumeEvolutionUI(uiStates);
        SetEvolutionInputEnabled(true);
        isEvolving = false;
    }

    void InitializeActiveYokai()
    {
        ResolveYokaiReferences();

        if (characterRoot == null)
            return;

        GameObject active = FindActiveYokai();
        if (active == null)
            active = FindYokaiByName(fireBallName) ?? FindYokaiByName(yokaiChildName);

        if (active != null)
        {
            ActivateOnly(active);
            currentYokaiPrefab = active;
            nextYokaiPrefab = FindNextYokaiPrefab(currentYokaiPrefab);
            if (stateController == null)
                Debug.LogError("[EVOLUTION] StateController not set in Inspector");
            else
                stateController.SetActiveYokai(currentYokaiPrefab);
            UpdateCurrentYokai(currentYokaiPrefab, "Initialize");
            RegisterEncyclopediaDiscovery(currentYokaiPrefab);
        }
    }

    bool IsEvolutionBlocked(out string reason)
    {
        if (stateController == null)
        {
            Debug.LogError("[EVOLUTION] StateController not set in Inspector");
            reason = "StateController missing";
            return true;
        }

        bool isPurityEmpty = stateController != null && stateController.currentState == YokaiState.PurityEmpty;
        bool isEnergyEmpty = stateController != null && stateController.currentState == YokaiState.EnergyEmpty;
        if (!isPurityEmpty && !isEnergyEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (isPurityEmpty && isEnergyEmpty)
            reason = "清浄度0 / 霊力0";
        else if (isPurityEmpty)
            reason = "清浄度0";
        else
            reason = "霊力0";

        return true;
    }

    void ResolveYokaiReferences()
    {
        EnsureEvolutionRules();

        if (characterRoot == null)
        {
            Debug.LogWarning("[EVOLUTION] CharacterRoot is not assigned.");
            return;
        }

        if (currentYokaiPrefab == null || !currentYokaiPrefab.activeInHierarchy)
        {
            currentYokaiPrefab = FindActiveYokai();
        }

        if (currentYokaiPrefab == null)
        {
            currentYokaiPrefab = FindYokaiByName(yokaiChildName);
            if (currentYokaiPrefab == null)
                Debug.LogWarning($"[EVOLUTION] Child yokai not found under CharacterRoot: {yokaiChildName}");
        }

        nextYokaiPrefab = FindNextYokaiPrefab(currentYokaiPrefab);
    }

    void UpdateCurrentYokai(GameObject activeYokai, string reason)
    {
        CurrentYokaiContext.SetCurrent(activeYokai, reason);
        RebindCurrentYokai(activeYokai);
        if (stateController != null)
        {
            stateController.MarkReady();
            stateController.ForceReevaluate("YokaiReady");
        }
    }

    void RebindCurrentYokai(GameObject activeYokai)
    {
        if (activeYokai == null)
            return;

        if (purityController == null)
            Debug.LogError("[EVOLUTION] PurityController not set in Inspector");

        if (stateController == null)
        {
            Debug.LogError("[EVOLUTION] StateController not set in Inspector");
        }
        else
        {
            stateController.BindCurrentYokai(activeYokai);
        }

        if (purifyButtonHandlers != null && purifyButtonHandlers.Length > 0)
        {
            foreach (var handler in purifyButtonHandlers)
            {
                if (handler == null)
                    continue;

                handler.BindStateController(stateController);
            }
        }

        foreach (var effect in activeYokai.GetComponentsInChildren<YokaiDangerEffect>(true))
            effect.RefreshOriginalColor();
    }

    void RegisterEncyclopediaDiscovery(GameObject yokaiObject)
    {
        if (yokaiObject == null)
            return;

        if (YokaiEncyclopedia.TryResolveYokaiId(yokaiObject.name, out var yokaiId, out _))
            YokaiEncyclopedia.RegisterDiscovery(yokaiId);
    }

    void RegisterEncyclopediaEvolution(GameObject yokaiObject)
    {
        if (yokaiObject == null)
            return;

        if (YokaiEncyclopedia.TryResolveYokaiId(yokaiObject.name, out var yokaiId, out var stage))
            YokaiEncyclopedia.RegisterEvolution(yokaiId, stage);
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

    GameObject FindActiveYokai()
    {
        if (characterRoot == null)
            return null;

        foreach (Transform child in characterRoot)
        {
            if (child != null && child.gameObject.activeInHierarchy)
                return child.gameObject;
        }

        return null;
    }

    GameObject FindNextYokaiPrefab(GameObject current)
    {
        if (current == null)
            return null;

        string nextName = GetNextName(current.name);
        if (string.IsNullOrEmpty(nextName))
        {
            Debug.LogWarning($"[EVOLUTION] No next evolution rule found for {current.name}");
            return null;
        }

        GameObject next = FindYokaiByName(nextName);
        if (next == null)
            Debug.LogWarning($"[EVOLUTION] Next yokai not found under CharacterRoot: {nextName}");

        return next;
    }

    string GetNextName(string currentName)
    {
        if (string.IsNullOrEmpty(currentName) || evolutionRules == null)
            return null;

        foreach (var rule in evolutionRules)
        {
            if (rule != null && rule.Matches(currentName))
                return rule.nextName;
        }

        return null;
    }

    void EnsureEvolutionRules()
    {
        if (evolutionRules != null && evolutionRules.Count > 0)
            return;

        evolutionRules = new List<EvolutionRule>
        {
            new EvolutionRule { currentName = fireBallName, nextName = yokaiChildName },
            new EvolutionRule { currentName = yokaiChildName, nextName = yokaiAdultName }
        };
    }

    void SwitchYokaiVisibility(GameObject previous, GameObject next)
    {
        if (characterRoot != null)
        {
            foreach (Transform child in characterRoot)
            {
                if (child == null)
                    continue;

                bool shouldEnable = next != null && child.gameObject == next;
                child.gameObject.SetActive(shouldEnable);
                SetYokaiInteractivity(child.gameObject, shouldEnable);
            }
        }
        else
        {
            if (previous != null)
            {
                SetYokaiInteractivity(previous, false);
                previous.SetActive(false);
            }

            if (next != null)
            {
                next.SetActive(true);
                SetYokaiInteractivity(next, true);
            }
        }
    }

    void ActivateOnly(GameObject target)
    {
        if (target == null)
            return;

        if (characterRoot == null)
        {
            target.SetActive(true);
            SetYokaiInteractivity(target, true);
            return;
        }

        foreach (Transform child in characterRoot)
        {
            if (child == null)
                continue;

            bool shouldEnable = child.gameObject == target;
            child.gameObject.SetActive(shouldEnable);
            SetYokaiInteractivity(child.gameObject, shouldEnable);
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

    void SetEvolutionInputEnabled(bool enabled)
    {
        if (currentYokaiPrefab != null)
            SetYokaiInteractivity(currentYokaiPrefab, enabled);
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

    IEnumerator PlayEvolutionCharge(Transform target)
    {
        if (target == null)
            yield break;

        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff("[EVOLUTION] Charge effect skipped.");
            yield break;
        }

        Vector3 originalScale = target.localScale;
        Vector3 originalLocalPosition = target.localPosition;
        Vector3 chargeScale = originalScale * ChargeScaleMultiplier;
        float timer = 0f;
        float duration = Random.Range(ChargeDurationMin, ChargeDurationMax);

        Debug.Log("[EVOLUTION] Charge start");
        Debug.Log($"{FormatEvolutionLog("Phase:Charge")} Charging evolution energy.");
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(originalScale, chargeScale, eased);
            float wobble = Mathf.Sin(Time.time * ChargeWobbleFrequency) * ChargeWobbleAmplitude * (0.3f + 0.7f * eased);
            float wobbleSecondary = Mathf.Cos(Time.time * (ChargeWobbleFrequency * 0.8f)) * ChargeWobbleAmplitude * 0.6f;
            target.localPosition = originalLocalPosition + new Vector3(wobble, wobbleSecondary, 0f);
            yield return null;
        }
        Debug.Log($"{FormatEvolutionLog("Phase:Charge")} Charge phase complete.");

        target.localScale = originalScale;
        target.localPosition = originalLocalPosition;
    }

    IEnumerator PlayEvolutionFlash(Transform target)
    {
        if (target == null)
            yield break;

        AudioHook.RequestPlay(YokaiSE.SE_EVOLUTION_FLASH);
        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff("[EVOLUTION] Flash effect skipped.");
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
        Vector3 flashScale = originalScale * FlashScaleMultiplier;
        float timer = 0f;
        float duration = Random.Range(FlashDurationMin, FlashDurationMax);
        Color flashColor = new Color(1f, 1f, 1f, 1f);
        float flashIntensity = 0.95f;

        Debug.Log("[EVOLUTION] Flash start");
        TriggerBurstRecoil();
        Debug.Log($"{FormatEvolutionLog("Phase:Flash")} Flash phase started.");
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(originalScale, flashScale, eased);
            target.localPosition = originalLocalPosition;
            ApplyFlashColors(sprites, spriteColors, images, imageColors, flashColor, flashIntensity, 0f);
            yield return null;
        }
        Debug.Log($"{FormatEvolutionLog("Phase:Flash")} Flash phase complete.");

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

    void PauseEvolutionUI(List<UIElementState> states)
    {
        if (states == null)
            return;

        string[] targets =
        {
            "UI_Action",
            "Btn_PurityRecover_Ad",
            "Btn_StopPurify",
            "MagicCircleImage",
            "Overlay_Danger",
            "Growth_Slider"
        };

        foreach (string targetName in targets)
        {
            if (string.IsNullOrEmpty(targetName))
                continue;

            GameObject target = GameObject.Find(targetName);
            if (target == null)
                continue;

            states.Add(new UIElementState
            {
                target = target,
                wasActive = target.activeSelf
            });

            target.SetActive(false);
        }
    }

    void ResumeEvolutionUI(List<UIElementState> states)
    {
        if (states == null || states.Count == 0)
            return;

        foreach (var state in states)
        {
            if (state.target == null)
                continue;

            state.target.SetActive(state.wasActive);
        }
    }

    void EnsureFireBallHidden(GameObject nextYokai)
    {
        if (string.IsNullOrEmpty(fireBallName))
            return;

        GameObject fireBall = FindYokaiByName(fireBallName);
        if (fireBall == null)
            return;

        if (nextYokai != null && fireBall == nextYokai)
            return;

        Destroy(fireBall);
    }

    string FormatEvolutionLog(string phase)
    {
        return $"[EVOLUTION][{Time.time:0.00}s][{phase}]";
    }

    string GetYokaiName(GameObject target)
    {
        return target != null ? target.name : "null";
    }

    void TriggerBurstRecoil()
    {
        if (isRecoilActive)
            return;

        if (!isEvolving && (stateController == null || !stateController.isPurifying))
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
