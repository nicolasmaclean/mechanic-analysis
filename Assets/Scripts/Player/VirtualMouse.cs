using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualMouse : MonoBehaviour
{
    #region Public Variables
    [Tooltip("The current virtual mouse position in ScreenSpace.")]
    public Vector2 Position;

    [Tooltip("Mouse movement speed. Default is 2500.")]
    public int Speed = 2500;

    [Tooltip("When activated, real mouse is locked and virtual mouse is shown/updated.")]
    public bool Activated = true;

    [Tooltip("The virtual mouse's cursor. Should be a UI element.")]
    [SerializeField]
    public Transform _cursor = null;
    #endregion

    #region Exposed Variables
    [Tooltip("Activates virtual mouse position tracking onAwake. If false will deactivate onAwake.")]
    [SerializeField]
    bool _activateOnAwake = false;
    #endregion

    void Awake()
    {
        Position = Input.mousePosition;

        if (_activateOnAwake)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    /// <summary>
    ///     Sets Position to given value and Activates VirtualMouse
    /// </summary>
    /// <param name="position"></param>
    public void Activate(Vector2 position)
    {
        Position = position;
        Activate();
    }

    /// <summary>
    ///     Toggles VirtualMouse position tracking on and locks cursor.
    /// </summary>
    public void Activate()
    {
        Activated = true;
        Cursor.lockState = CursorLockMode.Locked;

        if (_cursor != null)
        {
            _cursor.gameObject.SetActive(true);
        }
    }

    /// <summary>
    ///     Disables VirtualMouse position tracking and unlocks cursor.
    /// </summary>
    public void Deactivate()
    {
        Activated = false;
        Cursor.lockState = CursorLockMode.None;

        if (_cursor != null)
        {
            _cursor.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Activated)
        {
            UpdatePosition();

            if (_cursor != null)
            {
                UpdateCursor();
            }
        }

        PollInput();
    }

    /// <summary>
    ///     Updates the virtual position of the mouse.
    ///     Uses normalized mouse movement relative to the screen size.
    /// </summary>
    public event System.Action<Vector2> OnMouseMove;
    void UpdatePosition()
    {
        Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) / new Vector2(Screen.width, Screen.height);
        delta *= Speed;

        Vector2 prevVirtualPosition = Position;
        Position = new Vector2(Mathf.Clamp(Position.x + delta.x, 0, Screen.width), Mathf.Clamp(Position.y + delta.y, 0, Screen.height));

        if (prevVirtualPosition != Position)
        {
            OnMouseMove(delta);
        }
    }

    /// <summary>
    ///     Updates the cursor
    /// </summary>
    void UpdateCursor()
    {
        _cursor.position = Position;
    }

    /// <summary>
    ///     Polls mouse input events
    /// </summary>
    public event System.Action OnLeftClick;
    public event System.Action OnRightClick;
    void PollInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnLeftClick();
        }
        if (Input.GetMouseButtonDown(1))
        {
            OnRightClick();
        }
    }
}