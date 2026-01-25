using UnityEngine;

public class AudioInitializer : MonoBehaviour
{
    [SerializeField]
    SEClipLibrary seClipLibrary;

    void Awake()
    {
        if (seClipLibrary == null)
        {
            Debug.LogError("[AUDIO] SEClipLibrary is not assigned.");
            return;
        }

        AudioHook.ClipResolver = seClipLibrary.ResolveClip;
        Debug.Log("[AUDIO] SEClipLibrary registered to AudioHook.");
    }
}
