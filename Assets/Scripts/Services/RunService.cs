using MoonSharp.Interpreter;
using UnityEngine;
using System;
using System.Collections.Generic;

public class RunService : LuaService
{
    private Signal heartbeat;

    private readonly List<DynValue> closeBindings = new();
    private bool isClosing;

    public override void Register(Script script)
    {
        lua = script;

        heartbeat = new Signal(script, "RunService.Heartbeat");

        script.Globals["RunService"] = BuildTable(script);

        Application.quitting += OnUnityQuit;
    }

    void Update()
    {
        if (isClosing) return;

        float dt = Time.deltaTime;

        heartbeat.Fire(dt);
    }

    private void OnUnityQuit()
    {
        Close();
    }

    public void Close()
    {
        if (isClosing) return;
        isClosing = true;

        for (int i = 0; i < closeBindings.Count; i++)
        {
            var fn = closeBindings[i];
            if (fn.Type == DataType.Function)
                lua.Call(fn);
        }

        closeBindings.Clear();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private string GetEnvironment()
    {
        return Application.isEditor ? "InStudio" : "Deployed";
    }

    private DynValue BindToClose(DynValue fn)
    {
        if (fn.Type != DataType.Function)
            return DynValue.Nil;

        closeBindings.Add(fn);
        return fn;
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["Heartbeat"] = heartbeat.BuildTable();

        table["GetEnvironment"] = (Func<string>)GetEnvironment;
        table["Close"] = (Action)Close;
        table["BindToClose"] = (Func<DynValue, DynValue>)BindToClose;

        var mt = new Table(script);
        mt["__type"] = "RunService";
        table.MetaTable = mt;

        return table;
    }
}