using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaAnimationTrack
{
    public string ClassName => "AnimationTrack";

    [MoonSharpHidden] public LuaAnimator Animator { get; }
    [MoonSharpHidden] public LuaAnimation Animation { get; }
    public string Name { get; }

    public LuaAnimationTrack(LuaAnimator animator, LuaAnimation animation, string name)
    {
        Animator = animator;
        Animation = animation;
        Name = name;
    }

    public bool IsPlaying => Animator?.Component != null && Animator.Component.IsPlaying(Name);

    public float Length => Animation?.Clip != null ? Animation.Clip.length : 0f;

    public bool Looped
    {
        get
        {
            var st = GetState();
            return st != null && st.wrapMode == WrapMode.Loop;
        }
        set
        {
            var st = GetState();
            if (st != null) st.wrapMode = value ? WrapMode.Loop : WrapMode.Once;
        }
    }

    public float Speed
    {
        get { var st = GetState(); return st != null ? st.speed : 1f; }
        set { var st = GetState(); if (st != null) st.speed = value; }
    }

    public float TimePosition
    {
        get { var st = GetState(); return st != null ? st.time : 0f; }
        set { var st = GetState(); if (st != null) st.time = value; }
    }

    public float Weight
    {
        get { var st = GetState(); return st != null ? st.weight : 1f; }
        set { var st = GetState(); if (st != null) st.weight = Mathf.Clamp01(value); }
    }

    /// Higher Priority overrides lower on the same blend mode. Tracks at
    /// different priorities all play simultaneously and Unity weights them by
    /// layer (Animation.PlayMode.StopSameLayer is used by Play/CrossFade).
    public int Priority
    {
        get { var st = GetState(); return st != null ? st.layer : 0; }
        set { var st = GetState(); if (st != null) st.layer = value; }
    }

    public bool Additive
    {
        get { var st = GetState(); return st != null && st.blendMode == AnimationBlendMode.Additive; }
        set
        {
            var st = GetState();
            if (st != null) st.blendMode = value ? AnimationBlendMode.Additive : AnimationBlendMode.Blend;
        }
    }

    /// Plays this track. If fadeTime > 0, crossfades from any track on the
    /// same Priority layer over fadeTime seconds. Tracks on different layers
    /// keep playing in parallel.
    public void Play(float fadeTime = 0f)
    {
        var comp = Animator?.Component;
        if (comp == null) return;
        AnimatorService.Instance?.CancelFade(this);
        if (fadeTime > 0f) comp.CrossFade(Name, fadeTime, PlayMode.StopSameLayer);
        else comp.Play(Name, PlayMode.StopSameLayer);
    }

    /// Plays this track without stopping anything else. fadeTime ramps weight
    /// from 0 to the supplied value over fadeTime seconds (additive blend).
    public void Blend(float weight = 1f, float fadeTime = 0f)
    {
        var comp = Animator?.Component;
        if (comp == null) return;
        AnimatorService.Instance?.CancelFade(this);
        if (fadeTime > 0f)
        {
            comp.Blend(Name, weight, fadeTime);
        }
        else
        {
            comp.Play(Name);
            var st = comp[Name];
            if (st != null) st.weight = Mathf.Clamp01(weight);
        }
    }

    /// Crossfades to this animation, replacing every other animation on the
    /// animator over fadeTime seconds (PlayMode.StopAll).
    public void CrossFade(float fadeTime)
    {
        var comp = Animator?.Component;
        if (comp == null) return;
        AnimatorService.Instance?.CancelFade(this);
        if (fadeTime > 0f) comp.CrossFade(Name, fadeTime, PlayMode.StopAll);
        else comp.Play(Name, PlayMode.StopAll);
    }

    /// Fades the track's weight to a target value over fadeTime seconds.
    /// Useful for blend trees driven from script.
    public void Fade(float targetWeight, float fadeTime)
    {
        if (Animator?.Component == null) return;
        if (AnimatorService.Instance != null)
            AnimatorService.Instance.FadeTrack(this, targetWeight, fadeTime, false);
        else
            Weight = targetWeight;
    }

    /// Stops the track. fadeTime > 0 fades weight to 0, then stops.
    public void Stop(float fadeTime = 0f)
    {
        var comp = Animator?.Component;
        if (comp == null) return;
        if (fadeTime > 0f && AnimatorService.Instance != null)
            AnimatorService.Instance.FadeTrack(this, 0f, fadeTime, true);
        else
        {
            AnimatorService.Instance?.CancelFade(this);
            comp.Stop(Name);
        }
    }

    [MoonSharpHidden]
    public AnimationState GetState()
    {
        var comp = Animator?.Component;
        return comp != null ? comp[Name] : null;
    }

    public override string ToString() => $"AnimationTrack<{Name}>";
}
