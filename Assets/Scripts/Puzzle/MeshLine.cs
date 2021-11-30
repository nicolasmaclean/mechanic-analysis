using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshLine : MonoBehaviour
{
    #region PuzzleConfigs Wrappers
    public static GameObject DrawStartPoint(in PuzzleConfigs configs, Transform parent, Vector3 pos)
    {
        return DrawStartPoint(configs.startPrefab, parent, pos, configs.startNodeSize, configs.lineWidth);
    }

    public static GameObject DrawConnectedPath(in PuzzleConfigs configs, Transform parent, Vector3 start, Vector3 end)
    {
        return DrawConnectedPath(configs.quadPrefab, parent, start, end, configs.lineWidth);
    }

    public static GameObject[] DrawSplitPath(in PuzzleConfigs configs, Transform parent, Vector3 start, Vector3 end)
    {
        return DrawSplitPath(configs.quadPrefab, parent, start, end, configs.lineWidth, configs.splitGap);
    }

    public static GameObject DrawRoundedCorner(in PuzzleConfigs configs, Transform parent, Vector3 pos, float angle)
    {
        return DrawRoundedCorner(configs.cornerPrefab, parent, pos, configs.lineWidth, angle);
    }

    public static GameObject DrawSharpCorner(in PuzzleConfigs configs, Transform parent, Vector3 pos)
    {
        return DrawSharpCorner(configs.quadPrefab, parent, pos, configs.lineWidth);
    }

    public static GameObject[] DrawLineRounded(in PuzzleConfigs configs, Transform parent, Vector3 start, Vector3 end, bool first = true, bool last = true)
    {
        return DrawLineRounded(configs.quadPrefab, configs.capPrefab, parent, start, end, configs.lineWidth, first, last);
    }

    public static GameObject DrawEndCap(in PuzzleConfigs configs, Transform parent, Vector3 pos, float angle)
    {
        return DrawEndCap(configs.capPrefab, parent, pos, configs.lineWidth, angle);
    }

    /// <summary>
    ///     Draws a series of rouneded lines through the given verts. Will also create a start point at the first vert.
    /// </summary>
    /// <param name="configs"></param>
    /// <param name="parent"></param>
    /// <param name="verts"></param>
    /// <returns></returns>
    public static GameObject[] DrawStroke(in PuzzleConfigs configs, Transform parent, Vector3[] verts)
    {

        if (verts.Length > 0)
        {
            GameObject[] gos = new GameObject[1 + 2 * verts.Length];

            Vector3 prev = verts[0];
            gos[0] = DrawStartPoint(configs, parent, prev);

            for (int i = 1; i < verts.Length; i++)
            {
                GameObject[] lineGos = DrawLineRounded(configs, parent, prev, verts[i], false, true);
                gos[1 + i * 2] = lineGos[0];
                gos[2 + i * 2] = lineGos[2];

                prev = verts[i];
            }

            return gos;
        }

        return new GameObject[0];
    }

    public static GameObject DrawLine(in PuzzleConfigs configs, Transform parent, Vector3 start, Vector3 end)
    {
        return DrawLine(configs.quadPrefab, parent, start, end, configs.lineWidth);
    }
    #endregion

    #region Draw
    /// <summary>
    ///     Creates the start point visual
    /// </summary>
    /// <param name="prefab"> A circle prefab to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="pos"> The local space position for the start point </param>
    /// <param name="size"> The size of the visual relative prefabs scale of 1 </param>
    /// <param name="lineWidth"> 
    ///     The width of the lines used to size the start point proportional to the puzzle paths
    /// </param>
    /// <returns> a reference to the created visual </returns>
    public static GameObject DrawStartPoint(GameObject prefab, Transform parent, Vector3 pos, float size, float lineWidth)
    {
        GameObject go = Instantiate(prefab, parent);

        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(lineWidth * size, lineWidth * size, lineWidth * size);

        return go;
    }

    /// <summary>
    ///     Creates a line object upon the given path
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="start"> The local space start point </param>
    /// <param name="end"> The local space end point </param>
    /// <param name="lineWidth"> The width of the given line </param>
    /// <returns> A reference to the created visual </returns>
    public static GameObject DrawConnectedPath(GameObject prefab, Transform parent, Vector3 start, Vector3 end, float lineWidth)
    {
        Vector3[] points =
        {
            start,
            end
        };

        ShortenStroke(ref points, lineWidth / 2, true);
        ShortenStroke(ref points, lineWidth / 2, false);
        return DrawLine(prefab, parent, points[0], points[1], lineWidth);
    }

    /// <summary>
    ///     Creates line objects along the given path with _splitGap space in the middle
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="start"> The local space start point </param>
    /// <param name="end"> The local space end point </param>
    /// <param name="lineWidth"> The width of the given path </param>
    /// <param name="gapWidth"> The size of the gap in the center of the path. Specifically refers the total width </param>
    /// <returns> References to created visuals </returns>
    public static GameObject[] DrawSplitPath(GameObject prefab, Transform parent, Vector3 start, Vector3 end, float lineWidth, float gapWidth)
    {
        Vector3[] stroke1 =
        {
            start,
            Vector3.Lerp(start, end, 0.5f - gapWidth / 2)
        };
        Vector3[] stroke2 = {
            end,
            Vector3.Lerp(end, start, 0.5f - gapWidth / 2)
        };

        ShortenStroke(ref stroke1, lineWidth / 2, true);
        ShortenStroke(ref stroke2, lineWidth / 2, true);

        GameObject[] gos = {
            DrawLine(prefab, parent, stroke1[0], stroke1[1], lineWidth),
            DrawLine(prefab, parent, stroke2[0], stroke2[1], lineWidth)
        };

        return gos;
    }

    /// <summary>
    ///     Creates a rounded corner at the given point with given rotation
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="pos"> The local space corner point </param>
    /// <param name="lineWidth"> The width of the given path </param>
    /// <param name="angle"> The rotation of the prefab around the z axis </param>
    /// <returns> A reference to the created visual </returns>
    public static GameObject DrawRoundedCorner(GameObject prefab, Transform parent, Vector3 pos, float lineWidth, float angle)
    {
        GameObject go = Instantiate(prefab, parent);

        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(lineWidth, lineWidth, lineWidth);
        go.transform.localRotation = Quaternion.Euler(0, 0, angle);

        return go;
    }

    /// <summary>
    /// Creates a square around the given point of length _lineWidth.
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="pos"> The local space position of the sharp corner </param>
    /// <param name="width"> The width of the corner </param>
    /// <returns> A reference to the created visual </returns>
    public static GameObject DrawSharpCorner(GameObject prefab, Transform parent, Vector3 pos, float width)
    {
        pos.y -= width / 2;

        GameObject go = Instantiate(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(width, width, width);

        return go;
    }

    /// <summary>
    ///     Creates a line with rounded caps at either end.
    /// </summary>
    /// <param name="linePrefab"> A quad GameObject to be instantiated </param>
    /// <param name="capPrefab"> A semi-circle GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="start"> The local space position of the start position </param>
    /// <param name="end"> The local space position of the end position </param>
    /// <param name="lineWidth"> The local scale of the line and end caps</param>
    /// <param name="first"> True if the start of the stroke should have an end cap </param>
    /// <param name="last"> True if the end of the stroke should have an end cap </param>
    /// <returns>
    ///     An array of length 3: the first gameobject is the line, the second is the first end cap,
    ///     and the last is the second end cap. If arr[1] or arr[2] is null its corresponding end cap
    ///     was not created.
    /// </returns>
    public static GameObject[] DrawLineRounded(GameObject linePrefab, GameObject capPrefab, Transform parent, Vector3 start, Vector3 end, float lineWidth, bool first = true, bool last = true)
    {
        GameObject[] gos = new GameObject[3];

        gos[0] = DrawLine(linePrefab, parent, start, end, lineWidth);
        if (first)
        {
            gos[1] = DrawEndCap(capPrefab, parent, start, lineWidth, GetLineRotation(end, start, parent.up));
        }
        if (last)
        {
            gos[2] = DrawEndCap(capPrefab, parent, end, lineWidth, GetLineRotation(start, end, parent.up));
        }

        return gos;
    }

    /// <summary>
    ///     Creates an end cap at the given position and returns a reference to it.
    /// </summary>
    /// <param name="prefab"> A semi-circle GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="pos"> The local space position of the end cap </param>
    /// <param name="lineWidth"> The local scale of the end cap </param>
    /// <param name="angle"> The local rotation of the end cap around the z axis</param>
    /// <returns> Reference to the created visuals </returns>
    public static GameObject DrawEndCap(GameObject prefab, Transform parent, Vector3 pos, float lineWidth, float angle)
    {
        return DrawEndCap(prefab, parent, pos, lineWidth, Quaternion.Euler(0, 0, angle));
    }

    /// <summary>
    ///     Creates an end cap at the given position and returns a reference to it.
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="pos"> The local space position of the end cap </param>
    /// <param name="lineWidth"> The local scale of the end cap </param>
    /// <param name="rotation"> The local rotation of the end cap </param>
    /// <returns> Reference to the created visuals </returns>
    public static GameObject DrawEndCap(GameObject prefab, Transform parent, Vector3 pos, float lineWidth, Quaternion rotation)
    {
        GameObject go = Instantiate(prefab, parent);

        go.transform.localPosition = pos;
        go.transform.localRotation = rotation;
        go.transform.localScale = new Vector3(lineWidth, lineWidth, lineWidth);

        return go;
    }

    /// <summary>
    ///     Creates a quad that spans the given 2 points
    ///     Will be created in the local xy plane of the given transform.
    /// </summary>
    /// <param name="prefab"> A quad GameObject to be instantiated </param>
    /// <param name="parent"> A parent transform </param>
    /// <param name="start"> The local space start point </param>
    /// <param name="end"> The local space end point</param>
    /// <param name="lineWidth"> The width of the line relative to 1 local space unit for a value of 1 </param>
    /// <returns> A reference to the created visual </returns>
    public static GameObject DrawLine(GameObject prefab, Transform parent, Vector3 start, Vector3 end, float lineWidth)
    {
        GameObject go = Instantiate(prefab, parent);

        // set position
        go.transform.localPosition = start;

        // set rotation
        go.transform.localRotation = GetLineRotation(start, end, parent.up);

        // set scale
        Vector3 scal = Vector3.one;
        scal.x = lineWidth;
        scal.y = Vector3.Distance(end, start);
        go.transform.localScale = scal;

        return go;
    }
    #endregion

    #region Utility
    /// <summary>
    ///     Calculates the rotation between the start and end.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="up"></param>
    /// <returns> The quaternion representation of the calculated direction vector. </returns>
    public static Quaternion GetLineRotation(Vector2 start, Vector2 end, Vector2 up)
    {
        return Quaternion.Euler(0, 0, Vector2.SignedAngle(up, end - start));
    }

    /// <summary>
    ///     Moves the desired point to shorten the stroke.
    ///     The point internal to that being manipulated will not be affected.
    /// </summary>
    /// <param name="stroke"> The local space coordinates of a stroke </param>
    /// <param name="amount"> The amount to shorten the stroke by in local space </param>
    /// <param name="first"> Selects which end of the stroke to manipulate. True will manipulate stroke[0] </param>
    public static void ShortenStroke(ref Vector3[] stroke, float amount, bool first = true)
    {
        if (stroke.Length < 2)
        {
            throw new System.Exception("Unable to shorten stroke: the given stroke is too short.");
        }

        Vector3 dir;
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

        if (first)
        {
            stroke[0] -= dir;
        }
        else
        {
            stroke[stroke.Length - 1] -= dir;
        }
    }

    /// <summary>
    ///     Wrapper of Shorten Stroke: Moves the desired point to shorten the stroke.
    ///     The point internal to that being manipulated will not be affected.
    /// </summary>
    /// <param name="stroke"> The local space coordinates of a stroke </param>
    /// <param name="amount"> The amount to shorten the stroke by in local space </param>
    /// <param name="first"> Selects which end of the stroke to manipulate. True will manipulate stroke[0] </param>
    public static void LengthenStroke(ref Vector3[] stroke, float amount, bool first = true)
    {
        ShortenStroke(ref stroke, -amount, first);
    }
    #endregion
}