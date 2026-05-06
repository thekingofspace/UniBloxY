using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaAnimation
{
    public string ClassName => "Animation";
    public string Name { get; }
    [MoonSharpHidden] public AnimationClip Clip { get; }

    public float Length => Clip != null ? Clip.length : 0f;
    public bool Looped => Clip != null && Clip.isLooping;

    public LuaAnimation(string name, AnimationClip clip)
    {
        Name = name;
        Clip = clip;
    }

    public override string ToString() => $"Animation<{Name}>";
}
