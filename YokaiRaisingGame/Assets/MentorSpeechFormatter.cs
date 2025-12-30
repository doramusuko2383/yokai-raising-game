using UnityEngine;

public static class MentorSpeechFormatter
{
    const string MentorName = "おんみょうじい";

    public static string Format(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return $"{MentorName}：\n「{message}」";
    }
}
