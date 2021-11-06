using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Puzzle;

[CustomEditor(typeof(SOPuzzle))]
public class PathsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //serializedObject.Update();
        base.OnInspectorGUI();
        SOPuzzle puzzle = (SOPuzzle)target;

        #region buttons
        // temporary buttons for debugging
        if (GUILayout.Button("Add Path"))
        {
            Debug.Log("Adding Path");
            Vector2Int defaultPos = new Vector2Int(-1, -1);
            puzzle.AddPath(defaultPos, defaultPos, PathType.Split);
        }

        if (GUILayout.Button("Print Puzzle"))
        {
            Debug.Log(puzzle.ToString());
        }

        if (GUILayout.Button("Reset"))
        {
            Debug.Log("Puzzle has been reset");
            puzzle.ClearPaths();
        }
        #endregion

        #region paths dictionary
        // used batching because directly updating dictionary values in the foreach loop
        // would not allow live editing of path points or types
        // lists for batching
        List<Path> toRemove = new List<Path>();
        List<Path> pathsToAdd = new List<Path>();
        List<PathType> pathTypesToAdd = new List<PathType>();

        // maze path dictionary display
        EditorGUILayout.LabelField("Maze");
        puzzle.Foreach((path, pathType) =>
        {
            // fields for viewing/editing
            Vector2Int p1 = EditorGUILayout.Vector2IntField("p1", path.p1);
            Vector2Int p2 = EditorGUILayout.Vector2IntField("p2", path.p2);
            PathType pt = (PathType) EditorGUILayout.IntField("path type", (int) pathType);

            // cache paths and pathtype for batching removing/adding paths
            Path oPath = new Path(path.p1, path.p2);
            Path nPath = new Path(p1, p2);
            toRemove.Add(oPath);
            pathsToAdd.Add(nPath);
            pathTypesToAdd.Add(pt);
        });

        // batch removal of old paths and addition of new paths
        for (int i = 0; i < toRemove.Count; i++)
        {
            puzzle.RemovePath(toRemove[i].p1, toRemove[i].p2);
            puzzle.AddPath(pathsToAdd[i].p1, pathsToAdd[i].p2, pathTypesToAdd[i]);
        }
        #endregion

        serializedObject.ApplyModifiedProperties();
    }
}