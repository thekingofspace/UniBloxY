using System;
using System.Collections.Generic;
using System.Reflection;
using MoonSharp.Interpreter;
using UnityEngine;

public class Sound : LuaInstanceClass
{
    public override string ClassName => "Sound";
    public override bool ParentsUnityObject => true;
    public override bool Clonable => true;

    private class State
    {
        public LuaAudio Audio;
        public float Volume = 1f;
        public float Pitch = 1f;
        public bool Loop = false;
        public bool PlayOnCreate = false;
        public float MinDistance = 1f;
        public float MaxDistance = 50f;
        public string RolloffMode = "Logarithmic";

        public AudioSource Source;
        public bool PendingPlay;
        public float PendingTime;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.UserState = new State();
    }

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        if (source.UserState is State s && target.UserState is State d)
        {
            d.Audio = s.Audio;
            d.Volume = s.Volume;
            d.Pitch = s.Pitch;
            d.Loop = s.Loop;
            d.PlayOnCreate = s.PlayOnCreate;
            d.MinDistance = s.MinDistance;
            d.MaxDistance = s.MaxDistance;
            d.RolloffMode = s.RolloffMode;
        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        var s = (State)instance.UserState;
        if (s.Source != null) return;

        var go = instance.UnityObject;
        if (go == null)
        {
            go = new GameObject(instance.Name);
            instance.UnityObject = go;
        }

        s.Source = go.GetComponent<AudioSource>();
        if (s.Source == null) s.Source = go.AddComponent<AudioSource>();
        s.Source.playOnAwake = false;

        ApplyAll(instance, s);
        UpdateSpatial(instance, s);

        if (s.PendingPlay && s.Source.clip != null)
        {
            s.PendingPlay = false;
            s.Source.time = Mathf.Clamp(s.PendingTime, 0f, s.Source.clip.length - 0.01f);
            s.Source.Play();
        }
        else if (s.PlayOnCreate && s.Source.clip != null)
        {
            s.Source.Play();
        }
    }

    public override void OnExitScene(LuaInstance instance)
    {
        var s = (State)instance.UserState;
        if (s.Source != null && s.Source.isPlaying)
        {
            s.PendingPlay = true;
            s.PendingTime = s.Source.time;
            s.Source.Stop();
        }
        if (instance.UnityObject != null)
        {
            UnityEngine.Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
        s.Source = null;
    }

    public override void OnAncestryChanged(LuaInstance instance)
    {
        var s = (State)instance.UserState;
        UpdateSpatial(instance, s);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "SoundId":
            case "Audio":
                value = s.Audio != null ? UserData.Create(s.Audio) : DynValue.Nil; return true;
            case "Volume":      value = DynValue.NewNumber(s.Volume); return true;
            case "Pitch":
            case "PlaybackSpeed": value = DynValue.NewNumber(s.Pitch); return true;
            case "Loop":
            case "Looped":      value = DynValue.NewBoolean(s.Loop); return true;
            case "MinDistance": value = DynValue.NewNumber(s.MinDistance); return true;
            case "MaxDistance": value = DynValue.NewNumber(s.MaxDistance); return true;
            case "RolloffMode": value = DynValue.NewString(s.RolloffMode); return true;
            case "PlayOnCreate": value = DynValue.NewBoolean(s.PlayOnCreate); return true;
            case "Length":
                value = DynValue.NewNumber(s.Audio != null ? s.Audio.Length : 0f); return true;
            case "TimePosition":
                value = DynValue.NewNumber(s.Source != null ? s.Source.time : 0f); return true;
            case "Playing":
                value = DynValue.NewBoolean(s.Source != null && s.Source.isPlaying); return true;
            case "IsPositional":
                value = DynValue.NewBoolean(FindPartAncestor(instance) != null); return true;

            case "Play":
                value = DynValue.NewCallback((ctx, args) => { Play(instance, s); return DynValue.Nil; }); return true;
            case "Stop":
                value = DynValue.NewCallback((ctx, args) => { Stop(instance, s); return DynValue.Nil; }); return true;
            case "Pause":
                value = DynValue.NewCallback((ctx, args) => { if (s.Source != null) s.Source.Pause(); return DynValue.Nil; }); return true;
            case "Resume":
                value = DynValue.NewCallback((ctx, args) => { if (s.Source != null) s.Source.UnPause(); return DynValue.Nil; }); return true;

            case "SetMod":
                value = DynValue.NewCallback((ctx, args) => SetModCallback(instance, s, args)); return true;
            case "RemoveMod":
                value = DynValue.NewCallback((ctx, args) => RemoveModCallback(instance, s, args)); return true;
            case "GetMod":
                value = DynValue.NewCallback((ctx, args) => GetModCallback(instance, s, args)); return true;
            case "ClearMods":
                value = DynValue.NewCallback((ctx, args) => { ClearMods(s); return DynValue.Nil; }); return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "SoundId":
            case "Audio":
                s.Audio = ResolveAudio(value);
                if (s.Source != null) s.Source.clip = s.Audio?.Clip;
                return true;
            case "Volume":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Sound.Volume must be a number");
                s.Volume = Mathf.Max(0f, (float)value.Number);
                if (s.Source != null) s.Source.volume = s.Volume;
                return true;
            case "Pitch":
            case "PlaybackSpeed":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Sound.Pitch must be a number");
                s.Pitch = (float)value.Number;
                if (s.Source != null) s.Source.pitch = s.Pitch;
                return true;
            case "Loop":
            case "Looped":
                if (value.Type != DataType.Boolean) throw new ScriptRuntimeException("Sound.Loop must be a boolean");
                s.Loop = value.Boolean;
                if (s.Source != null) s.Source.loop = s.Loop;
                return true;
            case "MinDistance":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Sound.MinDistance must be a number");
                s.MinDistance = (float)value.Number;
                if (s.Source != null) s.Source.minDistance = s.MinDistance;
                return true;
            case "MaxDistance":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Sound.MaxDistance must be a number");
                s.MaxDistance = (float)value.Number;
                if (s.Source != null) s.Source.maxDistance = s.MaxDistance;
                return true;
            case "RolloffMode":
                if (value.Type != DataType.String) throw new ScriptRuntimeException("Sound.RolloffMode must be a string");
                s.RolloffMode = value.String;
                if (s.Source != null) s.Source.rolloffMode = ParseRolloff(s.RolloffMode);
                return true;
            case "PlayOnCreate":
                if (value.Type != DataType.Boolean) throw new ScriptRuntimeException("Sound.PlayOnCreate must be a boolean");
                s.PlayOnCreate = value.Boolean;
                return true;
            case "TimePosition":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Sound.TimePosition must be a number");
                if (s.Source != null && s.Source.clip != null)
                    s.Source.time = Mathf.Clamp((float)value.Number, 0f, s.Source.clip.length - 0.01f);
                else
                    s.PendingTime = (float)value.Number;
                return true;
            case "Playing":
                if (value.Type != DataType.Boolean) throw new ScriptRuntimeException("Sound.Playing must be a boolean");
                if (value.Boolean) Play(instance, s); else Stop(instance, s);
                return true;
        }
        return false;
    }

    private static void ApplyAll(LuaInstance instance, State s)
    {
        var src = s.Source;
        if (src == null) return;
        src.clip = s.Audio?.Clip;
        src.volume = s.Volume;
        src.pitch = s.Pitch;
        src.loop = s.Loop;
        src.minDistance = s.MinDistance;
        src.maxDistance = s.MaxDistance;
        src.rolloffMode = ParseRolloff(s.RolloffMode);
    }

    private static void UpdateSpatial(LuaInstance instance, State s)
    {
        if (s.Source == null) return;
        var partAncestor = FindPartAncestor(instance);
        s.Source.spatialBlend = partAncestor != null ? 1f : 0f;
        if (partAncestor == null && instance.UnityObject != null)
            instance.UnityObject.transform.localPosition = Vector3.zero;
    }

    private static LuaInstance FindPartAncestor(LuaInstance instance)
    {
        var p = instance.Parent;
        while (p != null)
        {
            if (p.ClassDef is BasePart) return p;
            p = p.Parent;
        }
        return null;
    }

    private static void Play(LuaInstance instance, State s)
    {
        if (s.Source == null)
        {
            s.PendingPlay = true;
            return;
        }
        if (s.Source.clip == null) return;
        s.Source.Play();
    }

    private static void Stop(LuaInstance instance, State s)
    {
        s.PendingPlay = false;
        if (s.Source == null) return;
        s.Source.Stop();
    }

    private static LuaAudio ResolveAudio(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData && value.UserData.Object is LuaAudio a) return a;
        if (value.Type == DataType.String)
        {
            if (AssetService.Instance == null)
                throw new ScriptRuntimeException("Sound.SoundId: AssetService not available");
            return AssetService.Instance.GetSound(value.String);
        }
        throw new ScriptRuntimeException("Sound.SoundId must be an Audio, a name string, or nil");
    }

    private static AudioRolloffMode ParseRolloff(string name)
    {
        if (string.IsNullOrEmpty(name)) return AudioRolloffMode.Logarithmic;
        switch (name.Trim().ToLowerInvariant())
        {
            case "linear": return AudioRolloffMode.Linear;
            case "custom": return AudioRolloffMode.Custom;
            default:        return AudioRolloffMode.Logarithmic;
        }
    }

    // ---- Modifiers ----------------------------------------------------------

    private struct ModSpec
    {
        public Type FilterType;
        public string PrimaryProperty;
    }

    private static readonly Dictionary<string, ModSpec> modSpecs = new()
    {
        { "distortion", new ModSpec { FilterType = typeof(AudioDistortionFilter), PrimaryProperty = "distortionLevel" } },
        { "echo",       new ModSpec { FilterType = typeof(AudioEchoFilter),       PrimaryProperty = "wetMix" } },
        { "reverb",     new ModSpec { FilterType = typeof(AudioReverbFilter),     PrimaryProperty = "reverbPreset" } },
        { "chorus",     new ModSpec { FilterType = typeof(AudioChorusFilter),     PrimaryProperty = "wetMix1" } },
        { "highpass",   new ModSpec { FilterType = typeof(AudioHighPassFilter),   PrimaryProperty = "cutoffFrequency" } },
        { "lowpass",    new ModSpec { FilterType = typeof(AudioLowPassFilter),    PrimaryProperty = "cutoffFrequency" } },
    };

    private static bool TryResolveMod(string name, out ModSpec spec)
    {
        if (string.IsNullOrEmpty(name)) { spec = default; return false; }
        return modSpecs.TryGetValue(name.Trim().ToLowerInvariant(), out spec);
    }

    private static DynValue SetModCallback(LuaInstance instance, State s, CallbackArguments args)
    {
        var name = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("Sound:SetMod(name, value): name string required");
        if (!TryResolveMod(name, out var spec))
            throw new ScriptRuntimeException($"Sound:SetMod: unknown modifier \"{name}\" (distortion/echo/reverb/chorus/highpass/lowpass)");

        var value = args.Count > 2 ? args[2] : DynValue.Nil;
        if (value.IsNil())
        {
            RemoveMod(s, spec);
            return DynValue.Nil;
        }

        if (s.Source == null)
            throw new ScriptRuntimeException("Sound:SetMod: sound is not yet in the scene (parent it first)");

        var component = s.Source.GetComponent(spec.FilterType) as Behaviour;
        if (component == null)
            component = s.Source.gameObject.AddComponent(spec.FilterType) as Behaviour;

        if (value.Type == DataType.Table)
        {
            foreach (var pair in value.Table.Pairs)
            {
                if (pair.Key.Type != DataType.String) continue;
                ApplyProperty(component, pair.Key.String, pair.Value);
            }
        }
        else
        {
            ApplyProperty(component, spec.PrimaryProperty, value);
        }
        return DynValue.Nil;
    }

    private static DynValue RemoveModCallback(LuaInstance instance, State s, CallbackArguments args)
    {
        var name = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("Sound:RemoveMod(name): name string required");
        if (!TryResolveMod(name, out var spec))
            throw new ScriptRuntimeException($"Sound:RemoveMod: unknown modifier \"{name}\"");
        RemoveMod(s, spec);
        return DynValue.Nil;
    }

    private static DynValue GetModCallback(LuaInstance instance, State s, CallbackArguments args)
    {
        var name = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("Sound:GetMod(name): name string required");
        if (!TryResolveMod(name, out var spec)) return DynValue.Nil;
        if (s.Source == null) return DynValue.Nil;
        var component = s.Source.GetComponent(spec.FilterType) as Behaviour;
        if (component == null) return DynValue.Nil;

        var script = instance.Script;
        var tbl = new Table(script);
        foreach (var prop in spec.FilterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            if (!IsModParamType(prop.PropertyType)) continue;
            if (prop.DeclaringType == typeof(Behaviour) || prop.DeclaringType == typeof(MonoBehaviour) ||
                prop.DeclaringType == typeof(Component) || prop.DeclaringType == typeof(UnityEngine.Object))
                continue;
            try
            {
                var v = prop.GetValue(component);
                tbl[prop.Name] = WrapValue(script, v);
            }
            catch { }
        }
        return DynValue.NewTable(tbl);
    }

    private static void ClearMods(State s)
    {
        if (s.Source == null) return;
        foreach (var kv in modSpecs)
        {
            var c = s.Source.GetComponent(kv.Value.FilterType);
            if (c != null) UnityEngine.Object.Destroy(c);
        }
    }

    private static void RemoveMod(State s, ModSpec spec)
    {
        if (s.Source == null) return;
        var c = s.Source.GetComponent(spec.FilterType);
        if (c != null) UnityEngine.Object.Destroy(c);
    }

    private static bool IsModParamType(Type t) =>
        t == typeof(float) || t == typeof(int) || t == typeof(bool) || t.IsEnum;

    private static DynValue WrapValue(Script script, object v)
    {
        if (v == null) return DynValue.Nil;
        switch (v)
        {
            case float f: return DynValue.NewNumber(f);
            case int i:   return DynValue.NewNumber(i);
            case bool b:  return DynValue.NewBoolean(b);
            case Enum e:  return DynValue.NewString(e.ToString());
            default:       return DynValue.NewString(v.ToString());
        }
    }

    private static void ApplyProperty(Behaviour component, string propName, DynValue value)
    {
        var prop = component.GetType().GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null || !prop.CanWrite)
            throw new ScriptRuntimeException(
                $"Sound:SetMod: filter \"{component.GetType().Name}\" has no writable property \"{propName}\"");

        var t = prop.PropertyType;
        object converted;
        try
        {
            if (t == typeof(float)) converted = (float)value.Number;
            else if (t == typeof(int)) converted = (int)value.Number;
            else if (t == typeof(bool)) converted = value.CastToBool();
            else if (t.IsEnum)
            {
                if (value.Type == DataType.String)
                    converted = Enum.Parse(t, value.String, true);
                else
                    converted = Enum.ToObject(t, (int)value.Number);
            }
            else
                throw new ScriptRuntimeException(
                    $"Sound:SetMod: property \"{propName}\" has unsupported type {t.Name}");
        }
        catch (ScriptRuntimeException) { throw; }
        catch (Exception e)
        {
            throw new ScriptRuntimeException(
                $"Sound:SetMod: cannot assign \"{propName}\" — {e.Message}");
        }
        prop.SetValue(component, converted);
    }
}
