using UnityEngine;

// Debug toggle for effects/SE/timeScale.
// 将来的に Inspector 化しても参照先を変えずに済むように集約する。
public static class EffectSettings
{
    public static bool enableEffects = true;

    public static bool EnableEffects => enableEffects;

    public static void SetEnableEffects(bool enable, string reason = null)
    {
        if (enableEffects == enable)
            return;

        enableEffects = enable;
        if (string.IsNullOrEmpty(reason))
            Debug.Log($"[EFFECTS {(enable ? "ON" : "OFF")}]");
        else
            Debug.Log($"[EFFECTS {(enable ? "ON" : "OFF")}] {reason}");
    }

    public static void LogEffectsOff(string context)
    {
        if (!enableEffects)
            Debug.Log($"[EFFECTS OFF] {context}");
    }
}
