using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    ///     Creates gameobject to play given clip.
    ///     Destroys object when done.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="volume"></param>
    /// <returns></returns>
    public GameObject PlayClip(AudioClip clip, float volume)
    {
        GameObject go = new GameObject($"SFX_{clip.name}");
        go.transform.parent = transform;
        AudioSource source = go.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.Play();
        Destroy(go, clip.length);

        return go;
    }

    /// <summary>
    ///     Creates gameobject to play given clip.
    ///     Destroys object when done.
    ///     Will perform callback after clip is created.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public void PlayClip(AudioClip clip, System.Action<GameObject> callback)
    {
        GameObject go = PlayClip(clip, 1);
        callback(go);
    }
}