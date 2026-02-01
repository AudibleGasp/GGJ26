using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager
{
    public static AudioManager Instance;
    
    private static readonly int[] semitoneOffsets = { -2, -1, 0, 1, 2 };
    private static readonly float[] detuneOffsets = { -0.02f, -0.01f, 0, 0.01f, 0.02f };
    private const float defaultTimeThreshold = 0.1f;

    private readonly AudioSource rawSource;
    private readonly AudioSource[] randomizedSources;

    private class AudioEvent
    {
        public readonly AudioClip[] Clips;
        public float LastPlayedTime;

        public AudioClip GetRandom()
        {
            return Clips.Length == 1 ? Clips[0] : Clips[Random.Range(0, Clips.Length)];
        }

        public AudioEvent(List<AudioClip> clips)
        {
            Clips = clips.ToArray();
            LastPlayedTime = 0;
        }
    }

    private readonly Dictionary<string, AudioEvent> clipDictionary;

    public AudioManager(float masterVolume = 1f, float musicVolume = 1f, float soundVolume = 1f)
    {
        Instance = this;
        
        Transform audioContainer = new GameObject("Audio Root").transform;

        rawSource = new GameObject("Raw Source").AddComponent<AudioSource>();
        rawSource.playOnAwake = false;
        rawSource.loop = false;

        rawSource.transform.SetParent(audioContainer);

        randomizedSources = new AudioSource[semitoneOffsets.Length];
        for (int i = 0; i < semitoneOffsets.Length; i++)
        {
            foreach (float detune in detuneOffsets)
            {
                randomizedSources[i] = new GameObject($"Semitone {semitoneOffsets[i]}, Detune {detune}")
                    .AddComponent<AudioSource>();
                randomizedSources[i].playOnAwake = false;
                randomizedSources[i].loop = false;

                float semitone = semitoneOffsets[i];
                float pitch = Mathf.Pow(2f, semitone / 12f); // convert semitone offset to pitch multiplier
                randomizedSources[i].pitch = pitch + detune;

                randomizedSources[i].transform.SetParent(audioContainer);
            }
        }

        AudioClip[] audioClips = Resources.LoadAll<AudioClip>("Sounds");

        clipDictionary = new Dictionary<string, AudioEvent>();

        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 0; i < audioClips.Length; i++)
        {
            var clip = audioClips[i];
            // "sound_FX1" => key: sound_FX

            bool hasCount = int.TryParse(clip.name.Substring(clip.name.Length - 1), out int order);

            if (hasCount)
            {
                int increment = 1;
                clips.Add(clip);

                // Find other 
                while (true)
                {
                    if (i + increment == audioClips.Length)
                        break;

                    clip = audioClips[i + increment];
                    bool sameKeyGroup = int.TryParse(clip.name.Substring(clip.name.Length - 1), out order) &&
                                        order != 1;
                    if (!sameKeyGroup)
                    {
                        clip = audioClips[i];
                        break;
                    }

                    clips.Add(clip);
                    increment++;
                }

                i += increment - 1;
            }
            else
            {
                clips.Add(clip);
            }

            string key = clip.name;
            if (hasCount)
            {
                key = clip.name.Substring(0, clip.name.Length - 1);
                key = key.TrimEnd(' ', '_');
            }

            clipDictionary.Add(key, new AudioEvent(clips));
            clips.Clear();
        }
    }

    public void PlayOneShotSound(string clipName, float volume = 1f, bool randomizePitch = true)
    {
        if (clipDictionary.TryGetValue(clipName, out AudioEvent audioEvent))
        {
            if (audioEvent.LastPlayedTime + defaultTimeThreshold < Time.time)
            {
                if (randomizePitch)
                {
                    randomizedSources[Random.Range(0, randomizedSources.Length)].PlayOneShot(audioEvent.GetRandom(), volume);
                }
                else
                {
                    rawSource.PlayOneShot(audioEvent.GetRandom(), volume);
                }

                audioEvent.LastPlayedTime = Time.time;
            }
        }
        else
        {
            Debug.LogError($"[AudioManager] {clipName} does not exist!");
        }
    }

    private static float LinearToDecibel(float linear)
    {
        // Clamp to avoid log(0)
        return linear > 0 ? 20f * Mathf.Log10(linear) : -80f;

        // Directly from GPT - Check
        // Human perception of loudness is roughly power-based (Stevens' power law).
        // So sometimes people use a small power curve before conversion:
        // float perceptual = Mathf.Pow(sliderValue, 2.2f); // tweak exponent for taste
        // float dB = LinearToDecibel(perceptual);
    }

    private static float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }
}
