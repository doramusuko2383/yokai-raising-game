using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yokai;

public class EnergyUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private YokaiStateController stateController;

    [Header("UI")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private GameObject adWatchButton;

    void OnEnable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
        }
    }

    void OnDisable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
        }
    }

    void Start()
    {
        RefreshUI();
    }

    void OnEnergyChanged(float current, float max)
    {
        RefreshUI();
    }

    void RefreshUI()
    {
        if (energyManager == null)
            return;

        if (energySlider != null)
        {
            energySlider.maxValue = energyManager.maxEnergy;
            energySlider.value = energyManager.energy;
        }

        if (energyText != null)
        {
            energyText.text = $"{energyManager.energy:0}/{energyManager.maxEnergy}";
        }

        if (adWatchButton != null)
        {
            adWatchButton.SetActive(energyManager.energy <= 0);
        }
    }
}
