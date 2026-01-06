using System.Collections.Generic;
using UnityEngine;

public static class SEHub
{
    enum SEPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    struct SEPlaybackPolicy
    {
        public float cooldownSeconds;
        public float blockLowerSeconds;
        public SEPriority priority;
    }

    static readonly Dictionary<YokaiSE, float> LastPlayedAt = new Dictionary<YokaiSE, float>();
    static float highPriorityUntil;
    static GameObject runtimeRoot;
    static AudioSource runtimeSource;

    // SE設計メモ:
    // - 命名規則（将来の音ファイル名）: se_<category>_<action>.wav
    //   例: se_evolution_charge.wav / se_purify_start.wav / se_kegare_max_enter.wav
    // - AudioSource は SEHubRuntime に一元管理する
    // - ミュート/親AudioGroup設定は AudioManager 側で一元管理

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        AudioHook.PlayRequested -= HandlePlayRequested;
        AudioHook.PlayRequested += HandlePlayRequested;
    }

    static void HandlePlayRequested(YokaiSE se)
    {
        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff($"[SE] {se} skipped.");
            return;
        }

        var policy = GetPolicy(se);
        if (!CanPlay(se, policy))
            return;

        if (!AudioHook.TryResolveClip(se, out var clip))
            return;

        EnsureRuntimeSource();
        runtimeSource.PlayOneShot(clip);

        float now = Time.unscaledTime;
        LastPlayedAt[se] = now;
        if (policy.priority == SEPriority.High)
            highPriorityUntil = Mathf.Max(highPriorityUntil, now + policy.blockLowerSeconds);

    }

    static bool CanPlay(YokaiSE se, SEPlaybackPolicy policy)
    {
        float now = Time.unscaledTime;

        if (policy.priority != SEPriority.High && now < highPriorityUntil)
            return false;

        if (LastPlayedAt.TryGetValue(se, out float lastTime) && now - lastTime < policy.cooldownSeconds)
            return false;

        return true;
    }

    static SEPlaybackPolicy GetPolicy(YokaiSE se)
    {
        switch (se)
        {
            case YokaiSE.SE_UI_CLICK:
                return new SEPlaybackPolicy
                {
                    cooldownSeconds = 0.08f,
                    blockLowerSeconds = 0f,
                    priority = SEPriority.Low
                };
            case YokaiSE.SE_PURIFY_START:
            case YokaiSE.SE_PURIFY_SUCCESS:
            case YokaiSE.SE_PURIFY_CANCEL:
                return new SEPlaybackPolicy
                {
                    cooldownSeconds = 0.25f,
                    blockLowerSeconds = 0f,
                    priority = SEPriority.Normal
                };
            case YokaiSE.SE_KEGARE_MAX_ENTER:
            case YokaiSE.SE_KEGARE_MAX_RELEASE:
                return new SEPlaybackPolicy
                {
                    cooldownSeconds = 0.4f,
                    blockLowerSeconds = 0.6f,
                    priority = SEPriority.High
                };
            case YokaiSE.SE_EVOLUTION_START:
            case YokaiSE.SE_EVOLUTION_CHARGE:
            case YokaiSE.SE_EVOLUTION_FLASH:
            case YokaiSE.SE_EVOLUTION_SWAP:
            case YokaiSE.SE_EVOLUTION_COMPLETE:
                return new SEPlaybackPolicy
                {
                    cooldownSeconds = 0.3f,
                    blockLowerSeconds = 0.5f,
                    priority = SEPriority.High
                };
            default:
                return new SEPlaybackPolicy
                {
                    cooldownSeconds = 0.2f,
                    blockLowerSeconds = 0.3f,
                    priority = SEPriority.Normal
                };
        }
    }

    static void EnsureRuntimeSource()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject("SEHubRuntime");
            Object.DontDestroyOnLoad(runtimeRoot);
        }

        if (runtimeSource != null)
            return;

        runtimeSource = runtimeRoot.AddComponent<AudioSource>();
        runtimeSource.playOnAwake = false;
        runtimeSource.loop = false;
    }
}
