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

    void OnEnable()
    {
        if (energyManager == null)
            energyManager = FindObjectOfType<EnergyManager>();

        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
            energyManager.WeakStateChanged += OnWeakStateChanged;
        }

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
    }

    void RefreshUI()
    {
        if (energyManager == null)
            return;

        OnEnergyChanged(energyManager.energy, energyManager.maxEnergy);
        OnWeakStateChanged(energyManager.energy <= 0f);
    }
}
