using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEngine;

public class ResourceScriptLoader : ScriptLoaderBase
{
    public ResourceScriptLoader()
    {
        ModulePaths = new[] { "?", "?.lua" };
    }

    public override string ResolveModuleName(string modname, Table globalContext)
    {
        return modname.Replace('.', '/');
    }

    public override object LoadFile(string file, Table globalContext)
    {
        var asset = Resources.Load<TextAsset>($"LuaScripts/{Normalize(file)}");

        if (asset == null)
            throw new ScriptRuntimeException($"Cannot find Lua file: {file}");

        return asset.text;
    }

    public override bool ScriptFileExists(string name)
    {
        return Resources.Load<TextAsset>($"LuaScripts/{Normalize(name)}") != null;
    }

    private static string Normalize(string path)
    {
        if (path.EndsWith(".lua"))
            path = path.Substring(0, path.Length - 4);
        return path;
    }
}
