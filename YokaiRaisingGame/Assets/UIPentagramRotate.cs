using UnityEngine;

public class UIPentagramRotate : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 30f; // 度/秒（20〜40がおすすめ）

    bool isActive;

    void Update()
    {
        if (!isActive) return;

        transform.Rotate(0f, 0f, rotateSpeed * Time.unscaledDeltaTime);
    }

    public void StartRotate()
    {
        isActive = true;
    }

    public void StopRotate()
    {
        isActive = false;
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
}
