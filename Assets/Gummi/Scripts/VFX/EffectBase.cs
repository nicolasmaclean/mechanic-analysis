using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gummi.VFX
{
    public abstract class EffectBase : MonoBehaviour
    {
        public abstract void Activate(System.Action OnComplete);
        public abstract void Deactivate(System.Action OnComplete);
    }
}