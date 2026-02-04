using UnityEngine;
using UnityEngine.UI;
using Yokai;

public class EvolutionButtonHandler : MonoBehaviour
{
    [SerializeField]
    YokaiStateController stateController;

    [SerializeField]
    GameObject evolutionRoot;

    [SerializeField]
    Button evolutionButton;

    bool hasWarnedMissingStateController;

    void OnEnable()
    {
        RefreshUI();
    }

    public void BindStateController(YokaiStateController controller)
    {
        if (controller == null)
            return;

        stateController = controller;
    }

    public void OnClick()
    {
        var controller = ResolveStateController();
        if (controller == null)
            return;

        controller.TryDo(YokaiAction.StartEvolution, "UI:Evolution");
    }

    public void RefreshUI()
    {
        var controller = ResolveStateController();
        if (controller == null)
        {
            SetAllDisabled();
            return;
        }

        bool canEvolve = controller.CanDo(YokaiAction.StartEvolution);
        if (evolutionRoot != null)
            evolutionRoot.SetActive(canEvolve);
        if (evolutionButton != null)
            evolutionButton.interactable = canEvolve;
    }

    void Update()
    {
        RefreshUI();
    }

    void SetAllDisabled()
    {
        if (evolutionRoot != null)
            evolutionRoot.SetActive(false);
        if (evolutionButton != null)
            evolutionButton.interactable = false;
    }

    YokaiStateController ResolveStateController()
    {
        stateController = CurrentYokaiContext.ResolveStateController();
        if (stateController == null)
            WarnMissingStateController();

        return stateController;
    }

    void WarnMissingStateController()
    {
        if (hasWarnedMissingStateController)
            return;

        Debug.LogWarning("[EVOLUTION] StateController not set in Inspector");
        hasWarnedMissingStateController = true;
    }
}
