using System;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 1;
    public long lastSavedUnixTime;

    public YokaiSaveData yokai = new YokaiSaveData();
    public DangoSaveData dango = new DangoSaveData();
    public BoostSaveData boost = new BoostSaveData();
}
