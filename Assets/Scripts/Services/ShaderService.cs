using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class ShaderService : LuaService
{
    private readonly Dictionary<string, LuaShader> cache = new();

    public override void Register(Script script)
    {
        lua = script;
        UserData.RegisterType<LuaShader>();
        script.Globals["ShaderService"] = BuildTable(script);
    }

    public LuaShader GetShader(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("ShaderService:GetShader requires a shader name");

        if (cache.TryGetValue(name, out var existing))
            return existing;

        var shader = Resources.Load<Shader>("Shaders/" + name);
        if (shader == null) shader = Shader.Find(name);
        if (shader == null)
            throw new ScriptRuntimeException($"ShaderService: shader \"{name}\" not found");

        var wrapper = new LuaShader(name, shader);
        cache[name] = wrapper;
        return wrapper;
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["GetShader"] = (Func<string, LuaShader>)GetShader;
        table["Get"] = (Func<string, LuaShader>)GetShader;

        var mt = new Table(script);
        mt["__type"] = "ShaderService";
        table.MetaTable = mt;

        return table;
    }
}
