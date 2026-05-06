using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class AnimatorService : LuaService
{
    public static AnimatorService Instance { get; private set; }

    private readonly Dictionary<string, LuaSkeleton> skeletonCache = new();
    private readonly Dictionary<string, LuaAnimation> animationCache = new();

    private Signal skeletonImportedSignal;
    private Signal animationImportedSignal;

    private class ActiveFade
    {
        public LuaAnimationTrack Track;
        public float StartWeight;
        public float TargetWeight;
        public float Duration;
        public float Elapsed;
        public bool StopAfter;
    }
    private readonly List<ActiveFade> activeFades = new();

    public override void Register(Script script)
    {
        lua = script;
        Instance = this;

        UserData.RegisterType<LuaMesh>();
        UserData.RegisterType<LuaSkeleton>();
        UserData.RegisterType<LuaAnimation>();
        UserData.RegisterType<LuaAnimator>();
        UserData.RegisterType<LuaAnimationTrack>();

        skeletonImportedSignal = new Signal(script, "AnimatorService.SkeletonImported");
        animationImportedSignal = new Signal(script, "AnimatorService.AnimationImported");

        script.Globals["AnimatorService"] = BuildTable(script);
    }

    public LuaSkeleton ImportSkeleton(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AnimatorService:ImportSkeleton requires a name");

        if (skeletonCache.TryGetValue(name, out var existing))
            return existing;

        var prefab = Resources.Load<GameObject>("Skeletons/" + name);
        if (prefab == null)
            throw new ScriptRuntimeException(
                $"AnimatorService: skeleton \"{name}\" not found at Resources/Skeletons/{name}");

        var wrapper = new LuaSkeleton(name, prefab);
        skeletonCache[name] = wrapper;
        skeletonImportedSignal.Fire(name);
        return wrapper;
    }

    void Update()
    {
        if (activeFades.Count == 0) return;
        var dt = Time.deltaTime;
        for (int i = activeFades.Count - 1; i >= 0; i--)
        {
            var f = activeFades[i];
            f.Elapsed += dt;
            var t = f.Duration <= 0f ? 1f : Mathf.Clamp01(f.Elapsed / f.Duration);
            var st = f.Track.GetState();
            if (st == null)
            {
                activeFades.RemoveAt(i);
                continue;
            }
            st.weight = Mathf.Lerp(f.StartWeight, f.TargetWeight, t);
            if (t >= 1f)
            {
                if (f.StopAfter) f.Track.Animator?.Component?.Stop(f.Track.Name);
                activeFades.RemoveAt(i);
            }
        }
    }

    public void FadeTrack(LuaAnimationTrack track, float targetWeight, float duration, bool stopAfter)
    {
        if (track == null) return;
        CancelFade(track);

        var st = track.GetState();
        if (st == null) return;

        if (duration <= 0f)
        {
            st.weight = Mathf.Clamp01(targetWeight);
            if (stopAfter) track.Animator?.Component?.Stop(track.Name);
            return;
        }

        activeFades.Add(new ActiveFade
        {
            Track = track,
            StartWeight = st.weight,
            TargetWeight = Mathf.Clamp01(targetWeight),
            Duration = duration,
            Elapsed = 0f,
            StopAfter = stopAfter
        });
    }

    public void CancelFade(LuaAnimationTrack track)
    {
        if (track == null) return;
        for (int i = activeFades.Count - 1; i >= 0; i--)
            if (activeFades[i].Track == track) activeFades.RemoveAt(i);
    }

    public LuaAnimation ImportAnimation(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AnimatorService:ImportAnimation requires a name");

        if (animationCache.TryGetValue(name, out var existing))
            return existing;

        var clip = Resources.Load<AnimationClip>("Animations/" + name);
        if (clip == null)
            throw new ScriptRuntimeException(
                $"AnimatorService: animation \"{name}\" not found at Resources/Animations/{name}");

        var wrapper = new LuaAnimation(name, clip);
        animationCache[name] = wrapper;
        animationImportedSignal.Fire(name);
        return wrapper;
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["ImportSkeleton"] = (Func<string, LuaSkeleton>)ImportSkeleton;
        table["ImportAnimation"] = (Func<string, LuaAnimation>)ImportAnimation;

        table["SkeletonImported"] = skeletonImportedSignal.BuildTable();
        table["AnimationImported"] = animationImportedSignal.BuildTable();

        var mt = new Table(script);
        mt["__type"] = "AnimatorService";
        table.MetaTable = mt;
        return table;
    }
}
