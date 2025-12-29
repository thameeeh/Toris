using System;

public readonly struct AudioVoiceHandle : IEquatable<AudioVoiceHandle>
{
    public readonly int id;

    public AudioVoiceHandle(int id)
    {
        this.id = id;
    }

    public bool Equals(AudioVoiceHandle other) => id == other.id;
    public override bool Equals(object obj) => obj is AudioVoiceHandle other && Equals(other);
    public override int GetHashCode() => id;
    public override string ToString() => $"AudioVoiceHandle({id})";

    public static AudioVoiceHandle Invalid => new AudioVoiceHandle(0);
    public bool IsValid => id != 0;
}
