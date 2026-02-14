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
            YokaiLogger.FSM("BeginPurifying");

            return PurifyCommand.BeginPurifying;
        }

        public PurifyCommand StartCharging()
        {
            if (currentState != PurifyInternalState.Idle)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Charging;
            YokaiLogger.FSM("Idle -> Charging");

            return PurifyCommand.None;
        }

        public PurifyCommand CancelCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Cancelled;
            YokaiLogger.FSM("Charging -> Cancelled");

            return PurifyCommand.CancelPurify;
        }

        public PurifyCommand CompleteCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Completed;
            YokaiLogger.FSM("Charging -> Completed");

            return PurifyCommand.CompletePurify;
        }
    }
}
