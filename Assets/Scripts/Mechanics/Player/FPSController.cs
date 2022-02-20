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

        [Tooltip("Camera to be manipulated. Only rotation will be adjusted.")]
        public Camera Camera;
        #endregion

        #region private variables
        [Header("Camera")]
        [SerializeField]
        bool _useMainCamera = false;

        [SerializeField]
        Vector2 _cameraSensitivity = new Vector2(1000, 1000);

        [SerializeField]
        [Tooltip("Bounds on the camera's vertical rotation.")]
        Vector2 _cameraXRotationBounds = new Vector2(-60, 60);

        [SerializeField]
        bool _invertMouse = false;

        [Header("Movement")]
        [SerializeField]
        float _playerSpeed = 2.0f;

        [SerializeField]
        float _jumpHeight = 1.0f;

        [SerializeField]
        bool _canRun = true;

        [SerializeField]
        KeyCode _runKeyCode = KeyCode.LeftShift;

        [SerializeField]
        float _runMultiplier = 1.3f;

        [SerializeField]
        [Tooltip("Input Manager virtual axis for horizontal movement.")]
        string _horizontalAxis = "Horizontal";

        [SerializeField]
        [Tooltip("Input Manager virtual axis for vertical movement.")]
        string _verticalAxis = "Vertical";

        [Header("Physics")]
        [SerializeField]
        float _gravity = -9.81f;

        [SerializeField]
        float _groundedDistance = 0.1f;

        [SerializeField]
        [Tooltip("Layers to be considered by isGrounded check. Default is everything")]
        LayerMask _groundedMask = ~0;


        CharacterController _controller;
        #endregion

        #region Monobehaviour
        private void Awake()
        {
            _controller = gameObject.GetComponent<CharacterController>();

            if (_useMainCamera)
            {
                Camera = Camera.main;
            }
            else if (Camera == null)
            {
                Debug.LogWarning("No camera was provided. Camera manipulation will be disabled.");
            }
        }

        void Update()
        {
            isGrounded = GroundedCheck();
            MoveByInput();
            Jump();
            ApplyGravity();
            MoveCamera();
        }

        void OnEnable()
        {
            if (Camera != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            if (Camera != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        #endregion

        /// <summary>
        /// Checks if this.transform is on the ground.
        /// </summary>
        /// <returns> true if within <see cref=""/> units of the ground </returns>
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
            Vector3 move = transform.right * x + transform.forward * z;

            // apply multipliers
            move *= _playerSpeed;
            if (_canRun && Input.GetKey(_runKeyCode))
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

        /// <summary>
        /// Rotates camera according to mouse movement delta
        /// </summary>
        void MoveCamera()
        {
            if (Camera != null)
            {
                Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
                delta *= Time.deltaTime * _cameraSensitivity;

                if (_invertMouse)
                {
                    delta.y = -delta.y;
                }

                // apply horizontal rotation
                Vector3 rootRotation = transform.rotation.eulerAngles;
                rootRotation.y += delta.x;
                transform.rotation = Quaternion.Euler(rootRotation);

                // apply vertical rotation
                Vector3 camRotation = Camera.transform.localRotation.eulerAngles;
                if (camRotation.x > 180)
                {
                    camRotation.x -= 360;
                }
                camRotation.x += delta.y;

                camRotation.x = Mathf.Clamp(camRotation.x, _cameraXRotationBounds.x, _cameraXRotationBounds.y);
                Camera.transform.localRotation = Quaternion.Euler(camRotation);
            }
        }


#if UNITY_EDITOR
        // is within FPSController to use nameof() on private fields instead of hardcoded strings
        [CustomEditor(typeof(FPSController))]
        public class FPSControllerEditor : Editor
        {
            SerializedProperty _useMainCameraProperty;
            SerializedProperty _cameraProperty;
            SerializedProperty _cameraSensitivityProperty;
            SerializedProperty _cameraXRotationBoundsProperty;

            SerializedProperty _playerSpeedProperty;
            SerializedProperty _jumpHeightProperty;
            SerializedProperty _canRunProperty;
            SerializedProperty _runKeyCodeProperty;
            SerializedProperty _runMultiplierProperty;
            SerializedProperty _horizontalAxisProperty;
            SerializedProperty _verticalAxisProperty;

            SerializedProperty _gravityProperty;
            SerializedProperty _groundedDistanceProperty;
            SerializedProperty _groundedMaskProperty;

            public void OnEnable()
            {
                _useMainCameraProperty = serializedObject.FindProperty(nameof(_useMainCamera));
                _cameraProperty = serializedObject.FindProperty(nameof(Camera));
                _cameraSensitivityProperty = serializedObject.FindProperty(nameof(_cameraSensitivity));
                _cameraXRotationBoundsProperty = serializedObject.FindProperty(nameof(_cameraXRotationBounds));

                _playerSpeedProperty = serializedObject.FindProperty(nameof(_playerSpeed));
                _jumpHeightProperty = serializedObject.FindProperty(nameof(_jumpHeight));
                _canRunProperty = serializedObject.FindProperty(nameof(_canRun));
                _runKeyCodeProperty = serializedObject.FindProperty(nameof(_runKeyCode));
                _runMultiplierProperty = serializedObject.FindProperty(nameof(_runMultiplier));
                _horizontalAxisProperty = serializedObject.FindProperty(nameof(_horizontalAxis));
                _verticalAxisProperty = serializedObject.FindProperty(nameof(_verticalAxis));

                _gravityProperty = serializedObject.FindProperty(nameof(_gravity));
                _groundedDistanceProperty = serializedObject.FindProperty(nameof(_groundedDistance));
                _groundedMaskProperty = serializedObject.FindProperty(nameof(_groundedMask));
            }

            public override void OnInspectorGUI()
            {
                // camera
                EditorGUILayout.PropertyField(_useMainCameraProperty);
                if (!_useMainCameraProperty.boolValue)
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(_cameraProperty);
                    EditorGUI.indentLevel -= 1;
                }
                if (_useMainCameraProperty.boolValue || _cameraProperty.objectReferenceValue != null)
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(_cameraSensitivityProperty);
                    EditorGUILayout.PropertyField(_cameraXRotationBoundsProperty);
                    EditorGUI.indentLevel -= 1;
                }

                // movement
                EditorGUILayout.PropertyField(_playerSpeedProperty);
                EditorGUILayout.PropertyField(_jumpHeightProperty);
                EditorGUILayout.PropertyField(_canRunProperty);
                if (_canRunProperty.boolValue)
                {
                    EditorGUI.indentLevel += 1;

                    EditorGUILayout.PropertyField(_runKeyCodeProperty);
                    if (_runKeyCodeProperty.enumValueIndex != 0)
                    {
                        EditorGUILayout.PropertyField(_runMultiplierProperty);
                    }

                    EditorGUI.indentLevel -= 1;
                }
                EditorGUILayout.PropertyField(_horizontalAxisProperty);
                EditorGUILayout.PropertyField(_verticalAxisProperty);

                // physics
                EditorGUILayout.PropertyField(_gravityProperty);
                EditorGUILayout.PropertyField(_groundedDistanceProperty);
                EditorGUILayout.PropertyField(_groundedMaskProperty);

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}