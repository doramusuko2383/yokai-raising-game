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

    [SerializeField]
    float growthDurationHours = 48f;

    [SerializeField]
    float growthMultiplier = 1.0f;

    public float growthRatePerSecond;
    public bool isGrowthStopped;
    public bool isEvolutionReady;
    public bool hasEvolved;

    [Header("Dependencies")]
    [SerializeField]
    private YokaiStateController stateController;

    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    EnergyManager energyManager;

    DateTime lastUpdateTime;

    void Awake()
    {
        CalculateGrowthRate();
        LoadState();
        ApplyElapsedTime(DateTime.Now);
        ApplyScale();
        SaveState();
        isGrowthStopped = ShouldStopGrowth();
    }

    void Update()
    {
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
        bool wasGrowthStopped = isGrowthStopped;
        isGrowthStopped = ShouldStopGrowth();

        if (isGrowthStopped || elapsedSeconds <= 0f)
        {
            if (!wasGrowthStopped && isGrowthStopped)
            {
            }
            return;
        }

        float growthAmount = elapsedSeconds * growthRatePerSecond * growthMultiplier;
        currentScale = Mathf.Clamp(currentScale + growthAmount, initialScale, maxScale);

        ApplyScale();
        TryMarkEvolutionReady();
    }

    bool ShouldStopGrowth()
    {
        if (hasEvolved)
        {
            return true;
        }

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && stateController.isPurifying)
            return true;

        bool isKegareMax = kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare;
        bool isEnergyZero = energyManager != null && energyManager.energy <= 0f;

        return isKegareMax || isEnergyZero;
    }

    void LogGrowthStoppedReason()
    {
        string reason = "unknown";

        if (hasEvolved)
        {
            reason = "evolution-ready";
        }
        else if (kegareManager != null && kegareManager.kegare >= kegareManager.maxKegare)
        {
            reason = "kegare-max";
        }
        else if (energyManager != null && energyManager.energy <= 0f)
        {
            reason = "energy-empty";
        }

#if UNITY_EDITOR
        Debug.Log($"[GROWTH] Growth stopped. reason={reason}");
#endif
    }

    void ApplyScale()
    {
        transform.localScale = Vector3.one * currentScale;
    }

    void LoadState()
    {
        if (PlayerPrefs.HasKey(CurrentScaleKey))
        {
            currentScale = PlayerPrefs.GetFloat(CurrentScaleKey, initialScale);
        }
        else
        {
            currentScale = initialScale;
        }

        if (PlayerPrefs.HasKey(LastUpdateTimeKey))
        {
            string stored = PlayerPrefs.GetString(LastUpdateTimeKey, string.Empty);
            if (long.TryParse(stored, out long binary))
            {
                lastUpdateTime = DateTime.FromBinary(binary);
            }
            else
            {
                lastUpdateTime = DateTime.Now;
            }
        }
        else
        {
            lastUpdateTime = DateTime.Now;
        }

        isEvolutionReady = PlayerPrefs.GetInt(EvolutionReadyKey, 0) == 1;
        hasEvolved = PlayerPrefs.GetInt(HasEvolvedKey, isEvolutionReady ? 1 : 0) == 1;
        if (hasEvolved)
            isEvolutionReady = true;
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
            stateController = FindObjectOfType<YokaiStateController>();

        if (stateController != null && stateController.isPurifying)
            return;

        if (transform.localScale.x < 2.0f)
            return;

        hasEvolved = true;
        isEvolutionReady = true;
        isGrowthStopped = true;
        currentScale = Mathf.Clamp(currentScale, initialScale, maxScale);
        ApplyScale();

        if (stateController != null)
            stateController.SetEvolutionReady();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CalculateGrowthRate();
    }
#endif
}
}
