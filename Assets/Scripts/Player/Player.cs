using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VirtualMouse), typeof(PlayerMovement))]
public class Player : MonoBehaviour
{
    #region Public Variables
    public PlayerState State { get; private set; } = PlayerState.FPS;
    public bool AtEnd { get; private set; } = false;
    public bool Won { get; private set; } = false;
    #endregion

    #region Exposed Variables
    [Header("Configurations")]
    [Tooltip("Multiplier to the intersection bounding-box. Value of 1 will default bounding-box to the line width.")]
    [SerializeField]
    float _intersectionSize = .5f;

    [Tooltip("The radius of the end point used to calculate when the player has reached the end.")]
    [SerializeField]
    float _endSize = 0.05f;

    [Tooltip("Reference to puzzle frame gameObject.")]
    [SerializeField]
    PuzzleFrame _puzzleFrame;

    [Tooltip("Clip to be played when the player starts drawing")]
    [SerializeField]
    AudioClip _startDrawingClip;

    [Tooltip("Clip to be played when the player interacts with a puzzle")]
    [SerializeField]
    AudioClip _interactionClip;

    [Tooltip("Clip to be played when the player wins a puzzle.")]
    [SerializeField]
    AudioClip _winClip;
    #endregion

    #region Private Variables
    public PuzzleRenderer _puzzle = null;
    PlayerPath _playerPath = null;
    Camera _cam = null;
    VirtualMouse _virtualMouse = null;

    RaycastHit _hitinfo;

    bool _atIntersection;
    List<Vector2Int> _neighbors = null;

    Vector2 _screenOrigin;
    Vector2 _spacing;
    float _intersectionWidth;
    float _puzzleLineWidth;
    float _startNodeRadius;

    Vector2 _position;
    Vector2Int _intersection;
    Vector2Int _targetPosition;
    bool _targetIsEnd;
    #endregion

    #region Monobehaviours
    void Awake()
    {
        _cam = Camera.main;
        _virtualMouse = GetComponent<VirtualMouse>();
        SubscribeToEvents();

        State = PlayerState.FPS;
    }

    void Start()
    {
        // default to looking for testing
        InteractWithPuzzle();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }
    }

    void OnDestroy()
    {
        UnsubscribeToEvents();
    }
    #endregion

    #region States
    /// <summary>
    ///     Update loop iteration while drawing. Updates position and _virtualMouse accordingly.
    ///     Updates visuals associated with _playerPath.
    ///     Intended to be used with VirtualMouse.onMouseMove
    /// </summary>
    /// <param name="mouseDelta"> Mouse input delta </param>
    void UpdateDrawing(Vector2 mouseDelta)
    {
        if (State != PlayerState.Drawing) return;

        if (_atIntersection)
        {
            //_position += ScreenToPuzzleSpacing(mouseDelta);
            _position = ScreenToPuzzle(_virtualMouse.Position);
            _position = ClampToIntersection(_position, _intersection);
            _virtualMouse.Position = PuzzleToScreen(_position);

            bool leavingIntersection = !WithinSquareRadius(_intersection, _intersectionWidth, _position);
            if (leavingIntersection)
            {
                LeaveIntersection();
                _virtualMouse.Position = PuzzleToScreen(_position);
            }
        }
        else
        {
            // clamp virtual mouse to current path
            _position = ScreenToPuzzle(_virtualMouse.Position);
            _position = ClampToPath(_position, _intersection, _targetPosition);
            _virtualMouse.Position = PuzzleToScreen(_position);

            // prevent moving into a gap or self-intersection
            // update _virtualMouse

            // checks if entering intersection
            bool enteringNextIntersection = WithinSquareRadius(_targetPosition, _intersectionWidth, _position);
            bool reenteringIntersection = WithinSquareRadius(_intersection, _intersectionWidth, _position);
            if (enteringNextIntersection)
            {
                EnterIntersection(_targetPosition);
            }
            else if (reenteringIntersection)
            {
                EnterIntersection(_intersection);
            }
        }

        _playerPath.UpdatePath(_position);
    }

    /// <summary>
    ///     Enters the given coordinate's intersection.
    ///     Updates state related info.
    /// </summary>
    /// <param name="coord"> Puzzle space coordinate of the intersection </param>
    void EnterIntersection(Vector2Int coord)
    {
        _intersection = coord;
        _atIntersection = true;

        List<Vector2Int> _referenceToNeighbors = _puzzle.GetAdjacency()[_intersection];
        _neighbors = new List<Vector2Int>();
        foreach (Vector2Int neighbor in _referenceToNeighbors)
        {
            _neighbors.Add(neighbor);
        }

        Direction dir = _puzzle.GetEndPoint(coord);
        if (dir != Direction.NULL)
        {
            Vector2Int directionVector = Vector2Int.zero;
            switch (dir)
            {
                case Direction.Up:
                    directionVector.y++;
                    break;

                case Direction.Right:
                    directionVector.x++;
                    break;

                case Direction.Down:
                    directionVector.y--;
                    break;

                default:
                case Direction.Left:
                    directionVector.x--;
                    break;
            }

            _neighbors.Add(_intersection + directionVector);
        }
    }

    /// <summary>
    ///     Performs calculations necessary after leaving an intersection
    /// </summary>
    void LeaveIntersection()
    {
        Vector2Int direction;
        if (Mathf.Abs(_position.x - _intersection.x) > Mathf.Abs(_position.y - _intersection.y))
        {
            direction = new Vector2Int((int)Mathf.Sign(_position.x - _intersection.x), 0);
            _position.y = _intersection.y;
        }
        else
        {
            direction = new Vector2Int(0, (int)Mathf.Sign(_position.y - _intersection.y));
            _position.x = _intersection.x;
        }

        _targetPosition = _intersection + direction;
        _playerPath.SetMarker(_intersection, _targetPosition);
        _atIntersection = false;

        if (_puzzle.GetEndPoint(_intersection) != Direction.NULL && !_puzzle.GetAdjacency()[_intersection].Contains(_targetPosition))
        {
            _targetIsEnd = true;
        }
        else
        {
            _targetIsEnd = false;
        }
    }

    /// <summary>
    ///     Stops PlayerPath drawing and changes to LookingAtPuzzle state.
    /// </summary>
    void StopDrawing()
    {
        if (State == PlayerState.Drawing)
        {
            _playerPath?.StopPath();

            _puzzle = null;
            _playerPath = null;
            Won = false;
            State = PlayerState.LookingAtPuzzle;
        }
    }

    /// <summary>
    ///     Raycasts from mouse position forward to find puzzle start. Starts drawing if found.
    ///     Will only perform check if the player is LookingAtPuzzle.
    ///     May update to Drawing state.
    /// </summary>
    void AttemptToStartDrawing()
    {
        if (State != PlayerState.LookingAtPuzzle) return;

        Ray ray = _cam.ScreenPointToRay(_virtualMouse.Position);
        if (Physics.Raycast(ray, out _hitinfo, 20f))
        {
            PuzzleCoordinate coordinate = _hitinfo.transform.GetComponent<PuzzleCoordinate>();
            if (coordinate != null)
            {
                _puzzle = coordinate.puzzle;
                _playerPath = _puzzle.GetComponentInChildren<PlayerPath>();

                Bounds startBounds = coordinate.GetComponentInChildren<Renderer>().bounds;
                _startNodeRadius = _puzzle.LocalToPuzzle(startBounds.extents.x);

                StartDrawing(coordinate.coord);
            }
        }
    }

    /// <summary>
    ///     Starts drawing with the cached PlayerPath.
    ///     Assumes _playerPath and _puzzle have non-null references.
    ///     Updates to Drawing state.
    /// </summary>
    /// <param name="coord"> The puzzle space coordinate for PlayerPath to start </param>
    void StartDrawing(Vector2Int coord)
    {
        if (!_playerPath.StartPath(coord, this)) return;

        AudioManager.instance.PlayClip(_startDrawingClip, .6f);
        State = PlayerState.Drawing;
        Won = false;
        EnterIntersection(coord);
    }

    /// <summary>
    ///     finishes the current puzzle. Updates to LookingAtPuzzle state.
    /// </summary>
    void Complete()
    {
        if (State != PlayerState.Drawing) return;

        if (AtEnd)
        {
            State = PlayerState.LookingAtPuzzle;
            Won = true;
            AudioManager.instance.PlayClip(_winClip, .6f);
            _playerPath.Complete();
            _virtualMouse.Deactivate();   // would need to consider if this is the first time the puzzle has been solved to auto-deactivate here
            _puzzleFrame.Deactivate();
            //_position = _intersection + ((Vector2) _targetPosition - _intersection) * (1 - _puzzle.configs.endLength);
        }
    }

    /// <summary>
    ///     Interacts with given puzzle.
    ///     Moves camera and locks player movement.
    ///     Updates to LookingAtPuzzle state.
    /// </summary>
    void InteractWithPuzzle()
    {
        if (State != PlayerState.FPS) return;

        State = PlayerState.LookingAtPuzzle;
        UpdateCoordinateConversionLogic();

        _virtualMouse.Activate(new Vector2(Screen.width / 2, Screen.height / 2), _spacing * _puzzleLineWidth);
        _puzzleFrame.Activate();

        AudioManager.instance.PlayClip(_interactionClip, (go) =>
        {
            StartCoroutine(WooshInFadeOut(go.GetComponent<AudioSource>()));
        });
        // move camera to view puzzle
        // lock player movement
    }

    /// <summary>
    ///     Switches from LookingAtPuzzle to FPS. Will hide the virtual cursor.
    /// </summary>
    void LookAwayFromPuzzle()
    {
        if (State != PlayerState.LookingAtPuzzle) return;

        State = PlayerState.FPS;
        _virtualMouse.Deactivate();
    }

    /// <summary>
    ///     Closes the Application
    /// </summary>
    void Quit()
    {
        Debug.Log("Quitting game.");
        Application.Quit();
    }
    #endregion

    #region Coordinate Space conversions
    /// <summary>
    ///     Converts from screen space (pixels non-normalized) to puzzle space.
    ///     Makes a great deal of assumptions: camera (and puzzle) is do not move while drawing
    ///     and view of the puzzle is relative orthographic.
    /// </summary>
    /// <param name="position"> </param>
    /// <returns> </returns>
    public Vector2 ScreenToPuzzle(Vector2 position)
    {
        if (_puzzle == null)
        {
            Debug.LogError("ERROR: _puzzle (type: PuzzleRenderer) is null");
            return Vector2.negativeInfinity;
        }

        return (position - _screenOrigin) / _spacing;
    }

    /// <summary>
    ///     Converts from screen space to puzzle space.
    ///     Ignores _screenOrigin.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2 ScreenToPuzzleSpacing(Vector2 value)
    {
        return value / _spacing;
    }

    /// <summary>
    ///     Converts from puzzle space to screen space.
    ///     Makes a great deal of assumptions: camera (and puzzle) is do not move while drawing
    ///     and view of the puzzle is relative orthographic.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector2 PuzzleToScreen(Vector2 position)
    {
        if (_puzzle == null)
        {
            Debug.LogError("ERROR: _puzzle (type: PuzzleRenderer) is null");
            return Vector2.negativeInfinity;
        }

        return _screenOrigin + position * _spacing;
    }

    /// <summary>
    ///     Converts from screen space to puzzle space.
    ///     Considers pixels to puzzle space, not the origin.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public float ScreenToPuzzle(float value)
    {
        return value / _spacing.x;
    }

    /// <summary>
    ///     Updates _spacing variable for ScreenToPuzzle conversions
    /// </summary>
    public void UpdateCoordinateConversionLogic()
    {
        _puzzle.UpdatePuzzleToLocalLogic();

        _screenOrigin = _cam.WorldToScreenPoint(_puzzle.transform.TransformPoint(_puzzle.PuzzleToLocal(Vector2.zero)));
        _spacing.x = (_cam.WorldToScreenPoint(_puzzle.transform.TransformPoint(_puzzle.PuzzleToLocal(Vector2.right))) - (Vector3) _screenOrigin).x;
        _spacing.y = (_cam.WorldToScreenPoint(_puzzle.transform.TransformPoint(_puzzle.PuzzleToLocal(Vector2.up))) - (Vector3) _screenOrigin).y;

        _intersectionWidth = _puzzleLineWidth = _puzzle.LocalToPuzzle(_puzzle.configs.lineWidth);
        _intersectionWidth *= _intersectionSize / 2;
    }
    #endregion

    #region Utility
    /// <summary>
    ///     Returns true if the point is within the square radius of the center.
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    static bool WithinSquareRadius(Vector2 center, float radius, Vector2 point)
    {
        return Mathf.Abs(point.x - center.x) < radius && Mathf.Abs(point.y - center.y) < radius;
    }

    /// <summary>
    ///     Clamps the position between start and end.
    ///     Considers path and node types.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    Vector2 ClampToPath(Vector2 position, Vector2 start, Vector2 end)
    {
        // clamp between start and end
        Vector3 projection = Vector3.Project(position - start, end - start);
        Vector2 clamped = new Vector2(projection.x, projection.y);

        bool resetEnd = true;

        // case: split path requires earlier clamping
        if (_puzzle.GetPathType(start, end) == PathType.Split)
        {
            clamped = Vector2.ClampMagnitude(clamped, .5f - _puzzle.configs.splitGap / 2 - _puzzleLineWidth / 2);
        }
        // case: prevent self-intersecting position
        else if (_playerPath.Verts.Contains(end) && _playerPath.Verts[_playerPath.Verts.Count - 1] != end)
        {
            // case: stop before startnode
            float maxLen;
            if (_puzzle.IsStartPoint(end))
            {
                maxLen = 1 - _startNodeRadius - _puzzleLineWidth / 2;
            }
            else
            {
                maxLen = 1 - _puzzleLineWidth;
            }
            clamped = Vector2.ClampMagnitude(clamped, maxLen);
        }
        // case: stop at end
        else if (_targetIsEnd)
        {
            float maxLen = _puzzle.configs.endLength + _puzzleLineWidth / 2;
            float len = clamped.magnitude;

            if (len > maxLen - _endSize)
            {
                AtEnd = true;
                resetEnd = false;
            }

            clamped = Vector2.ClampMagnitude(clamped, maxLen);
        }

        if (resetEnd)
        {
            AtEnd = false;
        }

        clamped += start;
        return clamped;
    }

    /// <summary>
    ///     Clamps position to 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="intersection"></param>
    /// <returns></returns>
    Vector2 ClampToIntersection(Vector2 position, Vector2 intersection)
    {
        Vector2 extentsX = Vector2.one * intersection.x;
        Vector2 extentsY = Vector2.one * intersection.y;

        foreach (Vector2Int neighbor in _neighbors)
        {
            Vector2 delta = neighbor - intersection;
            if (delta.x < 0)
            {
                extentsX.x += delta.x;
            }
            else
            {
                extentsX.y += delta.x;
            }

            if (delta.y < 0)
            {
                extentsY.x += delta.y;
            }
            else
            {
                extentsY.y += delta.y;
            }
        }

        Vector2 clampedPos;
        clampedPos.x = Mathf.Clamp(position.x, extentsX.x, extentsX.y);
        clampedPos.y = Mathf.Clamp(position.y, extentsY.x, extentsY.y);
        return clampedPos;
    }

    /// <summary>
    ///     Subscribes to VirtualMouse C# events
    /// </summary>
    void SubscribeToEvents()
    {
        _virtualMouse.OnMouseMove += UpdateDrawing;
        _virtualMouse.OnLeftClick += AttemptToStartDrawing;
        _virtualMouse.OnRightClick += StopDrawing;
        _virtualMouse.OnLeftClick += Complete;
    }

    /// <summary>
    ///     Unsubscribes to VirtualMouse C# events
    /// </summary>
    private void UnsubscribeToEvents()
    {
        _virtualMouse.OnMouseMove -= UpdateDrawing;
        _virtualMouse.OnLeftClick -= AttemptToStartDrawing;
        _virtualMouse.OnRightClick -= StopDrawing;
        _virtualMouse.OnLeftClick -= Complete;
    }
    #endregion

    #region IEnumerators
    IEnumerator WooshInFadeOut(AudioSource source)
    {
        float peakVol = .1f;
        float len = 1.75f;
        float peakFrac = .9f;
        float elapsedtime = 0;

        while (elapsedtime < len)
        {
            if (source == null) break;

            float nVol;
            if (elapsedtime < peakFrac * len)
            {
                nVol = Mathf.Lerp(0, peakVol, elapsedtime / (peakFrac * len));
            }
            else
            {
                nVol = Mathf.Lerp(peakVol, 0, (elapsedtime - peakFrac * len) / (len - peakFrac * len));
            }

            source.volume = nVol;
            yield return null;
            elapsedtime += Time.deltaTime;
        }

    }
    #endregion
}

public enum PlayerState
    {
        FPS = 0, LookingAtPuzzle = 1, Drawing = 2
    }