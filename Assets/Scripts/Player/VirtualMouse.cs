using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VirtualMouse : MonoBehaviour
{
    #region Public Variables
    [Tooltip("The current virtual mouse position in ScreenSpace.")]
    public Vector2 Position;

    [Tooltip("Mouse movement speed. Default is 2500.")]
    public int Speed = 2500;

    [Tooltip("When activated, real mouse is locked and virtual mouse is shown/updated.")]
    public bool Activated = true;
    #endregion

    #region Exposed Variables
    [Tooltip("The duration of the animation on activation")]
    [SerializeField]
    float _activationDuration = 1f;

    [Tooltip("The duration of the animation on deactivation")]
    [SerializeField]
    float _deactivationDuration = 1f;

    [Tooltip("The inital size of _cursorCenter relative to _cursor when activated.f")]
    [SerializeField]
    float _animationScale = 7;

    [Tooltip("The virtual mouse's cursor. Should be a UI element.")]
    [SerializeField]
    Transform _cursor = null;

    [Tooltip("The cursor's center sprite. Its color and scale are animated when the virtual mouse is activated/deactivated.")]
    [SerializeField]
    Transform _cursorCenter = null;
    #endregion

    void Awake()
    {
        Position = Input.mousePosition;

        if (_cursor == null || _cursorCenter == null)
        {
            Debug.LogError("ERROR: missing cursor.");
            return;
        }
    }

    /// <summary>
    ///     Sets Position to given value and Activates VirtualMouse
    /// </summary>
    /// <param name="position"></param>
    public void Activate(Vector2 position, Vector3 scale)
    {
        Activated = true;
        Cursor.lockState = CursorLockMode.Locked;
        Position = position;

        _cursor.gameObject.SetActive(true);
        _cursorCenter.gameObject.SetActive(true);

        _cursor.localScale = scale;
        StartCoroutine(CursorActivated(_cursorCenter.gameObject, scale, _activationDuration));
    }

    /// <summary>
    ///     Disables VirtualMouse position tracking and unlocks cursor.
    /// </summary>
    public void Deactivate()
    {
        Activated = false;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(CursorDeactivated(_cursor.gameObject, _cursorCenter.gameObject, _deactivationDuration));
    }

    void Update()
    {
        if (Activated)
        {
            UpdatePosition();
            UpdateCursor();
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
        _cursorCenter.position = Position;
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

    /// <summary>
    ///     Scales the given gameobject down to final size from a
    ///     _animationScale times finalScale. Also fades the alpha in.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="finalScale"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    IEnumerator CursorActivated(GameObject go, Vector3 finalScale, float duration)
    {
        Image img = go.GetComponent<Image>();
        Color initColor = img.color;
        float timestamp = Time.time;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - timestamp;
            float t = elapsedTime / duration;

            go.transform.localScale = finalScale * Mathf.Lerp(_animationScale, 1, t);
            img.color = new Color(initColor.r, initColor.g, initColor.b, Mathf.Lerp(0, .8f, t));

            yield return null;
        }

        go.transform.localScale = finalScale;
        img.color = new Color(initColor.r, initColor.g, initColor.b, .8f);
    }

    /// <summary>
    ///     Scales the given gameobject up to _finalSize times _animationScale
    ///     Also fades the alpha out.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="finalScale"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    IEnumerator CursorDeactivated(GameObject go, GameObject scaledGo, float duration)
    {
        Image img = go.GetComponent<Image>();
        Image scaledImg = scaledGo.GetComponent<Image>();

        Vector3 initScale = go.transform.localScale;
        Color initColor = img.color;
        float timestamp = Time.time;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - timestamp;
            float t = elapsedTime / duration;

            scaledGo.transform.localScale = initScale * Mathf.Lerp(1, _animationScale, t);
            initColor.a = Mathf.Lerp(.8f, 0, t);
            img.color = initColor;
            scaledImg.color = initColor;

            yield return null;
        }

        _cursor.gameObject.SetActive(false);
        _cursorCenter.gameObject.SetActive(false);
    }
}