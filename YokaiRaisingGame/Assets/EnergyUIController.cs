using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private Yokai.YokaiStateController stateController;

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

        if (stateController == null)
            stateController = CurrentYokaiContext.ResolveStateController();

        if (stateController != null)
            stateController.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
        }

        if (stateController != null)
            stateController.StateChanged -= OnStateChanged;
    }

    void Start()
    {
        RefreshUI();
        if (stateController != null)
            ApplyState(stateController.currentState);
    }

    void OnEnergyChanged(float current, float max)
    {
        RefreshUI();
    }

    void OnStateChanged(YokaiState previousState, YokaiState newState)
    {
        ApplyState(newState);
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
    }

    void ApplyState(YokaiState state)
    {
        if (adWatchButton != null)
        {
            adWatchButton.SetActive(state == YokaiState.EnergyEmpty);
        }
    }
}
