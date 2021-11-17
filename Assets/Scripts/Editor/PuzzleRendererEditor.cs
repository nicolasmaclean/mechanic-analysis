using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Puzzle;

[CustomEditor(typeof(PuzzleRenderer))]
public class PuzzleRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        PuzzleRenderer renderer = (PuzzleRenderer) target;

        if (GUILayout.Button("Render"))
        {
            renderer.CreatePuzzle();
        }

        if (GUILayout.Button("Clear"))
        {
            renderer.ClearLines();
        }
    }
}