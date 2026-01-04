using UnityEngine;
using UnityEngine.UI;

public class YokaiDangerEffect : MonoBehaviour
{
    [SerializeField]
    Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

    [SerializeField]
    float blinkInterval = 0.4f;

    [SerializeField]
    SpriteRenderer targetSprite;

    [SerializeField]
    Image targetImage;

    Color originalColor;
    bool isBlinking;
    float timer;
    bool showDangerColor;

    void Awake()
    {
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();

        if (targetImage == null)
            targetImage = GetComponentInChildren<Image>();

        originalColor = GetCurrentColor();
    }

    void OnEnable()
    {
        originalColor = GetCurrentColor();
        if (isBlinking)
            ApplyColor(originalColor);
    }

    void Update()
    {
        if (!isBlinking)
            return;

        timer += Time.deltaTime;
        if (timer < blinkInterval)
            return;

        timer = 0f;
        showDangerColor = !showDangerColor;
        ApplyColor(showDangerColor ? dangerColor : originalColor);
    }

    public void SetBlinking(bool enable)
    {
        if (isBlinking == enable)
            return;

        isBlinking = enable;
        timer = 0f;
        showDangerColor = false;

        if (!isBlinking)
        {
            ApplyColor(originalColor);
        }
        else
        {
            ApplyColor(dangerColor);
        }
    }

    public void RefreshOriginalColor()
    {
        originalColor = GetCurrentColor();
        if (!isBlinking)
            ApplyColor(originalColor);
    }

    Color GetCurrentColor()
    {
        if (targetSprite != null)
            return targetSprite.color;

        if (targetImage != null)
            return targetImage.color;

        return Color.white;
    }

    void ApplyColor(Color color)
    {
        if (targetSprite != null)
            targetSprite.color = color;

        if (targetImage != null)
            targetImage.color = color;
    }
}
