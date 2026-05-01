using MoonSharp.Interpreter;
using UnityEngine;

public class RunService : LuaService
{
    private Signal heartbeat;

    public override void Register(Script script)
    {
        lua = script;
        heartbeat = new Signal(script, "RunService.Heartbeat");
        script.Globals["RunService"] = BuildTable(script);
    }

    void Update()
    {
        heartbeat.Fire(Time.deltaTime);
    }

    private string GetEnvironment()
    {
        return Application.isEditor ? "InStudio" : "Deployed";
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);
        table["Heartbeat"] = heartbeat.BuildTable();
        table["GetEnvironment"] = (System.Func<string>)GetEnvironment;
        return table;
    }
}
