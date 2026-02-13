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

    [Test]
    public void CanDo_StartEvolution_IsAllowedOnlyInEvolutionReady()
    {
        var controller = CreateController();

        controller.currentState = YokaiState.Normal;
        Assert.That(controller.CanDo(YokaiAction.StartEvolution), Is.False);

        controller.currentState = YokaiState.EvolutionReady;
        Assert.That(controller.CanDo(YokaiAction.StartEvolution), Is.True);
    }


    [Test]
    public void OnSpiritEmpty_DoesNotChangeState_InEvolutionReady()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.EvolutionReady;
        SetPrivateBool(controller, "isSpiritEmpty", false);

        controller.OnSpiritEmpty();

        Assert.That(controller.CurrentState, Is.EqualTo(YokaiState.EvolutionReady));
        Assert.That(controller.IsSpiritEmpty, Is.False);
    }

    [Test]
    public void OnPurityEmpty_DoesNotChangeState_InEvolutionReady()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.EvolutionReady;
        SetPrivateBool(controller, "isPurityEmpty", false);

        controller.OnPurityEmpty();

        Assert.That(controller.CurrentState, Is.EqualTo(YokaiState.EvolutionReady));
        Assert.That(controller.IsPurityEmptyState, Is.False);
    }

    [Test]
    public void CanDo_EvolutionReady_AllowsOnlyStartEvolution()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.EvolutionReady;

        Assert.That(controller.CanDo(YokaiAction.StartEvolution), Is.True);
        Assert.That(controller.CanDo(YokaiAction.EatDango), Is.False);
        Assert.That(controller.CanDo(YokaiAction.EmergencyPurifyAd), Is.False);
        Assert.That(controller.CanDo(YokaiAction.EmergencySpiritRecover), Is.False);
    }

    [Test]
    public void DetermineRequestedState_PrioritizesRequestedState_WhenNotEmpty()
    {
        var controller = CreateController();
        controller.currentState = YokaiState.Normal;
        controller.isPurifying = false;

        SetPrivateBool(controller, "isPurityEmpty", false);
        SetPrivateBool(controller, "isSpiritEmpty", false);

        var requested = YokaiState.EvolutionReady;
        var nextState = InvokeDetermineRequestedState(controller, requested);

        Assert.That(nextState, Is.EqualTo(requested));
    }

    [Test]
    public void DetermineRequestedState_PrioritizesEmptyStates_OverRequestedState()
    {
        var controller = CreateController();

        SetPrivateBool(controller, "isPurityEmpty", true);
        SetPrivateBool(controller, "isSpiritEmpty", false);
        Assert.That(InvokeDetermineRequestedState(controller, YokaiState.EvolutionReady), Is.EqualTo(YokaiState.PurityEmpty));

        SetPrivateBool(controller, "isPurityEmpty", false);
        SetPrivateBool(controller, "isSpiritEmpty", true);
        Assert.That(InvokeDetermineRequestedState(controller, YokaiState.EvolutionReady), Is.EqualTo(YokaiState.EnergyEmpty));
    }

    [Test]
    public void YokaiStateRules_ForcedStateAndEvolutionReadyLock_WorkAsExpected()
    {
        var forcedByPurifying = YokaiStateRules.DetermineForcedState(
            YokaiState.Normal,
            isPurifying: true,
            isEvolving: true
        );
        Assert.That(forcedByPurifying, Is.EqualTo(YokaiState.Purifying));

        var forcedByEvolutionReady = YokaiStateRules.DetermineForcedState(
            YokaiState.EvolutionReady,
            isPurifying: false,
            isEvolving: false
        );
        Assert.That(forcedByEvolutionReady, Is.EqualTo(YokaiState.EvolutionReady));

        Assert.That(
            YokaiStateRules.CanDo(
                YokaiState.EvolutionReady,
                YokaiAction.StartEvolution,
                isPurifying: false,
                isPurifyCharging: false,
                isPurityEmpty: false,
                isSpiritEmpty: false
            ),
            Is.True
        );

        Assert.That(
            YokaiStateRules.CanDo(
                YokaiState.EvolutionReady,
                YokaiAction.EatDango,
                isPurifying: false,
                isPurifyCharging: false,
                isPurityEmpty: false,
                isSpiritEmpty: false
            ),
            Is.False
        );
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

    static YokaiState InvokeDetermineRequestedState(YokaiStateController controller, YokaiState requestedState)
    {
        MethodInfo method = typeof(YokaiStateController).GetMethod("DetermineRequestedState", InstanceFlags);
        Assert.That(method, Is.Not.Null, "Private method 'DetermineRequestedState' was not found.");
        return (YokaiState)method.Invoke(controller, new object[] { requestedState });
    }
}
