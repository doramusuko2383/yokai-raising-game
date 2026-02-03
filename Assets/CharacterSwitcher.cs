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
        if (fireBallPrefab != null && FindChildByName(fireBallPrefab.name) != null)
        {
            SwitchTo(fireBallPrefab);
            return;
        }

        if (yokaiChildPrefab != null && FindChildByName(yokaiChildPrefab.name) != null)
        {
            SwitchTo(yokaiChildPrefab);
            return;
        }

        if (yokaiAdultPrefab != null && FindChildByName(yokaiAdultPrefab.name) != null)
        {
            SwitchTo(yokaiAdultPrefab);
        }
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
            currentInstance.SetActive(false);
        }

        GameObject existing = FindChildByName(prefab.name);
        if (existing != null)
        {
            currentInstance = existing;
        }
        else
        {
            if (prefab == fireBallPrefab)
            {
                Debug.LogWarning("[STATE] FireBall prefab is not present under CharacterSwitcher.");
                return;
            }

            currentInstance = Instantiate(prefab, transform);
            currentInstance.name = prefab.name;
        }

        currentInstance.SetActive(true);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;

        CurrentYokaiContext.SetCurrent(currentInstance, "CharacterSwitcher");
        var stateController = CurrentYokaiContext.ResolveStateController();
        if (stateController != null)
        {
            stateController.BindCurrentYokai(currentInstance);
            stateController.MarkReady();
            stateController.ForceReevaluate("YokaiReady");
        }
        Debug.Log($"[STATE] CurrentYokaiContext.CurrentName={CurrentYokaiContext.CurrentName()}");
    }

    GameObject FindChildByName(string name)
    {
        foreach (Transform child in transform)
        {
            if (child.name == name)
                return child.gameObject;
        }

        return null;
    }
}
