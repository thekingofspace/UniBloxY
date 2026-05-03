using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;
using UnityEngine;

public abstract class Shadable : Renderable
{
    private class ShadableData
    {
        public readonly List<LuaShader> Shaders = new();
    }

    private static readonly ConditionalWeakTable<LuaInstance, ShadableData> data = new();

    private static ShadableData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new ShadableData());

    protected override void OnRenderStateChanged(LuaInstance instance)
    {
        ApplyShaders(instance);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        switch (key)
        {
            case "AddShader":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var shader = ResolveShader(args, 1);
                    AddShader(instance, shader);
                    return DynValue.Nil;
                });
                return true;

            case "RemoveShader":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var shader = ResolveShader(args, 1);
                    var removed = RemoveShader(instance, shader);
                    return DynValue.NewBoolean(removed);
                });
                return true;

            case "ListShaders":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var tbl = new Table(instance.Script);
                    var list = Get(instance).Shaders;
                    for (int i = 0; i < list.Count; i++)
                        tbl[i + 1] = UserData.Create(list[i]);
                    return DynValue.NewTable(tbl);
                });
                return true;

            case "SetShaderData":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var shader = ResolveShader(args, 1);
                    var propName = args.Count > 2 ? args[2].String : null;
                    var dataVal = args.Count > 3 ? args[3] : DynValue.Nil;
                    if (string.IsNullOrEmpty(propName))
                        throw new ScriptRuntimeException("SetShaderData requires a property name");
                    SetShaderData(instance, shader, propName, dataVal);
                    return DynValue.Nil;
                });
                return true;
        }

        return base.TryGetProperty(instance, key, out value);
    }

    private static LuaShader ResolveShader(CallbackArguments args, int index)
    {
        if (args.Count <= index)
            throw new ScriptRuntimeException("Shader argument missing");
        var v = args[index];
        if (v.Type == DataType.UserData && v.UserData.Object is LuaShader s)
            return s;
        throw new ScriptRuntimeException("Argument must be a Shader from ShaderService:GetShader");
    }

    public void AddShader(LuaInstance instance, LuaShader shader)
    {
        if (shader == null) return;
        var d = Get(instance);
        if (d.Shaders.Contains(shader)) return;
        d.Shaders.Add(shader);
        ApplyShaders(instance);
    }

    public bool RemoveShader(LuaInstance instance, LuaShader shader)
    {
        if (shader == null) return false;
        var d = Get(instance);
        var removed = d.Shaders.Remove(shader);
        if (removed) ApplyShaders(instance);
        return removed;
    }

    private static void ApplyShaders(LuaInstance instance)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var list = Get(instance).Shaders;
        if (list.Count == 0) return;

        var mats = new Material[list.Count];
        for (int i = 0; i < list.Count; i++)
            mats[i] = new Material(list[i].Shader);
        renderer.materials = mats;
    }

    private static void SetShaderData(LuaInstance instance, LuaShader shader, string prop, DynValue value)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].shader != shader.Shader) continue;
            ApplyValue(mats[i], prop, value);
        }
        renderer.materials = mats;
    }

    private static void ApplyValue(Material mat, string prop, DynValue value)
    {
        switch (value.Type)
        {
            case DataType.Number:
                mat.SetFloat(prop, (float)value.Number);
                break;
            case DataType.Boolean:
                mat.SetInt(prop, value.Boolean ? 1 : 0);
                break;
            case DataType.String:
                mat.SetTextureOffset(prop, Vector2.zero);
                break;
            case DataType.UserData:
                var obj = value.UserData.Object;
                if (obj is LuaVector3 v3)
                    mat.SetVector(prop, new Vector4(v3.X, v3.Y, v3.Z, 0f));
                else if (obj is Color col)
                    mat.SetColor(prop, col);
                else if (obj is Vector4 v4)
                    mat.SetVector(prop, v4);
                else if (obj is Texture tex)
                    mat.SetTexture(prop, tex);
                else
                    throw new ScriptRuntimeException($"SetShaderData: unsupported value type for \"{prop}\"");
                break;
            default:
                throw new ScriptRuntimeException($"SetShaderData: unsupported value type for \"{prop}\"");
        }
    }
}
