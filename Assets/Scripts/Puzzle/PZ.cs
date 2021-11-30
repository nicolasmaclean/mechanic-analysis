using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PZ : IEnumerable
{
    #region Data
    public Dictionary<Path, PathType> Paths;
    public Vector2Int Size { get; private set; } = Vector2Int.zero;
    public List<Vector2Int> StartNodes;
    public Dictionary<Vector2Int, Direction> EndNodes;
    #endregion

    public PZ(TextAsset puzzleJSON)
    {
        LoadFromJSON(puzzleJSON);
    }

    #region Dynamics
    /// <summary>
    ///     Parse the given JSON file and populates this maze
    /// </summary>
    /// <param name="file"></param>
    public void LoadFromJSON(TextAsset file)
    {
        MazeJSON json = JsonUtility.FromJson<MazeJSON>(file.text);

        Paths = new Dictionary<Path, PathType>();
        StartNodes = new List<Vector2Int>();
        EndNodes = new Dictionary<Vector2Int, Direction>();

        // parse adjacency paths
        string[] nums;
        foreach (string str in json.adjacencies)
        {
            nums = str.Split(' ');

            PathType type = GetPathTypeFromString(nums[0]);
            Vector2Int p1 = strToVec2Int(nums[1], nums[2]);
            Vector2Int p2 = strToVec2Int(nums[3], nums[4]);
            Path p = new Path(p1, p2);

            if (!Paths.ContainsKey(p))
            {
                Paths.Add(p, type);
            }
            else
            {
                Debug.LogWarning($"WARNING: \'{file.name}\' has a repeat path entry. Type \'{type}\' will be ignored and the previous type \'{Paths[p]}\' will remain.");
            }
        }

        // parse for start nodes
        foreach (string str in json.startNodes)
        {
            nums = str.Split(' ');
            Vector2Int point = strToVec2Int(nums[0], nums[1]);
            StartNodes.Add(point);
        }

        // parse for end node
        foreach (string str in json.endNodes)
        {
            nums = str.Split(' ');

            Direction dir = GetDirectionFromString(nums[0]);
            Vector2Int point = strToVec2Int(nums[1], nums[2]);

            if (!EndNodes.ContainsKey(point))
            {
                EndNodes.Add(point, dir);
            }
            else
            {
                Debug.LogWarning($"WARNING: \'{file.name}\' has a repeat end node entry. Direction \'{dir}\' will be ignored and the previous direction \'{EndNodes[point]}\' will remain.");
            }
        }

        // parse for size
        Size = new Vector2Int(json.width, json.height);

        Debug.Log($"Successfully load puzzle from file \'{file.name}\'.");

        Vector2Int strToVec2Int(string str1, string str2)
        {
            return new Vector2Int(int.Parse(str1), int.Parse(str2)); ;
        }
    }

    /// <summary>
    /// Clears the puzzle's paths
    /// </summary>
    public void ClearPaths()
    {
        Paths.Clear();
        Size = Vector2Int.zero;
    }

    /// <summary>
    /// Parses the puzzle entries to update Size.
    /// The min node is assumed to be 0.
    /// </summary>
    public void UpdateSize()
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
    /// Ignores start nodes, but includes additional paths created for end nodes.
    /// </summary>
    /// <returns>
    /// The key of the dictionary is the node central in the corner
    /// and the array contains its 2 non-parallel neighbors.
    /// </returns>
    public Dictionary<Vector2Int, Vector2Int[]> GetCorners()
    {
        // search nodes for corners
        Dictionary<Vector2Int, Vector2Int[]> corners = new Dictionary<Vector2Int, Vector2Int[]>();
        foreach (KeyValuePair<Vector2Int, List<Path>> node in GetAdjacencyListPaths())
        {
            // includes paths that will be made for end nodes
            if (EndNodes.ContainsKey(node.Key))
            {
                node.Value.Add(GetEndPath(node.Key));
            }

            if (node.Value.Count == 2 && !StartNodes.Contains(node.Key))
            {
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
    /// Creates and returns the adjacency list of the puzzle.
    /// </summary>
    /// <returns></returns>
    public Dictionary<Vector2Int, List<Path>> GetAdjacencyListPaths()
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

    public Dictionary<Vector2Int, List<Vector2Int>> GetAdjacencyList()
    {
        Dictionary<Vector2Int, List<Vector2Int>> nodes = new Dictionary<Vector2Int, List<Vector2Int>>();
        foreach (KeyValuePair<Path, PathType> pair in this)
        {
            if (pair.Value == PathType.NULL) continue;
            Vector2Int p1 = pair.Key.p1;
            Vector2Int p2 = pair.Key.p2;

            if (!nodes.ContainsKey(p1))
            {
                nodes.Add(p1, new List<Vector2Int>());
            }

            if (!nodes.ContainsKey(p2))
            {
                nodes.Add(p2, new List<Vector2Int>());
            }

            nodes[p1].Add(p2);
            nodes[p2].Add(p1);
        }

        return nodes;
    }

    /// <summary>
    ///     Creates a normalized vector corresponding to the requested direction
    /// </summary>
    /// <param name="dir"></param>
    /// <returns> The normalized direction vector. Return Vector2.zero if direction is NULL. </returns>
    public static Vector2 GetDirectionVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                return Vector2.up;

            case Direction.Down:
                return Vector2.down;

            case Direction.Right:
                return Vector2.right;

            case Direction.Left:
                return Vector2.left;

            default:
            case Direction.NULL:
                return Vector2.zero;
        }
    }

    /// <summary>
    /// Creates a path from the end point to the adjacent node in the direction stored.
    /// </summary>
    /// <param name="endPoint"></param>
    /// <returns> A path between end point and discussed adjacent node, respectively. </returns>
    public Path GetEndPath(Vector2Int endPoint)
    {
        Vector2Int pathDir;
        switch (EndNodes[endPoint])
        {
            case Direction.Down:
                pathDir = new Vector2Int(endPoint.x, endPoint.y - 1);
                break;

            case Direction.Left:
                pathDir = new Vector2Int(endPoint.x - 1, endPoint.y);
                break;

            case Direction.Right:
                pathDir = new Vector2Int(endPoint.x + 1, endPoint.y);
                break;

            default:
            case Direction.Up:
                pathDir = new Vector2Int(endPoint.x, endPoint.y + 1);
                break;
        }

        Path p = new Path(endPoint, pathDir);
        if (Paths.ContainsKey(p))
        {
            throw new System.Exception($"ERROR: unable to create end point {p.p1}. There is an adjacent node at {p.p2}.");
        }

        return p;
    }

    /// <summary>
    /// Uses Serializable Dictionary packages enumerator. Note use var in a foreach loop, not Path.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetEnumerator()
    {
        return Paths.GetEnumerator();
    }

    public static PathType GetPathTypeFromString(string str)
    {
        switch (str)
        {
            case "connected":
                return PathType.Connected;

            case "split":
                return PathType.Split;

            default:
            case "NULL":
                return PathType.NULL;
        }
    }

    public static Direction GetDirectionFromString(string str)
    {
        switch (str)
        {
            case "up":
                return Direction.Up;

            case "right":
                return Direction.Right;

            case "down":
                return Direction.Down;

            case "left":
                return Direction.Left;

            default:
                return Direction.NULL;
        }
    }
    #endregion
}

#region structs
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
        return ((Vector2)p2 - p1).normalized;
    }

    public override bool Equals(object obj)
    {
        Path other = (Path)obj;
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
}

[System.Serializable]
struct MazeJSON
{
    public string[] adjacencies;
    public string[] startNodes;
    public string[] endNodes;
    public int width;
    public int height;
}
#endregion

#region enums
public enum PathType
{
    NULL = 0,
    Connected = 1,
    Split = 2,
    End = 3
}

/// <summary>
/// The integer equivalent to each values are picked, so a sum of a direction and 2 times another direction is a unique integer
/// </summary>
public enum Direction
{
    NULL = -1, Up = 0, Down = 3, Right = 9, Left = 27
}
#endregion