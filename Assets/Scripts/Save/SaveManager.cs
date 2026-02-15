using System;
using System.IO;
using UnityEngine;
using Yokai;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameSaveData CurrentSave { get; private set; }

    bool isDirty;
    float autoSaveTimer;
    const float AutoSaveInterval = 5f;

    string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    string BackupPath => Path.Combine(Application.persistentDataPath, "save_backup.json");
    string TempPath => SavePath + ".tmp";

    void Awake()
    {
        Debug.Log($"[SaveManager] Awake at persistentDataPath={Application.persistentDataPath}");

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    void Update()
    {
        autoSaveTimer += Time.unscaledDeltaTime;

        if (autoSaveTimer >= AutoSaveInterval)
        {
            autoSaveTimer = 0f;

            if (isDirty)
                Save();
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            Save();
    }

    void OnApplicationQuit()
    {
        Save();
    }

    public void MarkDirty()
    {
        isDirty = true;
    }

    public void Save()
    {
        Debug.Log($"[SaveManager] Save called, path={SavePath}");

        if (CurrentSave == null)
            return;

        try
        {
            CurrentSave.lastSavedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = JsonUtility.ToJson(CurrentSave, true);

            if (File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);

            File.WriteAllText(TempPath, json);

            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(TempPath, SavePath);

            isDirty = false;

            Debug.Log("[SaveManager] Save completed");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e}");
        }
    }

    public void Load()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                CurrentSave = JsonUtility.FromJson<GameSaveData>(json);
            }
            else if (File.Exists(BackupPath))
            {
                string json = File.ReadAllText(BackupPath);
                CurrentSave = JsonUtility.FromJson<GameSaveData>(json);
            }
            else
            {
                CurrentSave = new GameSaveData();
            }

        }
        catch (Exception e)
        {
            Debug.LogWarning($"Load failed, creating new save. {e}");
            CurrentSave = new GameSaveData();
        }

        EnsureSaveDataInitialized();

        long deltaRaw = Math.Max(0L, now - CurrentSave.lastSavedUnixTime);
        long delta = Math.Min(deltaRaw, 60 * 60 * 8);
        HandleOfflineProgress(delta, now);
    }

    void EnsureSaveDataInitialized()
    {
        if (CurrentSave == null)
            CurrentSave = new GameSaveData();

        if (CurrentSave.yokai == null)
            CurrentSave.yokai = new YokaiSaveData();

        if (CurrentSave.dango == null)
            CurrentSave.dango = new DangoSaveData();

        if (CurrentSave.boost == null)
            CurrentSave.boost = new BoostSaveData();
    }

    void HandleOfflineProgress(long deltaSeconds, long now)
    {
        if (deltaSeconds <= 0 || CurrentSave == null)
            return;

        var boost = CurrentSave.boost;
        if (boost != null)
        {
            if (boost.dailyResetUnixTime <= 0)
                boost.dailyResetUnixTime = now - (now % 86400);

            long todayStart = now - (now % 86400);
            if (todayStart > boost.dailyResetUnixTime)
            {
                boost.dailyDecayBoostUsedCount = 0;
                boost.dailyResetUnixTime = todayStart;
            }

            if (boost.growthBoostExpireUnixTime < now)
                boost.growthBoostExpireUnixTime = 0;

            if (boost.decayHalfBoostExpireUnixTime < now)
                boost.decayHalfBoostExpireUnixTime = 0;
        }

        var dango = CurrentSave.dango;
        if (dango != null)
        {
            const int MaxDango = 3;
            const long Interval = 600;

            if (dango.lastGeneratedUnixTime <= 0)
            {
                dango.lastGeneratedUnixTime = now;
            }
            else
            {
                long elapsed = Math.Max(0L, now - dango.lastGeneratedUnixTime);
                long generated = elapsed / Interval;

                if (generated > 0)
                {
                    dango.currentCount = (int)Mathf.Min(dango.currentCount + generated, MaxDango);
                    dango.lastGeneratedUnixTime += generated * Interval;
                    FindObjectOfType<DangoButtonHandler>()?.RefreshUI();
                }
            }
        }

        var controller = YokaiStateController.Instance;
        if (controller != null)
            controller.ApplyOfflineProgress(deltaSeconds, now);

        MarkDirty();
    }
}
