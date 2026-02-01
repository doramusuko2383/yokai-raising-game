using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIPentagramBaseCircle : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 20f;

    CanvasGroup _canvasGroup;
    Image _image;
    RectTransform _rectTransform;
    Vector2 _baseSize;
    Coroutine _fadeCo;
    Coroutine _flashCo;
    bool _isRotating;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null)
        {
            _baseSize = _rectTransform.sizeDelta;
        }

        if (_image != null)
        {
            _image.raycastTarget = false;
        }
    }

    void Update()
    {
        if (!_isRotating) return;
        transform.Rotate(0f, 0f, rotateSpeed * Time.unscaledDeltaTime);
    }

    public void SetScale(float scale)
    {
        if (_rectTransform == null) return;
        _rectTransform.sizeDelta = _baseSize * scale;
    }

    public void Show()
    {
        if (_image != null) _image.enabled = true;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        if (_flashCo != null) StopCoroutine(_flashCo);
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_image != null) _image.enabled = false;
    }

    public void FadeIn(float duration)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(0f, 1f, duration));
    }

    public void FadeOut(float duration)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(1f, 0f, duration));
    }

    IEnumerator CoFade(float from, float to, float duration)
    {
        if (_image != null) _image.enabled = true;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = (duration <= 0f) ? 1f : (t / duration);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(from, to, k);
            }
            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = to;
        }

        if (to <= 0f && _image != null)
        {
            _image.enabled = false;
        }

        _fadeCo = null;
    }

    public void PlayCompleteFlash(float duration, float targetAlpha)
    {
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(CoFlash(duration, targetAlpha));
    }

    IEnumerator CoFlash(float duration, float targetAlpha)
    {
        if (_image == null)
        {
            _flashCo = null;
            yield break;
        }

        Color baseColor = _image.color;
        Color flashColor = baseColor;
        flashColor.a = targetAlpha;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = (duration <= 0f) ? 1f : (t / duration);
            _image.color = Color.Lerp(flashColor, baseColor, k);
            yield return null;
        }

        _image.color = baseColor;
        _flashCo = null;
    }

    public void StartRotate()
    {
        _isRotating = true;
    }

    public void StopRotate()
    {
        _isRotating = false;
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
}
