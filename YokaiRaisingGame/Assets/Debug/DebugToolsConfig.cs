using UnityEngine;

public static class DebugToolsConfig
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    const string EnabledKey = "DebugTools.Enabled";

    static bool? cachedEnabled;

    public static bool Enabled
    {
        get
        {
            if (!cachedEnabled.HasValue)
            {
                cachedEnabled = PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            }

            return cachedEnabled.Value;
        }
        set
        {
            cachedEnabled = value;
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsAvailable => true;
#else
    public static bool Enabled
    {
        get => false;
        set { }
    }

    public static bool IsAvailable => false;
#endif
}
