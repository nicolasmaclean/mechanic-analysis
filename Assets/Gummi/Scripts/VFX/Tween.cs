using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gummi.VFX
{
    public static class Tween
    {
        public static class Scale
        {
            /// <summary>
            /// Performs a linear animation of scale over <paramref name="duration"/>, then calls <paramref name="OnComplete"/>
            /// </summary>
            /// <param name="target"></param>
            /// <param name="from"></param>
            /// <param name="to"></param>
            /// <param name="duration"></param>
            /// <param name="OnComplete"></param>
            /// <returns></returns>
            public static IEnumerator Linear(Transform target, Vector3 from, Vector3 to, float duration, System.Action OnComplete = null)
            {
                // initial scale
                target.localScale = from;

                // animate scale
                float elapsedTime = 0;
                while (elapsedTime < duration)
                {
                    target.localScale = Vector3.Lerp(from, to, elapsedTime / duration);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // final scale
                target.localScale = to;
                if (OnComplete != null) { OnComplete(); }
                yield break;
            }
        }
    }
}