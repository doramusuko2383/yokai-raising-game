using System;

[Serializable]
public class BoostSaveData
{
    public long growthBoostExpireUnixTime;
    public long decayHalfBoostExpireUnixTime;

    public int dailyDecayBoostUsedCount;
    public long dailyResetUnixTime;
}
