using System;
using UnityEngine;
using UnityEngine.UI;

public class DangoUIController : MonoBehaviour
{
    public Image[] dangoSlots;        // 3つ
    public Text countdownText;        // CT表示
    public Color emptyColor = new Color(1f, 1f, 1f, 0.3f);
    public Color filledColor = Color.white;

    int previousCount = -1;
    const int MaxDango = 3;
    const int Interval = 600;

    void Update()
    {
        if (SaveManager.Instance == null)
            return;

        var save = SaveManager.Instance.CurrentSave;
        if (save == null || save.dango == null)
            return;

        UpdateUI(save);
    }

    void UpdateUI(GameSaveData save)
    {
        var dango = save.dango;

        int currentCount = Mathf.Clamp(dango.currentCount, 0, MaxDango);

        // --- 生成検知 ---
        if (previousCount >= 0 && currentCount > previousCount)
        {
            PlayPopAnimation(currentCount - 1);
        }

        previousCount = currentCount;

        // --- スロット表示 ---
        for (int i = 0; i < dangoSlots.Length; i++)
        {
            dangoSlots[i].color = i < currentCount ? filledColor : emptyColor;
        }

        // --- CT表示 ---
        if (currentCount >= MaxDango)
        {
            countdownText.text = "満タン";
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = now - dango.lastGeneratedUnixTime;
        long remain = Interval - (elapsed % Interval);
        remain = Mathf.Clamp((int)remain, 0, Interval);

        int minutes = (int)(remain / 60);
        int seconds = (int)(remain % 60);

        countdownText.text = $"{minutes:00}:{seconds:00}";
    }

    void PlayPopAnimation(int index)
    {
        if (index < 0 || index >= dangoSlots.Length)
            return;

        StartCoroutine(PopCoroutine(dangoSlots[index].transform));
    }

    System.Collections.IEnumerator PopCoroutine(Transform target)
    {
        Vector3 original = target.localScale;
        Vector3 enlarged = original * 1.2f;

        float t = 0f;
        float duration = 0.15f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(original, enlarged, t / duration);
            yield return null;
        }

        t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(enlarged, original, t / duration);
            yield return null;
        }

        target.localScale = original;
    }
}
