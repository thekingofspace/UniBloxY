using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using UnityEngine;

public class LuaRunner : MonoBehaviour
{
    public static LuaRunner Instance { get; private set; }
    public Script Lua { get; private set; }
    public LuaInstance Game { get; private set; }

    private readonly Dictionary<string, DynValue> loadedModules = new();
    private readonly Dictionary<string, string[]> sourceLines = new();

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

        RunScript(main.text, "main.lua");
    }

    private void RunScript(string source, string name)
    {
        sourceLines[name] = source.Replace("\r\n", "\n").Split('\n');
        try
        {
            var fn = Lua.LoadString(source, null, name);
            var co = Lua.CreateCoroutine(fn);
            co.Coroutine.Resume();
        }
        catch (SyntaxErrorException ex)
        {
            ReportSyntaxError(ex, name);
        }
        catch (ScriptRuntimeException ex)
        {
            ReportRuntimeError(ex);
        }
        catch (InterpreterException ex)
        {
            ReportRuntimeError(ex);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Lua host error] {ex}");
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

        var srcName = path + ".lua";
        sourceLines[srcName] = asset.text.Replace("\r\n", "\n").Split('\n');

        var fn = Lua.LoadString(asset.text, null, srcName);
        var result = Lua.Call(fn);

        if (result.IsNil())
            result = DynValue.True;

        loadedModules[path] = result;
        return result;
    }

    // ----------------------------------------------------------------------
    // Error reporting
    // ----------------------------------------------------------------------

    private static readonly Regex LocRegex =
        new Regex(@"^(?<src>[^:]+):\((?<line>\d+),(?<from>\d+)(?:-(?<to>\d+))?\):\s*(?<msg>.*)$",
            RegexOptions.Compiled);

    private void ReportRuntimeError(InterpreterException ex)
    {
        var sb = new StringBuilder();
        sb.Append("[Lua runtime error] ");

        var msg = ex.DecoratedMessage ?? ex.Message;
        sb.AppendLine(msg);

        var m = LocRegex.Match(msg ?? "");
        if (m.Success)
        {
            var src = m.Groups["src"].Value;
            int line = int.Parse(m.Groups["line"].Value);
            int from = int.Parse(m.Groups["from"].Value);
            int to = m.Groups["to"].Success ? int.Parse(m.Groups["to"].Value) : from;
            AppendSnippet(sb, src, line, from, to);
        }

        AppendStackTrace(sb, ex);
        Debug.LogError(sb.ToString());
    }

    private void ReportSyntaxError(SyntaxErrorException ex, string fallbackSource)
    {
        var sb = new StringBuilder();
        sb.Append("[Lua syntax error] ");
        var msg = ex.DecoratedMessage ?? ex.Message;
        sb.AppendLine(msg);

        var m = LocRegex.Match(msg ?? "");
        if (m.Success)
        {
            var src = m.Groups["src"].Value;
            int line = int.Parse(m.Groups["line"].Value);
            int from = int.Parse(m.Groups["from"].Value);
            int to = m.Groups["to"].Success ? int.Parse(m.Groups["to"].Value) : from;
            AppendSnippet(sb, src, line, from, to);
        }
        else if (sourceLines.ContainsKey(fallbackSource))
        {
            sb.AppendLine($"  in {fallbackSource}");
        }

        Debug.LogError(sb.ToString());
    }

    private void AppendSnippet(StringBuilder sb, string sourceName, int line, int fromCol, int toCol)
    {
        if (!sourceLines.TryGetValue(sourceName, out var lines)) return;
        if (line < 1 || line > lines.Length) return;

        int contextBefore = 1;
        int contextAfter = 1;
        int start = Math.Max(1, line - contextBefore);
        int end = Math.Min(lines.Length, line + contextAfter);

        int gutter = end.ToString().Length;

        sb.AppendLine();
        for (int i = start; i <= end; i++)
        {
            string prefix = i == line ? ">" : " ";
            sb.Append(prefix)
              .Append(' ')
              .Append(i.ToString().PadLeft(gutter))
              .Append(" | ")
              .AppendLine(lines[i - 1]);

            if (i == line && fromCol >= 0)
            {
                int caretStart = Math.Max(0, fromCol);
                int caretLen = Math.Max(1, toCol - fromCol);
                sb.Append("  ")
                  .Append(new string(' ', gutter))
                  .Append(" | ")
                  .Append(new string(' ', caretStart))
                  .Append(new string('^', caretLen))
                  .AppendLine();
            }
        }
    }

    private void AppendStackTrace(StringBuilder sb, InterpreterException ex)
    {
        var stack = ex.CallStack;
        if (stack == null || stack.Count == 0) return;

        sb.AppendLine("Stack traceback:");
        foreach (var frame in stack)
        {
            var loc = frame.Location;
            string where = "[C]";
            if (loc != null && loc.SourceIdx >= 0)
            {
                string srcName = null;
                try { srcName = Lua.GetSourceCode(loc.SourceIdx)?.Name; } catch { }
                if (!string.IsNullOrEmpty(srcName))
                    where = $"{srcName}:{loc.FromLine}";
            }

            string fnName = string.IsNullOrEmpty(frame.Name) ? "?" : frame.Name;
            sb.Append("  at ").Append(fnName).Append(" (").Append(where).AppendLine(")");
        }
    }
}
