using UnityEngine;
using UnityEngine.UI;

public class KegareUIController : MonoBehaviour
{
    [SerializeField]
    KegareManager kegareManager;

    [SerializeField]
    Slider kegareSlider;

    void OnEnable()
    {
        if (kegareManager == null)
            kegareManager = FindObjectOfType<KegareManager>();

        if (kegareManager != null)
            kegareManager.KegareChanged += OnKegareChanged;

        RefreshUI();
    }

    void OnDisable()
    {
        if (kegareManager != null)
            kegareManager.KegareChanged -= OnKegareChanged;
    }

    void OnKegareChanged(float current, float max)
    {
        if (kegareSlider != null)
            kegareSlider.value = max > 0f ? current / max : 0f;
    }

    void RefreshUI()
    {
        if (kegareManager == null)
            return;

        OnKegareChanged(kegareManager.kegare, kegareManager.maxKegare);
    }
}
