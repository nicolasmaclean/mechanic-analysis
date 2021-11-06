using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Puzzle")]
    public class SOPuzzle : ScriptableObject
    {
        private Dictionary<Path, PathType> _paths = new Dictionary<Path, PathType>();

        /// <summary>
        /// Adds/updates the given path and its type. Order of points does not matter.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="pathType"></param>
        /// <returns> true if the value was added and false if the value was updated. </returns>
        public bool AddPath(Vector2Int p1, Vector2Int p2, PathType pathType)
        {
            Path path = new Path(p1, p2);

            // case: create new entry
            if (!_paths.ContainsKey(path))
            {
                _paths.Add(path, pathType);
                return true;
            }
            // case: update entry
            else
            {
                _paths[path] = pathType;
                return false;
            }
        }

        /// <summary>
        /// Gets the pathtype of the given path entry.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns> returns pathtype. Default to PathType.NULL </returns>
        public PathType GetPath(Vector2Int start, Vector2Int end)
        {
            Path path = new Path(start, end);

            // case: create new entry
            if (_paths.ContainsKey(path))
            {
                return _paths[path];
            }
            // case: update entry
            else
            {
                return PathType.NULL;
            }
        }

        /// <summary>
        /// Removes the path from the puzzle
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns> returns true if there was a path to remove. Otherwise returns false. </returns>
        public bool RemovePath(Vector2Int start, Vector2Int end)
        {
            Path path = new Path(start, end);
            return _paths.Remove(path); 
        }

        /// <summary>
        /// Clears the puzzle's paths
        /// </summary>
        public void ClearPaths()
        {
            _paths = new Dictionary<Path, PathType>();
        }

        /// <summary>
        /// Performs callback for each path in the puzzle.
        /// </summary>
        /// <param name="callback"> Void callback that is given a path and its type. </param>
        public void Foreach(System.Action<Path, PathType> callback)
        {
            foreach (Path path in _paths.Keys)
            {
                callback(path, GetPath(path.p1, path.p2));
            }
        }

        /// <summary>
        /// Prints the puzzle's paths. There is no guarenteed order.
        /// </summary>
        /// <returns> returns the paths as a newline delimited string. </returns>
        public override string ToString()
        {
            string output = "";

            foreach (Path path in _paths.Keys)
            {
                output += _paths[path] + ": ";
                output += path;
                output += "\n";
            }

            return output;
        }
    }

    /// <summary>
    /// An undirected path between 2d points
    /// </summary>
    public struct Path
    {
        public Vector2Int p1;
        public Vector2Int p2;

        public Path(Vector2Int point1, Vector2Int point2)
        {
            p1 = point1;
            p2 = point2;
        }

        public override bool Equals(object obj)
        {
            Path other = (Path) obj;
            return (p1 == other.p1 && p2 == other.p2) || (p1 == other.p2 && p2 == other.p1);
        }

        public override string ToString()
        {
            return "(" + p1.x + ", " + p1.y + ") --- (" + p2.x + ", " + p2.y + ")";
        }
    }

    public enum PathType
    {
        NULL = 0,
        Connected = 1,
        Split = 2
    }
}