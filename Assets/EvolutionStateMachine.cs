using UnityEngine;

namespace Yokai
{
    public class EvolutionStateMachine
    {
        enum EvolutionInternalState
        {
            Idle,
            Ready,
            Evolving,
        }

        readonly YokaiStateController controller;
        EvolutionInternalState state = EvolutionInternalState.Idle;

        public EvolutionStateMachine(YokaiStateController controller)
        {
            this.controller = controller;
        }

        public void StartEvolution(string reason)
        {
            state = controller.CurrentState == YokaiState.EvolutionReady
                ? EvolutionInternalState.Ready
                : EvolutionInternalState.Idle;

            if (state != EvolutionInternalState.Ready)
                return;

            controller.SetEvolving(true);
            state = EvolutionInternalState.Evolving;
            controller.RequestEvaluateStateRequested(
                YokaiState.Evolving,
                reason ?? "BeginEvolution",
                false
            );
            Debug.Log("[EVOLUTION FSM] Ready -> Evolving");
        }

        public void CompleteEvolution()
        {
            controller.SetEvolving(false);

            // purity / spirit を初期値にリセット
            controller.SpiritController.SetSpirit(80f);
            controller.PurityController.SetPurity(80f);

            state = EvolutionInternalState.Idle;
            controller.RequestEvaluateStateRequested(
                YokaiState.Normal,
                "EvolutionComplete",
                false
            );
        }
    }
}
