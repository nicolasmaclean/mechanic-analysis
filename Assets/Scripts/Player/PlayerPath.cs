using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

public class PlayerPath : MonoBehaviour
{
    public List<Vector2> verts = new List<Vector2>();

    List<GameObject> visuals = new List<GameObject>();
    PuzzleRenderer _puzzle = null;

    void Start()
    {
        _puzzle = transform.parent.GetComponent<PuzzleRenderer>();

        //verts.Add(Vector2.zero);
        //verts.Add(Vector2.right);
        //verts.Add(Vector2.one);

        CreatePath();
    }

    void CreatePath()
    {
        //MeshLine.DrawStartPoint(_puzzle.configs, _puzzle.transform, Vector3.zero);
    }
}