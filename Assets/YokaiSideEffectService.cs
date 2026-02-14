namespace Yokai
{
public class YokaiSideEffectService
{
    private readonly YokaiStateController controller;

    public YokaiSideEffectService(YokaiStateController controller)
    {
        this.controller = controller;
    }

    public void ApplyStateSideEffects(YokaiState state)
    {
        ApplyEmptyStateEffects(state);
        SyncPresentation(state);
    }

    private void ApplyEmptyStateEffects(YokaiState state)
    {
        bool isLockedState = state == YokaiState.EvolutionReady || state == YokaiState.Evolving;
        bool shouldEnableDecay = !isLockedState && state != YokaiState.EnergyEmpty && state != YokaiState.PurityEmpty;
        bool shouldEnableGrowth = shouldEnableDecay;

        if (controller.SpiritController != null)
        {
            if (controller.SpiritController.SetNaturalDecayEnabled(shouldEnableDecay))
            {
                YokaiLogger.State($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={state})");
            }
        }

        if (controller.PurityController != null)
        {
            if (controller.PurityController.SetNaturalDecayEnabled(shouldEnableDecay))
            {
                YokaiLogger.State($"[DECAY] NaturalDecay {(shouldEnableDecay ? "enabled" : "disabled")} (State={state})");
            }
        }

        if (controller.GrowthController != null)
        {
            if (controller.GrowthController.SetGrowthEnabled(shouldEnableGrowth))
            {
                YokaiLogger.State($"[GROWTH] Growth {(shouldEnableGrowth ? "enabled" : "disabled")} (State={state})");
            }
        }
    }

    private void SyncPresentation(YokaiState state)
    {
        var presentationController = controller.ResolvePresentationController();
        if (presentationController == null)
            return;

        YokaiLogger.State($"[SYNC] ApplyState {state} force={false}");
        presentationController.ApplyState(state, force: false);
    }
}
}
