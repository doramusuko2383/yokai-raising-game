namespace Yokai
{
public static class YokaiStateRules
{
    public static YokaiState? DetermineForcedState(
        YokaiState currentState,
        bool isPurifying,
        bool isEvolving
    )
    {
        if (isPurifying)
            return YokaiState.Purifying;

        if (isEvolving)
            return YokaiState.Evolving;

        if (currentState == YokaiState.EvolutionReady)
            return YokaiState.EvolutionReady;

        return null;
    }

    public static YokaiState DetermineRequestedState(
        YokaiState currentState,
        YokaiState requestedState,
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        return requestedState;
    }

    public static YokaiState DetermineDefaultState(
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        if (isPurityEmpty)
            return YokaiState.PurityEmpty;

        if (isSpiritEmpty)
            return YokaiState.EnergyEmpty;

        return YokaiState.Normal;
    }

    public static bool CanDo(
        YokaiState currentState,
        YokaiAction action,
        bool isPurifying,
        bool isPurifyCharging,
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        if (currentState == YokaiState.EvolutionReady)
            return action == YokaiAction.StartEvolution;

        if (!IsAllowedByState(currentState, action))
            return false;

        return IsActionConditionSatisfied(currentState, action, isPurifying, isPurifyCharging);
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
        YokaiState currentState,
        YokaiAction action,
        bool isPurifying,
        bool isPurifyCharging
    )
    {
        switch (action)
        {
            case YokaiAction.PurifyStart:
                return currentState == YokaiState.Normal && !isPurifying;

            case YokaiAction.PurifyCancel:
            case YokaiAction.PurifyHold:
                return currentState == YokaiState.Purifying && isPurifying;

            case YokaiAction.PurifyHoldStart:
                return currentState == YokaiState.Purifying && isPurifying && !isPurifyCharging;

            case YokaiAction.PurifyHoldCancel:
                return currentState == YokaiState.Purifying && isPurifying && isPurifyCharging;

            case YokaiAction.EmergencyPurifyAd:
                return currentState == YokaiState.PurityEmpty && !isPurifying;
        }

        return true;
    }
}
}
