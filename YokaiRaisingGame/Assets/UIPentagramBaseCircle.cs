using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine;
using UnityEngine.UI;

public class UIPentagramBaseCircle : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void SetAlpha(float alpha)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        canvasGroup.alpha = alpha;
    }
}