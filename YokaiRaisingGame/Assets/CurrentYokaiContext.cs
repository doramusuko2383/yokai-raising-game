using UnityEngine;
using Yokai;

public static class CurrentYokaiContext
{
    static GameObject currentYokai;
    static YokaiStateController stateController;

    public static event System.Action<GameObject> CurrentChanged;
    public static event System.Action<GameObject> OnCurrentYokaiConfirmed;

    public static GameObject Current => currentYokai;
    public static YokaiStateController StateController => stateController;

    public static void SetCurrent(GameObject yokai, string reason = null)
    {
        if (currentYokai == yokai)
            return;

        currentYokai = yokai;
#if UNITY_EDITOR
#endif
        CurrentChanged?.Invoke(currentYokai);
        OnCurrentYokaiConfirmed?.Invoke(currentYokai);
    }

    public static void RegisterStateController(YokaiStateController controller)
    {
        stateController = controller;
    }

    public static void UnregisterStateController(YokaiStateController controller)
    {
        if (stateController == controller)
            stateController = null;
    }

    public static string CurrentName()
    {
        return currentYokai != null ? currentYokai.name : "null";
    }

    public static PurityController ResolvePurityController()
    {
        if (currentYokai != null)
        {
            var purity = currentYokai.GetComponentInChildren<PurityController>(true);
            if (purity != null)
                return purity;
        }

        return null;
    }

    public static YokaiStateController ResolveStateController()
    {
        return stateController;
    }
}
