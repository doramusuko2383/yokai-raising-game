using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public enum YokaiId
    {
        FireBall,
        Child,
        Adult
    }

    public enum YokaiEvolutionStage
    {
        FireBall,
        Child,
        Adult
    }

    [Serializable]
    public class YokaiMasterData
    {
        public YokaiId yokaiId;
        public string displayName;
        public YokaiEvolutionStage evolutionStage;
        public string description;
    }

    [Serializable]
    public class YokaiEncyclopediaEntry
    {
        public YokaiId yokaiId;
        public bool isDiscovered;
        public bool isFirstTime;
        public string registeredAt;
    }

    [Serializable]
    class YokaiEncyclopediaSaveData
    {
        public List<YokaiEncyclopediaEntry> entries = new List<YokaiEncyclopediaEntry>();
    }

    public static class YokaiEncyclopedia
    {
        const string SaveKey = "Yokai.Encyclopedia.Save";

        static readonly List<YokaiMasterData> MasterDataList = new List<YokaiMasterData>
        {
            new YokaiMasterData
            {
                yokaiId = YokaiId.FireBall,
                displayName = "火の玉",
                evolutionStage = YokaiEvolutionStage.FireBall,
                description = "誕生直後の小さな妖火。進化の兆しを秘めている。"
            },
            new YokaiMasterData
            {
                yokaiId = YokaiId.Child,
                displayName = "妖怪子",
                evolutionStage = YokaiEvolutionStage.Child,
                description = "成長途中の妖怪。まだ幼いが秘めた力は大きい。"
            },
            new YokaiMasterData
            {
                yokaiId = YokaiId.Adult,
                displayName = "妖怪大人",
                evolutionStage = YokaiEvolutionStage.Adult,
                description = "成長を遂げた妖怪。風格と力が備わっている。"
            }
        };

        static readonly Dictionary<string, YokaiId> NameToId = new Dictionary<string, YokaiId>
        {
            { "FireBall", YokaiId.FireBall },
            { "YokaiChild", YokaiId.Child },
            { "YokaiAdult", YokaiId.Adult }
        };

        static Dictionary<YokaiId, YokaiEncyclopediaEntry> entries;

        public static event Action<YokaiId> OnYokaiDiscovered;
        public static event Action<YokaiId, YokaiEvolutionStage> OnYokaiEvolved;

        public static IReadOnlyList<YokaiMasterData> MasterData => MasterDataList;

        public static IReadOnlyCollection<YokaiEncyclopediaEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return entries.Values;
            }
        }

        public static YokaiEncyclopediaEntry GetEntry(YokaiId yokaiId)
        {
            EnsureLoaded();
            return entries[yokaiId];
        }

        public static bool RegisterDiscovery(YokaiId yokaiId)
        {
            EnsureLoaded();

            var entry = entries[yokaiId];
            if (entry.isDiscovered)
                return false;

            entry.isDiscovered = true;
            entry.isFirstTime = true;
            entry.registeredAt = DateTime.UtcNow.ToString("o");
            Save();
            OnYokaiDiscovered?.Invoke(yokaiId);
            return true;
        }

        public static void RegisterEvolution(YokaiId yokaiId, YokaiEvolutionStage stage)
        {
            EnsureLoaded();
            RegisterDiscovery(yokaiId);
            OnYokaiEvolved?.Invoke(yokaiId, stage);
        }

        public static bool TryResolveYokaiId(string yokaiName, out YokaiId yokaiId, out YokaiEvolutionStage stage)
        {
            if (!string.IsNullOrEmpty(yokaiName) && NameToId.TryGetValue(yokaiName, out yokaiId))
            {
                stage = GetStage(yokaiId);
                return true;
            }

            yokaiId = YokaiId.FireBall;
            stage = YokaiEvolutionStage.FireBall;
            return false;
        }

        static YokaiEvolutionStage GetStage(YokaiId yokaiId)
        {
            switch (yokaiId)
            {
                case YokaiId.Child:
                    return YokaiEvolutionStage.Child;
                case YokaiId.Adult:
                    return YokaiEvolutionStage.Adult;
                default:
                    return YokaiEvolutionStage.FireBall;
            }
        }

        static void EnsureLoaded()
        {
            if (entries != null)
                return;

            entries = new Dictionary<YokaiId, YokaiEncyclopediaEntry>();
            foreach (var master in MasterDataList)
            {
                entries[master.yokaiId] = new YokaiEncyclopediaEntry
                {
                    yokaiId = master.yokaiId,
                    isDiscovered = false,
                    isFirstTime = false,
                    registeredAt = string.Empty
                };
            }

            Load();
        }

        static void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;

            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            var saveData = JsonUtility.FromJson<YokaiEncyclopediaSaveData>(json);
            if (saveData == null || saveData.entries == null)
                return;

            foreach (var entry in saveData.entries)
            {
                if (!entries.ContainsKey(entry.yokaiId))
                    continue;

                var target = entries[entry.yokaiId];
                target.isDiscovered = entry.isDiscovered;
                target.isFirstTime = entry.isFirstTime;
                target.registeredAt = entry.registeredAt;
            }
        }

        static void Save()
        {
            var saveData = new YokaiEncyclopediaSaveData();
            foreach (var entry in entries.Values)
                saveData.entries.Add(entry);

            var json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
