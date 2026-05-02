using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

public class LuaRunner : MonoBehaviour
{
    public static LuaRunner Instance { get; private set; }
    public Script Lua { get; private set; }
    public LuaInstance Game { get; private set; }

    private readonly Dictionary<string, DynValue> loadedModules = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("LuaRunner");
        DontDestroyOnLoad(go);
        go.AddComponent<LuaRunner>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Lua != null) return;

        Lua = new Script(CoreModules.Preset_Complete);
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

        Lua.Globals["typeof"] = new CallbackFunction((ctx, args) =>
        {
            var val = args.Count > 0 ? args[0] : DynValue.Nil;

            switch (val.Type)
            {
                case DataType.Nil: return DynValue.NewString("nil");
                case DataType.Boolean: return DynValue.NewString("boolean");
                case DataType.Number: return DynValue.NewString("number");
                case DataType.String: return DynValue.NewString("string");
                case DataType.Function:
                case DataType.ClrFunction: return DynValue.NewString("function");
                case DataType.Thread: return DynValue.NewString("thread");

                case DataType.Table:
                    var mt = val.Table.MetaTable;
                    if (mt != null)
                    {
                        var typeVal = mt.RawGet("__type");
                        if (typeVal.Type == DataType.String)
                            return typeVal;

                        if (typeVal.Type == DataType.Function || typeVal.Type == DataType.ClrFunction)
                            return ctx.GetScript().Call(typeVal, val);
                    }
                    return DynValue.NewString("table");

                case DataType.UserData:
                    if (val.UserData?.Object != null)
                    {
                        var prop = val.UserData.Object.GetType().GetProperty("ClassName");
                        if (prop != null)
                        {
                            var cn = prop.GetValue(val.UserData.Object) as string;
                            if (cn != null) return DynValue.NewString(cn);
                        }
                    }
                    return DynValue.NewString("userdata");

                default:
                    return DynValue.NewString(val.Type.ToString().ToLower());
            }
        });

        LuaTypes.Register(Lua);
        LuaInstance.EnsureRegistered(Lua);

        Game = new LuaInstance(Lua, "DataModel", "game");
        var dataModel = (DataModel)LuaInstance.GetClass("DataModel") ?? new DataModel();
        Game.ClassDef = dataModel;
        dataModel.Initialize(Game);
        Game.ForceEnterScene();
        dataModel.EnsureCamera(Game);
        Lua.Globals["game"] = Game.Table;

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
        catch (System.Exception ex)
        {
            Debug.LogError($"Lua host error: {ex}");
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
            throw new ScriptRuntimeException(
                $"module '{modname}' not found at Assets/Resources/LuaScripts/{path}.lua"
            );

        var fn = Lua.LoadString(asset.text, null, path + ".lua");
        var result = Lua.Call(fn);

        if (result.IsNil())
            result = DynValue.True;

        loadedModules[path] = result;
        return result;
    }
}