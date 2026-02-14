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

        public EvolutionCommand BeginEvolution()
        {
            if (controller.CurrentState != YokaiState.EvolutionReady)
                return EvolutionCommand.None;

            state = EvolutionInternalState.Evolving;
            Debug.Log("[EVO FSM] Ready -> Evolving");
            return EvolutionCommand.BeginEvolving;
        }

        public EvolutionCommand CompleteEvolution()
        {
            state = EvolutionInternalState.Idle;
            Debug.Log("[EVO FSM] Evolving -> Normal");
            return EvolutionCommand.CompleteEvolution;
        }
    }
}
