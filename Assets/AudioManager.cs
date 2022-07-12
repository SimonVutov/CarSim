using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    public GameObject Vehicle;

    float pitch;
    float volume;
    
    void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;

            s.source.loop = s.loop;
        }
        StartCoroutine(Change());
    }

    public void Update()
    {
        foreach (Sound s in sounds)
        {
            if (s.name == "Car")
            {
                s.source.volume = Mathf.Clamp(Vehicle.GetComponent<LoadCar>().clone.GetComponentInChildren<Vehicle>().currentEngineRPM / 7500f - 0.5f - (Vehicle.GetComponent<LoadCar>().clone.GetComponentInChildren<Vehicle>().wheelBrake / 4) + (Vehicle.GetComponent<LoadCar>().clone.GetComponentInChildren<Vehicle>().wheelThrottle / 4), 0.4f, 1f);
                s.source.pitch = Mathf.Lerp(s.source.pitch, Mathf.Clamp(Mathf.Sqrt(Vehicle.GetComponent<LoadCar>().clone.GetComponentInChildren<Vehicle>().currentEngineRPM) / 40f + 0.05f, 0.05f, 15f), 0.35f);
            }
            if (s.name == "Pop")
            {
                s.source.volume = pitch;
                s.source.pitch = volume;
            }
        }
    }

    public void Play (string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
    }

    IEnumerator Change()
    {
        pitch = UnityEngine.Random.Range(0.01f, 1.3f);
        volume = UnityEngine.Random.Range(0.01f, 2f);

        yield return new WaitForSeconds(0.2f);
        StartCoroutine(Change());
    }

}
