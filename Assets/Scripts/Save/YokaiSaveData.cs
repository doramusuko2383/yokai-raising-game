using System;

[Serializable]
public class YokaiSaveData
{
    public float purity;
    public float spirit;
    public float growth;
    public YokaiState state;

    public YokaiStatisticsData stats = new YokaiStatisticsData();
}
