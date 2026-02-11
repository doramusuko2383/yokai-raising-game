using NUnit.Framework;
using UnityEngine;
using Yokai;

public class YokaiStateTests
{
    YokaiStateController controller;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject();
        controller = go.AddComponent<YokaiStateController>();
    }

    [Test]
    public void PurifyStart_TransitionsToPurifying()
    {
        controller.SetState(YokaiState.Normal, "Test");
        var result = controller.TryDo(YokaiAction.PurifyStart, "Test");

        Assert.IsTrue(result);
        Assert.AreEqual(YokaiState.Purifying, controller.CurrentState);
    }

    [Test]
    public void HoldCancel_FromPurifying_ReturnsToNormal()
    {
        controller.SetState(YokaiState.Normal, "Test");
        controller.TryDo(YokaiAction.PurifyStart, "Test");
        controller.TryDo(YokaiAction.PurifyHoldStart, "Hold");
        controller.TryDo(YokaiAction.PurifyHoldCancel, "HoldCancel");

        Assert.AreEqual(YokaiState.Normal, controller.CurrentState);
    }

    [Test]
    public void EmergencyPurifyAd_FromPurityEmpty_ReturnsToNormal()
    {
        controller.SetState(YokaiState.PurityEmpty, "Test");
        var result = controller.TryDo(YokaiAction.EmergencyPurifyAd, "Emergency");

        Assert.IsTrue(result);
        Assert.AreEqual(YokaiState.Normal, controller.CurrentState);
    }
}
