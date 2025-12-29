using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/SFX Definition", fileName = "SfxDefinition")]
public sealed class SfxDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;

    [Header("Clips (random pick)")]
    [SerializeField] private AudioClip[] clips;

    [Header("Mixer Routing")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Volume / Pitch")]
    [SerializeField, Range(0f, 2f)] private float volumeMin = 1f;
    [SerializeField, Range(0f, 2f)] private float volumeMax = 1f;
    [SerializeField, Range(-3f, 3f)] private float pitchMin = 1f;
    [SerializeField, Range(-3f, 3f)] private float pitchMax = 1f;

    [Header("Spatial (2D/3D)")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f; // 0 = 2D, 1 = 3D
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 25f;

    [Header("Concurrency / Spam Control")]
    [SerializeField] private int maxSimultaneousInstances = 8;
    [SerializeField] private VoiceStealMode stealMode = VoiceStealMode.StealOldest;
    [SerializeField] private float cooldownSeconds = 0f;

    public string Id => id;
    public AudioMixerGroup OutputMixerGroup => outputMixerGroup;

    public float VolumeMin => volumeMin;
    public float VolumeMax => volumeMax;
    public float PitchMin => pitchMin;
    public float PitchMax => pitchMax;

    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;

    public int MaxSimultaneousInstances => maxSimultaneousInstances;
    public VoiceStealMode StealMode => stealMode;
    public float CooldownSeconds => cooldownSeconds;

    public bool HasAnyClips => clips != null && clips.Length > 0;

    public AudioClip PickClip(System.Random random)
    {
        if (clips == null || clips.Length == 0) return null;
        int index = random.Next(0, clips.Length);
        return clips[index];
    }

    public enum VoiceStealMode
    {
        DropNew,
        StealOldest,
        StealQuietest
    }
}
