using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Yokai;

public class PurifyChargeController : MonoBehaviour
{
    [Header("Settings")]
    public float chargeDuration = 2.0f;

    [Header("Complete Sequence")]
    public float completeWaitTime = 0.3f;
    public float completeFadeOutDuration = 0.25f;
    public float completeRotateBoost = 60f;
    public float baseCircleFlashAlpha = 1.2f;

    [Header("Refs")]
    public UIPentagramDrawer pentagramDrawer;
    public UIPentagramBaseCircle baseCircle;
    public UIPentagramRotate pentagramRotate;
    public YokaiStateController stateController;

    bool isCharging = false;
    bool hasSucceeded = false;
    float currentCharge = 0f;
    float baseRotateSpeed = 0f;
    Coroutine completeSequenceCoroutine;

    void Awake()
    {
        SyncBaseCircleScale();
        if (pentagramRotate != null)
        {
            baseRotateSpeed = pentagramRotate.rotateSpeed;
        }
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY] PointerDown");

        isCharging = true;
        currentCharge = 0f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.ClearSuppressRendering();
            pentagramDrawer.SetProgress(0f);
        }

        SyncBaseCircleScale();
        if (baseCircle != null)
        {
            baseCircle.Show();
            baseCircle.FadeIn(completeFadeOutDuration);
        }

        if (pentagramRotate != null)
        {
            baseRotateSpeed = pentagramRotate.rotateSpeed;
            pentagramRotate.StartRotate();
        }
    }

    public void OnPointerUp(BaseEventData eventData)
    {
        if (!isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY] PointerUp");

        isCharging = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.ReverseAndClear();
        }

        if (baseCircle != null)
        {
            baseCircle.FadeOut(completeFadeOutDuration);
        }

        if (pentagramRotate != null)
        {
            pentagramRotate.StopRotate();
            pentagramRotate.ResetRotation();
            pentagramRotate.rotateSpeed = baseRotateSpeed;
        }
    }

    private void Update()
    {
        Debug.Log($"[PURIFY] Update isCharging={isCharging}");

        if (!isCharging || hasSucceeded)
            return;

        currentCharge += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(currentCharge / chargeDuration);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(progress);
            Debug.Log("[PENTAGRAM] SetProgress called");
        }

        if (progress >= 1f)
        {
            CompletePurify();
        }
    }

    private void CompletePurify()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        Debug.Log("[PURIFY] Complete!");

        if (completeSequenceCoroutine != null)
        {
            StopCoroutine(completeSequenceCoroutine);
        }
        completeSequenceCoroutine = StartCoroutine(CoCompleteSequence());

        if (stateController != null)
        {
            stateController.NotifyPurifySucceeded();
        }
    }

    IEnumerator CoCompleteSequence()
    {
        float flashDuration = pentagramDrawer != null ? pentagramDrawer.completeFlashDuration : 0.2f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.PlayCompleteFlash();
        }

        if (baseCircle != null)
        {
            baseCircle.PlayCompleteFlash(flashDuration, baseCircleFlashAlpha);
        }

        if (pentagramRotate != null)
        {
            baseRotateSpeed = pentagramRotate.rotateSpeed;
            pentagramRotate.rotateSpeed = baseRotateSpeed + completeRotateBoost;
            pentagramRotate.StartRotate();
        }

        yield return new WaitForSecondsRealtime(completeWaitTime);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.FadeOutLines(completeFadeOutDuration);
        }

        if (baseCircle != null)
        {
            baseCircle.FadeOut(completeFadeOutDuration);
        }

        yield return new WaitForSecondsRealtime(completeFadeOutDuration);

        if (pentagramDrawer != null)
        {
            pentagramDrawer.ReverseAndClear();
        }

        if (baseCircle != null)
        {
            baseCircle.Hide();
        }

        if (pentagramRotate != null)
        {
            pentagramRotate.StopRotate();
            pentagramRotate.ResetRotation();
            pentagramRotate.rotateSpeed = baseRotateSpeed;
        }

        completeSequenceCoroutine = null;
    }

    public void ResetPurify()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        if (pentagramDrawer != null)
        {
            pentagramDrawer.SetProgress(0f);
        }

        if (baseCircle != null)
        {
            baseCircle.Hide();
        }

        if (pentagramRotate != null)
        {
            pentagramRotate.StopRotate();
            pentagramRotate.ResetRotation();
            pentagramRotate.rotateSpeed = baseRotateSpeed;
        }
    }

    void SyncBaseCircleScale()
    {
        if (baseCircle == null || pentagramDrawer == null) return;
        baseCircle.SetScale(pentagramDrawer.scale);
    }
}
