using UnityEngine;

public class UIPentagramRotate : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 30f; // 度/秒（20〜40がおすすめ）
    public bool rotateClockwise = true;

    bool isActive;

    void Update()
    {
        if (!isActive) return;

        float direction = rotateClockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, rotateSpeed * direction * Time.unscaledDeltaTime);
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
