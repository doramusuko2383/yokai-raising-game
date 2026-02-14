namespace Yokai
{
public static class YokaiStateEngine
{
    public static YokaiState DetermineNextState(
        YokaiState current,
        YokaiState? requested,
        bool isPurityEmpty,
        bool isSpiritEmpty,
        bool isPurifying,
        bool isEvolving,
        bool isEvolutionReady
    )
    {
        YokaiState? forcedState = DetermineForcedState(current, isPurifying, isEvolving, isEvolutionReady);
        if (forcedState.HasValue)
            return forcedState.Value;

        if (requested.HasValue)
            return DetermineRequestedState(current, requested.Value, isPurityEmpty, isSpiritEmpty);

        return DetermineDefaultState(isPurityEmpty, isSpiritEmpty);
    }

    public static YokaiState? DetermineForcedState(
        YokaiState current,
        bool isPurifying,
        bool isEvolving,
        bool isEvolutionReady
    )
    {
        return YokaiStateRules.DetermineForcedState(current);
    }

    public static YokaiState DetermineRequestedState(
        YokaiState current,
        YokaiState requested,
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        return YokaiStateRules.DetermineRequestedState(current, requested, isPurityEmpty, isSpiritEmpty);
    }

    public static YokaiState DetermineDefaultState(
        bool isPurityEmpty,
        bool isSpiritEmpty
    )
    {
        return YokaiStateRules.DetermineDefaultState(isPurityEmpty, isSpiritEmpty);
    }
}
}
