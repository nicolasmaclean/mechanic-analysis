using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using Gummi;

namespace Game.Puzzle
{
    [RequireComponent(typeof(Collider), typeof(PuzzleRenderer))]
    public class InteractablePuzzle : InteractableBase
    {
        [SerializeField]
        CinemachineVirtualCamera _camera;

        void Start()
        {
            _camera.enabled = false;
        }
        
        public override void Begin()
        {
            _camera.enabled = true;
        }

        public override void End()
        {
            _camera.enabled = false;
        }
    }
}