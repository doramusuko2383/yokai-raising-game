using UnityEngine;
using UnityEngine.UI;

public class YokaiDangerEffect : MonoBehaviour
{
    [SerializeField]
    Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

    [SerializeField]
    float pulseSpeed = 2.2f;

    [SerializeField]
    float shakeInterval = 0.1f;

    [SerializeField]
    float shakeAmplitude = 3f;

    [SerializeField]
    float shakeLerpSpeed = 12f;

    [SerializeField]
    SpriteRenderer targetSprite;

    [SerializeField]
    Image targetImage;

    Color originalColor;
    Vector3 originalLocalPosition;
    Vector3 currentShakeOffset;
    Vector3 targetShakeOffset;
    bool isBlinking;
    float shakeTimer;

    void Awake()
    {
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();

        if (targetImage == null)
            targetImage = GetComponentInChildren<Image>();

        originalColor = GetCurrentColor();
        originalLocalPosition = GetCurrentLocalPosition();
    }

    void OnEnable()
    {
        originalColor = GetCurrentColor();
        originalLocalPosition = GetCurrentLocalPosition();
        if (!isBlinking)
            ApplyColor(originalColor);
    }

    void Update()
    {
        if (!isBlinking)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        ApplyColor(Color.Lerp(originalColor, dangerColor, pulse));

        shakeTimer += Time.deltaTime;
        if (shakeTimer >= shakeInterval)
        {
            shakeTimer = 0f;
            Vector2 random = Random.insideUnitCircle * shakeAmplitude;
            targetShakeOffset = new Vector3(random.x, random.y, 0f);
        }

        currentShakeOffset = Vector3.Lerp(
            currentShakeOffset,
            targetShakeOffset,
            Time.deltaTime * shakeLerpSpeed);

        ApplyLocalPosition(originalLocalPosition + currentShakeOffset);
    }

    public void SetBlinking(bool enable)
    {
        if (isBlinking == enable)
            return;

        isBlinking = enable;
        shakeTimer = 0f;
        currentShakeOffset = Vector3.zero;
        targetShakeOffset = Vector3.zero;

        if (!isBlinking)
        {
            ApplyColor(originalColor);
            ApplyLocalPosition(originalLocalPosition);
        }
        else
        {
            ApplyColor(dangerColor);
        }
    }

    public void RefreshOriginalColor()
    {
        originalColor = GetCurrentColor();
        originalLocalPosition = GetCurrentLocalPosition();
        currentShakeOffset = Vector3.zero;
        targetShakeOffset = Vector3.zero;
        if (!isBlinking)
        {
            ApplyColor(originalColor);
            ApplyLocalPosition(originalLocalPosition);
        }
    }

    Color GetCurrentColor()
    {
        if (targetSprite != null)
            return targetSprite.color;

        if (targetImage != null)
            return targetImage.color;

        return Color.white;
    }

    Vector3 GetCurrentLocalPosition()
    {
        if (targetSprite != null)
            return targetSprite.transform.localPosition;

        if (targetImage != null)
            return targetImage.rectTransform.localPosition;

        return transform.localPosition;
    }

    void ApplyColor(Color color)
    {
        if (targetSprite != null)
            targetSprite.color = color;

        if (targetImage != null)
            targetImage.color = color;
    }

    void ApplyLocalPosition(Vector3 position)
    {
        if (targetSprite != null)
        {
            targetSprite.transform.localPosition = position;
            return;
        }

        if (targetImage != null)
        {
            targetImage.rectTransform.localPosition = position;
            return;
        }

        transform.localPosition = position;
    }
}
