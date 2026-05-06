using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public abstract class ShadableUI : GUIBase
{
    private class ShadableUIData
    {
        public readonly List<LuaShader> Shaders = new();
        public readonly List<Material> ShaderInstances = new();
        public readonly List<LuaMaterial> Materials = new();
        public readonly Dictionary<LuaMaterial, Material> MaterialInstances = new();
    }

    private static readonly ConditionalWeakTable<LuaInstance, ShadableUIData> data = new();
    private static ShadableUIData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new ShadableUIData());

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        base.CopyState(source, target);
        var src = Get(source);
        var dst = Get(target);

        for (int i = 0; i < src.Shaders.Count; i++)
        {
            var shader = src.Shaders[i];
            dst.Shaders.Add(shader);
            var srcMat = src.ShaderInstances[i];
            dst.ShaderInstances.Add(srcMat != null ? new Material(srcMat) : new Material(shader.Shader));
        }

        foreach (var mat in src.Materials)
        {
            dst.Materials.Add(mat);
            src.MaterialInstances.TryGetValue(mat, out var srcInst);
            dst.MaterialInstances[mat] = mat.CloneInstance(srcInst);
        }
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        switch (key)
        {
            case "AddShader":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    AddShader(instance, ResolveShader(args, 1));
                    return DynValue.Nil;
                });
                return true;

            case "RemoveShader":
                value = DynValue.NewCallback((ctx, args) =>
                    DynValue.NewBoolean(RemoveShader(instance, ResolveShader(args, 1))));
                return true;

            case "ListShaders":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var tbl = new Table(instance.Script);
                    var list = Get(instance).Shaders;
                    for (int i = 0; i < list.Count; i++) tbl[i + 1] = UserData.Create(list[i]);
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

            case "AddMaterial":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    AddMaterial(instance, ResolveMaterial(args, 1));
                    return DynValue.Nil;
                });
                return true;

            case "RemoveMaterial":
                value = DynValue.NewCallback((ctx, args) =>
                    DynValue.NewBoolean(RemoveMaterial(instance, ResolveMaterial(args, 1))));
                return true;

            case "ListMaterials":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var tbl = new Table(instance.Script);
                    var list = Get(instance).Materials;
                    for (int i = 0; i < list.Count; i++) tbl[i + 1] = UserData.Create(list[i]);
                    return DynValue.NewTable(tbl);
                });
                return true;

            case "SetMaterialProperty":
            case "SetMaterialData":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var mat = ResolveMaterial(args, 1);
                    var propName = args.Count > 2 ? args[2].String : null;
                    var dataVal = args.Count > 3 ? args[3] : DynValue.Nil;
                    if (string.IsNullOrEmpty(propName))
                        throw new ScriptRuntimeException("SetMaterialProperty requires a property name");
                    SetMaterialProperty(instance, mat, propName, dataVal);
                    return DynValue.Nil;
                });
                return true;
        }

        return base.TryGetProperty(instance, key, out value);
    }

    private static LuaShader ResolveShader(CallbackArguments args, int index)
    {
        if (args.Count <= index) throw new ScriptRuntimeException("Shader argument missing");
        var v = args[index];
        if (v.Type == DataType.UserData && v.UserData.Object is LuaShader s) return s;
        throw new ScriptRuntimeException("Argument must be a Shader from AssetService:GetShader");
    }

    private static LuaMaterial ResolveMaterial(CallbackArguments args, int index)
    {
        if (args.Count <= index) throw new ScriptRuntimeException("Material argument missing");
        var v = args[index];
        if (v.Type == DataType.UserData && v.UserData.Object is LuaMaterial m) return m;
        throw new ScriptRuntimeException("Argument must be a Material from AssetService:GetMaterial");
    }

    public void AddShader(LuaInstance instance, LuaShader shader)
    {
        if (shader == null) return;
        var d = Get(instance);
        if (d.Shaders.Contains(shader)) return;
        d.Shaders.Add(shader);
        d.ShaderInstances.Add(new Material(shader.Shader));
        ApplyMaterial(instance);
    }

    public bool RemoveShader(LuaInstance instance, LuaShader shader)
    {
        if (shader == null) return false;
        var d = Get(instance);
        var idx = d.Shaders.IndexOf(shader);
        if (idx < 0) return false;
        d.Shaders.RemoveAt(idx);
        d.ShaderInstances.RemoveAt(idx);
        ApplyMaterial(instance);
        return true;
    }

    public void AddMaterial(LuaInstance instance, LuaMaterial material)
    {
        if (material == null) return;
        var d = Get(instance);
        if (d.Materials.Contains(material)) return;
        d.Materials.Add(material);
        d.MaterialInstances[material] = material.CreateInstance();
        ApplyMaterial(instance);
    }

    public bool RemoveMaterial(LuaInstance instance, LuaMaterial material)
    {
        if (material == null) return false;
        var d = Get(instance);
        if (!d.Materials.Remove(material)) return false;
        if (d.MaterialInstances.TryGetValue(material, out var inst))
        {
            material.DropInstance(inst);
            d.MaterialInstances.Remove(material);
        }
        ApplyMaterial(instance);
        return true;
    }

    protected static void ApplyMaterial(LuaInstance instance)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var graphic = go.GetComponent<Graphic>();
        if (graphic == null) return;

        var d = Get(instance);
        Material active = null;
        if (d.ShaderInstances.Count > 0)
            active = d.ShaderInstances[d.ShaderInstances.Count - 1];
        else if (d.Materials.Count > 0)
        {
            var lastMat = d.Materials[d.Materials.Count - 1];
            d.MaterialInstances.TryGetValue(lastMat, out active);
        }
        graphic.material = active;
    }

    private static void SetShaderData(LuaInstance instance, LuaShader shader, string prop, DynValue value)
    {
        var d = Get(instance);
        for (int i = 0; i < d.Shaders.Count; i++)
        {
            if (d.Shaders[i] == shader)
                MaterialProps.Apply(d.ShaderInstances[i], prop, value);
        }
    }

    private static void SetMaterialProperty(LuaInstance instance, LuaMaterial material, string prop, DynValue value)
    {
        var d = Get(instance);
        if (!d.MaterialInstances.TryGetValue(material, out var inst) || inst == null)
            throw new ScriptRuntimeException($"SetMaterialProperty: material \"{material.Name}\" is not applied to this object");
        MaterialProps.Apply(inst, prop, value);
    }
}
