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
    public float completePulseScale = 1.05f;
    public float completePulseDuration = 0.12f;

    [Header("Refs")]
    public UIPentagramDrawer pentagramDrawer;
    public UIPentagramBaseCircle baseCircle;
    public UIPentagramRotate pentagramRotate;
    public RectTransform pentagramRoot;
    public YokaiStateController stateController;

    bool isCharging = false;
    bool hasSucceeded = false;
    float currentCharge = 0f;
    float baseRotateSpeed = 0f;
    Vector3 basePentagramScale = Vector3.one;
    Coroutine completeSequenceCoroutine;
    Coroutine pulseCoroutine;

    void Awake()
    {
        if (pentagramDrawer == null)
        {
            pentagramDrawer = FindObjectOfType<UIPentagramDrawer>();
        }

        if (baseCircle == null)
        {
            baseCircle = FindObjectOfType<UIPentagramBaseCircle>();
        }

        if (pentagramRotate == null)
        {
            pentagramRotate = FindObjectOfType<UIPentagramRotate>();
        }

        if (pentagramRoot == null && pentagramDrawer != null)
        {
            pentagramRoot = pentagramDrawer.GetComponent<RectTransform>();
        }

        if (pentagramRoot != null)
        {
            basePentagramScale = pentagramRoot.localScale;
        }

        SyncBaseCircleScale();
        if (pentagramRotate != null)
        {
            baseRotateSpeed = pentagramRotate.rotateSpeed;
        }
    }

    public void BeginCharge()
    {
        if (hasSucceeded)
        {
            ResetPurify();
        }

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
            pentagramRotate.StopRotate();
            pentagramRotate.ResetRotation();
            pentagramRotate.rotateSpeed = baseRotateSpeed;
        }
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        if (hasSucceeded)
            return;

        Debug.Log("[PURIFY] PointerDown");

        isCharging = true;
        currentCharge = 0f;

        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_START);
        AudioHook.RequestLoop(YokaiSE.SE_PURIFY_CHARGE);

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

        AudioHook.RequestStopLoop(YokaiSE.SE_PURIFY_CHARGE);
        ResetPulseScale();

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

        AudioHook.RequestStopLoop(YokaiSE.SE_PURIFY_CHARGE);

        if (completeSequenceCoroutine != null)
        {
            StopCoroutine(completeSequenceCoroutine);
        }
        completeSequenceCoroutine = StartCoroutine(CoCompleteSequence());

        var controller = ResolveStateController();
        if (controller != null)
        {
            controller.BeginPurifying("ChargeComplete");
            controller.NotifyPurifySucceeded();
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

        StartPulse();

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

        ResetPulseScale();
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

        AudioHook.RequestStopLoop(YokaiSE.SE_PURIFY_CHARGE);
        ResetPulseScale();
    }

    void SyncBaseCircleScale()
    {
        if (baseCircle == null || pentagramDrawer == null) return;
        baseCircle.SetScale(pentagramDrawer.scale);
    }

    YokaiStateController ResolveStateController()
    {
        if (stateController == null)
        {
            stateController = CurrentYokaiContext.ResolveStateController();
        }

        return stateController;
    }

    void StartPulse()
    {
        if (pentagramRoot == null)
            return;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(CoPulseScale());
    }

    IEnumerator CoPulseScale()
    {
        if (pentagramRoot == null)
        {
            pulseCoroutine = null;
            yield break;
        }

        pentagramRoot.localScale = basePentagramScale * completePulseScale;

        float t = 0f;
        while (t < completePulseDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = (completePulseDuration <= 0f) ? 1f : (t / completePulseDuration);
            pentagramRoot.localScale = Vector3.Lerp(basePentagramScale * completePulseScale, basePentagramScale, k);
            yield return null;
        }

        pentagramRoot.localScale = basePentagramScale;
        pulseCoroutine = null;
    }

    void ResetPulseScale()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (pentagramRoot != null)
        {
            pentagramRoot.localScale = basePentagramScale;
        }
    }
}
