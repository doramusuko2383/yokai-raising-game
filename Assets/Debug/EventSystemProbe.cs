using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemProbe : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("[PROBE] EventSystem.current is NULL");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[PROBE] MouseDown detected by Input system");
        }
    }
}
