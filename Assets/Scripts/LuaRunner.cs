using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

public class LuaRunner : MonoBehaviour
{
    public static LuaRunner Instance { get; private set; }
    public Script Lua { get; private set; }

    private readonly Dictionary<string, DynValue> loadedModules = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Lua = new Script();
        Lua.Options.ScriptLoader = new ResourceScriptLoader();

        Lua.Globals["print"] = new CallbackFunction((ctx, args) =>
        {
            var parts = new string[args.Count];
            for (int i = 0; i < args.Count; i++)
                parts[i] = args[i].ToPrintString();
            Debug.Log(string.Join("\t", parts));
            return DynValue.Nil;
        });

        Lua.Globals["require"] = (System.Func<string, DynValue>)Require;

        LuaTypes.Register(Lua);

        RegisterServices();

        var main = Resources.Load<TextAsset>("LuaScripts/main");

        if (main == null)
        {
            Debug.LogError("Could not find main.lua! Make sure it's at Assets/Resources/LuaScripts/main.lua");
            return;
        }

        try
        {
            var fn = Lua.LoadString(main.text, null, "main.lua");
            var co = Lua.CreateCoroutine(fn);
            co.Coroutine.Resume();
        }
        catch (ScriptRuntimeException ex)
        {
            Debug.LogError($"Lua error: {ex.DecoratedMessage}");
        }
    }

    private void RegisterServices()
    {
        var serviceTypes = typeof(LuaService).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(LuaService).IsAssignableFrom(t));
        foreach (var t in serviceTypes)
            ((LuaService)gameObject.AddComponent(t)).Register(Lua);
    }

    private DynValue Require(string modname)
    {
        var path = modname.Replace('.', '/');
        if (path.EndsWith(".lua"))
            path = path[..^4];

        if (loadedModules.TryGetValue(path, out var cached))
            return cached;

        var asset = Resources.Load<TextAsset>($"LuaScripts/{path}");
        if (asset == null)
            throw new ScriptRuntimeException($"module '{modname}' not found at Assets/Resources/LuaScripts/{path}.lua");

        var fn = Lua.LoadString(asset.text, null, path + ".lua");
        var result = Lua.Call(fn);
        if (result.IsNil()) result = DynValue.True;
        loadedModules[path] = result;
        return result;
    }
}
