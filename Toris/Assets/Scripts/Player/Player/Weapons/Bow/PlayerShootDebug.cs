using UnityEngine;

public static class PlayerShootDebug
{
#if UNITY_EDITOR
    public static bool Enabled { get; private set; }

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public static void Log(Object context, string channel, string message)
    {
        if (!Enabled)
            return;

        Debug.Log($"[{channel}][{Time.time:F3}] {message}", context);
    }
#else
    public static bool Enabled => false;

    public static void SetEnabled(bool enabled) { }

    public static void Log(Object context, string channel, string message) { }
#endif
}
