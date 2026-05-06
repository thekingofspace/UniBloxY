using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaAudio
{
    public string ClassName => "Audio";
    public string Name { get; }
    [MoonSharpHidden] public AudioClip Clip { get; }

    public float Length => Clip != null ? Clip.length : 0f;
    public int Channels => Clip != null ? Clip.channels : 0;
    public int Frequency => Clip != null ? Clip.frequency : 0;
    public int Samples => Clip != null ? Clip.samples : 0;

    public LuaAudio(string name, AudioClip clip)
    {
        Name = name;
        Clip = clip;
    }

    public override string ToString() => $"Audio<{Name}>";
}
