using System;

public interface IAdService
{
    event Action RewardedAdCompleted;
    bool IsRewardedAdReady();
    void Initialize();
    void ShowRewardedAd();
}

public class AdServiceStub : IAdService
{
    public event Action RewardedAdCompleted;

    public bool IsRewardedAdReady()
    {
        return false;
    }

    public void Initialize()
    {
    }

    public void ShowRewardedAd()
    {
    }

    public void SimulateRewardedAdCompletion()
    {
        RewardedAdCompleted?.Invoke();
    }
}
