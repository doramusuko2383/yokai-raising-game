namespace Yokai
{
    public class YokaiActionExecutor
    {
        readonly YokaiStateController controller;

        public YokaiActionExecutor(YokaiStateController controller)
        {
            this.controller = controller;
        }

        public void Execute(YokaiAction action, string reason)
        {
            YokaiLogger.Action($"action={action} reason={reason ?? "null"} state={controller.CurrentState}");

            switch (action)
            {
                case YokaiAction.Purify:
                    YokaiLogger.Action("[LEGACY] Purify action disabled");
                    break;

                case YokaiAction.PurifyStart:
                    controller.BeginPurifying(reason);
                    break;

                case YokaiAction.PurifyCancel:
                    if (reason == "ChargeComplete")
                    {
                        var command = controller.PurifyMachine.CompleteCharging();

                        if (command == PurifyCommand.CompletePurify)
                            controller.StopPurifyingForSuccess();
                    }
                    else
                    {
                        var command = controller.PurifyMachine.CancelCharging();

                        if (command == PurifyCommand.CancelPurify)
                            controller.CancelPurifying(reason);
                    }
                    break;

                case YokaiAction.PurifyHoldStart:
                {
                    var command = controller.PurifyMachine.StartCharging();
                    if (command == PurifyCommand.None)
                    {
                        controller.NotifyUserInteraction();
                    }
                    break;
                }

                case YokaiAction.PurifyHoldCancel:
                {
                    var command = controller.PurifyMachine.CancelCharging();

                    if (command == PurifyCommand.CancelPurify)
                        controller.CancelPurifying(reason ?? "HoldReleasedEarly");
                    break;
                }

                case YokaiAction.EmergencySpiritRecover:
                    controller.HandleEmergencySpiritRecover();
                    break;


                case YokaiAction.EatDango:
                    controller.HandleEatDango();
                    break;

                case YokaiAction.EmergencyPurifyAd:
                    controller.ExecuteEmergencyPurify(reason ?? "EmergencyPurify");
                    break;

                case YokaiAction.StartEvolution:
                    controller.BeginEvolution();
                    break;

                default:
                    YokaiLogger.Error($"Unhandled YokaiAction in ExecuteAction: {action}");
                    break;
            }
        }
    }
}
