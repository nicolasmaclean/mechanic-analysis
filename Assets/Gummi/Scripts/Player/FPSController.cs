using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gummi.Player
{
    [SelectionBase]
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        #region public variables
        [HideInInspector]
        public Vector3 Velocity;

        /// <summary>
        /// Grounded check is from transform.position, so the bottom
        /// of the player collider should be at transform.position. 
        /// </summary>
        public bool isGrounded { get; private set; }

        public Transform Camera
        {
            get
            {
                return _camera;
            }
        }
        #endregion

        #region private variables
        [Header("Movement")]
        [SerializeField]
        float _playerSpeed = 2.0f;

        [SerializeField]
        float _jumpHeight = 1.0f;

        [SerializeField]
        float _runMultiplier = 1.3f;
        
        [Header("Controls")]
        [SerializeField]
        KeyCode _runKeyCode = KeyCode.LeftShift;

        [SerializeField]
        [Tooltip("Input Manager virtual axis for horizontal movement.")]
        string _horizontalAxis = "Horizontal";

        [SerializeField]
        [Tooltip("Input Manager virtual axis for vertical movement.")]
        string _verticalAxis = "Vertical";

        [SerializeField]
        Transform _camera;
        
        [Header("Physics")]
        [SerializeField]
        float _gravity = -9.81f;

        [SerializeField]
        float _groundedDistance = 0.1f;

        [SerializeField]
        [Tooltip("Layers to be considered by isGrounded check. Default is everything")]
        LayerMask _groundedMask = ~0;

        CharacterController _controller;
        Transform _mainCamera;
        #endregion

        #region Monobehaviour
        void Awake()
        {
            _controller = gameObject.GetComponent<CharacterController>();
            _mainCamera = UnityEngine.Camera.main.transform;
        }

        void Update()
        {
            isGrounded = GroundedCheck();
            MoveByInput();
            Jump();
            ApplyGravity();
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        #endregion

        /// <summary>
        /// Checks if this.transform is on the ground.
        /// </summary>
        /// <returns> true if within <see cref="_groundedDistance"/> units of the ground </returns>
        bool GroundedCheck()
        {
            return Physics.CheckSphere(transform.position, _groundedDistance, _groundedMask);
        }

        /// <summary>
        /// Polls Input Systems "Horizontal" and "Vertical" axes 
        /// and moves the character controller accordingly.
        /// </summary>
        void MoveByInput()
        {
            // polls input
            float x = Input.GetAxis(_horizontalAxis);
            float z = Input.GetAxis(_verticalAxis);

            // relate input vector to player's direction
            float angle = 2f * Mathf.PI * _mainCamera.rotation.eulerAngles.y / 360f;
            Vector3 move = new Vector3(
                z * Mathf.Sin(angle) + x * Mathf.Cos(angle),
                0f,
                z * Mathf.Cos(angle) - x * Mathf.Sin(angle)
            );

            // apply multipliers
            move *= _playerSpeed;
            if (Input.GetKey(_runKeyCode))
            {
                move *= _runMultiplier;
            }

            _controller.Move(move * Time.deltaTime);
        }

        /// <summary>
        /// Polls Input Systems "Jump" button and will jump if appropriate.
        /// This modifies velocity, so ApplyGravity must be called
        /// to apply velocity to the CharacterController.
        /// </summary>
        void Jump()
        {
            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                Velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
        }

        /// <summary>
        /// Applies gravity to the player.
        /// </summary>
        void ApplyGravity()
        {
            if (isGrounded && Velocity.y < 0)
            {
                Velocity.y = -2f;
            }

            Velocity.y += _gravity * Time.deltaTime;
            _controller.Move(Velocity * Time.deltaTime);
        }
    }
}