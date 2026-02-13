using UnityEngine;

namespace Yokai
{
    public class PurifyStateMachine
    {
        enum PurifyInternalState
        {
            Idle,
            Charging,
            Completed,
            Cancelled
        }

        PurifyInternalState currentState;
        readonly YokaiStateController controller;

        public PurifyStateMachine(YokaiStateController controller)
        {
            this.controller = controller;
            currentState = PurifyInternalState.Idle;
        }

        public void StartPurify(string reason)
        {
            currentState = PurifyInternalState.Idle;
            controller.SetHasUserInteracted(false);
            controller.SetPurifyCharging(false);
            controller.SetPurifyTriggeredByUser(true);
            Debug.Log("[PURIFY HOLD] BeginPurifying started (UI will handle charge)");
            controller.SetState(YokaiState.Purifying, reason ?? "BeginPurify");
        }

        public void StartCharging()
        {
            if (currentState != PurifyInternalState.Idle)
                return;

            currentState = PurifyInternalState.Charging;
            Debug.Log("[PURIFY FSM] Idle -> Charging");
            controller.SetPurifyCharging(true);
            controller.MarkUserInteracted();
        }

        public void CancelCharging(string reason)
        {
            if (currentState != PurifyInternalState.Charging)
                return;

            currentState = PurifyInternalState.Cancelled;
            controller.SetPurifyCharging(false);
            controller.NotifyPurifyCancelled();
            controller.SetState(YokaiState.Normal, reason ?? "HoldReleasedEarly");
        }

        public void CompleteCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return;

            currentState = PurifyInternalState.Completed;
            controller.SetPurifyCharging(false);
            controller.NotifyPurifySucceeded();
            controller.SetState(YokaiState.Normal, "PurifySuccess");
        }

        public void FallbackComplete(string reason)
        {
            controller.NotifyPurifySucceeded();
            controller.SetState(YokaiState.Normal, reason ?? "PurifyFallback");
        }

        public void CancelPurify(string reason)
        {
            currentState = PurifyInternalState.Cancelled;
            controller.SetPurifyCharging(false);
            controller.NotifyPurifyCancelled();
            controller.SetState(YokaiState.Normal, reason ?? "Cancelled");
        }
    }
}
