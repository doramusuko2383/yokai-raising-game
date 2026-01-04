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
    bool isReturning;
    float shakeTimer;
    float returnTimer;
    float noiseSeedX;
    float noiseSeedY;
    Color returnFromColor;
    Vector3 returnFromPosition;

    const float ReturnDuration = 0.25f;
    const float PulseIntensity = 0.85f;
    const float NoiseSpeed = 1.6f;
    const float PulseExponent = 1.35f;

    void Awake()
    {
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();

        if (targetImage == null)
            targetImage = GetComponentInChildren<Image>();

        originalColor = GetCurrentColor();
        originalLocalPosition = GetCurrentLocalPosition();
        noiseSeedX = Random.value * 10f;
        noiseSeedY = Random.value * 10f;
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
        if (isBlinking)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            pulse = Mathf.Pow(pulse, PulseExponent);
            Color deepDanger = Color.Lerp(dangerColor, Color.black, 0.45f);
            Color pulseColor = Color.Lerp(deepDanger, dangerColor, pulse);
            ApplyColor(Color.Lerp(originalColor, pulseColor, PulseIntensity));

            shakeTimer += Time.deltaTime;
            if (shakeTimer >= shakeInterval)
            {
                shakeTimer = 0f;
                float timeSample = Time.time * NoiseSpeed;
                float offsetX = (Mathf.PerlinNoise(noiseSeedX, timeSample) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(noiseSeedY, timeSample + 2.3f) - 0.5f) * 2f;
                float pulseShake = Mathf.Lerp(0.7f, 1.1f, pulse);
                targetShakeOffset = new Vector3(offsetX, offsetY, 0f) * shakeAmplitude * pulseShake;
            }

            currentShakeOffset = Vector3.Lerp(
                currentShakeOffset,
                targetShakeOffset,
                Time.deltaTime * shakeLerpSpeed);

            ApplyLocalPosition(originalLocalPosition + currentShakeOffset);
            return;
        }

        if (isReturning)
        {
            returnTimer += Time.deltaTime;
            float t = Mathf.Clamp01(returnTimer / ReturnDuration);
            ApplyColor(Color.Lerp(returnFromColor, originalColor, t));
            ApplyLocalPosition(Vector3.Lerp(returnFromPosition, originalLocalPosition, t));

            if (t >= 1f)
            {
                isReturning = false;
                currentShakeOffset = Vector3.zero;
                targetShakeOffset = Vector3.zero;
            }
        }
    }

    public void SetBlinking(bool enable)
    {
        if (isBlinking == enable)
            return;

        isBlinking = enable;
        isReturning = !enable;
        shakeTimer = 0f;
        currentShakeOffset = Vector3.zero;
        targetShakeOffset = Vector3.zero;
        returnTimer = 0f;
        returnFromColor = GetCurrentColor();
        returnFromPosition = GetCurrentLocalPosition();

        if (!isBlinking)
        {
            Debug.Log("[DANGER] Effect OFF: returning to original visuals");
        }
        else
        {
            Debug.Log("[DANGER] Effect ON: pulsing danger visuals");
        }
    }

    public void RefreshOriginalColor()
    {
        originalColor = GetCurrentColor();
        originalLocalPosition = GetCurrentLocalPosition();
        currentShakeOffset = Vector3.zero;
        targetShakeOffset = Vector3.zero;
        returnFromColor = originalColor;
        returnFromPosition = originalLocalPosition;
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
