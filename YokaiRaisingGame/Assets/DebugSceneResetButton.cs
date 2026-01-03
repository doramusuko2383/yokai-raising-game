using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugSceneResetButton : MonoBehaviour
{
    [SerializeField] bool showButton = true;
    [SerializeField] string buttonLabel = "Reset Scene";
    [SerializeField] Rect buttonRect = new Rect(10, 10, 160, 40);

    void OnGUI()
    {
        if (!showButton)
            return;

        if (GUI.Button(buttonRect, buttonLabel))
            ResetScene();
    }

    public void ResetScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}
