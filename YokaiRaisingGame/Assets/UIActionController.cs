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
        Debug.Log($"[UIActionController][OnEnable][Enter] energyManager={(energyManager == null ? "null" : "ok")} stateController={(stateController == null ? "null" : "ok")}");
        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(energyManager.CurrentEnergy, energyManager.MaxEnergy);
        }
        Debug.Log("[UIActionController][OnEnable][Exit]");
    }

    void OnDisable()
    {
        Debug.Log($"[UIActionController][OnDisable][Enter] energyManager={(energyManager == null ? "null" : "ok")}");
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
        }
        Debug.Log("[UIActionController][OnDisable][Exit]");
    }

    void OnEnergyChanged(float current, float max)
    {
        Debug.Log($"[UIActionController][OnEnergyChanged][Enter] current={current:0.##} max={max:0.##} btnAdWatch={(btnAdWatch == null ? "null" : "ok")}");
        if (btnAdWatch == null)
        {
            Debug.Log("[UIActionController][OnEnergyChanged][EarlyReturn] reason=btnAdWatch null");
            return;
        }

        // 霊力ゼロ → 特おだんごのみ
        if (current <= 0)
        {
            SetNormalButtons(false);
            btnAdWatch.SetActive(true);
            Debug.Log("[UIActionController][OnEnergyChanged][Exit] state=energyEmpty");
            return;
        }

        // 通常状態
        btnAdWatch.SetActive(false);
        SetNormalButtons(true);
        Debug.Log("[UIActionController][OnEnergyChanged][Exit] state=normal");
    }

    void SetNormalButtons(bool active)
    {
        Debug.Log($"[UIActionController][SetNormalButtons][Enter] active={active} btnPurify={(btnPurify == null ? "null" : "ok")} btnDango={(btnDango == null ? "null" : "ok")}");
        if (btnPurify != null) btnPurify.SetActive(active);
        if (btnDango != null) btnDango.SetActive(active);
        Debug.Log("[UIActionController][SetNormalButtons][Exit]");
    }

    // ボタンから呼ばれる
    public void OnClickPurify()
    {
        Debug.Log($"[UIActionController][OnClickPurify][Enter] stateController={(stateController == null ? "null" : "ok")}");
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickPurify][EarlyReturn] reason=stateController null");
            return;
        }
        stateController.TryStartPurify();
        Debug.Log("[UIActionController][OnClickPurify][Exit]");
    }

    public void OnClickDango()
    {
        Debug.Log($"[UIActionController][OnClickDango][Enter] stateController={(stateController == null ? "null" : "ok")}");
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickDango][EarlyReturn] reason=stateController null");
            return;
        }
        stateController.TryFeedDango();
        Debug.Log("[UIActionController][OnClickDango][Exit]");
    }

    public void OnClickAdWatch()
    {
        Debug.Log($"[UIActionController][OnClickAdWatch][Enter] stateController={(stateController == null ? "null" : "ok")}");
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickAdWatch][EarlyReturn] reason=stateController null");
            return;
        }
        stateController.TryRecoverEnergyByAd();
        Debug.Log("[UIActionController][OnClickAdWatch][Exit]");
    }
}
