using UnityEngine;
using UnityEngine.EventSystems;

public class InputKillSwitchProbe : MonoBehaviour
{
    private bool? lastMousePresent;
    private int lastTouchCount = -1;
    private bool? lastEventSystemPresent;
    private bool? lastPointerOver;

    private void Update()
    {
        bool mousePresent = Input.mousePresent;
        if (lastMousePresent == null || lastMousePresent.Value != mousePresent)
        {
            Debug.Log($"[INPUT] mousePresent={mousePresent}");
            lastMousePresent = mousePresent;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[INPUT] MouseButtonDown detected");
        }

        int touchCount = Input.touchCount;
        if (lastTouchCount != touchCount)
        {
            Debug.Log($"[INPUT] touchCount={touchCount}");
            lastTouchCount = touchCount;
        }

        EventSystem currentEventSystem = EventSystem.current;
        bool hasEventSystem = currentEventSystem != null;
        if (lastEventSystemPresent == null || lastEventSystemPresent.Value != hasEventSystem)
        {
            if (hasEventSystem)
            {
                Debug.Log("[INPUT] EventSystem.current is present");
            }
            else
            {
                Debug.Log("[INPUT] EventSystem.current is NULL !!!");
            }

            lastEventSystemPresent = hasEventSystem;
        }

        if (hasEventSystem)
        {
            bool isPointerOver = currentEventSystem.IsPointerOverGameObject();
            if (lastPointerOver == null || lastPointerOver.Value != isPointerOver)
            {
                Debug.Log($"[INPUT] IsPointerOverGameObject={isPointerOver}");
                lastPointerOver = isPointerOver;
            }
        }
        else
        {
            lastPointerOver = null;
        }
    }
}
