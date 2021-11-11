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
    /// Creates line prefabs to display the given puzzle. Will reset the line renderer.
    /// </summary>
    void CreatePuzzle()
    {
        Clearlines();
        UpdatePuzzleToLocalLogic();

        List<Path> endPaths = new List<Path>();

        // draw end points
        foreach (KeyValuePair<Vector2Int, Direction> pair in _puzzle.EndNodes)
        {
            Path p = new Path(pair.Key, new Vector2Int(pair.Key.x, pair.Key.y+1));
            _puzzle.Paths.Add(p, PathType.End);
            DrawEndPoint();
        }

        Dictionary<Vector2Int, Vector2Int[]> corners = _puzzle.GetCorners();
        Dictionary<Vector2Int, List<Path>> nodes = _puzzle.GetAdjacencyList();
        UDictionaryPaths paths = _puzzle.Paths;

        // draw starting points
        foreach (Vector2Int startPoint in _puzzle.StartNodes)
        {
            // prevent corner from being created under start point
            if (corners.ContainsKey(startPoint))
            {
                corners.Remove(startPoint);
            }

            DrawStartPoints(startPoint);
        }


        // draw nodes
        foreach (KeyValuePair<Vector2Int, List<Path>> node in nodes)
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
        foreach (KeyValuePair<Path, PathType> entry in paths)
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

        // removes temporary paths used to draw end nodes
        foreach (Path path in endPaths)
        {
            _puzzle.Paths.Remove(path);
        }
    }

    #region coordinate space conversions
    /// <summary>
    /// Converts from local space to puzzle space. The coordinate will be expand/shrink
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
    }

    Quaternion GetLineRotation(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float angle = Vector3.SignedAngle(Vector3.up, dir, transform.forward);
        return Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Gets the stroke direction from start towards end
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
    /// Gets the z-rotation needed to draw a rounded corner for the given stroke. Assumes angle=0 when the corner goes in north and out west.
    /// </summary>
    /// <param name="stroke"></param>
    /// <returns></returns>
    float GetCornerAngle(Vector2Int[] stroke)
    {
        if (stroke.Length != 3)
        {
            Debug.LogError("ERROR: unable to get corner angle. Stroke should have 3 points");
            return 0;
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

    #region draw
    void DrawStartPoints(Vector2Int point)
    {
        Vector3 pos = PuzzleToLocal(point);
        GameObject go = Instantiate(_startPrefab, transform);

        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);
    }

    /// <summary>
    /// Draws a line object upon the given path. Is a wrapper of DrawConnectedStroke().
    /// </summary>
    /// <param name="path"></param>
    void DrawConnectedPath(Path path)
    {
        Vector2[] points =
        {
            path.p1,
            path.p2
        };

        ShortenStroke(ref points, true, _lineWidth / 2);
        ShortenStroke(ref points, false, _lineWidth / 2);
        DrawConnectedStroke(points);
    }

    /// <summary>
    /// Draws a split path with the _splitGap proportional gap in the middle.
    /// TODO: this needs to combine with corner logic
    /// </summary>
    /// <param name="path"></param>
    void DrawSplitPath(Path path)
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

        DrawConnectedStroke(stroke1);
        DrawConnectedStroke(stroke2);
    }

    GameObject DrawRoundedCorner(Vector2Int point, float angle)
    {
        GameObject go = Instantiate(_cornerPrefab, transform);

        go.transform.localPosition = PuzzleToLocal(point);
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);
        go.transform.localRotation = Quaternion.Euler(0, 0, angle);

        return go;
    }

    /// <summary>
    /// Draws a square around the given point of length _lineWidth.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    GameObject DrawSharpCorner(Vector2 point)
    {
        Vector3 bottom = PuzzleToLocal(point);
        bottom.y -= _lineWidth / 2;

        GameObject go = Instantiate(_quadPrefab, transform);
        go.transform.localPosition = bottom;
        go.transform.localScale = new Vector3(_lineWidth, _lineWidth, _lineWidth);

        return go;
    }

    GameObject DrawEndPoint(Path path)
    {
        // this doesn't work
        Vector2[] stroke =
        {
            path.p1,
            path.p2
        };
        return CreatePrefab(stroke, _capPrefab);
    }

    /// <summary>
    /// Draws Quad at the given point for the given length
    /// </summary>
    /// <param name="points"> An array of 2 vector2 objects for the line to begin and end at. </param>
    /// <returns> The newly created line object </returns>
    GameObject DrawConnectedStroke(Vector2[] points)
    {
        return CreatePrefab(points, _quadPrefab);
    }

    GameObject CreatePrefab(Vector2[] points, GameObject prefab)
    {
        if (points.Length != 2)
        {
            Debug.LogError("ERROR: unable to draw line with " + points.Length + " verts.");
        }

        GameObject go = Instantiate(prefab, transform);
        _lines.Add(go);

        // set position
        Vector3 pos = PuzzleToLocal(points[0]);
        Vector3 end = PuzzleToLocal(points[1]);
        go.transform.localPosition = pos;

        // set rotation
        go.transform.localRotation = GetLineRotation(pos, end);

        // set scale
        Vector3 scal = Vector3.one;
        scal.x = _lineWidth;
        scal.y = (end - pos).magnitude;
        go.transform.localScale = scal;

        return go;
    }

    /// <summary>
    /// Moves the desired point to shorten the stroke proportional to the line width.
    /// The point internal to that being manipulated will not be affected.
    /// This operation will consider local and puzzle space.
    /// </summary>
    /// <param name="stroke"> The puzzle space coordinates of a stroke. True is the first point in the stroke. </param>
    /// <param name="first"> Selects which end of the stroke to manipulate. </param>
    void ShortenStroke(ref Vector2[] stroke, bool first, float amount)
    {
        if (stroke.Length < 2)
        {
            Debug.LogError("Unable to shorten stroke: the given stroke is too short.");
            return;
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

    // wrapper of shorten stroke.
    void LengthenStroke(ref Vector2[] stroke, bool first, float amount)
    {
        ShortenStroke(ref stroke, first, -amount);
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