using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    void Awake()
    {
        foreach(Sound s in sounds){
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void ChangeVolume(string name, float newVolume){
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if(newVolume >= 0 && newVolume <=1f)
            s.source.volume = newVolume;
    }

    public float CurrentVolume(string name){
        Sound s = Array.Find(sounds, sound => sound.name == name);
        return s.source.volume;
    }

    public void Play(string name){
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
    }

    public void Stop(string name){
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }

    public bool IsPlaying(string name){
        Sound s = Array.Find(sounds, sound => sound.name == name);
        return s.source.isPlaying;
    }
}
