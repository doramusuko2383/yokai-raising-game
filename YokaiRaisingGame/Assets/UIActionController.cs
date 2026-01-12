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
        Debug.Log("[UIActionController][OnEnable][ENTER] energyManager=" + (energyManager == null ? "null" : "ok") + " stateController=" + (stateController == null ? "null" : "ok"));
        if (energyManager != null)
        {
            energyManager.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(energyManager.CurrentEnergy, energyManager.MaxEnergy);
        }
        Debug.Log("[UIActionController][OnEnable][Exit]");
        Debug.Log("[UIActionController][OnEnable][EXIT] energyManager=" + (energyManager == null ? "null" : "ok"));
    }

    void OnDisable()
    {
        Debug.Log($"[UIActionController][OnDisable][Enter] energyManager={(energyManager == null ? "null" : "ok")}");
        Debug.Log("[UIActionController][OnDisable][ENTER] energyManager=" + (energyManager == null ? "null" : "ok"));
        if (energyManager != null)
        {
            energyManager.EnergyChanged -= OnEnergyChanged;
        }
        Debug.Log("[UIActionController][OnDisable][Exit]");
        Debug.Log("[UIActionController][OnDisable][EXIT] energyManager=" + (energyManager == null ? "null" : "ok"));
    }

    void OnEnergyChanged(float current, float max)
    {
        Debug.Log($"[UIActionController][OnEnergyChanged][Enter] current={current:0.##} max={max:0.##} btnAdWatch={(btnAdWatch == null ? "null" : "ok")}");
        Debug.Log("[UIActionController][OnEnergyChanged][ENTER] current=" + current.ToString("0.##") + " max=" + max.ToString("0.##") + " btnAdWatch=" + (btnAdWatch == null ? "null" : "ok"));
        if (btnAdWatch == null)
        {
            Debug.Log("[UIActionController][OnEnergyChanged][EarlyReturn] reason=btnAdWatch null");
            Debug.Log("[UIActionController][OnEnergyChanged][EARLY_RETURN] btnAdWatch=null");
            return;
        }

        // 霊力ゼロ → 特おだんごのみ
        if (current <= 0)
        {
            SetNormalButtons(false);
            btnAdWatch.SetActive(true);
            Debug.Log("[UIActionController][OnEnergyChanged][Exit] state=energyEmpty");
            Debug.Log("[UIActionController][OnEnergyChanged][EXIT] state=energyEmpty");
            return;
        }

        // 通常状態
        btnAdWatch.SetActive(false);
        SetNormalButtons(true);
        Debug.Log("[UIActionController][OnEnergyChanged][Exit] state=normal");
        Debug.Log("[UIActionController][OnEnergyChanged][EXIT] state=normal");
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
        Debug.Log("[UIActionController][OnClickPurify][ENTER] stateController=" + (stateController == null ? "null" : "ok"));
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickPurify][EarlyReturn] reason=stateController null");
            Debug.Log("[UIActionController][OnClickPurify][EARLY_RETURN] stateController=null");
            return;
        }
        stateController.TryStartPurify();
        Debug.Log("[UIActionController][OnClickPurify][Exit]");
        Debug.Log("[UIActionController][OnClickPurify][EXIT] stateController=" + (stateController == null ? "null" : "ok"));
    }

    public void OnClickDango()
    {
        Debug.Log($"[UIActionController][OnClickDango][Enter] stateController={(stateController == null ? "null" : "ok")}");
        Debug.Log("[UIActionController][OnClickDango][ENTER] stateController=" + (stateController == null ? "null" : "ok"));
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickDango][EarlyReturn] reason=stateController null");
            Debug.Log("[UIActionController][OnClickDango][EARLY_RETURN] stateController=null");
            return;
        }
        stateController.TryFeedDango();
        Debug.Log("[UIActionController][OnClickDango][Exit]");
        Debug.Log("[UIActionController][OnClickDango][EXIT] stateController=" + (stateController == null ? "null" : "ok"));
    }

    public void OnClickAdWatch()
    {
        Debug.Log($"[UIActionController][OnClickAdWatch][Enter] stateController={(stateController == null ? "null" : "ok")}");
        Debug.Log("[UIActionController][OnClickAdWatch][ENTER] stateController=" + (stateController == null ? "null" : "ok"));
        if (stateController == null)
        {
            Debug.Log("[UIActionController][OnClickAdWatch][EarlyReturn] reason=stateController null");
            Debug.Log("[UIActionController][OnClickAdWatch][EARLY_RETURN] stateController=null");
            return;
        }
        stateController.TryRecoverEnergyByAd();
        Debug.Log("[UIActionController][OnClickAdWatch][Exit]");
        Debug.Log("[UIActionController][OnClickAdWatch][EXIT] stateController=" + (stateController == null ? "null" : "ok"));
    }
}
