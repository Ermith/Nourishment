using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;
    public AudioClip Clip;

    [Range(0f, 1f)]
    public float Volume = 1;
    [Range(.1f, 3f)]
    public float Pitch = 1;

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
            s.AudioSource.clip = s.Clip;
            s.AudioSource.volume = s.Volume;
            s.AudioSource.pitch = s.Pitch;
        }
    }

    public void Play(string name, bool loop = false)
    {
        foreach (Sound s in Sounds)
            if (s.Name == name)
            {
                s.AudioSource.loop = loop;
                s.AudioSource.Play();
                return;
            }
    }
}
