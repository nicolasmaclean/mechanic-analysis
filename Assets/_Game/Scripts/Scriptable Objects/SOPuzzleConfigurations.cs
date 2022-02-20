using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Puzzle
{
    [CreateAssetMenu(menuName = "Puzzle/Puzzle Renderer Configurations")]
    public class SOPuzzleConfigurations : ScriptableObject
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