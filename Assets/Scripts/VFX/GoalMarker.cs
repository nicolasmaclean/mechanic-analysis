using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
public class GoalMarker : MonoBehaviour
{
    #region Exposed Variables
    [Header("VFX")]
    [Tooltip("The length of a single cycle of the animation")]
    [SerializeField]
    float _cycleDuration = .7f;

    [Tooltip("The scale of the sprite at the end of each cycle.")]
    [SerializeField]
    float _finalScale = 0.3f;

    [Header("SFX")]
    [Tooltip("Percent for volume to decay by each playback.")]
    [SerializeField, Range(0, 1)]
    float _decayAmount = .2f;

    [Tooltip("Amount of seconds before each playback.")]
    [SerializeField]
    float _playbackDelay = .25f;

    [Tooltip("The minimum volume for a playback")]
    [SerializeField, Range(0, 1)]
    float _minVolume = .05f;

    [Header("General")]
    [SerializeField]
    Player _player;

    [Tooltip("")]
    [SerializeField]
    bool _isStartMarker = false;

    [Tooltip("")]
    [SerializeField]
    bool _isEndMarker = false;
    #endregion

    #region Private Variables
    SpriteRenderer _renderer;
    AudioSource _source;

    Color _nColor;
    float _initialVolume;

    float _elapsedTime = 0;
    bool _playingSFX = false;
    #endregion

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _source = GetComponent<AudioSource>();

        _nColor = _renderer.color;
        _initialVolume = _source.volume;

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if ( (_isStartMarker && _player.State == PlayerState.LookingAtPuzzle && !_player.Won) || (_isEndMarker && !_player.AtEnd && _player.State == PlayerState.Drawing) )
        {
            UpdateVFX();

            if (!_playingSFX && _source.clip != null)
            {
                _playingSFX = true;
                StartCoroutine(EchoSource(_playbackDelay, _decayAmount, _minVolume));
            }

        }
        else
        {
            _elapsedTime = 0;
            transform.localScale = Vector3.zero;

            _source.volume = _initialVolume;
            _playingSFX = false;
            StopAllCoroutines();
        }
    }

    void UpdateVFX()
    {
        float t = _elapsedTime / _cycleDuration;
        t = Mathf.Clamp(2f * t - 1f, 0, 1);

        transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * _finalScale, t);
        _nColor.a = Mathf.Cos(Mathf.PI * t / 2);
        _renderer.color = _nColor;

        _elapsedTime = (_elapsedTime + Time.deltaTime) % _cycleDuration;
    }

    IEnumerator EchoSource(float wait, float decayMult, float threshold)
    {
        yield return new WaitForSeconds(wait);

        _source.Play();
        yield return new WaitForSeconds(_source.clip.length);

        _source.volume *= decayMult;

        if (_source.volume >= threshold)
        {
            StartCoroutine(EchoSource(wait, decayMult, threshold));
        }
    }
}