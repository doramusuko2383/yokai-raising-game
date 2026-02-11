using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Yokai;

public class YokaiStateControllerStateModelTests
{
    static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void CanDo_HoldActions_AreOnlyAllowedInPurifyingState()
    {
        var controller = CreateController();

        controller.currentState = YokaiState.Normal;
        controller.isPurifying = false;
        SetPrivateBool(controller, "isPurifyCharging", false);

        Assert.That(controller.CanDo(YokaiAction.PurifyHoldStart), Is.False);
        Assert.That(controller.CanDo(YokaiAction.PurifyHoldCancel), Is.False);

        controller.currentState = YokaiState.Purifying;
        controller.isPurifying = true;

        Assert.That(controller.CanDo(YokaiAction.PurifyHoldStart), Is.True);
        Assert.That(controller.CanDo(YokaiAction.PurifyHoldCancel), Is.False);

        SetPrivateBool(controller, "isPurifyCharging", true);

        Assert.That(controller.CanDo(YokaiAction.PurifyHoldStart), Is.False);
        Assert.That(controller.CanDo(YokaiAction.PurifyHoldCancel), Is.True);
    }

    [Test]
    public void TryDo_PurifyHoldStart_OnlySetsChargeFlags()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.Purifying;
        controller.isPurifying = true;
        SetPrivateBool(controller, "isPurifyCharging", false);

        bool executed = controller.TryDo(YokaiAction.PurifyHoldStart, "HoldStart");

        Assert.That(executed, Is.True);
        Assert.That(controller.IsPurifyCharging, Is.True);
        Assert.That(controller.HasUserInteracted, Is.True);
        Assert.That(controller.isPurifying, Is.True);
    }

    [Test]
    public void TryDo_PurifyHoldCancel_StopsPurifyingAndClearsCharge()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.Purifying;
        controller.isPurifying = true;
        SetPrivateBool(controller, "isPurifyCharging", true);

        bool executed = controller.TryDo(YokaiAction.PurifyHoldCancel, "HoldCancel");

        Assert.That(executed, Is.True);
        Assert.That(controller.IsPurifyCharging, Is.False);
        Assert.That(controller.isPurifying, Is.False);
    }

    static YokaiStateController CreateController()
    {
        var go = new GameObject("StateController-Test");
        return go.AddComponent<YokaiStateController>();
    }

    static void SetPrivateBool(YokaiStateController controller, string fieldName, bool value)
    {
        FieldInfo field = typeof(YokaiStateController).GetField(fieldName, InstanceFlags);
        Assert.That(field, Is.Not.Null, $"Private field '{fieldName}' was not found.");
        field.SetValue(controller, value);
    }
}
