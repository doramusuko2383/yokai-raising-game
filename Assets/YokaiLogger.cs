using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Yokai
{
    public static class YokaiLogger
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void State(string message)
        {
            Debug.Log($"[STATE] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Action(string message)
        {
            Debug.Log($"[ACTION] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void FSM(string message)
        {
            Debug.Log($"[FSM] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message)
        {
            Debug.LogWarning($"[WARN] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Error(string message)
        {
            Debug.LogError($"[ERROR] {message}");
        }
    }
}
