using UnityEngine;

public class PurifyButtonHandler : MonoBehaviour
{
    public void OnClickPurify()
    {
        FindObjectOfType<MagicCircleActivator>()?.RequestNormalPurify();
    }

    public void OnClickEmergencyPurify()
    {
        FindObjectOfType<MagicCircleActivator>()?.RequestEmergencyPurify();
    }
}
