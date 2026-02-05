using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PentagramGestureHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] UIPentagramDrawer drawer;
    [SerializeField] float requiredHoldTime = 3.0f;
    public UnityEvent OnCompleted;

    float elapsed;
    bool isHolding;
    bool isCompleted;

    void Update()
    {
        if (!isHolding || isCompleted)
            return;

        elapsed += Time.unscaledDeltaTime;

        float progress = requiredHoldTime > 0f ? elapsed / requiredHoldTime : 1f;
        drawer?.SetProgress(progress);

        if (progress >= 1f)
        {
            isHolding = false;
            isCompleted = true;
            drawer?.SetProgress(1f);
            OnCompleted?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isCompleted)
            return;

        elapsed = 0f;
        isHolding = true;
        drawer?.SetProgress(0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHold();
    }

    void CancelHold()
    {
        if (!isHolding || isCompleted)
            return;

        isHolding = false;
        elapsed = 0f;
        drawer?.SetProgress(0f);
    }
}
