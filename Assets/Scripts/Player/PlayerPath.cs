using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

public class PlayerPath : MonoBehaviour
{
    #region Public Variables
    [HideInInspector]
    public List<Vector2> Verts = new List<Vector2>();
    #endregion

    #region Exposed Variables
    [Header("Visual Configurations")]
    [Tooltip("Material to be applied to created visuals.")]
    [SerializeField]
    Material _lineMaterial;

    [Tooltip("The animation duration of path start")]
    [SerializeField, Min(0)]
    float _startAnimationTime = .15f;

    [Tooltip("The animation duration of path clearing")]
    [SerializeField, Min(0)]
    float _stopAnimationTime = .2f;

    [Tooltip("The final color of the path before destroying itself. Should be the same as the puzzle path.")]
    [SerializeField]
    Color _finalColor;
    #endregion

    #region Private Variables
    PuzzleRenderer _puzzle;
    List<GameObject> _visuals = new List<GameObject>();
    bool _clearing = false;

    Material _instancedMaterial;
    Color _initialColor;
    #endregion

    void Awake()
    {
        _puzzle = transform.parent.GetComponent<PuzzleRenderer>();
        if (_puzzle == null)
        {
            Debug.LogError("ERROR: unable to find parent PuzzleRenderer.");
        }

        _instancedMaterial = new Material(_lineMaterial.shader);
        _instancedMaterial.CopyPropertiesFromMaterial(_lineMaterial);
        _initialColor = _instancedMaterial.color;
    }

    #region Drawing
    /// <summary>
    ///     Starts drawing PlayerPath at the given coordinate.
    ///     Will not do anything if a current playerPath is still clearing out.
    /// </summary>
    /// <param name="puzzle"></param>
    /// <param name="pos"></param>
    public bool StartPath(Vector2 position)
    {
        if (_clearing) return false;

        ClearPath();
        _instancedMaterial.color = _initialColor;

        Vector3 localPosition = _puzzle.PuzzleToLocal(position);
        GameObject go = MeshLine.DrawStartPoint(_puzzle.configs, transform, localPosition);
        AddVisual(go);

        StartCoroutine(ScaleFromZeroTo(go.transform, Vector3.one * _puzzle.configs.lineWidth * _puzzle.configs.startNodeSize, _startAnimationTime));
        return true;
    }

    /// <summary>
    ///     Begins destroying the path. _clearing is true while doing so.
    /// </summary>
    public void StopPath()
    {
        _clearing = true;

        StartCoroutine(FadeAway(_instancedMaterial, _finalColor, _stopAnimationTime));
        StartCoroutine(WaitBeforeCallback(_stopAnimationTime, ClearPath));

        StartCoroutine(WaitBeforeCallback(_stopAnimationTime, () =>
        {
            _clearing = false;
        }));
    }

    /// <summary>
    ///     Adds vert if appropriate. Assumes input does not include self-intersecting lines.
    /// </summary>
    /// <param name="position"> Desired marker position in puzzle space </param>
    /// <param name="target"> The target puzzle position </param>
    public void SetMarker(Vector2 position, Vector2 target)
    {
        if (!Verts.Contains(position) && (Verts.Count == 0 || Verts.Count > 0 && Verts[Verts.Count - 1] != target))
        {
            Verts.Add(position);
        }
        // case: retracing
        else if (Verts.Count > 1 && Verts[Verts.Count - 1] == position && Verts[Verts.Count - 2] == target)
        {
            RemoveMarker();
            return;
        }
        // case: 
        else if (!Verts.Contains(position) && Verts.Count > 0 && Verts[Verts.Count - 1] == target)
        {
            return;
        }
        // case: switch directions from previous intersection
        else
        {
            RemoveLastLine();
        }

        // snaps previous line segment to end point and starts next line segment
        UpdatePath(position);
        CreateLine(position, target);
    }

    /// <summary>
    ///     Updates the last line segment towards the given position.
    ///     Project position onto the segments direction vector.
    /// </summary>
    /// <param name="position"></param>
    public void UpdatePath(Vector2 position)
    {
        if (_visuals.Count < 2 || _clearing) return;

        Transform lineT = _visuals[_visuals.Count - 2].transform;
        Transform capT = _visuals[_visuals.Count - 1].transform;

        Vector3 localPosition = _puzzle.PuzzleToLocal(position);
        Vector3 localStart = _puzzle.transform.InverseTransformPoint(lineT.position);
        Vector3 localEnd = Vector3.Project(localPosition - localStart, lineT.up) + localStart;

        Vector3 nScal = lineT.localScale;
        nScal.y = Vector3.Distance(localStart, localEnd);
        lineT.localScale = nScal;
        capT.localPosition = localEnd;
    }

    /// <summary>
    ///     Pops the last marker
    /// </summary>
    void RemoveMarker()
    {
        if (Verts.Count == 0) return;
        Verts.RemoveAt(Verts.Count - 1);

        RemoveLastLine();
    }
    #endregion

    #region Utility
    /// <summary>
    ///     Destroys visuals composing this path.
    /// </summary>
    void ClearPath()
    {
        foreach (GameObject go in _visuals)
        {
            Destroy(go);
        }
        _visuals.Clear();
        Verts.Clear();
    }

    /// <summary>
    ///     Removes the last 2 gameObjects in _visuals, if able to.
    /// </summary>
    void RemoveLastLine()
    {
        if (_visuals.Count > 1)
        {
            int lastIndex = _visuals.Count - 1;
            Destroy(_visuals[lastIndex]);
            Destroy(_visuals[lastIndex - 1]);

            _visuals.RemoveRange(lastIndex - 1, 2);
        }
    }

    /// <summary>
    ///     Creates a line at position towards the target. Defaults length to _puzzle.configs.linewidth / 2
    /// </summary>
    /// <param name="position"> </param>
    /// <param name="target"> </param>
    void CreateLine(Vector2 position, Vector2 target)
    {
        Vector3 localPosition = _puzzle.PuzzleToLocal(position);
        Vector3 localEnd = localPosition + (Vector3)(target - position) * _puzzle.configs.lineWidth / 2;
        GameObject[] lineVisuals = MeshLine.DrawLineRounded(_puzzle.configs, transform, localPosition, localEnd, false, true);

        foreach (GameObject vis in lineVisuals)
        {
            AddVisual(vis);
        }
    }

    /// <summary>
    ///     Configures prefab instance. Assumes the visual is a child of this object.
    ///     Ignores null GameObjects.
    /// </summary>
    /// <param name="go"></param>
    void AddVisual(GameObject go)
    {
        if (go == null) return;
        _visuals.Add(go);

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        go.transform.GetComponentInChildren<Renderer>().material = _instancedMaterial;
    }
    #endregion

    #region Coroutines
    IEnumerator ScaleFromZeroTo(Transform t, Vector3 finalScale, float duration)
    {
        float timestamp = Time.time;

        while (Time.time - timestamp < duration)
        {
            t.localScale = Vector3.Lerp(Vector3.zero, finalScale, (Time.time - timestamp) / duration);
            yield return null;
        }

        t.localScale = finalScale;
    }

    IEnumerator FadeAway(Material mat, Color fColor, float duration)
    {
        float timestamp = Time.time;
        Color nColor = _initialColor;

        while (Time.time - timestamp < duration)
        {
            float t = (Time.time - timestamp) / duration;
            mat.color = Color.Lerp(nColor, fColor, t);
            yield return null;
        }

        mat.color = fColor;
    }

    IEnumerator WaitBeforeCallback(float waitTime, System.Action callback)
    {
        yield return new WaitForSeconds(waitTime);
        callback();
    }
    #endregion
}