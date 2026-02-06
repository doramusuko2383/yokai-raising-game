using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[PROBE] PointerDown received on " + gameObject.name);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[PROBE] PointerUp received on " + gameObject.name);
    }
}
