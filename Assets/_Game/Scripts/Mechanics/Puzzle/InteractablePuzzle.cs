using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gummi;

namespace Game.Puzzle
{
    [RequireComponent(typeof(Collider), typeof(PuzzleRenderer))]
    public class InteractablePuzzle : MonoBehaviour, IInteractable
    {
        #region IInteractable
        public void OnEnter() {    }

        public void OnLeave() {    }

        public void Interact()
        {
            // activate puzzle stuffs but
            // PlayerManager should deal with its own state though
        }
        #endregion
    }
}