﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

namespace Puzzle
{
    [RequireComponent(typeof(VirtualMouse), typeof(PlayerMovement))]
    public class Player : MonoBehaviour
    {
        #region Public Variables
        [HideInInspector]
        public PlayerState State = PlayerState.FPS;
        #endregion

        #region Exposed Variables
        [Header("Configurations")]
        [Tooltip("Multiplier to the intersection bounding-box. Value of 1 will default bounding-box to the line width.")]
        [SerializeField]
        float _intersectionSize = .5f;
        #endregion

        #region Private Variables
        PuzzleRenderer _puzzle = null;
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
        #endregion

        #region Monobehaviours
        void Awake()
        {
            _cam = Camera.main;
            _virtualMouse = GetComponent<VirtualMouse>();
            SubscribeToEvents();

            // default to looking for testing
            State = PlayerState.LookingAtPuzzle;
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
            _neighbors = _puzzle.GetAdjacency()[_intersection];
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
        }

        /// <summary>
        ///     Stops PlayerPath drawing and changes to LookingAtPuzzle state.
        /// </summary>
        void StopDrawing()
        {
            if (State == PlayerState.Drawing)
            {
                _virtualMouse.Deactivate();
                _playerPath?.StopPath();

                _puzzle = null;
                _playerPath = null;
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

            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
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
            State = PlayerState.Drawing;
            EnterIntersection(coord);
            UpdateCoordinateConversionLogic();

            _playerPath.StartPath(_intersection);
            _virtualMouse.Activate(PuzzleToScreen(coord));
            _virtualMouse._cursor.localScale = Vector3.one * _puzzle.configs.lineWidth;
        }

        /// <summary>
        ///     Interacts with given puzzle.
        ///     Moves camera and locks player movement.
        ///     Updates to LookingAtPuzzle state.
        /// </summary>
        void InteractWithPuzzle(SOPuzzle puzzle)
        {
            if (State != PlayerState.FPS) return;

            State = PlayerState.LookingAtPuzzle;
            // move camera to view puzzle
            // lock player movement
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

            _screenOrigin = _cam.WorldToScreenPoint(_puzzle.PuzzleToLocal(Vector2.zero));
            _spacing.x = (_cam.WorldToScreenPoint(_puzzle.PuzzleToLocal(Vector2.right)) - (Vector3) _screenOrigin).x;
            _spacing.y = (_cam.WorldToScreenPoint(_puzzle.PuzzleToLocal(Vector2.up)) - _cam.WorldToScreenPoint(_puzzle.PuzzleToLocal(Vector2.zero))).y;

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

        Vector2 ClampToPath(Vector2 position, Vector2 start, Vector2 end)
        {
            // clamp between start and end
            Vector3 projection = Vector3.Project(position - start, end - start);
            Vector2 clamped = new Vector2(projection.x, projection.y);

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
        ///     Subscribes to VirualMouse C# events
        /// </summary>
        void SubscribeToEvents()
        {
            _virtualMouse.OnMouseMove += UpdateDrawing;
            _virtualMouse.OnLeftClick += AttemptToStartDrawing;
            _virtualMouse.OnRightClick += StopDrawing;
        }

        /// <summary>
        ///     Unsubscribes to VirualMouse C# events
        /// </summary>
        private void UnsubscribeToEvents()
        {
            _virtualMouse.OnMouseMove -= UpdateDrawing;
            _virtualMouse.OnLeftClick -= AttemptToStartDrawing;
            _virtualMouse.OnRightClick -= StopDrawing;
        }
        #endregion
    }

    public enum PlayerState
    {
        FPS = 0, LookingAtPuzzle = 1, Drawing = 2
    }
}