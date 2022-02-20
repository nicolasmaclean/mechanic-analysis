using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gummi.VFX
{
    public static class Fade
    {
        /// <summary>
        /// Performs a linear animation of color over <paramref name="duration"/>. Performs <paramref name="OnComplete"/> when done.
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="OnComplete"></param>
        /// <returns></returns>
        public static IEnumerator Linear(MaskableGraphic graphic, Color from, Color to, float duration, System.Action OnComplete = null)
        {
            // initial color
            graphic.color = from;

            // animate color
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                graphic.color = Color.Lerp(from, to, elapsedTime / duration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // final color
            graphic.color = to;
            if (OnComplete != null) { OnComplete(); }
            yield break;
        }
    }
}