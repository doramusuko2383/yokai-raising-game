using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData : ISerializationCallbackReceiver
{
    public int saveVersion = 1;
    public long lastSavedUnixTime;

    public YokaiSaveData yokai = new YokaiSaveData();
    public DangoSaveData dango = new DangoSaveData();
    public BoostSaveData boost = new BoostSaveData();

    [NonSerialized]
    public HashSet<int> unlockedYokaiIds = new HashSet<int>();

    public List<int> unlockedYokaiIdList = new List<int>();

    public void OnBeforeSerialize()
    {
        if (unlockedYokaiIds == null)
            unlockedYokaiIds = new HashSet<int>();

        unlockedYokaiIdList.Clear();
        unlockedYokaiIdList.AddRange(unlockedYokaiIds);
    }

    public void OnAfterDeserialize()
    {
        unlockedYokaiIds = unlockedYokaiIdList != null ? new HashSet<int>(unlockedYokaiIdList) : new HashSet<int>();
    }
}
