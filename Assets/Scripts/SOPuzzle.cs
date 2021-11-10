using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Puzzle")]
    public class SOPuzzle : ScriptableObject, IEnumerable
    {
        public UDictionaryPaths Paths = new UDictionaryPaths();
        public Vector2Int Size { get; private set; } = Vector2Int.zero;

        public List<Vector2Int> StartNodes = new List<Vector2Int>();
        public List<Vector2Int> EndNodes = new List<Vector2Int>();

        #region Dynamics
        /// <summary>
        /// Clears the puzzle's paths
        /// </summary>
        public void ClearPaths()
        {
            Paths.Clear();
            Size = Vector2Int.zero;
        }

        void OnValidate()
        {
            UpdateSize();
        }

        /// <summary>
        /// Parses the puzzle entries to update Size.
        /// The min node is assumed to be 0.
        /// </summary>
        void UpdateSize()
        {
            Vector2Int max = Vector2Int.zero;
            foreach (KeyValuePair<Path, PathType> entry in this)
            {
                Path path = entry.Key;
                PathType pathType = entry.Value;
                if (pathType == PathType.NULL) continue;

                if (max.x < path.p1.x + 1)
                {
                    max.x = path.p1.x + 1;
                }
                if (max.x < path.p2.x + 1)
                {
                    max.x = path.p2.x + 1;
                }
                if (max.y < path.p1.y + 1)
                {
                    max.y = path.p1.y + 1;
                }
                if (max.y < path.p2.y + 1)
                {
                    max.y = path.p2.y + 1;
                }
            }

            Size = max;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Finds all corner nodes. A corner is a node with connections that are non-parallel.
        /// In a square grid, these connections would be perpendicular.
        /// </summary>
        /// <returns>
        /// The key of the dictionary is node central in the corner
        /// and the array contains its 2 non-parallel neighbors.
        /// </returns>
        public Dictionary<Vector2Int, Vector2Int[]> GetCorners()
        {
            // search nodes for corners
            Dictionary<Vector2Int, Vector2Int[]> corners = new Dictionary<Vector2Int, Vector2Int[]>();
            foreach (KeyValuePair<Vector2Int, List<Path>> node in GetAdjacencyMatrix())
            {
                if (node.Value.Count == 2)
                {
                    // ignores corners that have a split connection.
                    // due to limitations of LineRenderer this can not be represented.
                    if (Paths[node.Value[0]] == PathType.Split || Paths[node.Value[1]] == PathType.Split)
                    {
                        continue;
                    }

                    // calculates direction vectors
                    Vector2 dir1 = node.Value[0].GetDirection();
                    Vector2 dir2 = node.Value[1].GetDirection();

                    bool parallel = dir1 == dir2;
                    bool antiParallel = dir1 == -dir2;
                    if (!(parallel || antiParallel))
                    {
                        Vector2Int[] corner = {
                        node.Value[0].p1 == node.Key ? node.Value[0].p2 : node.Value[0].p1,
                        node.Value[1].p1 == node.Key ? node.Value[1].p2 : node.Value[1].p1
                    };

                        corners.Add(node.Key, corner);
                    }
                }
            }

            return corners;
        }

        /// <summary>
        /// Creates and returns the adjacency matrix of the puzzle.
        /// </summary>
        /// <returns></returns>
        public Dictionary<Vector2Int, List<Path>> GetAdjacencyMatrix()
        {
            Dictionary<Vector2Int, List<Path>> nodes = new Dictionary<Vector2Int, List<Path>>();
            foreach (KeyValuePair<Path, PathType> pair in this)
            {
                if (pair.Value == PathType.NULL) continue;
                Vector2Int p1 = pair.Key.p1;
                Vector2Int p2 = pair.Key.p2;

                if (!nodes.ContainsKey(p1))
                {
                    nodes.Add(p1, new List<Path>());
                }

                if (!nodes.ContainsKey(p2))
                {
                    nodes.Add(p2, new List<Path>());
                }

                nodes[p1].Add(pair.Key);
                nodes[p2].Add(pair.Key);
            }

            return nodes;
        }

        /// <summary>
        /// Uses Serializable Dictionary packages enumerator. Note use var in a foreach loop, not Path.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return Paths.GetEnumerator();
        }

        /// <summary>
        /// Prints the puzzle's paths. There is no guarenteed order.
        /// </summary>
        /// <returns> returns the paths as a newline delimited string. </returns>
        public override string ToString()
        {
            string output = "";

            foreach (Path path in Paths.Keys)
            {
                output += Paths[path] + ": ";

                switch (Paths[path])
                {
                    case PathType.Connected:    output += " ----- "; break;
                    case PathType.Split:        output += " -- -- "; break;
                    default:                    output += " XXXXX "; break;
                }
                
                output += "\n";
            }

            return output;
        }
        #endregion
    }

    /// <summary>
    /// An undirected path between 2d points
    /// </summary>
    [System.Serializable]
    public class Path
    {
        public Vector2Int p1;
        public Vector2Int p2;

        public Path(Vector2Int point1, Vector2Int point2)
        {
            p1 = point1;
            p2 = point2;
        }

        /// <summary>
        /// Calculates a normalized direction vector from p1 to p2.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetDirection()
        {
            return ((Vector2) p2 - p1).normalized;
        }

        public override bool Equals(object obj)
        {
            Path other = (Path) obj;
            return (p1 == other.p1 && p2 == other.p2) || (p1 == other.p2 && p2 == other.p1);
        }

        // Visual Studio's automagic hash
        public override int GetHashCode()
        {
            int hashCode = 1369944177;
            hashCode = hashCode * -1521134295 + p1.GetHashCode();
            hashCode = hashCode * -1521134295 + p2.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return "(" + p1.x + ", " + p1.y + ") --- (" + p2.x + ", " + p2.y + ")";
        }
    }

    [System.Serializable]
    public class UDictionaryPaths : SerializableDictionary<Path, PathType> { }

    public enum PathType
    {
        NULL = 0,
        Connected = 1,
        Split = 2
    }
};