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
        bool hasUserInteracted;
        readonly YokaiStateController controller;

        public PurifyStateMachine(YokaiStateController controller)
        {
            this.controller = controller;
            currentState = PurifyInternalState.Idle;
            hasUserInteracted = false;
        }

        public void StartPurify(string reason)
        {
            currentState = PurifyInternalState.Idle;
            hasUserInteracted = false;
            controller.SetHasUserInteracted(false);
            controller.SetPurifyCharging(false);
            controller.SetPurifying(true);
            controller.SetPurifyTriggeredByUser(true);
            Debug.Log("[PURIFY HOLD] BeginPurifying started (UI will handle charge)");
            controller.RequestEvaluateState(reason ?? "BeginPurify", false);
        }

        public void StartCharging()
        {
            if (currentState != PurifyInternalState.Idle)
                return;

            currentState = PurifyInternalState.Charging;
            controller.SetPurifyCharging(true);
            hasUserInteracted = true;
            controller.MarkUserInteracted();
        }

        public void CancelCharging(string reason)
        {
            if (currentState != PurifyInternalState.Charging)
                return;

            currentState = PurifyInternalState.Cancelled;
            controller.SetPurifyCharging(false);
            controller.CancelPurifying(reason ?? "HoldReleasedEarly");
        }

        public void CompleteCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return;

            currentState = PurifyInternalState.Completed;
            controller.SetPurifyCharging(false);
            controller.StopPurifyingForSuccess();
        }

        public void CancelPurify(string reason)
        {
            currentState = PurifyInternalState.Cancelled;
            controller.SetPurifyCharging(false);
            controller.CancelPurifying(reason ?? "Cancelled");
        }
    }
}
