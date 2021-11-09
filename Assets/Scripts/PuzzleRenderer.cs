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
    
    [Tooltip("The width and width (in world space) of the puzzle.")]
    [SerializeField]
    Vector2 _size = new Vector2(2, 2);

    [Tooltip("Margin size (in world space) between border of puzzle bounds and the lines within.")]
    [SerializeField]
    float _margin = .25f;

    [Tooltip("Empty GameObject with a LineRenderer Component. Positions will be reset, but other properties will remain.")]
    [SerializeField]
    GameObject _linePrefab;

    [Header("Data")]
    [SerializeField] SOPuzzle _puzzle;
    #endregion

    #region Private Variables
    List<LineRenderer> _lines;
    float _lineWidth;
    Vector2 _spacing;
    #endregion

    #region Editor Variables
#if UNITY_EDITOR
    [SerializeField] Color _boundingBoxColor = Color.red;
#endif
    #endregion

    void Start()
    {
        if (_puzzle == null || _linePrefab == null)
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
        _lineWidth = _linePrefab.GetComponent<LineRenderer>().startWidth;
        Dictionary<Vector2Int, Vector2Int[]> corners = _puzzle.GetCorners();
        UpdatePuzzleToLocalLogic();

        // draw corners
        foreach (KeyValuePair<Vector2Int, Vector2Int[]> corner in corners)
        {
            Vector2[] stroke =
            {
                corner.Value[0],
                corner.Key,
                corner.Value[1]
            };

            // shortens the stroke away from any adjacent corner(s)
            if (corners.ContainsKey(corner.Value[0]))
            {
                ShortenStroke(ref stroke, true, _lineWidth);
            }
            if (corners.ContainsKey(corner.Value[1]))
            {
                ShortenStroke(ref stroke, false, _lineWidth);
            }

            DrawConnectedStroke(stroke);
        }

        // draw standard paths
        foreach (KeyValuePair<Path, PathType> entry in _puzzle)
        {
            Path path = entry.Key;
            PathType connection = entry.Value;

            // skip corners that have already been drawn
            if (corners.ContainsKey(path.p1) || corners.ContainsKey(path.p2)) continue;

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
    #endregion

    #region draw
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

        LengthenStroke(ref points, true, _lineWidth / 2);
        LengthenStroke(ref points, false, _lineWidth / 2);
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

        LengthenStroke(ref stroke1, true, _lineWidth / 2);
        LengthenStroke(ref stroke2, true, _lineWidth / 2);

        DrawConnectedStroke(stroke1);
        DrawConnectedStroke(stroke2);
    }

    /// <summary>
    /// Draws a line object through the given points.
    /// </summary>
    /// <param name="points"></param>
    void DrawConnectedStroke(Vector2[] points)
    {
        LineRenderer line = Instantiate(_linePrefab, transform.position, transform.rotation, transform).GetComponent<LineRenderer>();
        _lines.Add(line);

        Vector3[] verts = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            verts[i] = PuzzleToLocal(points[i]);
        }

        line.positionCount = verts.Length;
        line.SetPositions(verts);
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
            foreach (LineRenderer line in _lines)
            {
                Destroy(line.gameObject);
            }
        }

        _lines = new List<LineRenderer>();
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