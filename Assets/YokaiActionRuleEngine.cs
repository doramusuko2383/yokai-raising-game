namespace Yokai
{
public static class YokaiActionRuleEngine
{
    public static bool CanDo(
        YokaiState current,
        YokaiAction action,
        bool isPurifyCharging,
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        if (current == YokaiState.EvolutionReady)
            return action == YokaiAction.StartEvolution;

        if (!IsAllowedByState(current, action))
            return false;

        return IsActionConditionSatisfied(current, action, isPurifyCharging);
    }

    static bool IsAllowedByState(YokaiState state, YokaiAction action)
    {
        if (state == YokaiState.Evolving)
            return false;

        if (state == YokaiState.EvolutionReady)
            return action == YokaiAction.StartEvolution;

        switch (action)
        {
            case YokaiAction.PurifyStart:
                return state == YokaiState.Normal;

            case YokaiAction.PurifyCancel:
            case YokaiAction.PurifyHold:
            case YokaiAction.PurifyHoldStart:
            case YokaiAction.PurifyHoldCancel:
                return state == YokaiState.Purifying;

            case YokaiAction.EatDango:
                return state == YokaiState.Normal;

            case YokaiAction.EmergencySpiritRecover:
                return state == YokaiState.EnergyEmpty;

            case YokaiAction.EmergencyPurifyAd:
                return state == YokaiState.PurityEmpty;

            case YokaiAction.StartEvolution:
                return state == YokaiState.EvolutionReady;
        }

        return false;
    }

    static bool IsActionConditionSatisfied(
        YokaiState current,
        YokaiAction action,
        bool isPurifyCharging
    )
    {
        switch (action)
        {
            case YokaiAction.PurifyStart:
                return current == YokaiState.Normal;

            case YokaiAction.PurifyCancel:
            case YokaiAction.PurifyHold:
                return current == YokaiState.Purifying;

            case YokaiAction.PurifyHoldStart:
                return current == YokaiState.Purifying && !isPurifyCharging;

            case YokaiAction.PurifyHoldCancel:
                return current == YokaiState.Purifying && isPurifyCharging;

            case YokaiAction.EmergencyPurifyAd:
                return current == YokaiState.PurityEmpty;
        }

        return true;
    }
}
}
