using UnityEngine;
using UnityEngine.UI;

public class EnergyUIController : MonoBehaviour
{
    [SerializeField]
    EnergyManager energyManager;

    [SerializeField]
    Slider energySlider;

    [SerializeField]
    GameObject adWatchButton;

    [SerializeField]
    GameObject weakMessage;

    [Header("Weak Visuals")]
    [SerializeField]
    SpriteRenderer yokaiSprite;

    [SerializeField]
    float weakScale = 0.8f;

    [SerializeField]
    float weakAlpha = 0.4f;

    Vector3 originalScale;
    Color originalColor;
    bool hasCachedOriginal;

    void OnEnable()
    {
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
            energyManager.WeakStateChanged += OnWeakStateChanged;
        }

        CacheOriginalVisuals();
        RefreshUI();
    }

    void OnDisable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
            energyManager.WeakStateChanged -= OnWeakStateChanged;
        }
    }

    void OnEnergyChanged(float current, float max)
    {
        if (energySlider != null)
            energySlider.value = max > 0f ? current / max : 0f;
    }

    void OnWeakStateChanged(bool isWeak)
    {
        if (adWatchButton != null)
            adWatchButton.SetActive(isWeak);

        if (weakMessage != null)
            weakMessage.SetActive(isWeak);

        ApplyWeakVisuals(isWeak);
    }

    void RefreshUI()
    {
        if (energyManager == null)
            return;

        OnEnergyChanged(energyManager.energy, energyManager.maxEnergy);
        OnWeakStateChanged(energyManager.energy <= 0f);
    }

    void CacheOriginalVisuals()
    {
        if (yokaiSprite == null)
            return;

        originalScale = yokaiSprite.transform.localScale;
        originalColor = yokaiSprite.color;
        hasCachedOriginal = true;
    }

    void ApplyWeakVisuals(bool isWeak)
    {
        if (yokaiSprite == null)
            return;

        if (!hasCachedOriginal)
            CacheOriginalVisuals();

        if (isWeak)
        {
            yokaiSprite.transform.localScale = originalScale * weakScale;
            yokaiSprite.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                weakAlpha
            );
        }
        else
        {
            yokaiSprite.transform.localScale = originalScale;
            yokaiSprite.color = originalColor;
        }
    }
}
