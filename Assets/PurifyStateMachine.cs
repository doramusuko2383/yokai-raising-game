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

        public bool IsPurifying { get; private set; }
        public bool IsCharging { get; private set; }
        public bool HasUserInteracted { get; private set; }

        public PurifyStateMachine()
        {
            currentState = PurifyInternalState.Idle;
            IsPurifying = false;
            IsCharging = false;
            HasUserInteracted = false;
        }

        public PurifyCommand StartPurify(string reason)
        {
            if (currentState != PurifyInternalState.Idle)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Idle;
            IsPurifying = true;
            IsCharging = false;
            HasUserInteracted = false;
            YokaiLogger.FSM("[Purify] BeginPurifying");

            return PurifyCommand.BeginPurifying;
        }

        public PurifyCommand StartCharging()
        {
            if (currentState != PurifyInternalState.Idle)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Charging;
            IsCharging = true;
            YokaiLogger.FSM("[Purify] Idle -> Charging");

            return PurifyCommand.None;
        }

        public PurifyCommand CancelCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Cancelled;
            IsCharging = false;
            IsPurifying = false;
            YokaiLogger.FSM("[Purify] Charging -> Cancelled");

            // 状態を初期化して次回操作を可能にする
            currentState = PurifyInternalState.Idle;

            return PurifyCommand.CancelPurify;
        }

        public PurifyCommand CompleteCharging()
        {
            if (currentState != PurifyInternalState.Charging)
                return PurifyCommand.None;

            currentState = PurifyInternalState.Completed;
            IsCharging = false;
            IsPurifying = false;
            YokaiLogger.FSM("[Purify] Charging -> Completed");

            // 状態を初期化して次回操作を可能にする
            currentState = PurifyInternalState.Idle;

            return PurifyCommand.CompletePurify;
        }

        public void MarkUserInteracted()
        {
            HasUserInteracted = true;
        }

        public void Reset()
        {
            currentState = PurifyInternalState.Idle;
            IsPurifying = false;
            IsCharging = false;
            HasUserInteracted = false;
        }
    }
}
