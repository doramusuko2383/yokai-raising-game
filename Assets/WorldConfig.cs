using UnityEngine;

[CreateAssetMenu(menuName = "World/World Config", fileName = "WorldConfig")]
public class WorldConfig : ScriptableObject
{
    public const string DefaultResourcePath = "WorldConfig_Yokai";

    [Header("Mentor")]
    public string mentorName;
    public string mentorSpeechStyle;

    [Header("Status Names")]
    public string energyStatusName;
    public string corruptionStatusName;

    [Header("Items")]
    public string recoveryItemName;

    [Header("State Messages")]
    [TextArea]
    public string normalMessage;
    [TextArea]
    public string weakMessage;
    [TextArea]
    public string dangerMessage;
    [TextArea]
    public string mononokeMessage;
    [TextArea]
    public string recoveredMessage;

    public static WorldConfig LoadDefault()
    {
        return Resources.Load<WorldConfig>(DefaultResourcePath);
    }
}
