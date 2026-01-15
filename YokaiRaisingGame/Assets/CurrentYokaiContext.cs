using UnityEngine;
using Yokai;

public static class CurrentYokaiContext
{
    static GameObject currentYokai;

    public static event System.Action<GameObject> CurrentChanged;

    public static GameObject Current => currentYokai;

    public static void SetCurrent(GameObject yokai, string reason = null)
    {
        if (currentYokai == yokai)
            return;

        currentYokai = yokai;
#if UNITY_EDITOR
#endif
        CurrentChanged?.Invoke(currentYokai);
    }

    public static string CurrentName()
    {
        return currentYokai != null ? currentYokai.name : "null";
    }

    public static KegareManager ResolveKegareManager()
    {
        if (currentYokai != null)
        {
            var kegare = currentYokai.GetComponentInChildren<KegareManager>(true);
            if (kegare != null)
                return kegare;
        }

        return Object.FindObjectOfType<KegareManager>();
    }

    public static YokaiStateController ResolveStateController()
    {
        return Object.FindObjectOfType<YokaiStateController>();
    }
}
