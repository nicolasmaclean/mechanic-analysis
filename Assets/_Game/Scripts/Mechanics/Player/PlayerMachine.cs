using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gummi.Player;

namespace Game.Player
{
    [RequireComponent(typeof(FPSController), typeof(VirtualMouse))]
    public class PlayerMachine : MonoBehaviour
    {
        #region public variables
        public PlayerState State { get; private set; } = PlayerState.FPS;
        public bool InTransition { get; private set; } = false;
        #endregion

        #region private variables
        [Header("Controls")]
        [SerializeField]
        KeyCode _primaryActionKeyCode = KeyCode.Mouse0;

        [SerializeField]
        KeyCode _secondaryActionKeyCode = KeyCode.Mouse1;

        [Header("Settings")]
        [SerializeField]
        float _interactionRange = 1.5f;

        FPSController _fpsController;
        VirtualMouse _mouse;

        Dictionary<PlayerState, List<Transition>> _transitions = new Dictionary<PlayerState, List<Transition>>();
        RaycastHit _hitinfo;
        #endregion

        #region Monobehaviour
        void Awake()
        {
            _fpsController = GetComponent<FPSController>();
            _mouse = GetComponent<VirtualMouse>();

            AddTransitions();
        }

        void AddTransitions()
        {
            // FPS -> Inspecting
            _transitions.Add(
                PlayerState.FPS,
                new List<Transition> { new Transition(
                    PlayerState.FPS,
                    PlayerState.Inspecting,
                    CFPSToInspecting,
                    TFPSToInspecting
                )}
            );

            // FPS <- Inspecting
            _transitions.Add(
                PlayerState.Inspecting,
                new List<Transition> { new Transition(
                    PlayerState.Inspecting,
                    PlayerState.FPS,
                    CInspectingToFPS,
                    TInspectingToFPS
                )}
            );

            // Inspecting -> Drawing
            _transitions[PlayerState.Inspecting].Add(
                new Transition(
                    PlayerState.Inspecting,
                    PlayerState.Drawing,
                    CInspectingToDrawing,
                    TInspectingToDrawing
                )
            );

            // Inspecting <- Drawing
            _transitions.Add(
                PlayerState.Drawing,
                new List<Transition> { new Transition(
                    PlayerState.Drawing,
                    PlayerState.Inspecting,
                    CDrawingToInspecting,
                    TDrawingToInspecting
                )}
            );
        }

        void Update()
        {
            if (InTransition) { return; }

            foreach (Transition trans in _transitions[State])
            {
                if (trans.condition(true))
                {
                    InTransition = true;
                    StartCoroutine(trans.coroutine());
                    State = trans.to;
                    break;
                }
            }
        }
        #endregion

        #region Transitions
        #region FPS -> Inspecting
        /// <summary>
        /// Return true if the player can/is trying to interact with a puzzle.
        /// </summary>
        /// <param name="canModifyState"></param>
        /// <returns></returns>
        bool CFPSToInspecting(bool canModifyState)
        {
            // abort if the player has not given appropriate input
            if (!Input.GetKeyDown(_primaryActionKeyCode)) { return false; }

            // look forwards for InteractablePuzzle
            LayerMask allExceptInteractables = ~0 - LayerMask.GetMask("Interactable");
            Ray forwardRay = _fpsController.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1));
            bool lookingAtSomething = Physics.Raycast(forwardRay, out _hitinfo, _interactionRange, allExceptInteractables);
            if (lookingAtSomething)
            {
                InteractablePuzzle puzzle = _hitinfo.collider.gameObject.GetComponent<InteractablePuzzle>();
                bool lookingAtPuzzle = puzzle != null;
                if (lookingAtPuzzle)
                {
                    if (canModifyState)
                    {
                        puzzle.Interact();
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Begin puzzle interaction
        /// </summary>
        IEnumerator TFPSToInspecting()
        {
            DisableFPS();
            _mouse.Activate();

            InTransition = false;
            yield break;
        }
        #endregion

        #region FPS <- Inspecting
        /// <summary>
        /// Returns true if the player is attempting to leave the puzzle.
        /// </summary>
        /// <param name="canModifyState"></param>
        /// <returns></returns>
        bool CInspectingToFPS(bool canModifyState)
        {
            return Input.GetKeyDown(_secondaryActionKeyCode);
        }

        IEnumerator TInspectingToFPS()
        {
            EnableFPS();
            _mouse.Deactivate();

            InTransition = false;
            yield break;
        }
        #endregion

        #region Inspecting -> Drawing
        /// <summary>
        /// Returns true if the player is attempting to start drawing.
        /// </summary>
        /// <param name="canModifyState"></param>
        /// <returns></returns>
        bool CInspectingToDrawing(bool canModifyState)
        {
            return Input.GetKeyDown(_primaryActionKeyCode);
        }

        IEnumerator TInspectingToDrawing()
        {
            InTransition = false;
            yield break;
        }
        #endregion

        #region Inspecting <- Drawing
        bool CDrawingToInspecting(bool canModifyState)
        {
            if (Input.GetKeyDown(_primaryActionKeyCode))
            {
                // check if puzzle is done
                //      complete puzzle
                //      mark this transition
                return false;
            }
            if (Input.GetKeyDown(_secondaryActionKeyCode))
            {
                return true;
            }

            return false;
        }

        IEnumerator TDrawingToInspecting()
        {
            InTransition = false;
            yield break;
        }
        #endregion
        #endregion

        /// <summary>
        /// Enables the <see cref="_fpsController"/> component
        /// </summary>
        void EnableFPS()
        {
            _fpsController.enabled = true;
        }

        /// <summary>
        /// Disables the <see cref="_fpsController"/> component
        /// </summary>
        void DisableFPS()
        {
            _fpsController.enabled = false;
        }

        class Transition
        {
            public PlayerState from { get; private set; }
            public PlayerState to { get; private set; }

            public System.Func<bool, bool> condition { get; private set; }
            public System.Func<IEnumerator> coroutine { get; private set; }

            public Transition(PlayerState from, PlayerState to, System.Func<bool, bool> condition, System.Func<IEnumerator> function)
            {
                this.from = from;
                this.to = to;
                this.condition = condition;
                this.coroutine = function;
            }
        }
    }

    public enum PlayerState
    {
        FPS = 0, Inspecting = 1, Drawing = 2
    }
}