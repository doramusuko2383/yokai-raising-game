using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DangoButtonHandler : MonoBehaviour
{
    public TMP_Text buttonText;
    public Image buttonBackground;
    public Color normalColor = Color.white;
    public Color adColor = new Color(0.4f, 0.7f, 1f);

    [SerializeField]
    UIActionController actionController;

    bool isAdMode;

    void Update()
    {
        if (SaveManager.Instance == null)
            return;

        var save = SaveManager.Instance.CurrentSave;
        if (save == null || save.dango == null)
            return;

        bool hasDango = save.dango.currentCount > 0;

        if (hasDango)
        {
            if (isAdMode)
            {
                buttonText.text = "だんご";
                buttonBackground.color = normalColor;
                StopPulse();
                isAdMode = false;
            }
        }
        else
        {
            if (!isAdMode)
            {
                buttonText.text = "広告で回復";
                buttonBackground.color = adColor;
                StartPulse();
                isAdMode = true;
            }
        }
    }

    public void OnClickDango()
    {
        var save = SaveManager.Instance?.CurrentSave;
        if (save == null || save.dango == null)
            return;

        if (save.dango.currentCount > 0)
        {
            if (actionController == null)
                actionController = FindObjectOfType<UIActionController>(true);

            if (actionController != null)
                actionController.Execute(YokaiAction.EatDango);
            else
                Debug.LogWarning("[DangoButtonHandler] UIActionController not found.");
        }
        else
        {
            ShowRewardAd();
        }
    }

    void ShowRewardAd()
    {
        Debug.Log("[AD] Rewarded Ad Simulated");

        var save = SaveManager.Instance.CurrentSave;
        save.dango.currentCount = 1;
        save.dango.lastGeneratedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveManager.Instance.MarkDirty();
    }

    #region Pulse Animation

    Coroutine pulseRoutine;

    void StartPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(Pulse());
    }

    void StopPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        transform.localScale = Vector3.one;
    }

    System.Collections.IEnumerator Pulse()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.05f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
        }
    }

    #endregion
}
