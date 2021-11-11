using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

[SelectionBase]
public class PuzzleRenderer : MonoBehaviour
{
    #region Exposed Variables
    [Header("Configuration")]
    [Tooltip("The size of the gap in the middle of a split connection. Value is directly proportional to the length of the connection.")]
    [SerializeField, Range(0, 1)]
    float _splitGap = .2f;

    [Tooltip("The width of the puzzle's paths.")]
    [SerializeField]
    float _lineWidth = 0.6f;

    [Tooltip("The distance of the end nub from the end node proportional to the length of a path.")]
    [SerializeField, Range(0, 1)]
    float _endLength = .2f;

    [Tooltip("The width and width (in world space) of the puzzle.")]
    [SerializeField]
    Vector2 _size = new Vector2(2, 2);

    [Tooltip("Margin size (in world space) between border of puzzle bounds and the lines within.")]
    [SerializeField]
    float _margin = .25f;

    [Header("Data")]
    [SerializeField] SOPuzzle _puzzle;

    [Header("Meshes")]
    [Tooltip("Quad mesh for rendering a straight line.")]
    [SerializeField]
    GameObject _quadPrefab;

    [Tooltip("Quarter-circle mesh for rendering rounded 90 degree corners.")]
    [SerializeField]
    GameObject _cornerPrefab;

    [Tooltip("Circle mesh for rendering the starting point")]
    [SerializeField]
    GameObject _startPrefab;

    [Tooltip("Half-circle mesh for rendering a rounded line end.")]
    [SerializeField]
    GameObject _capPrefab;
    #endregion

    #region Private Variables
    List<GameObject> _lines;
    Vector2 _spacing;
    #endregion

    #region Editor Variables
#if UNITY_EDITOR
    [SerializeField] Color _boundingBoxColor = Color.red;
#endif
    #endregion

    void Start()
    {
        if (_puzzle == null || _quadPrefab == null)
        {
            Debug.LogError("Puzzle is missing a SOPuzzle or LineRenderer prefab");
            return;
        }

        CreatePuzzle();
    }

    /// <summary>
    /// Creates line prefabs to display the given puzzle. Will reset the line renderer. Will skip null paths.
    /// </summary>
    void CreatePuzzle()
    {
        Clearlines();
        UpdatePuzzleToLocalLogic();

        Dictionary<Vector2Int, Vector2Int[]> corners = _puzzle.GetCorners();

        // draw end points
        foreach (KeyValuePair<Vector2Int, Direction> pair in _puzzle.EndNodes)
        {
            DrawEndPoint(pair.Key);
        }

        // draw starting points
        foreach (Vector2Int startPoint in _puzzle.StartNodes)
        {
            DrawStartPoint(startPoint);
        }

        // draw nodes
        foreach (KeyValuePair<Vector2Int, List<Path>> node in _puzzle.GetAdjacencyList())
        {
            Vector2Int pos = node.Key;

            // draw rounded corner
            if (corners.ContainsKey(pos))
            {
                Vector2Int[] stroke =
                {
                    corners[pos][0],
                    pos,
                    corners[pos][1]
                };

                DrawRoundedCorner(pos, GetCornerAngle(stroke));
            }
            // draw default (sharp) corner
            else
            {
                DrawSharpCorner(pos);
            }
        }

        // draw paths
        foreach (KeyValuePair<Path, PathType> entry in _puzzle)
        {
            Path path = entry.Key;
            PathType connection = entry.Value;

            switch (connection)
            {
                case PathType.Connected:
                    DrawConnectedPath(path);
                    break;

                case PathType.Split:
                    DrawSplitPath(path);
                    break;

                default: continue;
            }
        }
    }

    #region coordinate space conversions
    /// <summary>
    /// Converts from puzzle space to local space. The coordinate will be expand/shrink
    /// from 0 according to the puzzle size, margin, and nodes in the puzzle.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector3 PuzzleToLocal(Vector2 pos)
    {
        Vector3 pPos = Vector3.zero;
        pPos.x += _spacing.x * pos.x - (_size / 2).x + _margin;
        pPos.y += _spacing.y * pos.y - (_size / 2).y + _margin;

        return pPos;
    }

    /// <summary>
    /// Updates the spacing variable according to the size and margins of the puzzle.
    /// </summary>
    void UpdatePuzzleToLocalLogic()
    {
        _spacing = _size;
        _spacing.x -= _margin * 2;
        _spacing.y -= _margin * 2;

        _spacing.x /= _puzzle.Size.x - 1;
        _spacing.y /= _puzzle.Size.y - 1;

        _spacing.x = Mathf.Min(_spacing.x, _spacing.y);
        _spacing.y = Mathf.Min(_spacing.x, _spacing.y);
    }

    /// <summary>
    /// Gets the rotation of line relative to the direction of the puzzle and Vector3.Up
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    Quaternion GetLineRotation(Vector2 start, Vector2 end)
    {
        return Quaternion.Euler(0, 0, Vector2.SignedAngle(transform.up, end - start));
    }

    /// <summary>
    /// Returns the distance from p1 to p2
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    float GetLineLength(Vector3 p1, Vector3 p2)
    {
        return (p2 - p1).magnitude;
    }

    /// <summary>
    /// Gets the stroke direction from start towards end. Will return Direction.NULL if the stroke is not along a cardinal direction.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    Direction GetStrokeDirection(Vector2Int start, Vector2Int end)
    {
        if (start.x == end.x)
        {
            // case: stroke 1 is downwards
            if (start.y > end.y)
            {
                return Direction.Down;
            }
            // case: stroke1 1 is upwards
            else
            {
                return Direction.Up;
            }
        }
        else if (start.y == end.y)
        {
            // case: stroke 1 is leftwards
            if (start.x > end.x)
            {
                return Direction.Left;
            }
            // case: stroke 1 is rightwards
            else
            {
                return Direction.Right;
            }
        }

        return Direction.NULL;
    }

    /// <summary>
    /// Gets the z-rotation (in degrees) needed to draw a rounded corner for the given stroke. Assumes angle=0 when the corner goes in north and out west.
    /// </summary>
    /// <param name="stroke"></param>
    /// <returns></returns>
    float GetCornerAngle(Vector2Int[] stroke)
    {
        if (stroke.Length != 3)
        {
            throw new System.Exception("ERROR: unable to get corner angle. Stroke should have 3 points");
        }

        float angle;
        int directionSum = (int)GetStrokeDirection(stroke[0], stroke[1]) + 2 * (int)GetStrokeDirection(stroke[1], stroke[2]);
        switch (directionSum)
        {
            // case: north then west
            case 54:
            case 15:
                angle = -90;
                break;

            // case: south then east
            case 21:
            case 27:
                angle = 90;
                break;

            // case: south then west
            case 57:
            case 9:
                angle = 180;
                break;

            // case: north then east
            case 18:
            case 33:
            default:
                angle = 0;
                break;
        }

        return angle;
    }
    #endregion

    #region Drawing
    /// <summary>
    /// Creates the start point visual
    /// </summary>
    /// <param name="point"></param>
    /// <returns> a reference to the created visual </returns>
    GameObject DrawStartPoint(Vector2Int point)
    {
        Vector3 pos = PuzzleToLocal(point);
        GameObject go = Instantiate(_startPrefab, transform);

        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);

        return go;
    }

    /// <summary>
    /// Creates a line object upon the given path
    /// </summary>
    /// <param name="path"></param>
    /// <returns> A reference to the created visual </returns>
    GameObject DrawConnectedPath(Path path)
    {
        Vector2[] points =
        {
            path.p1,
            path.p2
        };

        ShortenStroke(ref points, true, _lineWidth / 2);
        ShortenStroke(ref points, false, _lineWidth / 2);
        return DrawConnectedStroke(points[0], points[1]);
    }

    /// <summary>
    /// Creates line objects along the given path with _splitGap space in the middle
    /// </summary>
    /// <param name="path"></param>
    /// <returns> References to created visuals </returns>
    GameObject[] DrawSplitPath(Path path)
    {
        Vector2[] stroke1 =
        {
            path.p1,
            Vector2.Lerp(path.p1, path.p2, 0.5f - _splitGap / 2)
        };
        Vector2[] stroke2 = {
            path.p2,
            Vector2.Lerp(path.p2, path.p1, 0.5f - _splitGap / 2)
        };

        ShortenStroke(ref stroke1, true, _lineWidth / 2);
        ShortenStroke(ref stroke2, true, _lineWidth / 2);

        GameObject[] gos = {
            DrawConnectedStroke(stroke1[0], stroke1[1]),
            DrawConnectedStroke(stroke2[0], stroke2[1])
        };

        return gos;
    }

    /// <summary>
    /// Creates a rounded corner at the given point with given rotation
    /// </summary>
    /// <param name="point"></param>
    /// <param name="angle"></param>
    /// <returns> A reference to the created visual </returns>
    GameObject DrawRoundedCorner(Vector2Int point, float angle)
    {
        GameObject go = Instantiate(_cornerPrefab, transform);

        go.transform.localPosition = PuzzleToLocal(point);
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);
        go.transform.localRotation = Quaternion.Euler(0, 0, angle);

        return go;
    }

    /// <summary>
    /// Creates a square around the given point of length _lineWidth.
    /// </summary>
    /// <param name="point"></param>
    /// <returns> A reference to the created visual </returns>
    GameObject DrawSharpCorner(Vector2 point)
    {
        Vector3 bottom = PuzzleToLocal(point);
        bottom.y -= _lineWidth / 2;

        GameObject go = Instantiate(_quadPrefab, transform);
        go.transform.localPosition = bottom;
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);

        return go;
    }

    /// <summary>
    /// Creates a line from the end node in its appropriate direction with an end cap.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns> References to the created visuals </returns>
    GameObject[] DrawEndPoint(Vector2Int pos)
    {
        Vector2[] stroke =
        {
            pos,
            Vector2.Lerp(pos, _puzzle.GetEndPath(pos).p2, _endLength)
        };
        ShortenStroke(ref stroke, true, _lineWidth / 2);

        GameObject[] _lines =
        {
            DrawConnectedStroke(stroke[0], stroke[1]),
            Instantiate(_capPrefab, transform)
        };

        // adjusts the end cap's transform
        _lines[1].transform.localPosition = PuzzleToLocal(stroke[1]);
        _lines[1].transform.localRotation = GetLineRotation(stroke[0], stroke[1]);
        _lines[1].transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);

        return _lines;
    }

    /// <summary>
    /// Creates a quad that spans the given 2 points
    /// </summary>
    /// <param name="points"> An array of 2 vector2 objects for the line to begin and end at. </param>
    /// <returns> A reference to the created visual </returns>
    GameObject DrawConnectedStroke(Vector2 start, Vector2 end)
    {
        GameObject go = Instantiate(_quadPrefab, transform);

        // set position
        Vector3 worldStart = PuzzleToLocal(start);
        Vector3 worldEnd = PuzzleToLocal(end);
        go.transform.localPosition = worldStart;

        // set rotation
        go.transform.localRotation = GetLineRotation(start, end);

        // set scale
        Vector3 scal = Vector3.one;
        scal.x = _lineWidth;
        scal.y = GetLineLength(worldStart, worldEnd);
        go.transform.localScale = scal;

        return go;
    }

    /// <summary>
    /// Moves the desired point to shorten the stroke.
    /// The point internal to that being manipulated will not be affected.
    /// This operation will consider local and puzzle space.
    /// </summary>
    /// <param name="stroke"> The puzzle space coordinates of a stroke. </param>
    /// <param name="first"> Selects which end of the stroke to manipulate. True will manipulate stroke[0] </param>
    void ShortenStroke(ref Vector2[] stroke, bool first, float amount)
    {
        if (stroke.Length < 2)
        {
            throw new System.Exception("Unable to shorten stroke: the given stroke is too short.");
        }

        Vector2 dir;
        if (first)
        {
            dir = (stroke[0] - stroke[1]).normalized;
        }
        else
        {
            int lastI = stroke.Length - 1;
            dir = (stroke[lastI] - stroke[lastI - 1]).normalized;
        }

        dir *= amount;
        dir /= _spacing;

        if (first)
        {
            stroke[0] -= dir;
        }
        else
        {
            int lastI = stroke.Length - 1;
            stroke[lastI] -= dir;
        }
    }

    /// <summary>
    /// Deletes current line renderers and removes their references.
    /// </summary>
    void Clearlines()
    {
        if (_lines != null)
        {
            foreach (GameObject line in _lines)
            {
                Destroy(line);
            }
        }

        _lines = new List<GameObject>();
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    /// <summary>
    /// Displays the 2D bounding box of the puzzle.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _boundingBoxColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        DrawBoundingBox(transform.position, _size);
    }

    /// <summary>
    /// Draws a 2D bounding box of the puzzle. Gizmos.matrix must be set to transform.localToWorldMatrix
    /// </summary>
    void DrawBoundingBox(Vector3 center, Vector2 size)
    {
        Vector2 extent = _size / 2;
        Vector3[] points =
        {
            new Vector3(-extent.x, -extent.y, center.z), Vector3.right * size.x, // bottom left - right
            new Vector3(-extent.x,  extent.y, center.z), Vector3.right * size.x, //    top left - right
            new Vector3(-extent.x,  extent.y, center.z), Vector3.down  * size.y, //    top left - down
            new Vector3( extent.x,  extent.y, center.z), Vector3.down  * size.y, //  right down - down
        };

        for (int i = 0; i < points.Length; i += 2)
        {
            Gizmos.DrawRay(points[i], points[i + 1]);
        }
    }
#endif
    #endregion
}