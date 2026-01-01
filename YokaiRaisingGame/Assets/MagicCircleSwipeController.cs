using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MagicCircleSwipeController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("参照")]
    [SerializeField] RectTransform circleRect;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text instructionText;

    [Header("判定")]
    [SerializeField] int requiredSegments = 6;
    [SerializeField] float innerRadiusRatio = 0.55f;

    bool isActive;
    bool isDragging;
    bool isCompleted;
    bool[] touchedSegments = new bool[8];
    int touchedCount;

    public event Action HealRequested;
    public event Action Completed;

    public void Show()
    {
        if (isActive)
            return;

        isActive = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        ResetSwipe();
    }

    public void Hide()
    {
        if (!isActive)
            return;

        isActive = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        ResetSwipe();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive)
            return;

        if (isCompleted)
        {
            HealRequested?.Invoke();
            return;
        }

        isDragging = true;
        RegisterTouch(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isActive || !isDragging || isCompleted)
            return;

        RegisterTouch(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isActive)
            return;

        isDragging = false;

        if (!isCompleted)
            ResetSwipe();
    }

    void RegisterTouch(PointerEventData eventData)
    {
        if (circleRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                circleRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float outerRadius = circleRect.rect.width * 0.5f;
        float innerRadius = outerRadius * innerRadiusRatio;
        float distance = localPoint.magnitude;

        if (distance < innerRadius || distance > outerRadius)
            return;

        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        int segment = Mathf.FloorToInt(angle / 45f);
        if (segment < 0 || segment >= touchedSegments.Length)
            return;

        if (touchedSegments[segment])
            return;

        touchedSegments[segment] = true;
        touchedCount++;

        if (touchedCount >= requiredSegments)
        {
            isCompleted = true;
            isDragging = false;
            UpdateInstruction(true);
            Completed?.Invoke();
        }
    }

    void ResetSwipe()
    {
        Array.Clear(touchedSegments, 0, touchedSegments.Length);
        touchedCount = 0;
        isCompleted = false;
        isDragging = false;
        UpdateInstruction(false);
    }

    void UpdateInstruction(bool completed)
    {
        if (instructionText == null)
            return;

        instructionText.text = completed ? "タップで回復" : "円をなぞって回復";
    }
}
