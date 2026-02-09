using UnityEngine;
using Yokai;
using System.Collections;
public class PurifyChargeController : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 2.0f;
    [SerializeField] YokaiStateController stateController;
    [SerializeField] private UIPentagramDrawer uiPentagramDrawer;
    [SerializeField] private PentagramDrawer linePentagramDrawer;
    [SerializeField] private RectTransform pentagramRoot;
    [SerializeField] private YokaiSE chargeSE = YokaiSE.SE_PURIFY_CHARGE;

    private bool isCharging = false;
    private bool hasSucceeded = false;
    private float currentCharge = 0f;
    private float currentVisualProgress = 0f;
    private Vector3 basePentagramScale = Vector3.one;
    private Coroutine reverseEraseRoutine;
    private Coroutine finishEffectRoutine;

    public void BindStateController(YokaiStateController controller)
    {
        stateController = controller;
    }

    private void Awake()
    {
        CachePentagramRoot();
    }

    private void OnEnable()
    {
        CachePentagramRoot();
        stateController =
            CurrentYokaiContext.ResolveStateController()
            ?? FindObjectOfType<YokaiStateController>(true);

        if (stateController == null)
            Debug.LogError("[PURIFY HOLD] StateController could not be resolved.");
    }

    YokaiStateController ResolveStateController()
    {
        if (stateController != null)
            return stateController;

        stateController =
            CurrentYokaiContext.ResolveStateController()
            ?? FindObjectOfType<YokaiStateController>(true);

        if (stateController == null)
            Debug.LogError("[PURIFY HOLD] StateController could not be resolved.");

        return stateController;
    }

    /// <summary>
    /// 五芒星の PointerDown から呼ばれる
    /// </summary>
    public void StartCharging()
    {
        if (ResolveStateController() == null)
            return;

        if (hasSucceeded)
        {
            hasSucceeded = false;
            currentCharge = 0f;
            isCharging = false;
        }

        if (reverseEraseRoutine != null)
        {
            StopCoroutine(reverseEraseRoutine);
            reverseEraseRoutine = null;
        }

        if (finishEffectRoutine != null)
        {
            StopCoroutine(finishEffectRoutine);
            finishEffectRoutine = null;
        }

        ResetVisual();

        if (isCharging)
            return;

        Debug.Log("[PURIFY HOLD] StartCharging CALLED");

        isCharging = true;
        currentCharge = 0f;

        AudioHook.RequestPlay(chargeSE);
    }

    /// <summary>
    /// PointerUp / Exit から呼ばれる
    /// </summary>
    public void CancelCharging()
    {
        if (!isCharging)
            return;

        Debug.Log("[PURIFY HOLD] CancelCharging");

        isCharging = false;
        currentCharge = 0f;
        StartReverseErase();
    }

    private void Update()
    {
        if (!isCharging || hasSucceeded)
            return;

        Debug.Log("[PURIFY HOLD] Update tick");

        currentCharge += Time.deltaTime;
        float progress = currentCharge / chargeDuration;

        Debug.Log($"[PURIFY HOLD] Progress={progress:F2}");
        UpdateVisual(progress);

        if (currentCharge >= chargeDuration)
        {
            Complete();
        }
    }

    private void Complete()
    {
        if (hasSucceeded)
            return;

        hasSucceeded = true;
        isCharging = false;

        Debug.Log("[PURIFY HOLD] Complete");
        AudioHook.RequestPlay(YokaiSE.SE_PURIFY_SUCCESS);
        var sc = ResolveStateController();
        if (sc == null)
            return;

        sc.StopPurifyingForSuccess();
        Debug.Log($"[PURIFY HOLD] StopPurifyingForSuccess called sc={(sc != null)}");
        UpdateVisual(1f);
        StartFinishEffect();
    }

    /// <summary>
    /// State 側から「おきよめ終了・中断」された場合に呼ぶ
    /// </summary>
    public void ResetCharge()
    {
        isCharging = false;
        hasSucceeded = false;
        currentCharge = 0f;

        Debug.Log("[PURIFY HOLD] ResetCharge");
    }

    void UpdateVisual(float progress)
    {
        currentVisualProgress = Mathf.Clamp01(progress);
        if (uiPentagramDrawer != null)
            uiPentagramDrawer.SetProgress(currentVisualProgress);

        if (linePentagramDrawer != null)
            linePentagramDrawer.SetProgress(currentVisualProgress);
    }

    void ResetVisual()
    {
        currentVisualProgress = 0f;
        if (uiPentagramDrawer != null)
            uiPentagramDrawer.SetProgress(0f);

        if (linePentagramDrawer != null)
            linePentagramDrawer.SetProgress(0f);

        if (pentagramRoot != null)
            pentagramRoot.localScale = basePentagramScale;
    }

    void CachePentagramRoot()
    {
        if (pentagramRoot == null && uiPentagramDrawer != null)
            pentagramRoot = uiPentagramDrawer.transform.parent as RectTransform;

        if (pentagramRoot != null)
            basePentagramScale = pentagramRoot.localScale;
    }

    void StartReverseErase()
    {
        if (finishEffectRoutine != null)
        {
            StopCoroutine(finishEffectRoutine);
            finishEffectRoutine = null;
        }

        if (reverseEraseRoutine != null)
            StopCoroutine(reverseEraseRoutine);

        reverseEraseRoutine = StartCoroutine(ReverseErase());
    }

    void StartFinishEffect()
    {
        if (reverseEraseRoutine != null)
        {
            StopCoroutine(reverseEraseRoutine);
            reverseEraseRoutine = null;
        }

        if (finishEffectRoutine != null)
            StopCoroutine(finishEffectRoutine);

        finishEffectRoutine = StartCoroutine(FinishEffect());
    }

    IEnumerator ReverseErase()
    {
        float start = currentVisualProgress;
        float t = 0f;
        float duration = 0.3f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Lerp(start, 0f, t / duration);
            UpdateVisual(p);
            yield return null;
        }

        UpdateVisual(0f);
        reverseEraseRoutine = null;
    }

    IEnumerator FinishEffect()
    {
        UpdateVisual(1f);
        yield return new WaitForSeconds(0.5f);

        float t = 0f;
        float duration = 0.25f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1.0f, 1.08f, t / duration);
            if (pentagramRoot != null)
                pentagramRoot.localScale = basePentagramScale * scale;
            yield return null;
        }

        if (pentagramRoot != null)
            pentagramRoot.localScale = basePentagramScale;

        StartReverseErase();

        yield return new WaitForSeconds(0.3f);

        ResetCharge();
        finishEffectRoutine = null;
    }

}
