using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaAnimator
{
    public string ClassName => "Animator";

    [MoonSharpHidden] public LuaInstance Owner { get; }
    [MoonSharpHidden] public Animation Component { get; }
    [MoonSharpHidden] private readonly Dictionary<string, LuaAnimationTrack> tracks = new();

    public LuaAnimator(LuaInstance owner, Animation component)
    {
        Owner = owner;
        Component = component;
    }

    public LuaAnimationTrack LoadAnimation(LuaAnimation animation)
    {
        if (animation == null || animation.Clip == null)
            throw new ScriptRuntimeException("Animator:LoadAnimation requires an Animation");
        if (Component == null)
            throw new ScriptRuntimeException("Animator: underlying GameObject is missing");

        var key = animation.Name;
        if (tracks.TryGetValue(key, out var existing) && Component.GetClip(key) != null)
            return existing;

        animation.Clip.legacy = true;
        Component.AddClip(animation.Clip, key);

        var track = new LuaAnimationTrack(this, animation, key);
        tracks[key] = track;
        return track;
    }

    public LuaAnimationTrack GetTrack(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        tracks.TryGetValue(name, out var t);
        return t;
    }

    public void StopAll()
    {
        if (Component != null) Component.Stop();
    }

    /// Crossfades to one of the loaded tracks, fading out everything currently
    /// playing on the same layer (Priority) over fadeTime seconds.
    public LuaAnimationTrack CrossFade(LuaAnimationTrack track, float fadeTime = 0.25f)
    {
        if (track == null)
            throw new ScriptRuntimeException("Animator:CrossFade requires an AnimationTrack");
        if (track.Animator != this)
            throw new ScriptRuntimeException("Animator:CrossFade — track was created by a different Animator");
        track.Play(fadeTime);
        return track;
    }

    public bool IsPlaying => Component != null && Component.isPlaying;

    public override string ToString() => $"Animator<{Owner?.Name}>";
}
