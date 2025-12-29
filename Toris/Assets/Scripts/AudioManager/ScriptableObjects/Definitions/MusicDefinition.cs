using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Music Definition", fileName = "MusicDefinition")]
public sealed class MusicDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private AudioClip clip;
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [SerializeField, Range(0f, 2f)] private float volume = 1f;

    [Header("Fade Defaults")]
    [SerializeField] private float fadeInSeconds = 1.0f;
    [SerializeField] private float fadeOutSeconds = 1.0f;

    public string Id => id;
    public AudioClip Clip => clip;
    public AudioMixerGroup OutputMixerGroup => outputMixerGroup;
    public float Volume => volume;
    public float FadeInSeconds => fadeInSeconds;
    public float FadeOutSeconds => fadeOutSeconds;
}
