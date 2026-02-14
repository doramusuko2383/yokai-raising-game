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

        public PurifyStateMachine()
        {
            currentState = PurifyInternalState.Idle;
        }

        public PurifyCommand StartPurify(string reason)
        {
            if (currentState != PurifyInternalState.Idle)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Idle;
            YokaiLogger.FSM("[Purify] BeginPurifying");

            return PurifyCommand.BeginPurifying;
        }

        public PurifyCommand StartCharging()
        {
            if (currentState != PurifyInternalState.Idle)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Charging;
            YokaiLogger.FSM("[Purify] Idle -> Charging");

            return PurifyCommand.None;
        }

        public PurifyCommand CancelCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Cancelled;
            YokaiLogger.FSM("[Purify] Charging -> Cancelled");

            return PurifyCommand.CancelPurify;
        }

        public PurifyCommand CompleteCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Completed;
            YokaiLogger.FSM("[Purify] Charging -> Completed");

            return PurifyCommand.CompletePurify;
        }
    }
}
