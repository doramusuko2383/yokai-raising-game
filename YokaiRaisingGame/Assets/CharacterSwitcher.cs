using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject fireBallPrefab;
    [SerializeField] GameObject yokaiChildPrefab;
    [SerializeField] GameObject yokaiAdultPrefab;

    GameObject currentInstance;

    void Start()
    {
        SwitchTo(fireBallPrefab);
    }

    public void ShowFireBall()
    {
        SwitchTo(fireBallPrefab);
    }

    public void ShowYokaiChild()
    {
        SwitchTo(yokaiChildPrefab);
    }

    public void ShowYokaiAdult()
    {
        SwitchTo(yokaiAdultPrefab);
    }

    void SwitchTo(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[STATE] Prefab is not assigned.");
            return;
        }

        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        currentInstance = Instantiate(prefab, transform);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;

        CurrentYokaiContext.SetCurrent(currentInstance, "CharacterSwitcher");
        var kegareManager = CurrentYokaiContext.ResolveKegareManager();
        if (kegareManager != null)
            kegareManager.BindCurrentYokai(currentInstance);
        var stateController = CurrentYokaiContext.ResolveStateController();
        if (stateController != null)
            stateController.BindCurrentYokai(currentInstance);
        Debug.Log($"[STATE] CurrentYokaiContext.CurrentName={CurrentYokaiContext.CurrentName()}");
    }
}
