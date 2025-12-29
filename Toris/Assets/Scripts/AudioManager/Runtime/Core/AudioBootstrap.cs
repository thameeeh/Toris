using UnityEngine;
using UnityEngine.Audio;

public static class AudioBootstrap
{
    public static ISfxManager Sfx { get; private set; }
    public static IMusicManager Music { get; private set; }

    public static IAudioRuntimeTick[] RuntimeTicks { get; private set; }

    public static void Initialize(
        GameObject owner,
        SfxLibrary sfxLibrary,
        MusicLibrary musicLibrary,
        int initialSfxVoices,
        AudioMixerGroup defaultSfxMixerGroup)
    {
        var voicePool = new AudioVoicePool(owner, initialSfxVoices, defaultSfxMixerGroup);

        Sfx = new SfxManager(sfxLibrary, voicePool);
        Music = new MusicManager(musicLibrary, owner);

        RuntimeTicks = new IAudioRuntimeTick[]
        {
            voicePool,
            (IAudioRuntimeTick)Music
        };
    }
}
