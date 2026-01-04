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
    bool isSuppressed;
    DangerIntensity intensityLevel = DangerIntensity.Medium;

    const float ReturnDuration = 0.25f;
    const float PulseIntensity = 0.85f;
    const float NoiseSpeed = 1.6f;
    const float PulseExponent = 1.35f;
    const float SoftPulseMultiplier = 0.6f;
    const float SoftShakeMultiplier = 0.4f;
    const float SoftSpeedMultiplier = 0.85f;
    const float SoftColorDepth = 0.65f;
    const float StrongPulseMultiplier = 1.1f;
    const float StrongShakeMultiplier = 1.15f;
    const float StrongSpeedMultiplier = 1.1f;
    const float StrongColorDepth = 1.2f;

    public bool IsBlinking => isBlinking;

    enum DangerIntensity
    {
        Soft,
        Medium,
        Strong
    }

    struct IntensitySettings
    {
        public float pulseMultiplier;
        public float shakeMultiplier;
        public float speedMultiplier;
        public float colorDepth;
    }

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
            IntensitySettings settings = GetIntensitySettings();
            float pulse = (Mathf.Sin(Time.time * (pulseSpeed * settings.speedMultiplier)) + 1f) * 0.5f;
            pulse = Mathf.Pow(pulse, PulseExponent);
            Color deepDanger = Color.Lerp(dangerColor, Color.black, 0.45f * settings.colorDepth);
            Color pulseColor = Color.Lerp(deepDanger, dangerColor, pulse);
            ApplyColor(Color.Lerp(originalColor, pulseColor, PulseIntensity * settings.pulseMultiplier));

            shakeTimer += Time.deltaTime;
            if (shakeTimer >= shakeInterval)
            {
                shakeTimer = 0f;
                float timeSample = Time.time * NoiseSpeed;
                float offsetX = (Mathf.PerlinNoise(noiseSeedX, timeSample) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(noiseSeedY, timeSample + 2.3f) - 0.5f) * 2f;
                float pulseShake = Mathf.Lerp(0.7f, 1.1f, pulse);
                targetShakeOffset = new Vector3(offsetX, offsetY, 0f) * shakeAmplitude * settings.shakeMultiplier * pulseShake;
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
            Debug.Log($"{FormatDangerLog("OFF")} Effect OFF: returning to original visuals");
        }
        else
        {
            Debug.Log($"{FormatDangerLog("ON")} Effect ON: pulsing danger visuals");
        }
    }

    public void SetSuppressed(bool suppress)
    {
        if (isSuppressed == suppress)
            return;

        isSuppressed = suppress;
        Debug.Log($"{FormatDangerLog(isSuppressed ? "Suppress:ON" : "Suppress:OFF")} Danger suppression {(isSuppressed ? "enabled" : "disabled")}.");
    }

    public void SetIntensityLevel(int level)
    {
        DangerIntensity newLevel = (DangerIntensity)Mathf.Clamp(level, 0, 2);
        if (intensityLevel == newLevel)
            return;

        intensityLevel = newLevel;
        Debug.Log($"{FormatDangerLog("Intensity")} Danger intensity set to {intensityLevel}.");
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

    IntensitySettings GetIntensitySettings()
    {
        DangerIntensity activeLevel = isSuppressed ? DangerIntensity.Soft : intensityLevel;
        switch (activeLevel)
        {
            case DangerIntensity.Soft:
                return new IntensitySettings
                {
                    pulseMultiplier = SoftPulseMultiplier,
                    shakeMultiplier = SoftShakeMultiplier,
                    speedMultiplier = SoftSpeedMultiplier,
                    colorDepth = SoftColorDepth
                };
            case DangerIntensity.Strong:
                return new IntensitySettings
                {
                    pulseMultiplier = StrongPulseMultiplier,
                    shakeMultiplier = StrongShakeMultiplier,
                    speedMultiplier = StrongSpeedMultiplier,
                    colorDepth = StrongColorDepth
                };
            default:
                return new IntensitySettings
                {
                    pulseMultiplier = 1f,
                    shakeMultiplier = 1f,
                    speedMultiplier = 1f,
                    colorDepth = 1f
                };
        }
    }

    string FormatDangerLog(string phase)
    {
        return $"[DANGER][{Time.time:0.00}s][{phase}]";
    }
}
