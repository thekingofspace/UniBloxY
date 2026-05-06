using System.Collections.Generic;
using MoonSharp.Interpreter;

public class ListenerService : LuaService
{
    public static ListenerService Instance { get; private set; }

    private readonly List<LuaListener> active = new();

    public override void Register(Script script)
    {
        lua = script;
        Instance = this;

        UserData.RegisterType<LuaListener>();

        script.Globals["ListenerService"] = BuildTable(script);
    }

    void Update()
    {

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var l = active[i];
            if (l == null || l.Destroyed) { active.RemoveAt(i); continue; }
            l.Tick();
        }
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["ListenToMouse"] = (System.Func<LuaListener>)CreateMouseListener;
        table["ListenToInstance"] = (System.Func<DynValue, LuaListener>)CreateInstanceListener;

        var mt = new Table(script);
        mt["__type"] = "ListenerService";
        table.MetaTable = mt;

        return table;
    }

    private LuaListener CreateMouseListener()
    {
        var l = new LuaListener(lua, ListenerKind.Mouse, null);
        active.Add(l);
        return l;
    }

    private LuaListener CreateInstanceListener(DynValue target)
    {
        var inst = LuaInstance.ResolveInstance(target);
        if (inst == null)
            throw new ScriptRuntimeException("ListenerService.ListenToInstance(target) requires an Instance");

        var l = new LuaListener(lua, ListenerKind.Instance, inst);
        active.Add(l);
        return l;
    }
}
