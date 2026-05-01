using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using LuaCoroutine = MoonSharp.Interpreter.Coroutine;

[MoonSharpUserData]
public class SignalConnection
{
    private readonly Signal signal;
    internal Closure Callback;
    internal bool IsOnce;
    public bool Connected { get; private set; } = true;

    internal SignalConnection(Signal s, Closure cb, bool once)
    {
        signal = s;
        Callback = cb;
        IsOnce = once;
    }

    public void Disconnect()
    {
        if (!Connected) return;
        Connected = false;
        signal.Remove(this);
    }
}

public class Signal
{
    private static bool typesRegistered;

    private readonly Script script;
    private readonly string label;
    private readonly List<SignalConnection> connections = new List<SignalConnection>();
    private readonly List<LuaCoroutine> waiters = new List<LuaCoroutine>();

    public Signal(Script script, string label)
    {
        this.script = script;
        this.label = label;
        if (!typesRegistered)
        {
            UserData.RegisterType<SignalConnection>();
            typesRegistered = true;
        }
    }

    private DynValue Connect(Closure cb)
    {
        if (cb == null)
        {
            Debug.LogError($"{label}:Connect callback is nil");
            return DynValue.Nil;
        }
        var c = new SignalConnection(this, cb, false);
        connections.Add(c);
        return UserData.Create(c);
    }

    private DynValue Once(Closure cb)
    {
        if (cb == null)
        {
            Debug.LogError($"{label}:Once callback is nil");
            return DynValue.Nil;
        }
        var c = new SignalConnection(this, cb, true);
        connections.Add(c);
        return UserData.Create(c);
    }

    private void AddWaiter(DynValue thread)
    {
        if (thread != null && thread.Type == DataType.Thread)
            waiters.Add(thread.Coroutine);
    }

    internal void Remove(SignalConnection c)
    {
        connections.Remove(c);
    }

    public void Fire(params object[] args)
    {
        if (connections.Count > 0)
        {
            var snapshot = connections.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var c = snapshot[i];
                if (!c.Connected) continue;
                if (c.IsOnce) c.Disconnect();
                try
                {
                    var co = script.CreateCoroutine(DynValue.NewClosure(c.Callback));
                    co.Coroutine.Resume(args);
                }
                catch (ScriptRuntimeException ex)
                {
                    Debug.LogError($"{label}: {ex.DecoratedMessage}");
                }
            }
        }

        if (waiters.Count > 0)
        {
            var snapshot = waiters.ToArray();
            waiters.Clear();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].Resume(args);
                }
                catch (ScriptRuntimeException ex)
                {
                    Debug.LogError($"{label} Wait: {ex.DecoratedMessage}");
                }
            }
        }
    }

    public Table BuildTable()
    {
        var t = new Table(script);
        t["Connect"] = (System.Func<DynValue, Closure, DynValue>)((_, cb) => Connect(cb));
        t["Once"] = (System.Func<DynValue, Closure, DynValue>)((_, cb) => Once(cb));
        t["_addWaiter"] = (System.Action<DynValue, DynValue>)((_, th) => AddWaiter(th));

        var chunk = script.LoadString(
            "return function(self) self:_addWaiter(coroutine.running()) return coroutine.yield() end",
            null, $"{label}.Wait");
        t["Wait"] = script.Call(chunk);

        return t;
    }
}
