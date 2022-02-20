using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gummi.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gummi.Player
{
    public class VirtualMouse : MonoBehaviour
    {
        #region public variables
        // The mouse position in viewport space
        public Vector2 Position
        {
            get => _position;
            set => SetPosition(value);
        }

        public MouseState State { get; private set; }
        #endregion

        #region private variables
        [Header("Feedback")]
        [SerializeField]
        VirtualCursor _virtualCursor = null;

        [SerializeField]
        [Header("Controls")]
        [Tooltip("Mouse sensitivity. Default is 15.")]
        int _speed = 15;

        [SerializeField]
        [Tooltip("Input Manager virtual axis for horizontal movement.")]
        string _horizontalAxis = "Mouse X";

        [SerializeField]
        [Tooltip("Input Manager virtual axis for vertical movement.")]
        string _verticalAxis = "Mouse Y";

        [Header("Viewport")]
        [SerializeField]
        [Tooltip("Use a custom viewport bounding box for the virtual cursor. Will ignore _padCursor.")]
        bool _useCustomBounds = false;

        [SerializeField]
        Vector2 _customBoundsX = new Vector2(0, 1);

        [SerializeField]
        Vector2 _customBoundsY = new Vector2(0, 1);

        [SerializeField]
        [Tooltip("If true, add padding to the edges of the screen so cursor can't go offscreen. Uses _virtualCursor's RectTransform.deltaSize.")]
        bool _padCursor = false;

        Vector2 _position = Vector2.zero;
        #endregion

        #region events
        [Header("Events")]
        [Tooltip("Called each time the mouse moves. The viewport position of the mouse is passed as parameter 1.")]
        public Vec2UnityEvent OnMouseMove;
        #endregion

        #region Monobehaviour
        void Start()
        {
            _virtualCursor.gameObject.SetActive(false);
        }

        void Update()
        {
            if (State == MouseState.Inactive) { return; }

            UpdatePosition();
            UpdateCursor();
        }
        #endregion

        /// <summary>
        /// Activate <see cref="this"/> and sets <see cref="Position"/> to the center of the screen
        /// </summary>
        public void Activate()
        {
            Activate(Vector2.one * .5f);
        }

        /// <summary>
        /// Activate <see cref="this"/> and sets <see cref="Position"/> to the <paramref name="initialPosition"/>
        /// </summary>
        /// <param name="initialPosition"> Viewport space coordinate (normalized) to set cursor to </param>
        public void Activate(Vector2 initialPosition)
        {
            // hide/lock cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // perform cursor in-animation
            if (_virtualCursor == null)
            {
                Debug.LogWarning("VirtualMouse._virtualCursor is null. This component will be disabled. Please provide a VirtualCursor and renable.");
                this.enabled = false;
            }
            else
            {
                State = MouseState.In;
                _virtualCursor.Activate(() =>
                {
                    State = MouseState.Active;
                });
            }

            // ensure virutal cursor's anchor is in the bottom left corner
            RectTransform trans = _virtualCursor.GetComponent<RectTransform>();
            trans.anchorMax = Vector2.zero;
            trans.anchorMin = Vector2.zero;

            SetPosition(initialPosition);
        }

        /// <summary>
        /// Disable <see cref="this"/> with visual/audio feedback.
        /// </summary>
        public void Deactivate()
        {
            // unlock/unhide cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // perform cursor out-animation
            if (_virtualCursor == null)
            {
                State = MouseState.Inactive;
            }
            else
            {
                State = MouseState.Out;
                _virtualCursor.Deactivate(() =>
                {
                    State = MouseState.Inactive;
                    this.enabled = false;
                });
            }
        }

        /// <summary>
        /// Update the <see cref="Position"/> and update the visuals.
        /// </summary>
        /// <param name="newPosition"></param>
        void SetPosition(Vector2 newPosition)
        {
            _position = newPosition;
            UpdateCursor();
        }

        /// <summary>
        /// Update the mouse position according to user input and dispatch <see cref="OnMouseMove"/> event.
        /// Will automatically clamp to appropriate bounds.
        /// </summary>
        void UpdatePosition()
        {
            // add input delta
            Vector2 delta = new Vector2(Input.GetAxis(_horizontalAxis), Input.GetAxis(_verticalAxis));
            delta *= _speed * Time.deltaTime * 500;
            delta /= new Vector2(Screen.width, Screen.height);
            delta += _position;

            // find clamping bounds. Defaults to screen size.
            Vector2 xBounds = new Vector2(0, 1);
            Vector2 yBounds = new Vector2(0, 1);

            if (_useCustomBounds)
            {
                xBounds = _customBoundsX;
                yBounds = _customBoundsY;
            }
            else if (_padCursor)
            {
                Vector2 cursorSize = _virtualCursor.GetComponent<RectTransform>().sizeDelta;
                cursorSize.x /= 2 * Screen.width;
                cursorSize.y /= 2 * Screen.height;

                xBounds.x += cursorSize.x;
                xBounds.y -= cursorSize.x;

                yBounds.x += cursorSize.y;
                yBounds.y -= cursorSize.y;
            }

            // clamp mouse to appropriate bounds
            delta.x = Mathf.Clamp(delta.x, xBounds.x, xBounds.y);
            delta.y = Mathf.Clamp(delta.y, yBounds.x, yBounds.y);

            // dispatch OnMouseMove event
            if (_position != delta)
            {
                OnMouseMove?.Invoke(delta);
            }

            Position = delta;
        }

        void UpdateCursor()
        {
            RectTransform rect = _virtualCursor.GetComponent<RectTransform>();
            rect.position = new Vector3(Position.x * Screen.width, Position.y * Screen.height, 0);
        }

#if UNITY_EDITOR
        // is within VirtualMouse to use nameof() on private fields instead of hardcoded strings
        [CustomEditor(typeof(VirtualMouse))]
        public class VirtualMouseEditor : Editor
        {
            SerializedProperty _virtualCursorProperty;

            SerializedProperty _speedProperty;
            SerializedProperty _horizontalAxisProperty;
            SerializedProperty _verticalAxisProperty;

            SerializedProperty _padCursorProperty;
            SerializedProperty _useCustomBoundsProperty;
            SerializedProperty _customBoundsXProperty;
            SerializedProperty _customBoundsYProperty;

            SerializedProperty _OnMouseMoveProperty;

            public void OnEnable()
            {
                _virtualCursorProperty = serializedObject.FindProperty(nameof(VirtualMouse._virtualCursor));

                _speedProperty = serializedObject.FindProperty(nameof(VirtualMouse._speed));
                _horizontalAxisProperty = serializedObject.FindProperty(nameof(VirtualMouse._horizontalAxis));
                _verticalAxisProperty = serializedObject.FindProperty(nameof(VirtualMouse._verticalAxis));

                _padCursorProperty = serializedObject.FindProperty(nameof(VirtualMouse._padCursor));
                _useCustomBoundsProperty = serializedObject.FindProperty(nameof(VirtualMouse._useCustomBounds));
                _customBoundsXProperty = serializedObject.FindProperty(nameof(VirtualMouse._customBoundsX));
                _customBoundsYProperty = serializedObject.FindProperty(nameof(VirtualMouse._customBoundsY));

                _OnMouseMoveProperty = serializedObject.FindProperty(nameof(VirtualMouse.OnMouseMove));
            }

            public override void OnInspectorGUI()
            {
                // Feedback
                EditorGUILayout.PropertyField(_virtualCursorProperty);

                // Controls
                EditorGUILayout.PropertyField(_speedProperty);
                EditorGUILayout.PropertyField(_horizontalAxisProperty);
                EditorGUILayout.PropertyField(_verticalAxisProperty);

                // Viewport
                EditorGUILayout.PropertyField(_useCustomBoundsProperty);
                if (_useCustomBoundsProperty.boolValue)
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(_customBoundsXProperty);
                    EditorGUILayout.PropertyField(_customBoundsYProperty);
                    EditorGUI.indentLevel -= 1;
                }
                else
                {
                    EditorGUILayout.PropertyField(_padCursorProperty);
                }

                // Events
                EditorGUILayout.PropertyField(_OnMouseMoveProperty);

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }

    public enum MouseState
    {
        Inactive = 0, In = 1, Active = 2, Out = 3
    }
}