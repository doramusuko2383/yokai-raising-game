using UnityEngine;

public class UIActionController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] GameObject btnPurify;
    [SerializeField] GameObject btnDango;
    [SerializeField] GameObject btnAdWatch;

    [Header("Managers")]
    [SerializeField] EnergyManager energyManager;
    [SerializeField] YokaiStateController stateController;

    void OnEnable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(energyManager.CurrentEnergy, energyManager.MaxEnergy);
        }
    }

    void OnDisable()
    {
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
        }
    }

    void OnEnergyChanged(float current, float max)
    {
        if (btnAdWatch == null)
        {
            return;
        }

        // 霊力ゼロ → 特おだんごのみ
        if (current <= 0)
        {
            SetNormalButtons(false);
            btnAdWatch.SetActive(true);
            return;
        }

        // 通常状態
        btnAdWatch.SetActive(false);
        SetNormalButtons(true);
    }

    void SetNormalButtons(bool active)
    {
        if (btnPurify != null) btnPurify.SetActive(active);
        if (btnDango != null) btnDango.SetActive(active);
    }

    // ボタンから呼ばれる
    public void OnClickPurify()
    {
        if (stateController == null) return;
        stateController.TryStartPurify();
    }

    public void OnClickDango()
    {
        if (stateController == null) return;
        stateController.TryFeedDango();
    }

    public void OnClickAdWatch()
    {
        if (stateController == null) return;
        stateController.TryRecoverEnergyByAd();
    }
}
