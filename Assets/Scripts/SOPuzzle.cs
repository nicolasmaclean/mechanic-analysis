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
            foreach (KeyValuePair<Path, PathType> entry in Paths)
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