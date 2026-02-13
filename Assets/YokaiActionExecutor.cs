using UnityEngine;

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
            Debug.Log($"[ACTION] action={action} reason={reason ?? "null"} state={controller.CurrentState}");

            switch (action)
            {
                case YokaiAction.Purify:
                    Debug.Log("[LEGACY] Purify action disabled");
                    break;

                case YokaiAction.PurifyStart:
                    controller.HandlePurifyStart(reason);
                    break;

                case YokaiAction.PurifyCancel:
                    controller.HandlePurifyCancel(reason);
                    break;

                case YokaiAction.PurifyHoldStart:
                    controller.SetPurifyCharging(true);
                    controller.MarkUserInteracted();
                    break;

                case YokaiAction.PurifyHoldCancel:
                    controller.HandlePurifyHoldCancel(reason);
                    controller.SetPurifyCharging(false);
                    break;

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
                    Debug.LogError($"Unhandled YokaiAction in ExecuteAction: {action}");
                    break;
            }
        }
    }
}
