using System;
using UnityEngine;
using UnityEngine.UI;

public class DangoUIController : MonoBehaviour
{
    public Image[] dangoSlots;
    public Text countdownText;
    public Button adGenerateButton;     // ←追加

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

        // --- 複数生成対応 ---
        if (previousCount >= 0 && currentCount > previousCount)
        {
            for (int i = previousCount; i < currentCount; i++)
            {
                PlayPopAnimation(i);
            }
        }

        previousCount = currentCount;

        // --- スロット表示 ---
        for (int i = 0; i < dangoSlots.Length; i++)
        {
            dangoSlots[i].color = i < currentCount ? filledColor : emptyColor;
        }

        // --- 広告ボタン表示制御 ---
        if (adGenerateButton != null)
        {
            adGenerateButton.gameObject.SetActive(currentCount < MaxDango);
        }

        // --- CT表示 ---
        if (currentCount >= MaxDango)
        {
            countdownText.text = "満タン";
            countdownText.color = Color.yellow;
            return;
        }

        if (dango.lastGeneratedUnixTime <= 0)
        {
            countdownText.text = "10:00";
            countdownText.color = Color.white;
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = now - dango.lastGeneratedUnixTime;
        long remain = Interval - (elapsed % Interval);
        remain = Mathf.Clamp((int)remain, 0, Interval);

        int minutes = (int)(remain / 60);
        int seconds = (int)(remain % 60);

        countdownText.text = $"{minutes:00}:{seconds:00}";
        countdownText.color = Color.white;
    }

    // --- 広告即生成ボタン ---
    public void OnClickAdGenerate()
    {
        if (SaveManager.Instance == null)
            return;

        var save = SaveManager.Instance.CurrentSave;
        if (save == null || save.dango == null)
            return;

        if (save.dango.currentCount >= MaxDango)
            return;

        // TODO: ここを広告SDK呼び出しに差し替える
        save.dango.currentCount += 1;

        SaveManager.Instance.MarkDirty();
    }

    public void OnClickAdGenerateDango()
    {
        Debug.Log("[DANGO] Ad generate button clicked");

        if (SaveManager.Instance == null)
            return;

        var save = SaveManager.Instance.CurrentSave;
        if (save == null || save.dango == null)
            return;

        const int MaxDango = 3;

        if (save.dango.currentCount >= MaxDango)
            return;

        save.dango.currentCount++;
        save.dango.lastGeneratedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveManager.Instance.MarkDirty();

        // 演出
        PlayPopAnimation(save.dango.currentCount - 1);
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
