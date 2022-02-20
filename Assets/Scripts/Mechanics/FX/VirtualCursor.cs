using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Gummi.VFX;

namespace Gummi.Player.Mouse
{
    /// <summary>
    /// Abstracts cursor vfx from Virtual Mouse logic.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualCursor : EffectBase
    {
        #region private variables
        [Header("Animation Times")]
        [SerializeField]
        float _fadeIn = 1f;

        [SerializeField]
        float _fadeOut = .25f;

        [Header("Color")]
        [SerializeField]
        Color _initialColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField]
        Color _activeColor = new Color(1f, 1f, 1f, .8f);

        [SerializeField]
        Color _finalColor = new Color(1f, 1f, 1f, 0f);

        [Header("Scale")]
        [SerializeField]
        Vector3 _initialScale = Vector3.one * 7;

        [SerializeField]
        Vector3 _activeScale = Vector3.one;

        [SerializeField]
        Vector3 _finalScale = Vector3.one * 7;

        [Header("UI References")]
        [SerializeField]
        MaskableGraphic _animationTarget = null;

        [SerializeField]
        [Tooltip("Will be enabled and disabled upon Activate and Deactive, respectively.")]
        GameObject _extras = null;
        #endregion

        void OnEnable()
        {
            _animationTarget.color = _initialColor;
            _animationTarget.transform.localScale = _initialScale;
        }

        /// <summary>
        /// Performs fade/scale in animation and performs <paramref name="OnComplete"/> callback
        /// </summary>
        /// <param name="OnComplete"></param>
        public override void Activate(System.Action OnComplete)
        {
            gameObject.SetActive(true);
            if (_animationTarget != null)
            {
                StartCoroutine(Tween.Scale.Linear(_animationTarget.transform, _initialScale, _activeScale, _fadeIn));
                StartCoroutine(Fade.Linear(_animationTarget, _initialColor, _activeColor, _fadeIn, OnComplete));
            }

            if (_extras != null)
            {
                _extras.SetActive(true);
            }
        }

        /// <summary>
        /// Performs fade/scale out animation and performs <paramref name="OnComplete"/> callback
        /// </summary>
        /// <param name="OnComplete"></param>
        public override void Deactivate(System.Action OnComplete)
        {
            if (_animationTarget != null)
            {
                StartCoroutine(Tween.Scale.Linear(_animationTarget.transform, _activeScale, _finalScale, _fadeOut));
                StartCoroutine(Fade.Linear(_animationTarget, _activeColor, _finalColor, _fadeOut, OnComplete));
            }

            if (_extras != null)
            {
                _extras.SetActive(false);
            }
        }
    }
}