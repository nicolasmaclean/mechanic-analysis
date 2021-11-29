using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class VFXGoalMarker : MonoBehaviour
{
    #region Exposed Variables
    [Tooltip("The length of a single cycle of the animation")]
    [SerializeField]
    float _cycleDuration = .7f;

    [Tooltip("The scale of the sprite at the end of each cycle.")]
    [SerializeField]
    float _finalScale = 0.3f;

    [SerializeField]
    Puzzle.Player _player;

    [Tooltip("")]
    [SerializeField]
    bool _isStartMarker = false;

    [Tooltip("")]
    [SerializeField]
    bool _isEndMarker = false;
    #endregion

    #region Private Variables
    SpriteRenderer _renderer;
    Color _nColor;
    float elapsedTime = 0;
    #endregion

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _nColor = _renderer.color;

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if ( (_isStartMarker && _player.State == Puzzle.PlayerState.LookingAtPuzzle && !_player.Won) || (_isEndMarker && !_player.AtEnd && _player.State == Puzzle.PlayerState.Drawing) )
        {
            float t = elapsedTime / _cycleDuration;
            t = Mathf.Clamp(2f * t - 1f, 0, 1);

            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * _finalScale, t);
            _nColor.a = Mathf.Cos(Mathf.PI * t / 2);
            _renderer.color = _nColor;

            elapsedTime = (elapsedTime + Time.deltaTime) % _cycleDuration;
        }
        else
        {
            elapsedTime = 0;
            transform.localScale = Vector3.zero;
        }
    }
}