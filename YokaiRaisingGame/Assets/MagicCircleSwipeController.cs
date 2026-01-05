using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MagicCircleSwipeController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("参照")]
    [SerializeField] RectTransform circleRect;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text instructionText;

    [Header("ガイド表示")]
    [SerializeField] Image guideRing;
    [SerializeField] Image progressRing;
    [SerializeField] Color guideRingColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] Color progressRingColor = new Color(0.3f, 0.9f, 1f, 0.6f);

    [Header("判定")]
    [SerializeField] float requiredAngle = 270f;
    [SerializeField] float innerRadiusRatio = 0.55f;
    [SerializeField] float outerRadiusRatio = 1.05f;
    [SerializeField] float maxAngleJump = 70f;
    [SerializeField] float minAngleDelta = 0.5f;
    [SerializeField] int minimumSamples = 12;

    bool isActive;
    bool isDragging;
    bool isCompleted;
    bool hasInvalidRadius;
    bool hasInvalidPath;
    bool hasDirection;
    bool hasRequestedHeal;
    float totalAngle;
    float previousAngle;
    float directionSign;
    int sampleCount;

    public event Action HealRequested;
    public event Action Completed;

    void Awake()
    {
        EnsureGuideVisuals();
    }

    void OnEnable()
    {
        Show();
    }

    void OnDisable()
    {
        Hide();
    }

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

        isDragging = true;
        ResetSwipe();
        RegisterTouch(eventData, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isActive || !isDragging || isCompleted)
            return;

        RegisterTouch(eventData, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isActive)
            return;

        isDragging = false;

        if (!isCompleted)
            ResetSwipe();
    }

    void ResetSwipe()
    {
        hasInvalidRadius = false;
        hasInvalidPath = false;
        hasDirection = false;
        totalAngle = 0f;
        directionSign = 0f;
        sampleCount = 0;
        isCompleted = false;
        isDragging = false;
        hasRequestedHeal = false;
        previousAngle = 0f;
        UpdateProgress();
        UpdateInstruction(false);
    }

    void UpdateInstruction(bool completed)
    {
        if (instructionText == null)
            return;

        instructionText.text = completed ? "おはらい 完了" : "円をなぞって おはらい";
    }

    void EnsureGuideVisuals()
    {
        if (circleRect == null)
            return;

        if (guideRing == null)
            guideRing = CreateRing("MagicCircleGuide", false, guideRingColor);

        if (progressRing == null)
            progressRing = CreateRing("MagicCircleProgress", true, progressRingColor);

        if (guideRing != null && progressRing != null)
        {
            guideRing.transform.SetSiblingIndex(0);
            progressRing.transform.SetAsLastSibling();
        }

        UpdateProgress();
    }

    Image CreateRing(string name, bool filled, Color color)
    {
        var ringObject = new GameObject(name);
        ringObject.transform.SetParent(circleRect, false);
        var image = ringObject.AddComponent<Image>();
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        image.color = color;
        image.raycastTarget = false;
        image.type = filled ? Image.Type.Filled : Image.Type.Simple;

        if (filled)
        {
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
        }

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }

    void UpdateProgress()
    {
        if (progressRing == null)
            return;

        float progress = requiredAngle <= 0f ? 0f : Mathf.Clamp01(Mathf.Abs(totalAngle) / requiredAngle);
        progressRing.fillAmount = progress;
    }

    bool IsGestureComplete()
    {
        if (sampleCount < minimumSamples)
            return false;

        if (Mathf.Abs(totalAngle) < requiredAngle)
            return false;

        if (hasInvalidRadius)
            return false;

        if (hasInvalidPath)
            return false;

        return true;
    }

    void RegisterTouch(PointerEventData eventData, bool isInitial)
    {
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
            return;

        if (!IsWithinRadius(localPoint))
        {
            hasInvalidRadius = true;
            return;
        }

        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        if (isInitial)
        {
            previousAngle = angle;
            return;
        }

        float deltaAngle = Mathf.DeltaAngle(previousAngle, angle);
        float absDelta = Mathf.Abs(deltaAngle);

        if (absDelta < minAngleDelta)
            return;

        if (!hasDirection)
        {
            directionSign = Mathf.Sign(deltaAngle);
            hasDirection = true;
        }
        else if (Mathf.Sign(deltaAngle) != directionSign)
        {
            hasInvalidPath = true;
        }

        if (absDelta > maxAngleJump)
        {
            hasInvalidPath = true;
        }

        totalAngle += deltaAngle;
        sampleCount++;
        previousAngle = angle;
        UpdateProgress();

        if (IsGestureComplete())
        {
            isCompleted = true;
            isDragging = false;
            UpdateInstruction(true);
            Completed?.Invoke();
            RequestHealOnce();
        }
    }

    bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (circleRect == null)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            circleRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);
    }

    bool IsWithinRadius(Vector2 localPoint)
    {
        if (circleRect == null)
            return false;

        float outerRadius = circleRect.rect.width * 0.5f;
        float innerRadius = outerRadius * innerRadiusRatio;
        float maxRadius = outerRadius * outerRadiusRatio;
        float distance = localPoint.magnitude;
        return distance >= innerRadius && distance <= maxRadius;
    }

    void RequestHealOnce()
    {
        if (hasRequestedHeal)
            return;

        hasRequestedHeal = true;
        HealRequested?.Invoke();
    }
}
