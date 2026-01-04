using System.Collections.Generic;
using UnityEngine;

public static class SEHub
{
    static readonly HashSet<YokaiSE> PlayedThisFrame = new HashSet<YokaiSE>();
    static int lastFrame = -1;

    public static void Play(YokaiSE se)
    {
        int frame = Time.frameCount;
        if (frame != lastFrame)
        {
            lastFrame = frame;
            PlayedThisFrame.Clear();
        }

        if (!PlayedThisFrame.Add(se))
            return;

        Debug.Log($"[SE] {se}");
        // TODO: Replace with AudioManager hook when available.
    }
}
