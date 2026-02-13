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
        Debug.Log($"[ACTION] action={action} reason={reason ?? "(null)"} state={controller.CurrentState}");

        switch (action)
        {
            case YokaiAction.Purify:
                Debug.Log("[LEGACY] Purify action disabled");
                return;

            case YokaiAction.PurifyStart:
                if (reason == null)
                    controller.BeginPurifying();
                else
                    controller.BeginPurifying(reason);
                return;

            case YokaiAction.PurifyCancel:
                if (reason == "ChargeComplete")
                    controller.StopPurifyingForSuccess();
                else if (reason == null)
                    controller.CancelPurifying();
                else
                    controller.CancelPurifying(reason);
                return;

            case YokaiAction.PurifyHoldStart:
                controller.SetPurifyCharging(true);
                controller.MarkUserInteracted();
                return;

            case YokaiAction.PurifyHoldCancel:
                if (controller.IsPurifying)
                    controller.CancelPurifying(reason ?? "HoldReleasedEarly");

                controller.SetPurifyCharging(false);
                return;

            case YokaiAction.EmergencySpiritRecover:
                Debug.Log("[YokaiStateController] ExecuteAction EmergencySpiritRecover reached");
                controller.RecoverSpiritInternal();
                return;

            case YokaiAction.EatDango:
                controller.RecoverSpiritInternal();
                return;

            case YokaiAction.EmergencyPurifyAd:
                controller.ExecuteEmergencyPurify(reason ?? "EmergencyPurify");
                return;

            case YokaiAction.StartEvolution:
                controller.BeginEvolution();
                return;

            default:
                Debug.LogError($"Unhandled YokaiAction in ExecuteAction: {action}");
                return;
        }
    }
}
}
