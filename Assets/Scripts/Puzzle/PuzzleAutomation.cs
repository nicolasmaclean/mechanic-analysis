using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle;

public class PuzzleAutomation : MonoBehaviour
{
    [Tooltip("SOPuzzle to be mutated.")]
    [SerializeField]
    SOPuzzle _puzzle;

    [Tooltip("WARNING: if enabled, the given puzzle WILL be mutated.")]
    [SerializeField]
    bool _mutate = false;

    [Tooltip("Size of grid to be filled")]
    [SerializeField]
    int _size;

    void Awake()
    {
        if (_mutate && _puzzle != null)
        {
            int insertions = FillGrid();
            _puzzle.UpdateSize();
            Debug.Log($"{_puzzle.name} has been overwritten. {insertions} path insertions were made.");
        }
        else
        {
            Debug.LogError("Puzzle Automation is activated, but there was no given puzzle to mutate.");
        }
    }

    /// <summary>
    /// Fills the puzzle with a square grid of connected paths.
    /// </summary>
    /// <returns> the number of paths inserted </returns>
    int FillGrid()
    {
        int counter = 0;

        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                Vector2Int curPos = new Vector2Int(x, y);

                if (y < _size-1)
                {
                    Path path1 = new Path(curPos, curPos + Vector2Int.up);
                    if (!_puzzle.Paths.Contains(path1))
                    {
                        counter++;
                        _puzzle.Paths.Add(path1, PathType.Connected);
                    }
                }

                if (x < _size-1)
                {
                    Path path2 = new Path(curPos, curPos + Vector2Int.right);
                    if (!_puzzle.Paths.Contains(path2))
                    {
                        counter++;
                        _puzzle.Paths.Add(path2, PathType.Connected);
                    }
                }
            }
        }

        return counter;
    }
}