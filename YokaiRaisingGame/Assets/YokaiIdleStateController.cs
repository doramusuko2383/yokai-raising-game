using System.Collections;
using UnityEngine;

public class YokaiIdleStateController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private KegareManager kegareManager;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private YokaiStateDisplay stateDisplay;

    [Header("放置判定")]
    [SerializeField] private float idleThreshold = 30f;

    [Header("候補フラグ（Inspector用）")]
    [SerializeField] private bool isMononokeCandidate;
    [SerializeField] private bool isCriticalCandidate;

    [SerializeField] private float lastActionTime;

    void Start()
    {
        lastActionTime = Time.time;
        StartCoroutine(CheckIdleLoop());
    }

    public void RecordAction()
    {
        lastActionTime = Time.time;
    }

    public void SetMononokeCandidate(bool value)
    {
        isMononokeCandidate = value;
    }

    public void SetCriticalCandidate(bool value)
    {
        isCriticalCandidate = value;
    }

    public void UpdateStateDisplay(YokaiState state)
    {
        if (stateDisplay != null)
        {
            stateDisplay.SetState(state);
        }
    }

    public bool IsIdleNow()
    {
        return Time.time - lastActionTime >= idleThreshold;
    }

    IEnumerator CheckIdleLoop()
    {
        var wait = new WaitForSeconds(1f);

        while (true)
        {
            yield return wait;

            if (!IsIdle())
            {
                continue;
            }

            if (IsStateLocked())
            {
                continue;
            }

            if (isCriticalCandidate)
            {
                if (energyManager != null)
                {
                    energyManager.EnterCriticalFromIdle();
                    UpdateStateDisplay(YokaiState.Critical);
                }

                isCriticalCandidate = false;
                continue;
            }

            if (isMononokeCandidate)
            {
                if (kegareManager != null)
                {
                    kegareManager.EnterMononokeFromIdle();
                    UpdateStateDisplay(YokaiState.Mononoke);
                }

                isMononokeCandidate = false;
            }
        }
    }

    bool IsIdle()
    {
        return IsIdleNow();
    }

    bool IsStateLocked()
    {
        bool isCritical = energyManager != null && energyManager.IsCritical;
        bool isMononoke = kegareManager != null && kegareManager.IsMononoke;
        return isCritical || isMononoke;
    }
}
