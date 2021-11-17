using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

namespace Puzzle
{
    [SelectionBase]
    public class PuzzleRenderer : MonoBehaviour
    {
        #region Public Variables
        public PuzzleConfigs configs;
        #endregion

        #region Exposed Variables
        [Header("Data")]
        [SerializeField] SOPuzzle _puzzle;
        #endregion

        #region Private Variables
        List<GameObject> _lineSegments;
        Vector2 _spacing;
        #endregion

        #region Editor Variables
    #if UNITY_EDITOR
        [SerializeField] Color _boundingBoxColor = Color.red;
    #endif
        #endregion

        void Start()
        {
            CreatePuzzle();
        }

        /// <summary>
        /// Creates line prefabs to display the given puzzle. Will reset the line renderer. Will skip null paths.
        /// </summary>
        public void CreatePuzzle()
        {
            ClearLines();
            UpdatePuzzleToLocalLogic();

            Dictionary<Vector2Int, Vector2Int[]> corners = _puzzle.GetCorners();

            // draw end points
            foreach (KeyValuePair<Vector2Int, Direction> pair in _puzzle.EndNodes)
            {
                Vector2 dir = SOPuzzle.GetDirectionVector(pair.Value);
                Vector3 localNode = PuzzleToLocal(pair.Key + (dir * configs.lineWidth / 4));
                Vector3 localEnd = PuzzleToLocal(pair.Key + (dir * configs.lineWidth / 4) + (dir * configs.endLength));

                GameObject[] lineSegments = MeshLine.DrawLineRounded(configs, transform, localNode, localEnd, false, true);
                foreach (GameObject segment in lineSegments)
                {
                    if (segment != null)
                    {
                        _lineSegments.Add(segment);
                    }
                }
            }

            // draw starting points
            foreach (Vector2Int startPoint in _puzzle.StartNodes)
            {
                _lineSegments.Add(MeshLine.DrawStartPoint(configs, transform, PuzzleToLocal(startPoint)));
            }

            // draw nodes
            foreach (KeyValuePair<Vector2Int, List<Path>> node in _puzzle.GetAdjacencyList())
            {
                GameObject segment;
                Vector2Int pos = node.Key;
                Vector3 localPos = PuzzleToLocal(pos);

                // draw rounded corner
                if (corners.ContainsKey(pos))
                {
                    Vector2Int[] stroke =
                    {
                        corners[pos][0],
                        pos,
                        corners[pos][1]
                    };

                    segment = MeshLine.DrawRoundedCorner(configs, transform, localPos, GetCornerAngle(stroke));
                }
                // draw default (sharp) corner
                else
                {
                    segment = MeshLine.DrawSharpCorner(configs, transform, localPos);
                }

                _lineSegments.Add(segment);
            }

            // draw paths
            foreach (KeyValuePair<Path, PathType> entry in _puzzle)
            {
                Vector3 localStart = PuzzleToLocal(entry.Key.p1);
                Vector3 localEnd = PuzzleToLocal(entry.Key.p2);

                switch (entry.Value)
                {
                    case PathType.Connected:
                        _lineSegments.Add(MeshLine.DrawConnectedPath(configs, transform, localStart, localEnd));
                        break;

                    case PathType.Split:
                        GameObject[] lineSegments = MeshLine.DrawSplitPath(configs, transform, localStart, localEnd);
                        _lineSegments.Add(lineSegments[0]);
                        _lineSegments.Add(lineSegments[1]);
                        break;

                    default: continue;
                }
            }
        }

        #region Vector Utilities
        /// <summary>
        /// Converts from puzzle space to local space. The coordinate will be expand/shrink
        /// from 0 according to the puzzle size, margin, and nodes in the puzzle.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        Vector3 PuzzleToLocal(Vector2 pos)
        {
            Vector3 pPos = Vector3.zero;
            pPos.x += _spacing.x * pos.x - (configs.size / 2).x + configs.margin;
            pPos.y += _spacing.y * pos.y - (configs.size / 2).y + configs.margin;

            return pPos;
        }

        /// <summary>
        /// Updates the spacing variable according to the size and margins of the puzzle.
        /// </summary>
        void UpdatePuzzleToLocalLogic()
        {
            _spacing = configs.size;
            _spacing.x -= configs.margin * 2;
            _spacing.y -= configs.margin * 2;

            _spacing.x /= _puzzle.Size.x - 1;
            _spacing.y /= _puzzle.Size.y - 1;

            _spacing.x = Mathf.Min(_spacing.x, _spacing.y);
            _spacing.y = Mathf.Min(_spacing.x, _spacing.y);
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

        /// <summary>
        /// Deletes current line renderers and removes their references.
        /// </summary>
        public void ClearLines()
        {
            if (_lineSegments != null)
            {
                foreach (GameObject line in _lineSegments)
                {
                    DestroyImmediate(line);
                }
            }

            _lineSegments = new List<GameObject>();
        }

        #region Editor
    #if UNITY_EDITOR
        /// <summary>
        /// Displays the 2D bounding box of the puzzle.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _boundingBoxColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawBoundingBox(transform.position, configs.size);
        }

        /// <summary>
        /// Draws a 2D bounding box of the puzzle. Gizmos.matrix must be set to transform.localToWorldMatrix
        /// </summary>
        void DrawBoundingBox(Vector3 center, Vector2 size)
        {
            Vector2 extent = configs.size / 2;
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

    [System.Serializable]
    public class PuzzleConfigs
    {
        [Tooltip("The width and width (in world space) of the puzzle.")]
        public Vector2 size = new Vector2(2, 2);

        [Tooltip("Margin size (in world space) between border of puzzle bounds and the lines within.")]
        public float margin = .25f;

        [Header("Configuration")]
        [Tooltip("The size of the gap in the middle of a split connection. Value is directly proportional to the length of the connection.")]
        [Range(0, 1)]
        public float splitGap = .2f;

        [Tooltip("The width of the puzzle's paths.")]
        public float lineWidth = 0.6f;

        [Tooltip("The distance of the end nub from the end node proportional to the length of a path.")]
        [Range(0, 1)]
        public float endLength = .2f;

        [Tooltip("The size of the start node.")]
        public float startNodeSize = 1f;

        [Header("Meshes")]
        [Tooltip("Quad mesh for rendering a straight line.")]
        public GameObject quadPrefab;

        [Tooltip("Quarter-circle mesh for rendering rounded 90 degree corners.")]
        public GameObject cornerPrefab;

        [Tooltip("Circle mesh for rendering the starting point")]
        public GameObject startPrefab;

        [Tooltip("Half-circle mesh for rendering a rounded line end.")]
        public GameObject capPrefab;
    }
}