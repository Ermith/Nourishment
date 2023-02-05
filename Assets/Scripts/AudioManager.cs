using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1;
    [Range(.1f, 3f)]
    public float pitch = 1;

    [HideInInspector]
    public AudioSource AudioSource;
}

public class AudioManager : MonoBehaviour
{
    public Sound[] Sounds;

    private void Awake()
    {
        foreach(Sound s in Sounds)
        {
            s.AudioSource = gameObject.AddComponent<AudioSource>();
            s.AudioSource.clip = s.clip;
            s.AudioSource.volume = s.volume;
            s.AudioSource.pitch = s.pitch;
        }
    }

    public void Play(string name)
    {
        foreach (Sound s in Sounds)
            if (s.Name == name)
            {
                s.AudioSource.Play();
                return;
            }
    }
}
