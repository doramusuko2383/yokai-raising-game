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

            if (CurrentSave == null)
                CurrentSave = new GameSaveData();

            if (CurrentSave.yokai == null)
                CurrentSave.yokai = new YokaiSaveData();

            if (CurrentSave.dango == null)
                CurrentSave.dango = new DangoSaveData();

            if (CurrentSave.boost == null)
                CurrentSave.boost = new BoostSaveData();

            long delta = Math.Max(0L, now - CurrentSave.lastSavedUnixTime);
            HandleOfflineProgress(delta);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Load failed, creating new save. {e}");
            CurrentSave = new GameSaveData();

            if (CurrentSave.yokai == null)
                CurrentSave.yokai = new YokaiSaveData();

            if (CurrentSave.dango == null)
                CurrentSave.dango = new DangoSaveData();

            if (CurrentSave.boost == null)
                CurrentSave.boost = new BoostSaveData();
        }
    }

    void HandleOfflineProgress(long deltaSeconds)
    {
        if (deltaSeconds <= 0)
            return;

        var controller = YokaiStateController.Instance;
        if (controller != null)
            controller.ApplyOfflineProgress(deltaSeconds);

        var dango = CurrentSave.dango;
        if (dango != null)
        {
            long generated = deltaSeconds / 600;
            long lastGen = dango.lastGeneratedUnixTime;
            long newCount = dango.currentCount + generated;
            dango.currentCount = (int)Mathf.Min(newCount, 3);
            dango.lastGeneratedUnixTime = lastGen + generated * 600;
        }

        var boost = CurrentSave.boost;
        if (boost != null)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now >= boost.dailyResetUnixTime + 86400)
            {
                boost.dailyDecayBoostUsedCount = 0;
                boost.dailyResetUnixTime = now - (now % 86400);
            }

            if (boost.growthBoostExpireUnixTime < now)
                boost.growthBoostExpireUnixTime = 0;

            if (boost.decayHalfBoostExpireUnixTime < now)
                boost.decayHalfBoostExpireUnixTime = 0;
        }

        MarkDirty();
    }
}
