using System.Diagnostics;
using UnityEngine;

namespace Yokai
{
    public static class YokaiLogger
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        const bool ENABLE_LOG = true;
#else
        const bool ENABLE_LOG = false;
#endif

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void State(string message)
        {
            if (!ENABLE_LOG) return;
            Debug.Log($"[STATE] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Action(string message)
        {
            if (!ENABLE_LOG) return;
            Debug.Log($"[ACTION] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void FSM(string message)
        {
            if (!ENABLE_LOG) return;
            Debug.Log($"[FSM] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message)
        {
            if (!ENABLE_LOG) return;
            Debug.LogWarning($"[WARN] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Error(string message)
        {
            if (!ENABLE_LOG) return;
            Debug.LogError($"[ERROR] {message}");
        }
    }
}
