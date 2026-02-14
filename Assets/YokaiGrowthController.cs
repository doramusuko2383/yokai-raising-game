using System;
using UnityEngine;

namespace Yokai
{
public class YokaiGrowthController : MonoBehaviour
{
    string SavePrefix => gameObject.name;
    string CurrentScaleKey => $"{SavePrefix}_Growth_CurrentScale";
    string LastUpdateTimeKey => $"{SavePrefix}_Growth_LastUpdateTime";
    string EvolutionReadyKey => $"{SavePrefix}_Growth_EvolutionReady";
    string HasEvolvedKey => $"{SavePrefix}_Growth_HasEvolved";

    [Header("Scale")]
    [SerializeField]
    float initialScale = 1.0f;

    public float currentScale = 1.0f;
    public float maxScale = 2.0f;
    public float InitialScale => initialScale;

    [SerializeField]
    float growthDurationHours = 48f;

    [SerializeField]
    float growthMultiplier = 1.0f;

    float debugGrowthMultiplier = 1.0f;

    [SerializeField]
    bool resetEvolutionReadyOnStartup = true;

    public float growthRatePerSecond;
    public bool isGrowthStopped;
    public bool isEvolutionReady;
    public bool hasEvolved;
    bool isGrowthEnabled = true;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiStateController stateController;

    [Header("Growth Particles")]
    [SerializeField]
    ParticleSystem[] growthParticles;

    DateTime lastUpdateTime;
    bool hasLoggedMissingStateController;

    void Awake()
    {
        transform.localScale = Vector3.one;
        InitializeGrowthParticles();
        CalculateGrowthRate();
        LoadState(false);
        LogMissingStateController();
        bool resetDone = false;
        if (resetEvolutionReadyOnStartup && currentScale >= maxScale - 0.001f)
        {
            currentScale = initialScale;
            isEvolutionReady = false;
            hasEvolved = false;
            lastUpdateTime = DateTime.Now;
            ApplyScale();
            SaveState();
            resetDone = true;
        }
        if (!resetDone)
            ApplyElapsedTime(DateTime.Now);
        ApplyScale();
        SaveState();
        isGrowthStopped = ShouldStopGrowth();
    }

    void Update()
    {
        if (!isGrowthEnabled)
        {
            return;
        }

        DateTime now = DateTime.Now;
        double elapsedSeconds = (now - lastUpdateTime).TotalSeconds;
        lastUpdateTime = now;

        bool wasEvolutionReady = isEvolutionReady;
        float previousScale = currentScale;

        UpdateGrowth((float)elapsedSeconds);

        if (!Mathf.Approximately(previousScale, currentScale) || wasEvolutionReady != isEvolutionReady)
        {
            SaveState();
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            lastUpdateTime = DateTime.Now;
            SaveState();
        }
    }

    void OnApplicationQuit()
    {
        lastUpdateTime = DateTime.Now;
        SaveState();
    }

    void CalculateGrowthRate()
    {
        float durationSeconds = Mathf.Max(growthDurationHours, 0.01f) * 3600f;
        float scaleRange = Mathf.Max(maxScale - initialScale, 0f);
        growthRatePerSecond = scaleRange / durationSeconds;
    }

    void ApplyElapsedTime(DateTime now)
    {
        double elapsedSeconds = (now - lastUpdateTime).TotalSeconds;
        lastUpdateTime = now;
        UpdateGrowth((float)elapsedSeconds);
    }

    void UpdateGrowth(float elapsedSeconds)
    {
        if (!isGrowthEnabled)
        {
            return;
        }

        bool wasGrowthStopped = isGrowthStopped;
        isGrowthStopped = ShouldStopGrowth();

        if (stateController == null)
            LogMissingStateController();

        bool shouldPlayParticles = stateController != null && stateController.currentState == YokaiState.Evolving;
        if (!shouldPlayParticles)
            StopGrowthParticles();

        if (isGrowthStopped || elapsedSeconds <= 0f)
        {
            if (!wasGrowthStopped && isGrowthStopped)
            {
                StopGrowthParticles();
            }
            return;
        }

        float growthAmount = elapsedSeconds * growthRatePerSecond * growthMultiplier * debugGrowthMultiplier;
        currentScale = Mathf.Clamp(currentScale + growthAmount, initialScale, maxScale);

        ApplyScale();
        if (growthAmount > 0f && shouldPlayParticles)
            PlayGrowthParticles();
        TryMarkEvolutionReady();
    }

    bool ShouldStopGrowth()
    {
        if (!isGrowthEnabled)
        {
            return true;
        }

        if (hasEvolved)
        {
            return true;
        }

        if (stateController == null)
            LogMissingStateController();

        if (stateController != null && stateController.currentState == YokaiState.Purifying)
            return true;

        if (stateController != null)
        {
            var currentState = stateController.currentState;
            if (currentState == YokaiState.EnergyEmpty || currentState == YokaiState.PurityEmpty)
                return true;

            if (currentState == YokaiState.EvolutionReady || currentState == YokaiState.Evolving)
                return true;
        }

        return false;
    }

    public bool SetGrowthEnabled(bool enabled)
    {
        if (isGrowthEnabled == enabled)
        {
            return false;
        }

        isGrowthEnabled = enabled;
        if (!isGrowthEnabled)
        {
            lastUpdateTime = DateTime.Now;
        }

        return true;
    }

    public float DebugGrowthMultiplier => debugGrowthMultiplier;

    public void SetDebugGrowthMultiplier(float multiplier)
    {
        debugGrowthMultiplier = Mathf.Max(multiplier, 0f);
    }

    void LogGrowthStoppedReason()
    {
        string reason = "unknown";

        if (hasEvolved)
        {
            reason = "evolution-ready";
        }
        else if (stateController != null)
        {
            if (stateController.currentState == YokaiState.EnergyEmpty)
                reason = "energy-empty";
            else if (stateController.currentState == YokaiState.PurityEmpty)
                reason = "purity-empty";
        }

#if UNITY_EDITOR
        Debug.Log($"[GROWTH] Growth stopped. reason={reason}");
#endif
    }


    public void AddGrowth(float amount)
    {
        if (amount <= 0f)
            return;

        currentScale = Mathf.Clamp(currentScale + amount, initialScale, maxScale);
        ApplyScale();
        TryMarkEvolutionReady();
    }

    public void ApplyScale()
    {
        transform.localScale = Vector3.one * currentScale;
    }

    void LoadState(bool loadFromPrefs)
    {
        currentScale = initialScale;
        lastUpdateTime = DateTime.Now;
        isEvolutionReady = false;
        hasEvolved = false;

        if (!loadFromPrefs)
            return;

        if (PlayerPrefs.HasKey(CurrentScaleKey))
            currentScale = PlayerPrefs.GetFloat(CurrentScaleKey, initialScale);

        if (PlayerPrefs.HasKey(LastUpdateTimeKey))
        {
            string savedTime = PlayerPrefs.GetString(LastUpdateTimeKey, string.Empty);
            if (long.TryParse(savedTime, out var binaryTime))
                lastUpdateTime = DateTime.FromBinary(binaryTime);
        }

        if (PlayerPrefs.HasKey(EvolutionReadyKey))
            isEvolutionReady = PlayerPrefs.GetInt(EvolutionReadyKey, 0) == 1;

        if (PlayerPrefs.HasKey(HasEvolvedKey))
            hasEvolved = PlayerPrefs.GetInt(HasEvolvedKey, 0) == 1;
    }

    public void LoadGrowthFromPrefs()
    {
        LoadState(true);
        ApplyElapsedTime(DateTime.Now);
        ApplyScale();
        isGrowthStopped = ShouldStopGrowth();
    }

    void SaveState()
    {
        PlayerPrefs.SetFloat(CurrentScaleKey, currentScale);
        PlayerPrefs.SetString(LastUpdateTimeKey, lastUpdateTime.ToBinary().ToString());
        PlayerPrefs.SetInt(EvolutionReadyKey, isEvolutionReady ? 1 : 0);
        PlayerPrefs.SetInt(HasEvolvedKey, hasEvolved ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetGrowthState()
    {
        currentScale = initialScale;
        isEvolutionReady = false;
        hasEvolved = false;
        isGrowthStopped = false;
        lastUpdateTime = DateTime.Now;
        ApplyScale();
        SaveState();
    }

    [ContextMenu("DEBUG / Reset Growth State")]
    void DebugResetGrowthState()
    {
        ResetGrowthState();
    }

    [ContextMenu("DEBUG / Clear PlayerPrefs (This Yokai Only)")]
    void DebugClearPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(CurrentScaleKey);
        PlayerPrefs.DeleteKey(LastUpdateTimeKey);
        PlayerPrefs.DeleteKey(EvolutionReadyKey);
        PlayerPrefs.DeleteKey(HasEvolvedKey);
        PlayerPrefs.Save();
    }

    void TryMarkEvolutionReady()
    {
        if (hasEvolved)
            return;

        if (stateController == null)
            LogMissingStateController();

        if (stateController != null && stateController.currentState == YokaiState.Purifying)
            return;

        if (currentScale < maxScale)
            return;

        hasEvolved = true;
        isEvolutionReady = true;
        isGrowthStopped = true;
        currentScale = Mathf.Clamp(currentScale, initialScale, maxScale);
        ApplyScale();

        if (stateController != null)
            stateController.SetEvolutionReady();
    }

    void LogMissingStateController()
    {
        if (stateController != null || hasLoggedMissingStateController)
            return;

        hasLoggedMissingStateController = true;
        Debug.LogError("[GROWTH] StateController not set in Inspector");
    }

    void InitializeGrowthParticles()
    {
        if (growthParticles == null || growthParticles.Length == 0)
            growthParticles = GetComponentsInChildren<ParticleSystem>(true);

        if (growthParticles == null)
            return;

        foreach (var particle in growthParticles)
        {
            if (particle == null)
                continue;

            var main = particle.main;
            main.playOnAwake = false;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void PlayGrowthParticles()
    {
        if (growthParticles == null || growthParticles.Length == 0)
            return;

        foreach (var particle in growthParticles)
        {
            if (particle == null)
                continue;

            if (!particle.isPlaying)
                particle.Play();
        }
    }

    void StopGrowthParticles()
    {
        if (growthParticles == null || growthParticles.Length == 0)
            return;

        foreach (var particle in growthParticles)
        {
            if (particle == null)
                continue;

            if (particle.isPlaying)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CalculateGrowthRate();
    }
#endif
}
}
