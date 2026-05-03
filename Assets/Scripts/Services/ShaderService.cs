using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class ShaderService : LuaService
{
    private readonly Dictionary<string, LuaShader> shaderCache = new();
    private readonly Dictionary<string, LuaMaterial> materialCache = new();
    private readonly Dictionary<string, LuaTexture> textureCache = new();

    public override void Register(Script script)
    {
        lua = script;
        UserData.RegisterType<LuaShader>();
        UserData.RegisterType<LuaMaterial>();
        UserData.RegisterType<LuaTexture>();
        script.Globals["ShaderService"] = BuildTable(script);
    }

    public LuaShader GetShader(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("ShaderService:GetShader requires a shader name");

        if (shaderCache.TryGetValue(name, out var existing))
            return existing;

        var shader = Resources.Load<Shader>("Shaders/" + name);
        if (shader == null) shader = Shader.Find(name);
        if (shader == null)
            throw new ScriptRuntimeException($"ShaderService: shader \"{name}\" not found");

        var wrapper = new LuaShader(name, shader);
        shaderCache[name] = wrapper;
        return wrapper;
    }

    public LuaMaterial GetMaterial(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("ShaderService:GetMaterial requires a material name");

        if (materialCache.TryGetValue(name, out var existing))
            return existing;

        var mat = Resources.Load<Material>("Materials/" + name);
        if (mat == null)
            throw new ScriptRuntimeException($"ShaderService: material \"{name}\" not found at Resources/Materials/{name}");

        var wrapper = new LuaMaterial(name, mat);
        materialCache[name] = wrapper;
        return wrapper;
    }

    public LuaMaterial CreateMaterial(string shaderName, string materialName)
    {
        var shader = GetShader(shaderName);
        var mat = new Material(shader.Shader) { name = materialName ?? shaderName };
        var key = materialName ?? ($"_runtime:{shaderName}:{System.Guid.NewGuid():N}");
        var wrapper = new LuaMaterial(key, mat);
        if (!string.IsNullOrEmpty(materialName))
            materialCache[materialName] = wrapper;
        return wrapper;
    }

    public LuaTexture GetTexture(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("ShaderService:GetTexture requires a texture name");

        if (textureCache.TryGetValue(name, out var existing))
            return existing;

        var tex = Resources.Load<Texture>("Textures/" + name);
        if (tex == null) tex = Resources.Load<Texture2D>("Textures/" + name);
        if (tex == null)
            throw new ScriptRuntimeException($"ShaderService: texture \"{name}\" not found at Resources/Textures/{name}");

        var wrapper = new LuaTexture(name, tex);
        textureCache[name] = wrapper;
        return wrapper;
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["GetShader"] = (Func<string, LuaShader>)GetShader;
        table["Get"] = (Func<string, LuaShader>)GetShader;

        table["GetMaterial"] = (Func<string, LuaMaterial>)GetMaterial;
        table["GetTexture"] = (Func<string, LuaTexture>)GetTexture;

        table["CreateMaterial"] = DynValue.NewCallback((ctx, args) =>
        {
            if (args.Count < 1 || args[0].Type != DataType.String)
                throw new ScriptRuntimeException("ShaderService.CreateMaterial(shaderName [, name])");
            var shaderName = args[0].String;
            var matName = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
            return UserData.Create(CreateMaterial(shaderName, matName));
        });

        var mt = new Table(script);
        mt["__type"] = "ShaderService";
        table.MetaTable = mt;

        return table;
    }
}
