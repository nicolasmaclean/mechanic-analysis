using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PuzzleFrame : MonoBehaviour
{
    #region Exposed Variables
    [Tooltip("The animation length.")]
    [SerializeField]
    float _animationDuration = 1.5f;

    [Tooltip("The alpha value when fully \'opague\'.")]
    [SerializeField, Range(0, 1)]
    float _fullAlpha = .7f;
    #endregion

    #region Private Variables
    Image _img;
    #endregion

    void Awake()
    {
        _img = GetComponent<Image>();
        _img.enabled = false;
    }

    public void Activate()
    {
        _img.enabled = true;
        StartCoroutine(FadeIn(_fullAlpha, _animationDuration));
    }

    public void Deactivate()
    {
        StartCoroutine(FadeOut(_animationDuration * 2 / 3));
    }

    IEnumerator FadeIn(float finalAlpha, float duration)
    {
        float elapsedTime = 0;
        Color nColor = _img.color;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            nColor.a = Mathf.Lerp(0, finalAlpha, t);
            _img.color = nColor;

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        nColor.a = finalAlpha;
        _img.color = nColor;
    }

    IEnumerator FadeOut(float duration)
    {
        float elapsedTime = 0;
        Color nColor = _img.color;
        float initialAlpha = nColor.a;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            nColor.a = Mathf.Lerp(initialAlpha, 0, t);
            _img.color = nColor;

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        nColor.a = 0;
        _img.color = nColor;
    }
}